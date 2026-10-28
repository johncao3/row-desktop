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
        if(index>=settings.Slots.Count)return Tuple.Create(new Item{Key="empty:",Name="＋",Value="添加关注",Group="",Detail="点击打开我的关注"},false); Slot slot=settings.Slots[index];if(slot.Key.StartsWith("auto:"))return Pick(slot.Group);
        if(slot.Key.StartsWith("team:")){
            Item match=FollowedMatch(slot,data.SelectMany(b=>b.Items));
            if(match!=null){var batch=data.First(b=>b.Items.Contains(match));return Tuple.Create(match,batch.Error!=""||(Feed.Now-batch.Fetched).TotalMinutes>3);}
            bool failed=data.Where(b=>b.Group==slot.Group).Any(b=>b.Error!="");
            return Tuple.Create(new Item{Key=slot.Key,Group=slot.Group,Name=slot.Name,Value=failed?"赛程离线":"暂无近场",Source="ESPN",Detail="跟随球队：自动选择进行中、今天完场或下一场。当前仅检查已启用联赛的近三日赛程。"},failed);
        }
        foreach(var batch in data){var item=batch.Items.FirstOrDefault(x=>string.Equals(x.Key,slot.Key,StringComparison.OrdinalIgnoreCase));if(item!=null)return Tuple.Create(item,batch.Error!=""||(Feed.Now-batch.Fetched).TotalMinutes>3);}
        return Tuple.Create(new Item{Key=slot.Key,Group=slot.Group,Name=slot.Name,Value=slot.Key.StartsWith("game:")?"赛程已过期":"等待数据",Detail=slot.Key.StartsWith("game:")?"此场比赛已不在近三日赛程内，可在自选中换一场。":"检查连接或在自选中重新查询。",Source=slot.Group=="加密"?"CoinLore":slot.Group=="A股"||slot.Group=="美股"?"腾讯行情":"ESPN"},true);
    }

}
}
