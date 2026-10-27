using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
namespace CornerBar {
public partial class Bar {
    public void VerifyInteractions(string folder){
        data.Clear();var game=new Item{Key="game:NBA:test",Group="NBA",Name="湖人 · 勇士",Home="湖人",Away="勇士",HomeId="13",AwayId="9",League="NBA",Day=Feed.Now.ToString("yyyy-MM-dd"),Value="98 : 94"};data.Add(new Batch{Group="NBA",Source="fixture",Fetched=Feed.Now,Items=new List<Item>{game}});
        using(var form=MakeSettings()){
            form.Show();Application.DoEvents();
            var category=form.Controls.OfType<ComboBox>().First();var list=form.Controls.OfType<ListView>().Single();
            for(int i=0;i<20;i++){category.SelectedItem=i%2==0?"足球":"NBA";Application.DoEvents();}
            category.SelectedItem="NBA";
            list.Items.Cast<ListViewItem>().First(r=>((Slot)r.Tag).Key==game.Key).Selected=true;
            var add=form.Controls.OfType<Button>().First(b=>b.Text=="添加到顶部…");
            using(var timer=new Timer{Interval=60}){
                timer.Tick+=(o,e)=>{var picker=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text=="ROW · 放到哪个位置");if(picker!=null){timer.Stop();picker.Controls.OfType<Button>().First(b=>b.Text.StartsWith("位置 5")).PerformClick();}};
                timer.Start();add.PerformClick();
            }
            if(!form.Controls.OfType<Button>().Any(b=>b.Text=="5  "+game.Name))throw new Exception("match position assignment failed");
            using(var timer=new Timer{Interval=60}){
                int stage=0;timer.Tick+=(o,e)=>{var picker=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text==(stage==0?"ROW · 跟随哪支球队":"ROW · 放到哪个位置"));if(picker==null)return;if(stage++==0)picker.Controls.OfType<Button>().First(b=>b.Text=="湖人").PerformClick();else{timer.Stop();picker.Controls.OfType<Button>().First(b=>b.Text.StartsWith("位置 2")).PerformClick();}};
                timer.Start();form.Controls.OfType<Button>().First(b=>b.Text=="跟随球队…").PerformClick();
            }
            if(!form.Controls.OfType<Button>().Any(b=>b.Text=="2  湖人"))throw new Exception("team assignment failed");
            using(var timer=new Timer{Interval=60}){timer.Tick+=(o,e)=>{var picker=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text=="ROW · 放到哪个位置");if(picker!=null){timer.Stop();picker.Close();}};timer.Start();add.PerformClick();}
            if(!form.Controls.OfType<Button>().Any(b=>b.Text=="5  "+game.Name))throw new Exception("cancel changed assignment");
            form.Close();
        }
        File.WriteAllText(Path.Combine(folder,"interaction-check.txt"),"PASS: switch category 20 times; add NBA match to position 5; follow home team to position 2; cancel replacement preserves selection; close settings.");
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
