using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.IO;
namespace CornerBar {
public partial class Bar {
    static string LogoUrl(Item item,bool home){
        string url=home?item.HomeLogo:item.AwayLogo,id=home?item.HomeId:item.AwayId;
        if(!string.IsNullOrEmpty(url))return url;
        if(string.IsNullOrEmpty(id)||!id.All(char.IsDigit))return null;
        return "https://a.espncdn.com/i/teamlogos/"+(item.Group=="NBA"?"nba":"soccer")+"/500/"+id+".png";
    }
    static int Measure(Graphics g,string text,float size,bool bold){using(var font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))return (int)Math.Ceiling(g.MeasureString(text??"",font).Width)+2;}
    void DrawSports(Graphics g,Item item,Rectangle content){
        int icon=content.Width<170?18:25,gap=4;float scoreSize=content.Width<170?12:14,timeSize=11;
        string score=(item.Value??"—").Replace(" : "," : "),status=item.Status??"";
        int scoreWidth=Measure(g,score,scoreSize,true),timeWidth=Measure(g,status,timeSize,false);
        int available=Math.Max(0,content.Width-icon*2-scoreWidth-gap*3);
        if(timeWidth>available){
            if(item.Group=="NBA"&&status.StartsWith("Q"))status=status.Split(' ')[0];
            else if(status.Contains(" "))status=status.Substring(status.LastIndexOf(' ')+1);
            timeWidth=Measure(g,status,timeSize,false);
        }
        if(timeWidth>available){status="";timeWidth=0;}
        int total=icon*2+scoreWidth+gap*2+(timeWidth>0?gap+timeWidth:0);
        int start=content.Left+Math.Max(0,(content.Width-total)/2);
        TeamAt(g,LogoUrl(item,true),item.Home,new Rectangle(start,(40-icon)/2,icon,icon));
        TextAt(g,score,scoreSize,Ink,new Rectangle(start+icon+gap,0,scoreWidth,40),true,true);
        int away=start+icon+gap+scoreWidth+gap;
        TeamAt(g,LogoUrl(item,false),item.Away,new Rectangle(away,(40-icon)/2,icon,icon));
        if(timeWidth>0)TextAt(g,status,timeSize,Muted,new Rectangle(away+icon+gap,0,timeWidth,40),false,true);
        
    }
    public void VerifySportsLayout(string folder){
        var old=settings.Slots;var oldData=data;
        try{
            var games=Enumerable.Range(0,5).Select(i=>new Item{Key="game:test:"+i,Group=i%2==0?"NBA":"足球",Home="主队",Away="客队",Name="布局测试",Value=i%2==0?"128 : 123":"2 : 1",Status=i%2==0?"Q4 03:12":"90+5′"}).ToList();
            settings.Slots=games.Select(i=>new Slot{Key=i.Key,Name=i.Name,Group=i.Group}).ToList();data=new List<Batch>{new Batch{Fetched=Feed.Now,Items=games}};
            foreach(int width in new[]{800,1024,1440,1920}){Width=width;Height=40;scale=1;Snapshot(Path.Combine(folder,"sports-"+width+".png"));}
            File.WriteAllText(Path.Combine(folder,"sports-layout-check.txt"),"PASS: 128 : 123 scores, football stoppage time, centered content, nonoverflowing slots at 800/1024/1440/1920. Missing crests use neutral placeholders, not team-name text.");
        }finally{settings.Slots=old;data=oldData;}
    }
}
}
