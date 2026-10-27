using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
namespace CornerBar {
internal static class DetailData {
    static bool Flag(object obj,string key){return string.Equals(Feed.S(Feed.Get(obj,key)),"true",StringComparison.OrdinalIgnoreCase);}
    static string Score(object team,int period){var p=Feed.Arr(Feed.Get(team,"linescores")).FirstOrDefault(x=>Feed.S(Feed.Get(x,"period"))==period.ToString());return p==null?"—":Feed.S(Feed.Get(p,"displayValue")??Feed.Get(p,"value"));}
    internal static string Periods(object home,object away){
        var periods=Feed.Arr(Feed.Get(home,"linescores")).Concat(Feed.Arr(Feed.Get(away,"linescores"))).Select(x=>{int n;return int.TryParse(Feed.S(Feed.Get(x,"period")),out n)?n:0;}).Where(n=>n>0).Distinct().OrderBy(n=>n).ToArray();
        if(periods.Length==0)return "各节比分：接口暂无数据";
        return "各节比分（主 : 客）\r\n"+string.Join("   ",periods.Select(n=>(n<=4?"Q"+n:"OT"+(n-4))+"  "+Score(home,n)+" : "+Score(away,n)));
    }
    internal static string Margin(object home,object away,string state){
        int h,a;if(state=="pre")return "尚未开赛";
        if(!int.TryParse(Feed.S(Feed.Get(home,"score")),out h)||!int.TryParse(Feed.S(Feed.Get(away,"score")),out a))return "分差暂无数据";
        return h==a?"双方战平":(h>a?"主队领先 ":"客队领先 ")+Math.Abs(h-a)+" 分";
    }
    internal static string Events(object comp,object home,object away){
        if(Feed.Get(comp,"details")==null)return "进球 / 红牌：接口暂无事件数据";
        var rows=new List<string>();
        foreach(var e in Feed.Arr(Feed.Get(comp,"details"))){
            bool red=Flag(e,"redCard"),goal=Flag(e,"scoringPlay");if(!red&&!goal)continue;
            string id=Feed.S(Feed.Get(e,"team","id"));
            string team=id==Feed.S(Feed.Get(home,"team","id"))?"主队":id==Feed.S(Feed.Get(away,"team","id"))?"客队":"球队未注明";
            string kind=red?"红牌":Flag(e,"shootout")?"点球大战进球":Flag(e,"ownGoal")?"乌龙球":Flag(e,"penaltyKick")?"点球进球":"进球";
            string player=string.Join(" / ",Feed.Arr(Feed.Get(e,"athletesInvolved")).Select(a=>Feed.S(Feed.Get(a,"displayName"))).Where(s=>s!=""));
            rows.Add(Feed.S(Feed.Get(e,"clock","displayValue"))+"  "+kind+" · "+team+"  "+player);
        }
        return rows.Count==0?"接口当前未返回进球或红牌事件":string.Join("\r\n",rows.Distinct());
    }
    internal static void Verify(){
        object h=Feed.Json("{\"score\":\"101\",\"team\":{\"id\":\"1\"},\"linescores\":[{\"period\":1,\"value\":25},{\"period\":5,\"value\":8}]}");
        object a=Feed.Json("{\"score\":\"99\",\"team\":{\"id\":\"2\"},\"linescores\":[{\"period\":1,\"value\":24}]}");
        if(!Periods(h,a).Contains("OT1  8 : —")||Margin(h,a,"in")!="主队领先 2 分"||Margin(h,a,"pre")!="尚未开赛")throw new Exception("period/margin parser");
        object c=Feed.Json("{\"details\":[{\"scoringPlay\":true,\"ownGoal\":true,\"team\":{\"id\":\"1\"}},{\"redCard\":true,\"team\":{\"id\":\"2\"}},{\"yellowCard\":true}]}");
        string events=Events(c,h,a);if(!events.Contains("乌龙球")||!events.Contains("红牌")||events.Split(new[]{"\r\n"},StringSplitOptions.None).Length!=2||!Events(Feed.Json("{}"),h,a).Contains("暂无"))throw new Exception("football events parser");
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"details-check.txt"),"PASS: per-period scores including OT/missing quarters; pregame and signed margin; goals/own goals/red cards; missing events distinct from empty events.");
    }
}
internal class QuotePoint { internal DateTime Time; internal double Price; internal string SourceTime; }
internal sealed class Sparkline : Control {
    internal Func<List<QuotePoint>> Points;
    internal Palette Colors;
    internal Sparkline(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;g.Clear(Colors.Soft);g.SmoothingMode=SmoothingMode.AntiAlias;
        var pts=Points();if(pts.Count<2){TextRenderer.DrawText(g,"正在积累走势 · 至少需要两次有效报价",Font,ClientRectangle,Colors.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        double min=pts.Min(p=>p.Price),max=pts.Max(p=>p.Price),span=Math.Max(0.00000001,max-min);double duration=Math.Max(1,(pts.Last().Time-pts.First().Time).TotalSeconds);
        Func<QuotePoint,PointF> xy=p=>new PointF(12+(float)((p.Time-pts[0].Time).TotalSeconds/duration)*(Width-24),24+(float)((max-p.Price)/span)*(Height-54));
        using(var pen=new Pen(Brand.Orange,2))for(int i=1;i<pts.Count;i++)if((pts[i].Time-pts[i-1].Time).TotalSeconds<=150)g.DrawLine(pen,xy(pts[i-1]),xy(pts[i]));
        foreach(var p in pts)using(var brush=new SolidBrush(Brand.Orange)){var v=xy(p);g.FillEllipse(brush,v.X-2,v.Y-2,4,4);}
        TextRenderer.DrawText(g,min.ToString("0.########")+" — "+max.ToString("0.########"),Font,new Point(8,4),Colors.Muted);
        TextRenderer.DrawText(g,pts[0].Time.ToString("HH:mm:ss")+"  —  "+pts.Last().Time.ToString("HH:mm:ss")+"  北京时间",Font,new Point(8,Height-22),Colors.Muted);
    }
}
public partial class Bar {
    readonly Dictionary<string,List<QuotePoint>> quoteHistory=new Dictionary<string,List<QuotePoint>>();
    readonly Dictionary<string,string> lastScoreUpdate=new Dictionary<string,string>();
    internal static void ReportError(Exception error){try{File.AppendAllText(Path.Combine(Base,"crash-log.txt"),DateTime.Now.ToString("o")+"\r\n"+error+"\r\n\r\n");}catch{}}
    int ChooseOption(Form owner,string title,string[] choices){
        using(var picker=new Surface(title,Colors,new Size(460,112+choices.Length*48))){
            picker.Heading(title,"选择后返回自选；点击 × 取消");int selected=-1;
            for(int i=0;i<choices.Length;i++){int n=i;picker.ButtonAt(choices[i],24,104+i*48,412,40,()=>{selected=n;picker.Close();},false);}
            picker.TopMost=true;picker.ShowDialog(owner);return selected;
        }
    }
    void RecordDetails(Batch batch){
        if(batch.Error!="")return;
        var old=data.FirstOrDefault(b=>b.Source==batch.Source&&b.Group==batch.Group);
        foreach(var item in batch.Items){
            if(string.IsNullOrEmpty(item.Key))continue;
            var previous=old==null?null:old.Items.FirstOrDefault(i=>i.Key==item.Key);
            if(ScoreChanged(previous,item,old,batch))lastScoreUpdate[item.Key]=previous.Value+" → "+item.Value+" · "+batch.Fetched.ToString("HH:mm:ss")+" 观察到（含比分修正）";
            if(!item.Price.HasValue)continue;
            List<QuotePoint> history;if(!quoteHistory.TryGetValue(item.Key,out history)){if(quoteHistory.Count>=200)quoteHistory.Remove(quoteHistory.Keys.First());history=new List<QuotePoint>();quoteHistory[item.Key]=history;}
            if(history.Count>0&&item.Group!="加密"&&history.Last().SourceTime==item.SourceTime)continue;
            history.Add(new QuotePoint{Time=batch.Fetched,Price=item.Price.Value,SourceTime=item.SourceTime});if(history.Count>180)history.RemoveAt(0);
        }
        if(lastScoreUpdate.Count>200)lastScoreUpdate.Remove(lastScoreUpdate.Keys.First());
    }
    Surface MakeDetail(Item x,bool stale){
        var f=new Surface("详情",Colors,new Size(560,560));f.Heading(x.Name,x.Group+" / "+(stale?"旧数据 · 等待连接":"最近一次快照"));
        var value=f.LabelAt("",24,108,512,42,26,false);
        var body=new TextBox{Bounds=new Rectangle(24,162,512,210),Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Bg,ForeColor=Ink,ScrollBars=ScrollBars.Vertical,Font=new Font("Microsoft YaHei UI",10)};f.Controls.Add(body);
        var received=f.LabelAt("",24,452,512,26,12,true);var age=f.LabelAt("",24,482,512,50,11,true);
        Sparkline chart=null;
        if(x.Home==null){body.Height=113;chart=new Sparkline{Bounds=new Rectangle(24,282,512,139),Colors=Colors,Font=f.Font,Points=()=>{List<QuotePoint> h;return x.Key!=null&&quoteHistory.TryGetValue(x.Key,out h)?h.ToList():new List<QuotePoint>();}};f.Controls.Add(chart);f.LabelAt("本次运行采样走势 · 最多180点 · 非完整日内K线",24,425,512,22,11,true);}
        Action update=()=>{
            if(f.IsDisposed)return;
            var current=data.SelectMany(b=>b.Items).FirstOrDefault(i=>!string.IsNullOrEmpty(x.Key)&&i.Key==x.Key);if(current!=null)x=current;
            value.Text=x.Value+(x.Change.HasValue?"   "+x.Change.Value.ToString("+0.00;-0.00;0.00")+"%":"");
            string details;
            if(x.Group=="NBA"&&x.Home!=null){string change;details=(x.Phase??x.Detail)+"\r\n"+(x.ScoreMargin??"分差暂无数据")+"\r\n\r\n"+(x.PeriodScores??"各节比分暂无数据")+"\r\n\r\n最近比分变化\r\n"+(x.Key!=null&&lastScoreUpdate.TryGetValue(x.Key,out change)?change:"本次运行尚未观察到变化");}
            else if(x.Home!=null)details="比赛阶段 · "+(x.Phase??x.Detail)+"\r\n\r\n进球与红牌（接口事件）\r\n"+(x.FootballEvents??"接口暂无事件数据");
            else details=x.Detail+"\r\n源报价时间 · "+x.SourceTime+"\r\n来源 · "+x.Source;
            if(body.Text!=details)body.Text=details;
            received.Text=ReceivedText(x);age.Text=(x.Home!=null?"来源 · "+x.Source+"\r\n":"")+QuoteAge(x);if(chart!=null)chart.Invalidate();
        };update();
        if(interactive){var timer=new Timer{Interval=2000};timer.Tick+=(o,e)=>update();f.Shown+=(o,e)=>timer.Start();f.FormClosed+=(o,e)=>{timer.Stop();timer.Dispose();};}
        return f;
    }
}
}
