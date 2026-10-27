using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CornerBar {
public class Slot { public string Key,Name,Group; public override string ToString(){return Name;} }
public class Settings {
    public string Stocks = "sh000001,sh600519,sz300750,us.INX,us.IXIC,us.DJI,usAAPL,usMSFT,usNVDA";
    public string CryptoIds="90,80"; public List<Slot> Slots=new[]{"A股","美股","加密","NBA","足球"}.Select(g=>new Slot{Key="auto:"+g,Name=g+"轮播",Group=g}).ToList();
    public string Leagues = "eng.1,esp.1,uefa.champions";
    public bool Top = true, Compact = false, Quiet = false, Rotate = true;
    public int Width = 1120, Category = 0, Opacity = 96;
    public int Version = 0;
    public string Theme = "auto", Monitor = "";
}
public class Item {
    public string HomeId,AwayId,League; public DateTime StartTime; public string Key,Day; public string Group, Name, Value, Detail, Source, SourceTime, Url; public string Home, Away, HomeLogo, AwayLogo, Status;
    public double? Change,Price; public string PeriodScores,ScoreMargin,Phase,FootballEvents;
    public int Rank;
}
public class Batch {
    public string Group, Source, Error = "";
    public DateTime Fetched;
    public List<Item> Items = new List<Item>();
}
public static class Feed {
    public static DateTime Now { get { return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime; } }
    static readonly HttpClient Client = MakeClient();
    static HttpClient MakeClient() {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var c = new HttpClient(); c.Timeout = TimeSpan.FromSeconds(14);
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ROW/1.05 (personal desktop ticker)"); return c;
    }
    public static object Json(string s) { return new JavaScriptSerializer { MaxJsonLength=8388608 }.DeserializeObject(s); }
    public static object Get(object obj, params string[] path) {
        foreach (string key in path) { var d = obj as Dictionary<string,object>; object value;
            if (d == null || !d.TryGetValue(key, out value)) return null; obj = value; } return obj;
    }
    public static string S(object x) { return x == null ? "" : Convert.ToString(x, CultureInfo.InvariantCulture); }
    public static IEnumerable<object> Arr(object o) { return o as object[] ?? new object[0]; }
    public static double Number(object x) { double n; if (!double.TryParse(S(x), NumberStyles.Float, CultureInfo.InvariantCulture, out n) || double.IsNaN(n) || double.IsInfinity(n)) throw new Exception("数据缺少有效数值"); return n; }
    public static string[] Tokens(string s) { return Regex.Split(s.Trim(), @"[\s,，;；]+").Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }
    public static List<Item> ParseStocks(string text, string[] codes, string group) {
        var result = new List<Item>();
        foreach (string code in codes) {
            var m = Regex.Match(text, "v_" + Regex.Escape(code) + "=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            if (!m.Success) throw new Exception("未返回代码 " + code);
            string[] f = m.Groups[1].Value.Split('~'); if (f.Length < 34) throw new Exception("无效代码或行情格式变化：" + code);
            double price = Number(f[3]), change = Number(f[32]); if (price <= 0) throw new Exception("行情价格无效：" + code);
            bool index=code.StartsWith("sh000",StringComparison.OrdinalIgnoreCase)||code.StartsWith("sz399",StringComparison.OrdinalIgnoreCase)||code.StartsWith("us.",StringComparison.OrdinalIgnoreCase)||f[2].StartsWith(".");
            result.Add(new Item { Key="stock:"+code.ToLowerInvariant(), Group=group, Name=f[1], Value=(index?"":group=="A股"?"¥":"$")+price.ToString("N2"), Change=change, Price=price,
                Detail=(index?"指数点数":code.ToUpperInvariant())+" · 较前收盘", Source="腾讯行情", SourceTime=f[30],
                Url="https://gu.qq.com/"+code });
        } return result;
    }
    public static List<Item> ParseCrypto(string text) { return ParseCoins(text,new[]{"90","80"},true); }
    public static List<Item> ParseCoins(string text,string[] ids,bool legacy=false) {
        var result = new List<Item>();
        foreach (object coin in Arr(Json(text))) {
            string symbol = S(Get(coin,"symbol")), id=S(Get(coin,"id")); if(legacy&&id=="")id=symbol=="BTC"?"90":symbol=="ETH"?"80":""; if(!ids.Contains(id))continue;
            double price=Number(Get(coin,"price_usd")); if (price<=0) throw new Exception("币价无效");
            result.Add(new Item { Key="coin:"+id, Group="加密", Name=symbol, Value="$"+price.ToString(price<1?"0.########":"N2"), Change=Number(Get(coin,"percent_change_24h")),Price=price,
                Detail="USD · 过去24小时", Source="CoinLore", SourceTime="未提供报价时刻", Url="https://www.coinlore.com/coin/"+S(Get(coin,"nameid")) });
        }
        if (result.Select(x=>x.Key).Distinct().Count()!=ids.Length) throw new Exception("所选币种数据不完整"); return result;
    }
    static readonly Dictionary<string,string> Names = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
        {"Los Angeles Lakers","湖人"},{"Golden State Warriors","勇士"},{"Boston Celtics","凯尔特人"},{"LA Clippers","快船"},
        {"Houston Rockets","火箭"},{"Dallas Mavericks","独行侠"},{"Denver Nuggets","掘金"},{"Oklahoma City Thunder","雷霆"},
        {"Cleveland Cavaliers","骑士"},{"New York Knicks","尼克斯"},{"Milwaukee Bucks","雄鹿"},{"Phoenix Suns","太阳"},
        {"San Antonio Spurs","马刺"},{"Minnesota Timberwolves","森林狼"},{"Miami Heat","热火"},{"Philadelphia 76ers","76人"},
        {"Atlanta Hawks","老鹰"},{"Brooklyn Nets","篮网"},{"Charlotte Hornets","黄蜂"},{"Chicago Bulls","公牛"},
        {"Detroit Pistons","活塞"},{"Indiana Pacers","步行者"},{"Memphis Grizzlies","灰熊"},{"New Orleans Pelicans","鹈鹕"},
        {"Orlando Magic","魔术"},{"Portland Trail Blazers","开拓者"},{"Sacramento Kings","国王"},{"Toronto Raptors","猛龙"},{"Utah Jazz","爵士"},{"Washington Wizards","奇才"},
        {"Arsenal","阿森纳"},{"Chelsea","切尔西"},{"Liverpool","利物浦"},{"Manchester City","曼城"},{"Manchester United","曼联"},
        {"Ipswich Town","伊普斯维奇"},{"Nottingham Forest","诺丁汉森林"},{"Aston Villa","阿斯顿维拉"},{"Everton","埃弗顿"},
        {"Coventry City","考文垂"},{"Fulham","富勒姆"},{"Brighton & Hove Albion","布莱顿"},{"Newcastle United","纽卡斯尔"},
        {"West Ham United","西汉姆联"},{"Crystal Palace","水晶宫"},{"Brentford","布伦特福德"},{"Bournemouth","伯恩茅斯"},
        {"Wolverhampton Wanderers","狼队"},{"Leeds United","利兹联"},{"Burnley","伯恩利"},{"Sunderland","桑德兰"},
        {"Tottenham Hotspur","热刺"},{"Real Madrid","皇马"},{"Barcelona","巴塞罗那"},{"Bayern Munich","拜仁"},
        {"Paris Saint-Germain","巴黎圣日耳曼"},{"Juventus","尤文图斯"},{"Internazionale","国际米兰"},{"AC Milan","AC米兰"}
    };
    static string Team(object competitor) { string name=S(Get(competitor,"team","displayName")), cn; return Names.TryGetValue(name,out cn)?cn:name; }
    public static List<Item> ParseSports(string text, string group, string leagueName) {
        object root=Json(text); if (Get(root,"events")==null) throw new Exception("比分响应缺少 events");
        var result=new List<Item>();
        foreach(object ev in Arr(Get(root,"events"))) {
            var comp=Arr(Get(ev,"competitions")).FirstOrDefault(); var teams=Arr(Get(comp,"competitors")).ToArray(); if(teams.Length!=2) continue;
            object home=teams.FirstOrDefault(x=>S(Get(x,"homeAway"))=="home")??teams[0];
            object away=teams.FirstOrDefault(x=>S(Get(x,"homeAway"))=="away")??teams[1];
            string state=S(Get(ev,"status","type","state")), status=S(Get(ev,"status","type","shortDetail"));
            DateTimeOffset date; bool hasDate=DateTimeOffset.TryParse(S(Get(ev,"date")), out date);
            if(state=="pre" && hasDate) status="未开始 · "+date.ToOffset(TimeSpan.FromHours(8)).ToString("MM-dd HH:mm");
            else if(state=="post") status="已结束";
            else if(state=="in") status=S(Get(ev,"status","type","name")).Contains("HALFTIME")?"中场休息":"进行中 · "+S(Get(ev,"status","displayClock"))+" / "+S(Get(ev,"status","period"))+ (group=="NBA"?"节":"半场");
            string value=state=="pre"?"VS":S(Get(home,"score"))+" : "+S(Get(away,"score"));
            result.Add(new Item { Key="game:"+group+":"+S(Get(ev,"id")), HomeId=S(Get(home,"team","id")), AwayId=S(Get(away,"team","id")), League=leagueName, StartTime=hasDate?date.UtcDateTime:DateTime.MaxValue, Day=hasDate?date.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd"):"", Group=group, Name=Team(home)+" · "+Team(away), Value=value,
                PeriodScores=DetailData.Periods(home,away),ScoreMargin=DetailData.Margin(home,away,state),Phase=status,FootballEvents=DetailData.Events(comp,home,away),Home=Team(home), Away=Team(away), HomeLogo=S(Get(home,"team","logo")), AwayLogo=S(Get(away,"team","logo")), Status=state=="in"?(S(Get(ev,"status","type","name")).Contains("HALFTIME")?"中场":group=="NBA"?"Q"+S(Get(ev,"status","period"))+" "+S(Get(ev,"status","displayClock")):S(Get(ev,"status","displayClock"))):state=="post"?"完场":hasDate?date.ToOffset(TimeSpan.FromHours(8)).ToString("MM-dd HH:mm"):"待定", Detail=leagueName+" · "+status, Source="ESPN", SourceTime=hasDate?"开赛 "+date.ToOffset(TimeSpan.FromHours(8)).ToString("MM-dd HH:mm")+" 北京时间":"",
                Rank=state=="in"?0:state=="pre"?1:2, Url=S(Get(Arr(Get(ev,"links")).FirstOrDefault(),"href")) });
        } return result.OrderBy(x=>x.Rank).ToList();
    }
    public static async Task<Batch> Load(string group, string url, Func<string,List<Item>> parser, string source, bool gb) {
        var b=new Batch { Group=group, Source=source };
        try { byte[] bytes=await Client.GetByteArrayAsync(url); string text=(gb?Encoding.GetEncoding(936):Encoding.UTF8).GetString(bytes); b.Items=parser(text); b.Fetched=Now; }
        catch(Exception e) { b.Error=e is TaskCanceledException?"连接超时":e.Message; }
        return b;
    }
    public static async Task<Batch> LoadSports(string group, string path, string label) {
        string source="ESPN · "+label;
        var requests=Enumerable.Range(-1,3).Select(offset=>Load(group,"https://site.api.espn.com/apis/site/v2/sports/"+path+"/scoreboard?dates="+Now.AddDays(offset).ToString("yyyyMMdd"),s=>ParseSports(s,group,label),source,false));
        Batch[] days=await Task.WhenAll(requests);
        var result=new Batch{Group=group,Source=source,Fetched=days.Any(d=>d.Error=="")?Now:DateTime.MinValue};
        result.Items=days.SelectMany(d=>d.Items).GroupBy(i=>i.Name+"|"+i.SourceTime).Select(g=>g.First()).OrderBy(i=>i.Rank).ToList();
        result.Error=string.Join(" / ",days.Where(d=>d.Error!="").Select(d=>d.Error).Distinct());return result;
    }
    public static void RetainLastKnown(Batch fresh,IEnumerable<Batch> previous) {
        if(fresh.Error==""||fresh.Items.Count>0)return;
        Batch old=previous.FirstOrDefault(x=>x.Group==fresh.Group&&x.Source==fresh.Source);
        if(old!=null){fresh.Items=old.Items;fresh.Fetched=old.Fetched;}
    }
    public static Task<Batch>[] Jobs(Settings settings) {
        var jobs=new List<Task<Batch>>(); string[] codes=Tokens(settings.Stocks).Select(MarketCatalog.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach(string group in new[]{"A股","美股"}) {
            string g=group; string[] list=codes.Where(x=>g=="美股"?x.StartsWith("us",StringComparison.OrdinalIgnoreCase):!x.StartsWith("us",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(list.Length==0) { jobs.Add(Task.FromResult(new Batch { Group=g, Source="腾讯行情", Fetched=Now })); continue; }
            foreach(string code in list){string c=code;jobs.Add(Load(g,"https://qt.gtimg.cn/q="+c,s=>ParseStocks(s,new[]{c},g),"腾讯行情 · "+c,true));}
        }
        string[] coinIds=Tokens(settings.CryptoIds??"90,80").Where(id=>Regex.IsMatch(id,@"^\d+$")).Take(30).ToArray(); if(coinIds.Length>0)jobs.Add(Load("加密","https://api.coinlore.net/api/ticker/?id="+string.Join(",",coinIds),s=>ParseCoins(s,coinIds),"CoinLore",false));
        jobs.Add(LoadSports("NBA","basketball/nba","NBA"));
        foreach(string league in Tokens(settings.Leagues)) {
            string l=league; string label=l=="eng.1"?"英超":l=="esp.1"?"西甲":l=="uefa.champions"?"欧冠":l=="ita.1"?"意甲":l=="ger.1"?"德甲":l=="fra.1"?"法甲":l=="chn.1"?"中超":l;
            jobs.Add(LoadSports("足球","soccer/"+l,label));
        } return jobs.ToArray();
    }
}

static class Program {
    [STAThread] static void Main(string[] args){try{Run(args);}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error-log.txt"),ex.ToString());Environment.ExitCode=1;if(args.Length==0)MessageBox.Show("ROW 启动失败，详情已写入 error-log.txt。\n"+ex.Message);}}
    static void Run(string[] args){
        #if SETTINGS_TEST
        args=new[]{"--settings"};
        #endif
        #if DOCK_TEST
        args=new[]{"--dock-test"};
        #endif
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(o,e)=>{Bar.ReportError(e.Exception);if(args.Length>0)Environment.Exit(1);MessageBox.Show("操作未完成，请关闭当前窗口后重试。记录已保存到 crash-log.txt。", "ROW");};DesktopDock.EnableDpi();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        var settings=new Settings();string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
        try{if(File.Exists(file))settings=new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(file));}catch{ }
        if(settings==null)settings=new Settings();
        if(settings.Version<2){var codes=Feed.Tokens(settings.Stocks??new Settings().Stocks);settings.Stocks=string.Join(",",codes.Where(c=>!c.StartsWith("us",StringComparison.OrdinalIgnoreCase)).Concat(new[]{"us.INX","us.IXIC","us.DJI"}).Concat(codes.Where(c=>c.StartsWith("us",StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.OrdinalIgnoreCase));settings.Top=true;settings.Compact=false;settings.Theme="auto";settings.Opacity=100;settings.Version=2;}
        if(settings.Slots==null||settings.Slots.Count!=5||settings.Slots.Any(s=>s==null||string.IsNullOrEmpty(s.Key)||string.IsNullOrEmpty(s.Group)))settings.Slots=new Settings().Slots;settings.Version=105;
        if(args.Length>0&&args[0]=="--catalog-check"){var coins=MarketCatalog.Coins().GetAwaiter().GetResult();var probe=new Settings{Stocks="sh600519,sz000001,usTSLA,usQQQ",CryptoIds="90,80,58"};var batches=Task.WhenAll(Feed.Jobs(probe)).GetAwaiter().GetResult();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"catalog-check.txt"),"Coin catalog: "+coins.Count+"; XRP="+coins.Any(c=>c.Key=="coin:58")+Environment.NewLine+string.Join(Environment.NewLine,batches.Select(b=>b.Group+" "+b.Source+": "+b.Items.Count+" items "+b.Error)));if(batches.Any(b=>b.Error!=""))Environment.ExitCode=1;return;}
        if(args.Length>0&&args[0]=="--check") {Batch[] batches=Task.WhenAll(Feed.Jobs(settings)).GetAwaiter().GetResult();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data-check.json"),new JavaScriptSerializer().Serialize(batches),Encoding.UTF8);Environment.ExitCode=batches.Any(b=>b.Error!="")?1:0;return;}
        if(args.Length>0&&args[0]=="--desktop-check"){try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"desktop-check.txt"),new DesktopIcons().Inspect());}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"desktop-check.txt"),ex.ToString());Environment.ExitCode=1;}return;}
        if(args.Length>0&&args[0]=="--interaction-test"){using(var test=new Bar(settings,false))test.VerifyInteractions(AppDomain.CurrentDomain.BaseDirectory);return;} if(args.Length>1&&args[0]=="--detail-test"){using(var test=new Bar(settings,false))test.VerifyDetailFixtures(args[1],AppDomain.CurrentDomain.BaseDirectory);return;} if(args.Length>0&&args[0]=="--sports-layout-test"){using(var test=new Bar(settings,false))test.VerifySportsLayout(AppDomain.CurrentDomain.BaseDirectory);return;} if(args.Length>0&&args[0]=="--self-test"){SelfTest();return;}
        if(args.Length>0&&args[0]=="--dock-test"){using(var testBar=new Bar(settings)){testBar.StartDockVerification(AppDomain.CurrentDomain.BaseDirectory);Application.Run(testBar);}return;}
        if(args.Length>0&&args[0]=="--selection-test"){using(var test=new Bar(settings,false)){test.VerifySelections(AppDomain.CurrentDomain.BaseDirectory);}return;}
        if(args.Length>0&&args[0]=="--preview"){using(var preview=new Bar(settings,false)){preview.CreateControl();preview.VerifyAndCapture(AppDomain.CurrentDomain.BaseDirectory);}return;}
        using(var mutex=new System.Threading.Mutex(false,"Local\\CornerBar_Personal_01")){if(!mutex.WaitOne(0,false)){MessageBox.Show("ROW 或旧版 CornerBar 已运行。请从右下角菜单退出旧版，再打开 ROW。");return;}
            try {bool snapshot=args.Length>0&&args[0]=="--snapshot";using(var bar=new Bar(settings,!snapshot)) {if(snapshot){bar.CreateControl();Task.Run(()=>bar.RefreshFeeds()).GetAwaiter().GetResult();bar.VerifyAndCapture(AppDomain.CurrentDomain.BaseDirectory);}else{try{File.WriteAllText(file,new JavaScriptSerializer().Serialize(settings),Encoding.UTF8);}catch{}if(args.Length>0&&args[0]=="--settings")bar.Shown+=(o,e)=>bar.BeginInvoke((Action)(()=>bar.OpenSettings()));Application.Run(bar);}}}
            finally{mutex.ReleaseMutex();}
        }
    }
    static void SelfTest(){ DetailData.Verify(); Bar.VerifyAwareness();
        var tests=new List<string>();
        if(MarketCatalog.Normalize("600519")!="sh600519"||MarketCatalog.Normalize("AAPL")!="usAAPL"||MarketCatalog.Normalize("sz000001")!="sz000001")throw new Exception("stock normalization");
        var parsed=Feed.ParseCoins("[{\"id\":\"58\",\"symbol\":\"XRP\",\"price_usd\":\"0.123456\",\"percent_change_24h\":\"2.4\"}]",new[]{"58"});if(parsed.Single().Key!="coin:58"||!parsed.Single().Value.Contains("123456"))throw new Exception("arbitrary crypto and small price");
        var point=DesktopIcons.FreePosition(new Point(8,8),new Size(80,100),new Rectangle(0,50,500,700),new[]{new Point(8,108)});if(!point.HasValue||point.Value.Y<50||Math.Abs(point.Value.Y-108)<96)throw new Exception("desktop collision planning");
        if(!MarketCatalog.Today(new Item{Day="2026-10-23"},new DateTime(2026,10,23)))throw new Exception("today filter");tests.Add("stock code normalization; arbitrary crypto IDs / sub-dollar precision; desktop collision avoidance; Beijing date filter");
        string[] f=Enumerable.Repeat("0",34).ToArray();f[1]="测试";f[3]="100";f[30]="20261023120000";f[32]="-2.5";
        var a=Feed.ParseStocks("v_sh600519=\""+string.Join("~",f)+"\";",new[]{"sh600519"},"A股");if(a.Count!=1||a[0].Change!=-2.5||a[0].SourceTime!=f[30])throw new Exception("stock parser");tests.Add("stock GBK-decoded payload, negative percent, source timestamp");
        f[2]=".INX";var index=Feed.ParseStocks("v_us.INX=\""+string.Join("~",f)+"\";",new[]{"us.INX"},"美股").Single();if(index.Value.StartsWith("$")||!index.Detail.Contains("指数点数"))throw new Exception("index must use points");tests.Add("US major indices parsed as points, not dollar amounts");
        bool rejected=false;try{Feed.ParseStocks("v_sh600519=\"~\";",new[]{"sh600519"},"A股");}catch{rejected=true;}if(!rejected)throw new Exception("invalid stocks accepted");tests.Add("reject malformed / missing stock quotes");
        string crypto="[{\"symbol\":\"BTC\",\"price_usd\":\"100\",\"percent_change_24h\":\"-1.2\"},{\"symbol\":\"ETH\",\"price_usd\":\"50\",\"percent_change_24h\":\"2\"}]";
        if(Feed.ParseCrypto(crypto).Count!=2)throw new Exception("crypto parser");rejected=false;try{Feed.ParseCrypto("[]");}catch{rejected=true;}if(!rejected)throw new Exception("missing crypto accepted");tests.Add("crypto completeness and 24h change");
        if(Feed.ParseSports("{\"events\":[]}","足球","英超").Count!=0)throw new Exception("empty schedule");rejected=false;try{Feed.ParseSports("{}","NBA","NBA");}catch{rejected=true;}if(!rejected)throw new Exception("invalid sports accepted");tests.Add("empty schedule distinct from invalid response");
        string match="{\"events\":[{\"date\":\"2026-10-23T12:00Z\",\"status\":{\"type\":{\"state\":\"pre\"}},\"competitions\":[{\"competitors\":[{\"homeAway\":\"away\",\"score\":\"0\",\"team\":{\"displayName\":\"Chelsea\"}},{\"homeAway\":\"home\",\"score\":\"0\",\"team\":{\"displayName\":\"Arsenal\"}}]}]}]}";
        var game=Feed.ParseSports(match,"足球","英超").Single();if(game.Value!="VS"||game.Name!="阿森纳 · 切尔西")throw new Exception("home/away or pregame");tests.Add("home/away order, Chinese teams, upcoming games show VS");
        if(game.Home!="阿森纳"||game.Away!="切尔西"||game.Status!="10-23 20:00")throw new Exception("single-line sports metadata");
        using(var icon=Brand.LoadIcon()){if(icon.Width<16)throw new Exception("embedded icon");}
        tests.Add("single-line sports metadata and embedded ROW icon");
        var old=new Batch{Group="美股",Source="腾讯行情",Fetched=Feed.Now.AddMinutes(-4),Items=a};
        var failed=new Batch{Group="美股",Source="腾讯行情",Error="offline"};Feed.RetainLastKnown(failed,new[]{old});
        if(failed.Error!="offline"||failed.Fetched!=old.Fetched||failed.Items!=old.Items)throw new Exception("stale fallback");
        var missing=new Batch{Group="NBA",Source="ESPN",Error="offline"};Feed.RetainLastKnown(missing,new[]{old});if(missing.Items.Count!=0)throw new Exception("cross-source fallback");
        var empty=new Batch{Group="美股",Source="腾讯行情",Fetched=Feed.Now};Feed.RetainLastKnown(empty,new[]{old});if(empty.Items.Count!=0)throw new Exception("valid empty should clear old");
        var partial=new Batch{Group="美股",Source="腾讯行情",Error="partial",Items=new List<Item>{game}};Feed.RetainLastKnown(partial,new[]{old});if(partial.Items.Single()!=game)throw new Exception("partial fresh overwritten");
        tests.Add("network failure retains old timestamp and error; no cross-source fallback; valid empty clears old; partial fresh preserved");
        File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),tests.Concat(new[]{"PASS"}),Encoding.UTF8);
    }
}
}








