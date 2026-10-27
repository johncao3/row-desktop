using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
namespace CornerBar {
public partial class Bar {
    readonly Dictionary<string,DateTime> scoreChanges=new Dictionary<string,DateTime>();
    internal static Slot TeamSlot(Item game,bool home){return new Slot{Key="team:"+game.Group+":"+game.League+":"+(home?game.HomeId:game.AwayId),Name=home?game.Home:game.Away,Group=game.Group};}
    internal static Item FollowedMatch(Slot slot,IEnumerable<Item> items){
        var matches=items.Where(i=>i.Home!=null&&(TeamSlot(i,true).Key==slot.Key||TeamSlot(i,false).Key==slot.Key)).ToArray();
        return matches.Where(i=>i.Rank==0).OrderBy(i=>i.StartTime).FirstOrDefault()
            ??matches.Where(i=>i.Rank==2&&i.Day==Feed.Now.ToString("yyyy-MM-dd")).OrderByDescending(i=>i.StartTime).FirstOrDefault()
            ??matches.Where(i=>i.Rank==1).OrderBy(i=>i.StartTime).FirstOrDefault();
    }
    internal static bool ScoreChanged(Item before,Item after,Batch old,Batch fresh){
        return old!=null&&old.Error==""&&fresh.Error==""&&before!=null&&before.Key==after.Key&&before.Home!=null&&after.Home!=null&&before.Rank==0&&(after.Rank==0||after.Rank==2)
            &&fresh.Fetched>=old.Fetched&&(fresh.Fetched-old.Fetched).TotalSeconds<=150&&before.Value!=after.Value;
    }
    void ApplyBatch(Batch batch){ RecordDetails(batch);
        var old=data.FirstOrDefault(b=>b.Group==batch.Group&&b.Source==batch.Source);
        foreach(var item in batch.Items){var before=old==null?null:old.Items.FirstOrDefault(i=>i.Key==item.Key);if(ScoreChanged(before,item,old,batch)&&item.Key!=null)scoreChanges[item.Key]=Feed.Now;}
        foreach(string key in scoreChanges.Where(x=>(Feed.Now-x.Value).TotalSeconds>=12).Select(x=>x.Key).ToArray())scoreChanges.Remove(key);
        Feed.RetainLastKnown(batch,data);data.RemoveAll(b=>b.Group==batch.Group&&b.Source==batch.Source);data.Add(batch);
    }
    string ReceivedText(Item item){
        var batch=data.FirstOrDefault(b=>b.Items.Contains(item));
        if(batch==null||batch.Fetched==DateTime.MinValue)return "尚未接收到数据";
        string prefix=batch.Error!=""?"连接异常 · 最近接收 ":"本机接收 ";
        return prefix+batch.Fetched.ToString("MM-dd HH:mm:ss")+" 北京时间";
    }
    internal static string QuoteAge(Item item){
        if(item.Home!=null)return "比分为最近一次接口快照；时间不会在本地模拟前进。";
        if(item.Group!="A股"&&item.Group!="美股")return "接口未提供报价时间，无法判断源数据延迟。";
        DateTime stamp;
        if(!DateTime.TryParseExact(item.SourceTime,new[]{"yyyyMMddHHmmss","yyyy-MM-dd HH:mm:ss"},CultureInfo.InvariantCulture,DateTimeStyles.None,out stamp))return "无法解析源报价时间；不标记为实时。";
        try{
            var zone=TimeZoneInfo.FindSystemTimeZoneById(item.Group=="美股"?"Eastern Standard Time":"China Standard Time");
            double seconds=(DateTime.UtcNow-TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(stamp,DateTimeKind.Unspecified),zone)).TotalSeconds;
            if(seconds< -60)return "源时间晚于本机时间，请核对时钟。";
            string age=seconds<60?Math.Max(0,(int)seconds)+"秒":seconds<3600?((int)(seconds/60))+"分钟":((int)(seconds/3600))+"小时";
            return "报价距今 "+age+"（含休市时间，不等于接口延迟）";
        }catch{return "源报价时间按市场当地时间显示。";}
    }
    internal static void VerifyAwareness(){
        var game=new Item{Key="game:NBA:1",Group="NBA",League="NBA",Home="湖人",Away="勇士",HomeId="13",AwayId="9",Rank=0,Value="98 : 94",Day=Feed.Now.ToString("yyyy-MM-dd"),StartTime=DateTime.UtcNow};
        var next=new Item{Key="game:NBA:2",Group="NBA",League="NBA",Home="湖人",Away="其他",HomeId="13",AwayId="99",Rank=1,Value="VS",StartTime=DateTime.UtcNow.AddDays(1)};
        var slot=TeamSlot(game,true);
        if(FollowedMatch(slot,new[]{next,game})!=game)throw new Exception("team live priority");
        game.Rank=2;if(FollowedMatch(slot,new[]{next,game})!=game)throw new Exception("retain today's final");
        game.Day=Feed.Now.AddDays(-1).ToString("yyyy-MM-dd");if(FollowedMatch(slot,new[]{next,game})!=next)throw new Exception("team next game");
        if(FollowedMatch(slot,new[]{game})!=null)throw new Exception("no old game fallback");
        next.League="other";if(FollowedMatch(slot,new[]{next})!=null)throw new Exception("team IDs must be namespaced");
        var previous=new Item{Key=game.Key,Home="湖人",Rank=0,Value="96 : 94"};game.Rank=0;
        var old=new Batch{Fetched=Feed.Now.AddSeconds(-60)};var fresh=new Batch{Fetched=Feed.Now};
        if(!ScoreChanged(previous,game,old,fresh))throw new Exception("score update missing");
        previous.Value=game.Value;if(ScoreChanged(previous,game,old,fresh))throw new Exception("repeated score alert");
        previous.Value="100 : 94";if(!ScoreChanged(previous,game,old,fresh))throw new Exception("score correction missing");
        old.Error="offline";if(ScoreChanged(previous,game,old,fresh))throw new Exception("reconnect alert");
        old.Error="";old.Fetched=Feed.Now.AddMinutes(-5);if(ScoreChanged(previous,game,old,fresh))throw new Exception("stale baseline alert");
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"awareness-check.txt"),"PASS: live/final/next team selection; league-scoped IDs; score changes and corrections; no duplicate/reconnect/stale alerts.");
    }
}
}
