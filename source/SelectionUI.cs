using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace CornerBar {
internal static class MarketCatalog {
    internal static List<Slot> Seeds(){return new[]{
        "sh000001|上证指数","sz399001|深证成指","sz399006|创业板指","sh000300|沪深300","sh000905|中证500","sh600519|贵州茅台","sz300750|宁德时代","sz002594|比亚迪","sh601318|中国平安","sh600036|招商银行","sh601398|工商银行","sh600900|长江电力","sh601899|紫金矿业","sz000333|美的集团","sz000858|五粮液","sh688981|中芯国际","sz002415|海康威视","sh600030|中信证券",
        "us.INX|标普500","us.IXIC|纳斯达克","us.DJI|道琼斯","usAAPL|苹果","usMSFT|微软","usNVDA|英伟达","usTSLA|特斯拉","usAMZN|亚马逊","usGOOGL|谷歌","usMETA|Meta","usAMD|AMD","usINTC|英特尔","usBABA|阿里巴巴","usPDD|拼多多","usCOIN|Coinbase","usSPY|标普500 ETF","usQQQ|纳指100 ETF"
    }.Select(s=>s.Split('|')).Select(a=>new Slot{Key="stock:"+a[0].ToLowerInvariant(),Name=a[1],Group=a[0].StartsWith("us")?"美股":"A股"}).Concat(new[]{new Slot{Key="coin:90",Name="BTC · Bitcoin",Group="加密"},new Slot{Key="coin:80",Name="ETH · Ethereum",Group="加密"}}).ToList();}
    internal static string Normalize(string input){string s=input.Trim();if(Regex.IsMatch(s,@"^\d{6}$"))s=(s[0]=='6'?"sh":s[0]=='4'||s[0]=='8'||s[0]=='9'?"bj":"sz")+s;else if(Regex.IsMatch(s,@"^[A-Za-z][A-Za-z0-9.\-]{0,14}$")&&!s.StartsWith("us",StringComparison.OrdinalIgnoreCase)&&!Regex.IsMatch(s,@"^(sh|sz|bj)\d{6}$",RegexOptions.IgnoreCase))s="us"+s;
        if(!Regex.IsMatch(s,@"^(?:(?:sh|sz|bj)\d{6}|us\.?[A-Za-z][A-Za-z0-9.\-]{0,14})$",RegexOptions.IgnoreCase))throw new Exception("请输入股票代码，例如 600519、sz000001、AAPL；上证指数用 sh000001。");return s.Substring(0,2).ToLowerInvariant()+s.Substring(2).ToUpperInvariant();}
    internal static bool Today(Item item,DateTime date){return item.Day==date.ToString("yyyy-MM-dd");}
    internal static async Task<List<Slot>> Coins(){using(var client=new HttpClient{Timeout=TimeSpan.FromSeconds(20)}){var root=Feed.Json(await client.GetStringAsync("https://api.coinlore.net/api/assets/"));var result=Feed.Arr(Feed.Get(root,"data")).Select(c=>new Slot{Key="coin:"+Feed.S(Feed.Get(c,"id")),Name=Feed.S(Feed.Get(c,"symbol"))+" · "+Feed.S(Feed.Get(c,"name")),Group="加密"}).Where(c=>Regex.IsMatch(c.Key,@"^coin:\d+$")).ToList();if(result.Count==0)throw new Exception("币种目录暂不可用，请稍后重试");return result;}}
}
public partial class Bar {
    public void VerifySelections(string folder){
        var previous=settings.Slots;var old=data;int oldPage=page;
        try{
            data=new List<Batch>();var slots=new List<Slot>();
            for(int i=0;i<5;i++){string key="stock:sh60000"+i;slots.Add(new Slot{Key=key,Name="测试股票"+i,Group="A股"});data.Add(new Batch{Group="A股",Source="fixture",Fetched=Feed.Now,Items=new List<Item>{new Item{Key=key,Group="A股",Name=slots[i].Name,Value=(100+i).ToString(),Change=i}}});}
            settings.Slots=slots;for(int i=0;i<5;i++){page=77;if(PickSlot(i).Item1.Key!=slots[i].Key)throw new Exception("fixed slots rotated or reordered");}
            var json=new System.Web.Script.Serialization.JavaScriptSerializer();var restored=json.Deserialize<Settings>(json.Serialize(settings));if(!restored.Slots.Select(s=>s.Key).SequenceEqual(slots.Select(s=>s.Key)))throw new Exception("slot persistence");
            slots[2]=new Slot{Key="coin:58",Group="加密",Name="XRP"};data.Add(new Batch{Group="加密",Fetched=Feed.Now,Items=new List<Item>{new Item{Key="coin:58",Group="加密",Name="XRP",Value="$0.12"}}});
            if(PickSlot(2).Item1.Key!="coin:58")throw new Exception("mixed category selection");slots[4]=new Slot{Key="game:NBA:expired",Group="NBA",Name="历史比赛"};if(PickSlot(4).Item1.Value!="赛程已过期")throw new Exception("expired match silently replaced");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"selection-check.txt"),"PASS: five stocks freely assigned; fixed slots remain pinned while paging; JSON order persisted; mixed crypto slot; expired match clearly labelled.");
        }finally{settings.Slots=previous;data=old;page=oldPage;}
    }
    Tuple<Item,bool> PickSlot(int index){
        Slot slot=settings.Slots[index];if(slot.Key.StartsWith("auto:"))return Pick(slot.Group);
        foreach(var batch in data){var item=batch.Items.FirstOrDefault(x=>string.Equals(x.Key,slot.Key,StringComparison.OrdinalIgnoreCase));if(item!=null)return Tuple.Create(item,batch.Error!=""||(Feed.Now-batch.Fetched).TotalMinutes>3);}
        return Tuple.Create(new Item{Key=slot.Key,Group=slot.Group,Name=slot.Name,Value=slot.Key.StartsWith("game:")?"赛程已过期":"等待数据",Detail=slot.Key.StartsWith("game:")?"此场比赛已不在近三日赛程内，可在自选中换一场。":"检查连接或在自选中重新查询。",Source=slot.Group=="加密"?"CoinLore":slot.Group=="A股"||slot.Group=="美股"?"腾讯行情":"ESPN"},true);
    }
    Surface MakeSettings(){
        var f=new Surface("设置",Colors,new Size(820,716));f.Heading("设置与自选","选五个关注对象，股票、币种与比赛自由搭配");
        var selected=settings.Slots.Select(s=>new Slot{Key=s.Key,Name=s.Name,Group=s.Group}).ToList();int active=0;
        var catalog=MarketCatalog.Seeds();Action rebuild=()=>{foreach(var item in data.SelectMany(b=>b.Items).Where(i=>!string.IsNullOrEmpty(i.Key))){var old=catalog.FirstOrDefault(c=>c.Key==item.Key);if(old!=null)catalog.Remove(old);catalog.Add(new Slot{Key=item.Key,Name=item.Name,Group=item.Group});}foreach(var slot in selected.Where(s=>!s.Key.StartsWith("auto:")&&!catalog.Any(c=>c.Key==s.Key)))catalog.Add(slot);};rebuild();
        var slots=new Button[5];Action repaint=()=>{for(int i=0;i<5;i++){slots[i].Text=(i+1)+"  "+selected[i].Name;slots[i].BackColor=i==active?Ink:Colors.Soft;slots[i].ForeColor=i==active?Bg:Ink;slots[i].Invalidate();}};
        for(int i=0;i<5;i++){int n=i;slots[i]=f.ButtonAt("",24+i*156,108,148,44,()=>{active=n;repaint();},false);}repaint();
        f.LabelAt("① 点选上方位置     ② 在列表选内容     ③ 点击“放入所选位置”",24,161,772,22,12,true);
        var group=new ComboBox{Bounds=new Rectangle(24,194,126,30),DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Colors.Soft,ForeColor=Ink};group.Items.AddRange(new object[]{"全部","A股","美股","加密","NBA","足球"});group.SelectedIndex=0;StyleCombo(group);f.Controls.Add(group);
        var search=new TextBox{Bounds=new Rectangle(164,194,280,30),BackColor=Colors.Soft,ForeColor=Ink,BorderStyle=BorderStyle.FixedSingle};f.Controls.Add(search);
        var day=new ComboBox{Bounds=new Rectangle(458,194,136,30),DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Colors.Soft,ForeColor=Ink};day.Items.AddRange(new object[]{"今日比赛","近三日比赛"});day.SelectedIndex=0;StyleCombo(day);f.Controls.Add(day);
        var query=f.ButtonAt("查询股票代码",608,192,188,32,()=>{},false);
        var list=new ListView{Bounds=new Rectangle(24,238,772,206),View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,BackColor=Bg,ForeColor=Ink,BorderStyle=BorderStyle.FixedSingle};list.Columns.Add("名称 / 比赛",242);list.Columns.Add("类别 / 代码",155);list.Columns.Add("最近报价 / 比分",150);list.Columns.Add("状态 / 时间",195);f.Controls.Add(list);
        var status=f.LabelAt("常用股票 + 已添加自选；更多股票输入代码查询。比赛按北京时间筛选。",24,452,772,22,11,true);
        Action render=()=>{if(f.IsDisposed)return;string q=search.Text.Trim();string category=group.SelectedItem.ToString();list.BeginUpdate();list.Items.Clear();foreach(var c in catalog.Where(c=>(category=="全部"||c.Group==category)&&(q==""||(c.Name+" "+c.Key).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0))){var item=data.SelectMany(b=>b.Items).FirstOrDefault(i=>i.Key==c.Key);if(c.Key.StartsWith("game:")&&day.SelectedIndex==0&&(item==null||!MarketCatalog.Today(item,Feed.Now)))continue;var row=new ListViewItem(c.Name){Tag=c};row.SubItems.Add(c.Group+" · "+c.Key.Substring(c.Key.IndexOf(':')+1));row.SubItems.Add(item==null?"选入后加载":item.Value);row.SubItems.Add(item==null?"":item.Home!=null?item.Day+" "+item.Status:item.Change.HasValue?item.Change.Value.ToString("+0.00;-0.00;0.00")+"%":"");list.Items.Add(row);}list.EndUpdate();};group.SelectedIndexChanged+=(o,e)=>render();search.TextChanged+=(o,e)=>render();day.SelectedIndexChanged+=(o,e)=>render();
        Action assign=()=>{if(list.SelectedItems.Count==0){status.Text="先从列表选择一项。";return;}var choice=(Slot)list.SelectedItems[0].Tag;selected[active]=new Slot{Key=choice.Key,Name=choice.Name,Group=choice.Group};repaint();status.Text="已放入位置 "+(active+1)+"："+choice.Name+"；点击保存后生效。";};
        f.ButtonAt("放入所选位置",24,486,178,36,assign,true);list.DoubleClick+=(o,e)=>assign();
        var coinButton=f.ButtonAt("加载全部币种",214,486,178,36,()=>{},false);coinButton.Click+=async(o,e)=>{coinButton.Enabled=false;status.Text="正在加载 CoinLore 币种目录…";try{var coins=await MarketCatalog.Coins();if(f.IsDisposed)return;catalog.RemoveAll(c=>c.Group=="加密");catalog.AddRange(coins);group.SelectedItem="加密";render();status.Text="已加载 "+coins.Count+" 个币种，输入名称或代码筛选；同名币以 ID 区分。";}catch(Exception ex){if(!f.IsDisposed)status.Text=ex.Message;}finally{if(!f.IsDisposed)coinButton.Enabled=true;}};
        var games=f.ButtonAt("刷新今日比赛",404,486,178,36,()=>{},false);games.Click+=async(o,e)=>{games.Enabled=false;status.Text="正在刷新 NBA 和已启用足球联赛…";try{if(refreshing){status.Text="主栏正在更新，完成后再点击刷新。";return;}await RefreshFeeds();if(f.IsDisposed)return;catalog.RemoveAll(c=>c.Key.StartsWith("game:"));rebuild();render();bool failed=data.Where(b=>b.Group=="NBA"||b.Group=="足球").Any(b=>b.Error!="");status.Text=failed?"部分赛程加载失败，已保留可用内容，请稍后重试。":"赛程已刷新；今日列表为空表示已启用赛事今天暂无比赛。";}finally{if(!f.IsDisposed)games.Enabled=true;}};
        f.ButtonAt("此位置分类轮播",594,486,202,36,()=>{string g=group.SelectedIndex>0?group.SelectedItem.ToString():selected[active].Group;selected[active]=new Slot{Key="auto:"+g,Name=g+"轮播",Group=g};repaint();},false);
        query.Click+=async(o,e)=>{query.Enabled=false;try{string code=MarketCatalog.Normalize(search.Text);string g=code.StartsWith("us")?"美股":"A股";status.Text="正在查询 "+code+"…";var batch=await Feed.Load(g,"https://qt.gtimg.cn/q="+code,t=>Feed.ParseStocks(t,new[]{code},g),"腾讯行情 · "+code,true);if(f.IsDisposed)return;if(batch.Error!="")throw new Exception("未取得该代码报价，请检查代码或网络。其他自选不受影响。");var item=batch.Items.Single();data.RemoveAll(b=>b.Source==batch.Source);data.Add(batch);rebuild();group.SelectedItem=g;search.Text=item.Name;render();status.Text="找到 "+item.Name+"（"+code+"），选中后放入上方位置。";}catch(Exception ex){if(!f.IsDisposed)status.Text=ex.Message;}finally{if(!f.IsDisposed)query.Enabled=true;}};
        f.LabelAt("足球联赛",24,537,110,22,12,true);var leagues=new TextBox{Bounds=new Rectangle(112,534,684,28),Text=settings.Leagues,BackColor=Colors.Soft,ForeColor=Ink,BorderStyle=BorderStyle.FixedSingle};f.Controls.Add(leagues);
        f.LabelAt("eng.1 英超 · esp.1 西甲 · uefa.champions 欧冠 · chn.1 中超；改动保存后生效",24,567,772,20,10,true);
        var theme=new ComboBox{Bounds=new Rectangle(24,598,228,30),DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Colors.Soft,ForeColor=Ink};theme.Items.AddRange(new object[]{"自动 · 白天浅色 / 夜间深色","浅色","深色"});theme.SelectedIndex=settings.Theme=="light"?1:settings.Theme=="dark"?2:0;StyleCombo(theme);f.Controls.Add(theme);
        Screen[] screens=Screen.AllScreens;var monitor=new ComboBox{Bounds=new Rectangle(264,598,218,30),DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Colors.Soft,ForeColor=Ink};foreach(var s in screens)monitor.Items.Add((s.Primary?"主屏幕":"屏幕")+" "+s.Bounds.Width+" × "+s.Bounds.Height);monitor.SelectedIndex=Math.Max(0,Array.FindIndex(screens,s=>s.DeviceName==SelectedScreen.DeviceName));StyleCombo(monitor);f.Controls.Add(monitor);
        var quiet=new CheckBox{Text="单色涨跌",Checked=settings.Quiet,Bounds=new Rectangle(494,598,130,30)};var rotate=new CheckBox{Text="分类自动轮播",Checked=settings.Rotate,Bounds=new Rectangle(636,598,160,30)};f.Controls.AddRange(new Control[]{quiet,rotate});
        f.LabelAt("股票：腾讯行情  ·  币种：CoinLore  ·  比赛：ESPN\n每 60 秒更新；非交易下单软件，行情可能延迟。",24,653,575,44,11,true);
        var save=f.ButtonAt("保存五个位置",618,653,178,40,()=>{},true);save.Click+=async(o,e)=>{var ls=Feed.Tokens(leagues.Text);if(ls.Length>8||ls.Any(l=>!Regex.IsMatch(l,@"^[a-z]+\.[a-z0-9]+$"))){status.Text="联赛格式有误；用逗号分隔，最多 8 个。";return;}if(refreshing){status.Text="正在更新，请稍后保存。";return;}settings.Slots=selected;settings.Stocks=string.Join(",",Feed.Tokens(settings.Stocks).Concat(selected.Where(s=>s.Key.StartsWith("stock:")).Select(s=>s.Key.Substring(6))).Distinct(StringComparer.OrdinalIgnoreCase));settings.CryptoIds=string.Join(",",selected.Where(s=>s.Key.StartsWith("coin:")).Select(s=>s.Key.Substring(5)).Concat(Feed.Tokens(settings.CryptoIds)).Distinct().Take(30));settings.Leagues=string.Join(",",ls);settings.Theme=theme.SelectedIndex==1?"light":theme.SelectedIndex==2?"dark":"auto";settings.Monitor=screens[monitor.SelectedIndex].DeviceName;settings.Quiet=quiet.Checked;settings.Rotate=rotate.Checked;settings.Version=102;Save();ReleaseDock();Place();f.Close();await RefreshFeeds();};f.Shown+=async(o,e)=>{if(!interactive)return;while(refreshing&&!f.IsDisposed)await Task.Delay(200);if(f.IsDisposed)return;rebuild();render();};render();return f;
    }
}
}


