using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace CornerBar {
public partial class Bar {
    readonly ToolTip tip=new ToolTip{InitialDelay=500,ReshowDelay=200,AutoPopDelay=15000};
    readonly Dictionary<string,Image> logos=new Dictionary<string,Image>();
    readonly Dictionary<string,DateTime> logoAttempts=new Dictionary<string,DateTime>();
    readonly List<Tuple<Rectangle,Item,bool>> shown=new List<Tuple<Rectangle,Item,bool>>();
    static readonly HttpClient logoClient=new HttpClient{Timeout=TimeSpan.FromSeconds(6)};
    Surface panel;
    string currentTip="";
    Palette Colors{get{return new Palette(Light);}}
    async Task LoadLogos(){
        var urls=Enumerable.Range(0,5).Select(i=>PickSlot(i).Item1).Concat(data.SelectMany(b=>b.Items)).SelectMany(i=>new[]{LogoUrl(i,true),LogoUrl(i,false)}).Where(u=>!string.IsNullOrEmpty(u)&&u.StartsWith("https://",StringComparison.OrdinalIgnoreCase)).Distinct().Where(u=>!logos.ContainsKey(u)&&(!logoAttempts.ContainsKey(u)||(DateTime.UtcNow-logoAttempts[u]).TotalMinutes>=1)).ToArray();
        using(var gate=new System.Threading.SemaphoreSlim(4)){await Task.WhenAll(urls.Select(async url=>{await gate.WaitAsync();try{
            if(closing)return;logoAttempts[url]=DateTime.UtcNow;byte[] bytes=null;try{bytes=await logoClient.GetByteArrayAsync(url);}catch{if(!url.Contains("/scoreboard/"))throw;}if(bytes==null)bytes=await logoClient.GetByteArrayAsync(url.Replace("/scoreboard/","/"));if(closing||bytes.Length>2097152)return;
            using(var ms=new MemoryStream(bytes))using(var src=Image.FromStream(ms)){var bmp=new Bitmap(src,64,64);if(closing)bmp.Dispose();else{logos[url]=bmp;Invalidate();}}
        }catch{}finally{gate.Release();}}));}
    }
    static int Index(int n,int count){return ((n%count)+count)%count;}
    Tuple<Item,bool> Pick(string group){
        var batches=data.Where(b=>b.Group==group).ToArray();var items=batches.SelectMany(b=>b.Items.Select(i=>Tuple.Create(i,b.Error!=""||(Feed.Now-b.Fetched).TotalMinutes>3))).OrderBy(x=>x.Item1.Rank).ToList();
        if(items.Count>0)return items[Index(page,items.Count)];
        bool offline=batches.Any(b=>b.Error!="");return Tuple.Create(new Item{Group=group,Name=group,Value=batches.Length==0?"连接中":offline?"暂时离线":group=="NBA"||group=="足球"?"暂无比赛":"未添加",Detail=offline?"连接失败，稍后自动重试":"点击查看详情或调整自选",Source=string.Join(" / ",batches.Select(b=>b.Source).Distinct()),SourceTime=""},offline);
    }
    void TextAt(Graphics g,string text,float size,Color color,RectangleF r,bool bold=false,bool center=false){
        if(r.Width<=0)return;using(var font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(color))using(var f=new StringFormat{Alignment=center?StringAlignment.Center:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(text??"",font,brush,r,f);
    }
    string ShortName(string name){if(name==null)return "";return name.Replace("标准普尔500指数","标普500").Replace("标普500指数","标普500").Replace("纳斯达克综合指数","纳斯达克").Replace("道琼斯指数","道琼斯").Replace("上证指数","上证");}
    void TeamAt(Graphics g,string url,string name,Rectangle r){Image img;if(url!=null&&logos.TryGetValue(url,out img))g.DrawImage(img,r);else{Brand.Fill(g,r,6,Colors.Soft);using(var pen=new Pen(Muted,1.3f)){g.DrawEllipse(pen,r.X+3,r.Y+3,r.Width-6,r.Height-6);g.DrawLine(pen,r.X+5,r.Y+r.Height/2,r.Right-5,r.Y+r.Height/2);g.DrawLine(pen,r.X+r.Width/2,r.Y+5,r.X+r.Width/2,r.Bottom-5);}}}
    void MarketIcon(Graphics g,Item item,Rectangle r){
        bool coin=item.Group=="加密",btc=coin&&(item.Name??"").StartsWith("BTC"),eth=coin&&(item.Name??"").StartsWith("ETH");
        Color accent=coin?Color.FromArgb(224,139,33):item.Group=="A股"?Color.FromArgb(193,68,66):Color.FromArgb(59,106,168);
        Brand.Fill(g,r,coin?11:6,Color.FromArgb(Light?22:45,accent));
        if(eth){float cx=r.X+11,cy=r.Y+11;using(var brush=new SolidBrush(Muted)){g.FillPolygon(brush,new[]{new PointF(cx,cy-8),new PointF(cx+5,cy),new PointF(cx,cy+3),new PointF(cx-5,cy)});g.FillPolygon(brush,new[]{new PointF(cx-5,cy+2),new PointF(cx,cy+9),new PointF(cx+5,cy+2),new PointF(cx,cy+5)});}}
        else TextAt(g,btc?"₿":coin?"C":item.Group=="A股"?"A":"US",btc?17:10,accent,r,true,true);
    }
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;g.Clear(Bg);g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;hits.Clear();shown.Clear();int w=LogicalWidth;
        Brand.Draw(g,new Rectangle(10,7,26,26),true);hits.Add(Tuple.Create(new Rectangle(4,0,38,40),(Action)(()=>ShowMenu(false))));
        int left=46,right=w-38;double slot=(right-left)/5.0;
        for(int i=0;i<5;i++){
            int x=left+(int)Math.Round(i*slot),end=left+(int)Math.Round((i+1)*slot);var r=new Rectangle(x,0,end-x,40);var picked=PickSlot(i);Item item=picked.Item1;bool stale=picked.Item2;
            if(r.Contains(mouse))Brand.Fill(g,new Rectangle(r.X+3,4,r.Width-6,32),7,Colors.Soft);
            using(var p=new Pen(Light?Color.FromArgb(176,183,193):Color.FromArgb(105,112,124),1.3f))g.DrawLine(p,x,8,x,32);
            var content=new Rectangle(r.X+12,0,Math.Max(1,r.Width-24),40);float fs=r.Width<210?11:13;
            if(item.Group=="A股"||item.Group=="美股"||item.Group=="加密"){MarketIcon(g,item,new Rectangle(content.X,9,22,22));content.X+=29;content.Width=Math.Max(1,content.Width-29);}
            if(item.Home!=null){
                DrawSports(g,item,content);
            }else if(item.Change.HasValue){
                int percentWidth=r.Width<210?51:66;int available=content.Width-percentWidth;int nameWidth=Math.Max(22,(int)(available*.42));
                TextAt(g,ShortName(item.Name),fs,Ink,new Rectangle(content.X,0,nameWidth,40));
                TextAt(g,item.Value,fs,Ink,new Rectangle(content.X+nameWidth,0,available-nameWidth,40));
                TextAt(g,item.Change.Value.ToString("+0.00;-0.00;0.00")+"%",fs-1,settings.Quiet?Muted:item.Change>=0?Up:Down,new Rectangle(content.Right-percentWidth,0,percentWidth,40),false,true);
            }else TextAt(g,item.Name+"  "+item.Value,fs,Muted,content,false,true);
            DateTime changed;if(item.Key!=null&&scoreChanges.TryGetValue(item.Key,out changed)&&(Feed.Now-changed).TotalSeconds<12)using(var p=new Pen(Brand.Orange,2))g.DrawLine(p,r.Left+12,37,r.Right-12,37); if(stale)using(var b=new SolidBrush(Warning))g.FillEllipse(b,r.Right-6,6,3,3);
            Item selected=item;bool old=stale;hits.Add(Tuple.Create(r,(Action)(()=>ShowDetail(selected,old))));shown.Add(Tuple.Create(r,item,stale));
        }
        var more=new Rectangle(w-34,4,30,32);if(more.Contains(mouse))Brand.Fill(g,more,7,Colors.Soft);TextAt(g,"···",18,Muted,more,true,true);hits.Add(Tuple.Create(more,(Action)(()=>ShowMenu(false))));
        using(var p=new Pen(Line))g.DrawLine(p,0,39,w,39);
    }
    void UpdateTip(){var item=shown.FirstOrDefault(x=>x.Item1.Contains(mouse));string text=item==null?"":(item.Item3?"旧数据 · ":"")+item.Item2.Name+"\n"+item.Item2.Value+(item.Item2.Change.HasValue?"  "+item.Item2.Change.Value.ToString("+0.00;-0.00;0.00")+"%":"")+"\n"+item.Item2.Detail+"\n"+item.Item2.Source+" · "+item.Item2.SourceTime+"\n"+ReceivedText(item.Item2)+"\n"+QuoteAge(item.Item2)+"\n点击展开详情";if(text!=currentTip){currentTip=text;tip.SetToolTip(this,text);}}
    Surface MakeMenu(){
        var f=new Surface("控制中心",Colors,new Size(328,435));f.Heading("ROW", "1.05  /  时刻在场");
        if(desktopIcons.Status!="")f.LabelAt(desktopIcons.Status,24,128,280,20,10,true);bool error=data.Any(b=>b.Error!="");f.LabelAt(dockError!=""?dockError:refreshing?"正在更新行情…":error?"部分数据离线 · 保留最近报价":lastAttempt==DateTime.MinValue?"等待连接":lastAttempt.ToString("HH:mm")+" 更新 · 每 60 秒刷新",24,109,280,24,12,true);
        f.ButtonAt("刷新行情",24,151,280,40,async()=>{f.Close();await RefreshFeeds();},false);
        f.ButtonAt(settings.Rotate?"暂停轮播":"开启轮播",24,199,136,40,()=>{settings.Rotate=!settings.Rotate;Save();f.Close();},false);
        f.ButtonAt("下一组  →",168,199,136,40,()=>{page++;Invalidate();f.Close();},false);
        f.ButtonAt("设置与自选",24,247,280,40,()=>{f.Close();Configure();},true);
        f.ButtonAt(Visible?"隐藏信息栏":"显示信息栏",24,295,136,40,()=>{f.Close();Toggle();},false);
        f.ButtonAt("退出 ROW",168,295,136,40,()=>{f.Close();Close();},false);
        f.LabelAt("Ctrl + Alt + B   显示 / 隐藏",24,360,280,22,12,true);f.LabelAt("滚轮切换 · 悬停查看 · 点击展开",24,386,280,22,12,true);return f;
    }
    void ShowMenu(bool fromTray){
        if(panel!=null&&!panel.IsDisposed){panel.Close();return;}panel=MakeMenu();panel.TopMost=true;panel.StartPosition=FormStartPosition.Manual;
        Point anchor=fromTray?Cursor.Position:new Point(Right,Bottom);var work=Screen.FromPoint(anchor).WorkingArea;
        panel.Location=new Point(Math.Max(work.Left,Math.Min(anchor.X-panel.Width,work.Right-panel.Width)),Math.Max(work.Top,Math.Min(fromTray?anchor.Y-panel.Height:Bottom+8,work.Bottom-panel.Height)));
        var openedMenu=panel;panel.Deactivate+=(o,e)=>{if(!openedMenu.IsDisposed)openedMenu.Close();};panel.Opacity=0;panel.Show();panel.Location=new Point(Math.Max(work.Left,Math.Min(anchor.X-panel.Width,work.Right-panel.Width)),Math.Max(work.Top,Math.Min(fromTray?anchor.Y-panel.Height:Bottom+8,work.Bottom-panel.Height)));panel.Opacity=1;panel.Activate();
    }
    void ShowDetail(Item x,bool stale){if(panel!=null&&!panel.IsDisposed)panel.Close();var f=MakeDetail(x,stale);panel=f;f.TopMost=true;f.StartPosition=FormStartPosition.Manual;var work=SelectedScreen.WorkingArea;f.Location=new Point(Math.Max(work.Left,Math.Min(Cursor.Position.X-f.Width/2,work.Right-f.Width)),Math.Max(work.Top,Math.Min(Bottom+8,work.Bottom-f.Height)));f.Deactivate+=(o,e)=>{if(!f.IsDisposed)f.Close();};f.Show();f.Activate();}
    void StyleCombo(ComboBox box){box.DrawMode=DrawMode.OwnerDrawFixed;box.ItemHeight=22;box.DrawItem+=(o,e)=>{if(e.Index<0)return;using(var brush=new SolidBrush(Colors.Soft))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,box.Items[e.Index].ToString(),box.Font,e.Bounds,Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};}
    public void OpenSettings(){Configure();}
    void Configure(){using(var f=MakeSettings()){f.TopMost=true;f.ShowDialog(this);}}
    public void Snapshot(string path){using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    static void CaptureForm(Form form,string path){using(form){form.Show();form.PerformLayout();form.ActiveControl=null;foreach(Control c in form.Controls){var box=c as TextBox;if(box!=null){box.SelectionStart=0;box.SelectionLength=0;}}Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}}
    public void VerifyAndCapture(string folder){
        bool fixture=data.Count==0;if(fixture){data=new List<Batch>{new Batch{Group="A股",Fetched=Feed.Now,Items=new List<Item>{new Item{Group="A股",Name="上证指数",Value="3,609.23",Change=-1.33,Source="示例",Detail="演示数据"}}},new Batch{Group="美股",Fetched=Feed.Now,Items=new List<Item>{new Item{Group="美股",Name="标普500",Value="7,663.43",Change=-1.21,Source="示例",Detail="演示数据"}}},new Batch{Group="加密",Fetched=Feed.Now,Items=new List<Item>{new Item{Group="加密",Name="BTC",Value="$81,880",Change=2.14,Source="示例",Detail="过去24小时"}}},new Batch{Group="NBA",Fetched=Feed.Now,Items=new List<Item>{new Item{Key="game:NBA:demo",Day=Feed.Now.ToString("yyyy-MM-dd"),League="NBA",HomeId="13",AwayId="9",Group="NBA",Name="湖人 · 勇士",Home="湖人",Away="勇士",Value="98 : 94",Status="Q4 03:12",Detail="NBA · 第四节",Source="示例"}}},new Batch{Group="足球",Fetched=Feed.Now,Items=new List<Item>{new Item{Key="game:足球:demo",Day=Feed.Now.ToString("yyyy-MM-dd"),League="英超",HomeId="359",AwayId="363",Group="足球",Name="阿森纳 · 切尔西",Home="阿森纳",Away="切尔西",Value="2 : 1",Status="73′",Detail="英超 · 进行中",Source="示例"}}}};}
        string oldTheme=settings.Theme;scale=1;Height=40;
        foreach(int width in new[]{800,1024,1440,1536,1920})foreach(string theme in new[]{"light","dark"}){Width=width;settings.Theme=theme;page=-100;Snapshot(Path.Combine(folder,"preview-"+width+"-"+theme+".png"));if(shown.Count!=5||shown.Max(x=>x.Item1.Width)-shown.Min(x=>x.Item1.Width)>1)throw new Exception("five equal groups");for(int i=0;i<hits.Count;i++)for(int j=i+1;j<hits.Count;j++)if(hits[i].Item1.IntersectsWith(hits[j].Item1))throw new Exception("overlapping targets");}
        settings.Theme="light";CaptureForm(MakeMenu(),Path.Combine(folder,"preview-menu.png"));CaptureForm(MakeSettings(),Path.Combine(folder,"preview-settings.png"));CaptureForm(MakeDetail(data.SelectMany(b=>b.Items).First(),false),Path.Combine(folder,"preview-detail.png"));var sportsSettings=MakeSettings();sportsSettings.Controls.OfType<ComboBox>().First().SelectedItem="NBA";CaptureForm(sportsSettings,Path.Combine(folder,"preview-sports-settings.png"));settings.Theme="dark";CaptureForm(MakeSettings(),Path.Combine(folder,"preview-settings-dark.png"));
        foreach(float dpi in new[]{1.25f,1.5f,2f}){scale=dpi;Width=1920;Height=(int)(40*dpi);Snapshot(Path.Combine(folder,"preview-dpi-"+(int)(dpi*100)+".png"));}
        settings.Theme=oldTheme;scale=1;Height=40;if(!IsLight("auto",7)||IsLight("auto",19))throw new Exception("theme boundaries");
        File.WriteAllText(Path.Combine(folder,"ui-check.txt"),"PASS: five equal columns, 40 DIP, light/dark, widths 800/1024/1440/1536/1920, nonoverlapping click targets, 125/150/200% DPI, menu/settings/detail rendering.\nPreview data: "+(fixture?"DEMO fixtures; not live quotes":"live feed responses"),Encoding.UTF8);
    }
}
}





