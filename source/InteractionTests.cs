using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
namespace CornerBar {
public partial class Bar {
    public void VerifyInteractions(string folder){
        var original=settings.Slots;string originalPath=settingsPath;
        settingsPath=Path.Combine(folder,"interaction-settings.tmp.json");settings.Slots=new List<Slot>();
        data.Clear();var game=new Item{Key="game:NBA:test",Group="NBA",Name="湖人 · 勇士",Home="湖人",Away="勇士",HomeId="13",AwayId="9",League="NBA",Day=Feed.Now.ToString("yyyy-MM-dd"),Value="98 : 94"};data.Add(new Batch{Group="NBA",Source="fixture",Fetched=Feed.Now,Items=new List<Item>{game}});
        try{using(var form=MakeSettings()){
            form.Show();Application.DoEvents();
            var search=(TextBox)form.Controls["search"];var results=(Panel)form.Controls["results"];var favorites=(Panel)form.Controls["favorites"];
            Action flush=()=>{Application.DoEvents();Application.DoEvents();};
            Action<string> add=key=>{search.Text="";flush();var row=results.Controls.Cast<Control>().First(r=>r.Tag is Slot&&((Slot)r.Tag).Key==key);((Button)row.Controls["add"]).PerformClick();flush();};
            add(game.Key);if(settings.Slots.Count!=1||settings.Slots[0].Key!=game.Key)throw new Exception("append failed");
            var team=TeamSlot(game,true);add(team.Key);
            if(settings.Slots.Count!=2||settings.Slots[1].Key!=team.Key)throw new Exception("follow team failed");
            add(team.Key);if(settings.Slots.Count!=2)throw new Exception("duplicate added");
            ((Button)favorites.Controls[1].Controls["up"]).PerformClick();flush();if(settings.Slots[0].Key!=team.Key)throw new Exception("move up failed");
            var drag=new DataObject();drag.SetData("ROW.WatchKey",team.Key);
            typeof(Control).GetMethod("OnDragDrop",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(favorites.Controls[1],new object[]{new DragEventArgs(drag,0,0,0,DragDropEffects.Move,DragDropEffects.Move)});flush();
            if(settings.Slots[1].Key!=team.Key)throw new Exception("drop reorder failed");
            ((Button)favorites.Controls[0].Controls["remove"]).PerformClick();flush();if(settings.Slots.Count!=1||settings.Slots[0].Key!=team.Key)throw new Exception("remove compact failed");
            var seeds=MarketCatalog.Seeds().Take(5).ToList();foreach(var s in seeds.Take(4))add(s.Key);
            using(var timer=new Timer{Interval=60}){timer.Tick+=(o,e)=>{var picker=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f!=form&&f.Text.Contains("已满"));if(picker!=null){timer.Stop();picker.Controls.OfType<Button>().First(b=>b.Text==WatchName(settings.Slots[0])).PerformClick();}};timer.Start();add(seeds[4].Key);}
            if(settings.Slots.Count!=5||settings.Slots[0].Key!=seeds[4].Key)throw new Exception("full replacement failed");
            string before=string.Join(",",settings.Slots.Select(s=>s.Key));
            using(var timer=new Timer{Interval=60}){timer.Tick+=(o,e)=>{var picker=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f!=form&&f.Text.Contains("已满"));if(picker!=null){timer.Stop();picker.Close();}};timer.Start();add(game.Key);}
            if(before!=string.Join(",",settings.Slots.Select(s=>s.Key)))throw new Exception("cancel changed list");
            while(settings.Slots.Count>0){((Button)favorites.Controls[0].Controls["remove"]).PerformClick();flush();}
            var saved=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(settingsPath));if(saved.Slots.Count!=0)throw new Exception("empty not persisted");
            if(PickSlot(4).Item1.Key!="empty:")throw new Exception("empty bar failed");
            settingsPath=Path.Combine(folder,"missing-test-directory", "settings.json");if(CommitWatchlist(seeds.Take(1).ToList())||settings.Slots.Count!=0)throw new Exception("failed save did not roll back");
            form.Close();
        }
        CaptureForm(MakePreferences(),Path.Combine(folder,"preview-preferences.png"));
        File.WriteAllText(Path.Combine(folder,"interaction-check.txt"),"PASS: append, direct team follow, duplicate prevention, move up, synthetic drag-drop event, remove/compact, full replacement, cancel, empty persistence, empty bar, failed-save rollback, preferences rendering.");
        }finally{settings.Slots=original;settingsPath=originalPath;var testFile=Path.Combine(folder,"interaction-settings.tmp.json");if(File.Exists(testFile))File.Delete(testFile);}
    }
    public void VerifyDetailFixtures(string input,string output){
        foreach(string kind in new[]{"nba","soccer"}){
            var items=Feed.ParseSports(File.ReadAllText(Path.Combine(input,kind+"-details-fixture.json")),kind=="nba"?"NBA":"足球",kind=="nba"?"NBA":"西甲");var item=items.First(x=>x.Rank==2);data.Add(new Batch{Group=item.Group,Source="ESPN",Fetched=Feed.Now,Items=items});
            if(kind=="nba"&&(item.PeriodScores==null||!item.PeriodScores.Contains("Q1")))throw new Exception("real NBA quarters missing");
            if(kind=="soccer"&&(item.FootballEvents==null||!item.FootballEvents.Contains("进球")))throw new Exception("real soccer goals missing");
            CaptureForm(MakeDetail(item,false),Path.Combine(output,"preview-"+kind+"-detail.png"));
        }
        var market=new Item{Key="stock:demo",Group="A股",Name="走势图演示 · 模拟数据",Value="¥101.00",Price=101,Change=1,Source="示例",SourceTime=Feed.Now.ToString("yyyyMMddHHmmss")};
        for(int i=0;i<6;i++){market=new Item{Key=market.Key,Group=market.Group,Name=market.Name,Value=market.Value,Price=100+i*.2,Change=1,Source="示例",SourceTime=Feed.Now.AddMinutes(i-5).ToString("yyyyMMddHHmmss")};ApplyBatch(new Batch{Group="A股",Source="示例",Fetched=Feed.Now.AddMinutes(i-5),Items=new List<Item>{market}});}
        if(quoteHistory[market.Key].Count!=6)throw new Exception("price history missing");
        ApplyBatch(new Batch{Group="A股",Source="示例",Fetched=Feed.Now,Items=new List<Item>{market}});if(quoteHistory[market.Key].Count!=6)throw new Exception("duplicate quote timestamp added");
        ApplyBatch(new Batch{Group="A股",Source="示例",Error="offline",Fetched=Feed.Now});if(quoteHistory[market.Key].Count!=6)throw new Exception("failed quote added");
        CaptureForm(MakeDetail(market,false),Path.Combine(output,"preview-market-detail.png"));
        File.WriteAllText(Path.Combine(output,"detail-fixture-check.txt"),"PASS: captured ESPN NBA quarter scores and soccer goals; sampled chart; duplicate timestamps and failed batches not appended. Market preview uses explicit simulated prices.");
    }
}
}
