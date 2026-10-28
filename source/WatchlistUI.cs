using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace CornerBar {
public partial class Bar {
    string settingsPath=Path.Combine(Base,"settings.json");
    bool watchRefreshQueued;
    static Slot CopySlot(Slot s){return new Slot{Key=s.Key,Name=s.Name,Group=s.Group};}
    static bool SameSlot(Slot a,Slot b){return string.Equals(a.Key,b.Key,StringComparison.OrdinalIgnoreCase);}
    static string WatchName(Slot s){return s.Name+(s.Key.StartsWith("team:")?" · 跟随球队":"");}
    bool CommitWatchlist(List<Slot> proposed){
        if(proposed.Count>5||proposed.Any(s=>s==null||string.IsNullOrEmpty(s.Key))||proposed.Select(s=>s.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=proposed.Count)return false;
        var old=settings.Slots;string oldStocks=settings.Stocks,oldCoins=settings.CryptoIds;
        settings.Slots=proposed.Select(CopySlot).ToList();
        settings.Stocks=string.Join(",",Feed.Tokens(settings.Stocks??"").Concat(proposed.Where(s=>s.Key.StartsWith("stock:")).Select(s=>s.Key.Substring(6))).Distinct(StringComparer.OrdinalIgnoreCase));
        settings.CryptoIds=string.Join(",",proposed.Where(s=>s.Key.StartsWith("coin:")).Select(s=>s.Key.Substring(5)).Concat(Feed.Tokens(settings.CryptoIds??"")).Distinct().Take(30));
        if(!Save()){settings.Slots=old;settings.Stocks=oldStocks;settings.CryptoIds=oldCoins;return false;}
        Invalidate();if(interactive)RequestWatchRefresh();return true;
    }
    async void RequestWatchRefresh(){
        if(watchRefreshQueued||closing)return;watchRefreshQueued=true;
        try{await Task.Delay(350);while(refreshing&&!closing)await Task.Delay(200);if(!closing)await RefreshFeeds();}
        catch(Exception e){ReportError(e);}finally{watchRefreshQueued=false;}
    }
    bool ReorderWatch(string key,int target){
        var next=settings.Slots.Select(CopySlot).ToList();int from=next.FindIndex(s=>s.Key==key);
        if(from<0||target<0||target>=next.Count||target==from)return false;
        var item=next[from];next.RemoveAt(from);next.Insert(target,item);return CommitWatchlist(next);
    }
    Surface MakeSettings(){
        var f=new Surface("设置",Colors,new Size(680,720));f.Heading("我的关注","添加、移除、拖动排序，立即生效");
        f.ButtonAt("偏好设置",510,52,140,28,()=>{using(var p=MakePreferences()){p.TopMost=true;p.ShowDialog(f);}},false).Name="preferences";
        var count=f.LabelAt("",24,105,610,23,14,false);count.Name="watch-count";
        var favorites=new Panel{Bounds=new Rectangle(24,134,632,180),BackColor=Bg,AllowDrop=true,Name="favorites"};f.Controls.Add(favorites);
        f.LabelAt("搜索股票、币种、球队或比赛",24,324,450,20,12,true);
        var search=new TextBox{Name="search",Bounds=new Rectangle(24,349,486,29),BorderStyle=BorderStyle.FixedSingle,BackColor=Colors.Soft,ForeColor=Ink};f.Controls.Add(search);
        var query=f.ButtonAt("查询代码",522,347,134,32,()=>{},false);query.Name="query";
        string category="全部";var filters=new List<Button>();Action render=null;Action queueRender=null;bool scheduled=false;
        foreach(string c in new[]{"全部","行情","篮球","足球"}){string selected=c;var b=f.ButtonAt(c,24+filters.Count*88,391,80,30,()=>{category=selected;queueRender();},false);b.Name="filter-"+c;filters.Add(b);}
        var refresh=f.ButtonAt("刷新比赛",392,391,126,30,()=>{},false);
        var coins=f.ButtonAt("更多币种",530,391,126,30,()=>{},false);
        var results=new Panel{Name="results",Bounds=new Rectangle(24,433,632,222),AutoScroll=true,BackColor=Bg};f.Controls.Add(results);
        var status=f.LabelAt("最多关注 5 项；拖动 ≡ 调整顶部顺序。",24,666,632,35,11,true);status.Name="watch-status";
        var extras=new List<Slot>();var initial=settings.Slots.Select(CopySlot).ToList();
        Action<Slot> add=choice=>{
            if(settings.Slots.Any(s=>SameSlot(s,choice))){status.Text="已经关注："+WatchName(choice);return;}
            var next=settings.Slots.Select(CopySlot).ToList();
            if(next.Count<5)next.Add(CopySlot(choice));
            else{int replacement=ChooseOption(f,"已满 5 项，替换哪一项",next.Select(WatchName).ToArray());if(replacement<0||f.IsDisposed)return;next[replacement]=CopySlot(choice);}
            status.Text=CommitWatchlist(next)?"已关注 "+WatchName(choice)+" · 已保存":"保存失败，已保留原关注列表";queueRender();
        };
        Action<string> remove=key=>{var next=settings.Slots.Where(s=>s.Key!=key).Select(CopySlot).ToList();status.Text=CommitWatchlist(next)?"已移除 · 已保存":"保存失败，已保留原关注列表";queueRender();};
        Action<Control,int> makeDrop=(control,index)=>{
            control.AllowDrop=true;
            control.DragEnter+=(o,e)=>{if(e.Data.GetDataPresent("ROW.WatchKey"))e.Effect=DragDropEffects.Move;};
            control.DragDrop+=(o,e)=>{var key=e.Data.GetData("ROW.WatchKey") as string;if(key!=null&&ReorderWatch(key,index)){status.Text="顺序已保存";queueRender();}};
        };
        render=()=>{
            if(f.IsDisposed)return;
            count.Text="我的关注                                      "+settings.Slots.Count+" / 5";
            foreach(var b in filters){b.BackColor=b.Text==category?Ink:Colors.Soft;b.ForeColor=b.Text==category?Bg:Ink;}
            foreach(Control c in favorites.Controls.Cast<Control>().ToArray())c.Dispose();favorites.Controls.Clear();
            int rowWidth=favorites.ClientSize.Width;float dpi=DesktopDock.Scale(f.Handle);int rowH=(int)(34*dpi);
            if(settings.Slots.Count==0){favorites.Controls.Add(new Label{Text="还没有关注内容\r\n从下方搜索并点击 ＋，添加你的第一项。",ForeColor=Muted,Bounds=new Rectangle(12,18,rowWidth-24,80),Font=favorites.Font});}
            for(int i=0;i<settings.Slots.Count;i++){
                Slot slot=settings.Slots[i];int index=i;var row=new Panel{Name="watch-"+i,Tag=slot,BackColor=Colors.Soft,Bounds=new Rectangle(0,i*rowH,rowWidth-2,rowH-3)};
                var grip=new Label{Text="≡",TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.SizeAll,Bounds=new Rectangle(0,0,(int)(35*dpi),row.Height),ForeColor=Muted};
                var name=new Label{Text=WatchName(slot),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Bounds=new Rectangle((int)(38*dpi),0,rowWidth-(int)(145*dpi),row.Height)};
                var minus=new RowButton{Name="remove",Text="−",AccessibleName="移除 "+slot.Name,Bounds=new Rectangle(rowWidth-(int)(39*dpi),0,(int)(34*dpi),row.Height),BackColor=Colors.Soft,ForeColor=Ink,FlatStyle=FlatStyle.Flat};minus.FlatAppearance.BorderSize=0;minus.Click+=(o,e)=>remove(slot.Key);
                var up=new RowButton{Name="up",Text="↑",AccessibleName="上移 "+slot.Name,Enabled=i>0,Bounds=new Rectangle(rowWidth-(int)(76*dpi),0,(int)(32*dpi),row.Height),BackColor=Colors.Soft,ForeColor=Muted,FlatStyle=FlatStyle.Flat};up.FlatAppearance.BorderSize=0;up.Click+=(o,e)=>{if(ReorderWatch(slot.Key,index-1)){status.Text="顺序已保存";queueRender();}};
                Point start=Point.Empty;grip.MouseDown+=(o,e)=>{if(e.Button==MouseButtons.Left)start=e.Location;};
                grip.MouseMove+=(o,e)=>{if(e.Button==MouseButtons.Left&&(Math.Abs(e.X-start.X)+Math.Abs(e.Y-start.Y)>SystemInformation.DragSize.Width)){var drag=new DataObject();drag.SetData("ROW.WatchKey",slot.Key);grip.DoDragDrop(drag,DragDropEffects.Move);}};
                row.Controls.AddRange(new Control[]{grip,name,minus,up});makeDrop(row,index);makeDrop(name,index);makeDrop(grip,index);favorites.Controls.Add(row);
            }
            var catalog=MarketCatalog.Seeds().Concat(extras).Concat(initial).Concat(settings.Slots).ToList();
            foreach(var item in data.SelectMany(b=>b.Items).Where(i=>!string.IsNullOrEmpty(i.Key))){
                catalog.Add(new Slot{Key=item.Key,Name=item.Name,Group=item.Group});
                if(item.Home!=null&&!string.IsNullOrEmpty(item.HomeId)&&!string.IsNullOrEmpty(item.AwayId)){catalog.Add(TeamSlot(item,true));catalog.Add(TeamSlot(item,false));}
            }
            catalog.AddRange(new[]{"A股","美股","加密","NBA","足球"}.Select(g=>new Slot{Key="auto:"+g,Name=g+" · 自动轮播",Group=g}));
            string q=search.Text.Trim();
            var filtered=catalog.GroupBy(s=>s.Key,StringComparer.OrdinalIgnoreCase).Select(g=>g.Last()).Where(s=>(category=="全部"||(category=="行情"&&(s.Group=="A股"||s.Group=="美股"||s.Group=="加密"))||(category=="篮球"&&s.Group=="NBA")||(category=="足球"&&s.Group=="足球"))&&(q==""||(s.Name+" "+s.Key+" "+s.Group).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0)).OrderBy(s=>s.Key.StartsWith("team:")?0:s.Key.StartsWith("game:")?1:2).ToList();
            foreach(Control c in results.Controls.Cast<Control>().ToArray())c.Dispose();results.Controls.Clear();results.AutoScrollPosition=Point.Empty;
            int width=results.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-3;int h=(int)(49*dpi);
            int rowIndex=0;
            foreach(var choice in filtered.Take(60)){
                var candidate=choice;bool exists=settings.Slots.Any(s=>SameSlot(s,candidate));
                var row=new Panel{Tag=candidate,Bounds=new Rectangle(0,rowIndex++*h,width,h-3),BackColor=Colors.Soft};
                var live=data.SelectMany(b=>b.Items).FirstOrDefault(i=>i.Key==choice.Key);
                string caption=choice.Group+(choice.Key.StartsWith("team:")?" · 跟随球队 · "+choice.Key.Split(':')[2]:choice.Key.StartsWith("game:")?" · "+(live==null?"场次不在近三日赛程":live.Day+"  "+live.Value+"  "+live.Status):choice.Key.StartsWith("auto:")?" · 自动切换这一分类": " · "+choice.Key.Substring(choice.Key.IndexOf(':')+1));
                row.Controls.Add(new Label{Text=WatchName(choice),AutoEllipsis=true,Bounds=new Rectangle(12,2,width-(int)(75*dpi),(int)(24*dpi)),TextAlign=ContentAlignment.MiddleLeft,Font=results.Font});
                row.Controls.Add(new Label{Text=caption,AutoEllipsis=true,Bounds=new Rectangle(12,(int)(25*dpi),width-(int)(75*dpi),(int)(19*dpi)),ForeColor=Muted,Font=new Font(results.Font.FontFamily,Math.Max(8,results.Font.Size-1))});
                var plus=new RowButton{Name="add",Text=exists?"✓":"＋",AccessibleName="关注 "+WatchName(choice),Enabled=!exists,Bounds=new Rectangle(width-(int)(43*dpi),4,(int)(36*dpi),h-10),BackColor=Colors.Soft,ForeColor=exists?Muted:Ink,FlatStyle=FlatStyle.Flat};plus.FlatAppearance.BorderSize=0;plus.Click+=(o,e)=>add(candidate);row.Controls.Add(plus);results.Controls.Add(row);
            }
            if(filtered.Count==0)results.Controls.Add(new Label{Text="没有找到内容。股票可输入代码后点“查询代码”；\r\n币种可加载更多；球队来自已启用联赛的近三日赛程。",Bounds=new Rectangle(10,18,width-20,90),ForeColor=Muted,Font=results.Font});
            if(filtered.Count>60)results.Controls.Add(new Label{Text="已显示前 60 项，请输入名称缩小范围",Bounds=new Rectangle(10,rowIndex*h,width-20,30),ForeColor=Muted,Font=results.Font});
        };
        queueRender=()=>{if(f.IsDisposed||scheduled)return;if(!f.IsHandleCreated){render();return;}scheduled=true;f.BeginInvoke((Action)(()=>{scheduled=false;if(!f.IsDisposed)render();}));};
        search.TextChanged+=(o,e)=>queueRender();
        query.Click+=async(o,e)=>{query.Enabled=false;try{string code=MarketCatalog.Normalize(search.Text);string group=code.StartsWith("us")?"美股":"A股";status.Text="正在查询 "+code+"…";var batch=await Feed.Load(group,"https://qt.gtimg.cn/q="+code,t=>Feed.ParseStocks(t,new[]{code},group),"腾讯行情 · "+code,true);if(f.IsDisposed)return;if(batch.Error!="")throw new Exception("查询失败，请检查股票代码或网络");ApplyBatch(batch);var item=batch.Items.Single();extras.Add(new Slot{Key=item.Key,Name=item.Name,Group=item.Group});category="全部";search.Text=item.Name;queueRender();status.Text="找到 "+item.Name+"，点击 ＋ 添加";}catch(Exception ex){if(!f.IsDisposed)status.Text=ex.Message;}finally{if(!f.IsDisposed)query.Enabled=true;}};
        coins.Click+=async(o,e)=>{coins.Enabled=false;try{status.Text="正在加载币种目录…";var all=await MarketCatalog.Coins();if(f.IsDisposed)return;extras.RemoveAll(s=>s.Group=="加密");extras.AddRange(all);category="行情";queueRender();status.Text="已加载 "+all.Count+" 个币种，输入名称或代码搜索";}catch(Exception ex){if(!f.IsDisposed)status.Text=ex.Message;}finally{if(!f.IsDisposed)coins.Enabled=true;}};
        refresh.Click+=async(o,e)=>{refresh.Enabled=false;try{if(refreshing){status.Text="正在更新，稍后列表会自动更新";return;}await RefreshFeeds();if(f.IsDisposed)return;queueRender();status.Text=data.Where(b=>b.Group=="NBA"||b.Group=="足球").Any(b=>b.Error!="")?"部分赛程离线，已保留可用内容":"赛程已更新，球队可直接关注";}finally{if(!f.IsDisposed)refresh.Enabled=true;}};
        var timer=new Timer{Interval=2000};string signature="";timer.Tick+=(o,e)=>{string current=string.Join("|",data.Select(b=>b.Fetched.Ticks+":"+b.Items.Count+":"+b.Error));if(current!=signature&&!refreshing){signature=current;queueRender();}};
        f.Shown+=(o,e)=>{render();if(interactive)timer.Start();};f.FormClosed+=(o,e)=>{timer.Stop();timer.Dispose();};return f;
    }
    Surface MakePreferences(){
        var f=new Surface("偏好设置",Colors,new Size(600,470));f.Heading("偏好设置","更改立即保存；关注内容在“我的关注”管理");bool binding=true;
        f.LabelAt("外观",24,112,100,26,14,false);
        var theme=new ComboBox{Bounds=new Rectangle(144,110,430,28),DropDownStyle=ComboBoxStyle.DropDownList};theme.Items.AddRange(new object[]{"自动 · 白天浅色 / 夜间深色","浅色","深色"});theme.SelectedIndex=settings.Theme=="light"?1:settings.Theme=="dark"?2:0;StyleCombo(theme);f.Controls.Add(theme);
        f.LabelAt("显示屏幕",24,156,115,26,14,false);Screen[] screens=Screen.AllScreens;
        var monitor=new ComboBox{Bounds=new Rectangle(144,154,430,28),DropDownStyle=ComboBoxStyle.DropDownList};foreach(var screen in screens)monitor.Items.Add((screen.Primary?"主屏幕 ":"屏幕 ")+screen.Bounds.Width+" × "+screen.Bounds.Height);monitor.SelectedIndex=Math.Max(0,Array.FindIndex(screens,s=>s.DeviceName==SelectedScreen.DeviceName));StyleCombo(monitor);f.Controls.Add(monitor);
        var quiet=new CheckBox{Text="单色涨跌",Checked=settings.Quiet,Bounds=new Rectangle(24,202,220,28)};var rotate=new CheckBox{Text="分类自动轮播",Checked=settings.Rotate,Bounds=new Rectangle(285,202,260,28)};f.Controls.AddRange(new Control[]{quiet,rotate});
        f.LabelAt("足球联赛",24,254,200,25,14,false);var leagues=new FlowLayoutPanel{Bounds=new Rectangle(24,288,552,76)};f.Controls.Add(leagues);
        var choices=new Dictionary<string,string>{{"eng.1","英超"},{"esp.1","西甲"},{"uefa.champions","欧冠"},{"ita.1","意甲"},{"ger.1","德甲"},{"fra.1","法甲"},{"chn.1","中超"}};
        foreach(string code in Feed.Tokens(settings.Leagues))if(!choices.ContainsKey(code))choices.Add(code,code);
        foreach(var pair in choices){var box=new CheckBox{Text=pair.Value,Tag=pair.Key,Checked=Feed.Tokens(settings.Leagues).Contains(pair.Key),Width=115,Height=26};leagues.Controls.Add(box);box.CheckedChanged+=(o,e)=>{if(binding)return;string old=settings.Leagues;var selected=leagues.Controls.OfType<CheckBox>().Where(c=>c.Checked).Select(c=>(string)c.Tag).ToArray();if(selected.Length>8){binding=true;box.Checked=false;binding=false;return;}settings.Leagues=string.Join(",",selected);if(!Save()){settings.Leagues=old;binding=true;box.Checked=!box.Checked;binding=false;}else if(interactive)RequestWatchRefresh();};}
        theme.SelectedIndexChanged+=(o,e)=>{if(binding)return;string old=settings.Theme;settings.Theme=theme.SelectedIndex==1?"light":theme.SelectedIndex==2?"dark":"auto";if(!Save()){settings.Theme=old;binding=true;theme.SelectedIndex=old=="light"?1:old=="dark"?2:0;binding=false;}Invalidate();};
        monitor.SelectedIndexChanged+=(o,e)=>{if(binding||monitor.SelectedIndex<0)return;string old=settings.Monitor;settings.Monitor=screens[monitor.SelectedIndex].DeviceName;if(!Save()){settings.Monitor=old;binding=true;monitor.SelectedIndex=Math.Max(0,Array.FindIndex(screens,s=>s.DeviceName==old));binding=false;}else if(interactive){ReleaseDock();Place();}};
        quiet.CheckedChanged+=(o,e)=>{if(binding)return;bool old=settings.Quiet;settings.Quiet=quiet.Checked;if(!Save()){settings.Quiet=old;binding=true;quiet.Checked=old;binding=false;}Invalidate();};
        rotate.CheckedChanged+=(o,e)=>{if(binding)return;bool old=settings.Rotate;settings.Rotate=rotate.Checked;if(!Save()){settings.Rotate=old;binding=true;rotate.Checked=old;binding=false;}};
        f.LabelAt("每 60 秒更新 · 股票、币种、比赛可能存在源数据延迟\n更改主题后，重新打开窗口即可应用新外观。",24,396,550,48,11,true);binding=false;return f;
    }
}
}
