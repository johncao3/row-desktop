using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CornerBar {
public partial class Bar : Form {
    static readonly string Base=AppDomain.CurrentDomain.BaseDirectory;
    readonly Settings settings; readonly DesktopIcons desktopIcons=new DesktopIcons();
    List<Batch> data=new List<Batch>();
    readonly List<Tuple<Rectangle,Action>> hits=new List<Tuple<Rectangle,Action>>();
    readonly Timer refreshTimer=new Timer(),rotateTimer=new Timer(),layoutTimer=new Timer();
    NotifyIcon tray;
    bool refreshing,registered,positioning,ready,closing,queued,fullscreen,dockTest;
    readonly bool interactive;
    uint callback,taskbarRestart;
    float scale=1;
    int page;
    Point mouse=new Point(-1,-1);
    DateTime lastAttempt=DateTime.MinValue;
    string dockError="";
    readonly string[] categories={"全部","A股","美股","加密","NBA","足球"};
    public static bool IsLight(string mode,int hour){return mode=="light"||(mode!="dark"&&hour>=7&&hour<19);}
    bool Light{get{return IsLight(settings.Theme,Feed.Now.Hour);}}
    Color Bg{get{return Light?Color.White:Color.FromArgb(26,28,32);}}
    Color Card{get{return Light?Color.White:Color.FromArgb(39,40,46);}}
    Color Ink{get{return Light?Color.FromArgb(29,29,31):Color.FromArgb(245,245,247);}}
    Color Muted{get{return Light?Color.FromArgb(111,113,121):Color.FromArgb(158,160,170);}}
    Color Line{get{return Light?Color.FromArgb(227,228,233):Color.FromArgb(58,59,66);}}
    Color Accent{get{return Light?Color.FromArgb(0,113,227):Color.FromArgb(100,170,255);}}
    Color Up{get{return Light?Color.FromArgb(205,62,66):Color.FromArgb(255,130,137);}}
    Color Down{get{return Light?Color.FromArgb(0,130,99):Color.FromArgb(92,211,164);}}
    Color Warning{get{return Light?Color.FromArgb(166,104,0):Color.FromArgb(255,195,95);}}
    int LogicalWidth{get{return (int)Math.Round(Width/scale);}}
    int LogicalHeight{get{return 40;}}
    Screen SelectedScreen{get{return Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==settings.Monitor)??Screen.PrimaryScreen;}}
    protected override bool ShowWithoutActivation{get{return true;}}
    public Bar(Settings s):this(s,true){}
    public Bar(Settings s,bool live){
        settings=s;interactive=live;Text="ROW 1.06";Icon=Brand.LoadIcon();Font=new Font("Microsoft YaHei UI",9);AutoScaleMode=AutoScaleMode.None;
        BackColor=Bg;ForeColor=Ink;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;DoubleBuffered=true;StartPosition=FormStartPosition.Manual;
        callback=DesktopDock.RegisterWindowMessage("ROW.Appbar.101");taskbarRestart=DesktopDock.RegisterWindowMessage("TaskbarCreated");
        var menu=new ContextMenuStrip();menu.Items.Add("显示 / 隐藏  Ctrl+Alt+B",null,(o,e)=>Toggle());menu.Items.Add("我的关注",null,(o,e)=>Configure());
        menu.Items.Add("暂停 / 开启轮播",null,(o,e)=>{settings.Rotate=!settings.Rotate;Save();Invalidate();});menu.Items.Add("退出",null,(o,e)=>Close());ContextMenuStrip=menu;menu.Font=new Font("Microsoft YaHei UI",9);menu.ShowImageMargin=false;menu.Opening+=(o,e)=>{menu.Renderer=new MenuRenderer(new Palette(Light));};
        tray=new NotifyIcon{Icon=Icon,Text="ROW 1.06 · Ctrl+Alt+B 显示 / 隐藏",Visible=live,ContextMenuStrip=menu};tray.MouseClick+=(o,e)=>{if(e.Button==MouseButtons.Left)ShowMenu(true);};
        refreshTimer.Interval=60000;refreshTimer.Tick+=async(o,e)=>{if(!closing)await RefreshFeeds();};
        rotateTimer.Interval=12000;rotateTimer.Tick+=(o,e)=>{if(settings.Rotate&&!Bounds.Contains(Cursor.Position)){page++;Invalidate();}};
        layoutTimer.Interval=2000;layoutTimer.Tick+=(o,e)=>{if(closing)return;BackColor=Bg;Invalidate();if(registered&&Visible){DesktopDock.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x13);desktopIcons.Avoid(Bounds,DesktopDock.WorkArea(SelectedScreen.Bounds));DesktopDock.KeepWindowsBelow(Handle,Bounds,SelectedScreen.Bounds,IntPtr.Zero);}};
        MouseMove+=(o,e)=>{mouse=new Point((int)(e.X/scale),(int)(e.Y/scale));Cursor=hits.Any(h=>h.Item1.Contains(mouse))?Cursors.Hand:Cursors.Default;UpdateTip();Invalidate();};MouseLeave+=(o,e)=>{mouse=new Point(-1,-1);tip.Hide(this);Invalidate();};
        MouseDown+=(o,e)=>{if(e.Button!=MouseButtons.Left)return;Point p=new Point((int)(e.X/scale),(int)(e.Y/scale));foreach(var h in hits.ToArray())if(h.Item1.Contains(p)){h.Item2();return;}};
        MouseWheel+=(o,e)=>{page+=e.Delta<0?1:-1;Invalidate();};KeyPreview=true;KeyDown+=(o,e)=>{if(e.KeyCode==Keys.Escape)Hide();if(e.KeyCode==Keys.Right){page++;Invalidate();}if(e.KeyCode==Keys.Left){page--;Invalidate();}};
        Shown+=async(o,e)=>{if(!interactive||dockTest)return;ready=true;desktopIcons.Restore();DesktopDock.RegisterHotKey(Handle,1,0x4003,(uint)Keys.B);Place();refreshTimer.Start();rotateTimer.Start();layoutTimer.Start();await RefreshFeeds();};
        Width=SelectedScreen.Bounds.Width;Height=LogicalHeight;Location=SelectedScreen.Bounds.Location;Opacity=1;
    }
    bool Save(){
        string temp=settingsPath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(settings),Encoding.UTF8);if(File.Exists(settingsPath))File.Replace(temp,settingsPath,null);else File.Move(temp,settingsPath);return true;}
        catch(Exception e){ReportError(e);if(interactive)MessageBox.Show(this,"设置未保存："+e.Message);return false;}
        finally{try{if(File.Exists(temp))File.Delete(temp);}catch{}}
    }
    void ReleaseDock(){if(registered&&IsHandleCreated){registered=false;DesktopDock.Remove(Handle);}if(interactive)desktopIcons.Restore();}
    void QueuePlace(){if(queued||closing||!IsHandleCreated)return;queued=true;BeginInvoke((Action)(()=>{queued=false;if(Visible&&ready)Place();}));}
    void Place(){
        if(positioning||closing)return;positioning=true;
        try{
            Screen screen=SelectedScreen;scale=IsHandleCreated?DesktopDock.Scale(Handle):1;
            int h=(int)Math.Round(LogicalHeight*scale);
            if(interactive&&Visible&&ready){
                if(!registered)registered=DesktopDock.Register(Handle,callback);
                if(!registered){dockError="未能预留工作区，请重启工具";Bounds=new Rectangle(screen.Bounds.Left,screen.Bounds.Top,screen.Bounds.Width,h);}
                else{dockError="";Bounds=DesktopDock.Position(Handle,screen.Bounds,h);}
            }else Bounds=new Rectangle(screen.Bounds.Left,screen.Bounds.Top,screen.Bounds.Width,h);
            TopMost=true;if(registered){DesktopDock.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x13);desktopIcons.Avoid(Bounds,DesktopDock.WorkArea(screen.Bounds));}BackColor=Bg;Invalidate();
        }finally{positioning=false;}
    }
    void Toggle(){if(Visible)Hide();else{Show();if(ready)Place();}}
    void Compact(){settings.Compact=!settings.Compact;Place();Save();}
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(!interactive||!ready||closing)return;if(Visible)Place();else ReleaseDock();}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x0312){Toggle();return;}
        if(callback!=0&&m.Msg==callback){
            if(m.WParam.ToInt32()==1&&!positioning)QueuePlace();
            if(m.WParam.ToInt32()==2){fullscreen=m.LParam!=IntPtr.Zero;TopMost=true;DesktopDock.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x13);}
            return;
        }
        if(taskbarRestart!=0&&m.Msg==taskbarRestart){registered=false;QueuePlace();}
        if(m.Msg==0x007E||m.Msg==0x02E0)QueuePlace();
        if(registered&&m.Msg==0x0006)DesktopDock.Notify(Handle,6,new IntPtr((m.WParam.ToInt64()&0xffff)==0?0:1));
        base.WndProc(ref m);
        if(registered&&m.Msg==0x0047&&!positioning)DesktopDock.Notify(Handle,9,IntPtr.Zero);
    }
    protected override void OnFormClosing(FormClosingEventArgs e){closing=true;ReleaseDock();base.OnFormClosing(e);}
    protected override void OnHandleDestroyed(EventArgs e){ReleaseDock();base.OnHandleDestroyed(e);}
    protected override void Dispose(bool disposing){if(disposing){closing=true;ReleaseDock();refreshTimer.Dispose();rotateTimer.Dispose();layoutTimer.Dispose();tip.Dispose();foreach(var img in logos.Values)img.Dispose();logos.Clear();if(panel!=null)panel.Close();if(IsHandleCreated)DesktopDock.UnregisterHotKey(Handle,1);if(tray!=null){tray.Visible=false;tray.Dispose();tray=null;}}base.Dispose(disposing);}
    public async Task RefreshFeeds(){
        if(refreshing||closing)return;refreshing=true;lastAttempt=Feed.Now;Invalidate();
        try{
            var pending=Feed.Jobs(settings).ToList();var sources=new HashSet<string>();
            while(pending.Count>0){var done=await Task.WhenAny(pending);pending.Remove(done);Batch b=await done;if(closing)return;sources.Add(b.Group+"|"+b.Source);ApplyBatch(b);Invalidate();}
            data.RemoveAll(b=>!sources.Contains(b.Group+"|"+b.Source));await LoadLogos();
        }
        catch(Exception ex){ReportError(ex);}finally{refreshing=false;if(!closing&&!IsDisposed)Invalidate();}
    }
    public async Task VerifyDock(string folder){
        string iconsBefore=desktopIcons.Inspect();var report=new List<string>();Rectangle screen=SelectedScreen.Bounds,before=DesktopDock.WorkArea(screen);
        using(var probe=new Form{Text="CornerBar 布局验证",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(screen.Left+120,screen.Top+3,680,420),ShowInTaskbar=false}){
            try{
                ready=true;Show();Place();await Task.Delay(800);
                if(desktopIcons.Status!=""||desktopIcons.CountCovered(Bounds)!=0)throw new Exception("Desktop icon avoidance: "+desktopIcons.Status);report.Add("PASS: no desktop icon anchor covered by the bar");Rectangle reserved=DesktopDock.WorkArea(screen);if(!registered||reserved.Top<Bounds.Bottom||Width!=screen.Width)throw new Exception("AppBar 未正确预留全宽工作区: registered="+registered+", screen="+screen+", bar="+Bounds+", work="+reserved);report.Add("PASS: full-width AppBar reserves top work area "+before+" -> "+reserved);
                probe.Show();probe.WindowState=FormWindowState.Maximized;await Task.Delay(400);if(probe.RectangleToScreen(probe.ClientRectangle).Top<Bounds.Bottom)throw new Exception("最大化窗口覆盖信息栏");report.Add("PASS: maximized window client area below bar");
                probe.WindowState=FormWindowState.Normal;probe.Bounds=new Rectangle(screen.Left+120,screen.Top+3,680,420);DesktopDock.KeepWindowsBelow(Handle,Bounds,screen,probe.Handle);await Task.Delay(400);if(probe.Top<Bounds.Bottom)throw new Exception("普通窗口未下移");report.Add("PASS: overlapping normal window moved below bar");
                settings.Compact=true;Place();await Task.Delay(500);if(DesktopDock.WorkArea(screen).Top!=Bounds.Bottom)throw new Exception("紧凑模式工作区没有更新");report.Add("PASS: fixed 40-DIP height remains reserved");
                Hide();await Task.Delay(500);if(DesktopDock.WorkArea(screen)!=before)throw new Exception("隐藏后工作区未恢复");report.Add("PASS: hide restores work area");
                Show();Place();await Task.Delay(500);if(DesktopDock.WorkArea(screen).Top<Bounds.Bottom)throw new Exception("再次显示未预留空间");report.Add("PASS: show reserves area again");
            }finally{probe.Close();Hide();ReleaseDock();}
        }
        await Task.Delay(300);
        if(DesktopDock.WorkArea(screen)!=before)throw new Exception("退出清理未恢复工作区");report.Add("PASS: cleanup restores original work area");if(desktopIcons.Status!="")throw new Exception(desktopIcons.Status);if(desktopIcons.Inspect()!=iconsBefore)throw new Exception("Desktop icon positions were not restored");report.Add("PASS: desktop icon layout restored exactly after hide / show / exit");File.WriteAllLines(Path.Combine(folder,"dock-check.txt"),report,Encoding.UTF8);
    }
    public void StartDockVerification(string folder){dockTest=true;Shown+=async(o,e)=>{try{await VerifyDock(folder);}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"dock-check.txt"),"FAIL: "+ex,Encoding.UTF8);Environment.ExitCode=1;}finally{Close();}};}
}
}




