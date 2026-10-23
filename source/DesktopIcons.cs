using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
namespace CornerBar {
// Shell's marshalled IFolderView API: no injected code, Explorer memory writes or global icon setting changes.
internal sealed class DesktopIcons {
    [ComImport,Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Services{[PreserveSig]int QueryService(ref Guid service,ref Guid iid,[MarshalAs(UnmanagedType.Interface)]out object value);}
    [ComImport,Guid("000214E2-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Browser{
        void GetWindow(out IntPtr h);void ContextSensitiveHelp(bool b);void InsertMenus();void SetMenu();void RemoveMenus();void SetStatusTextSB();void EnableModelessSB();void TranslateAcceleratorSB();void BrowseObject();void GetViewStateStream();void GetControlWindow();void SendControlMsg();void QueryActiveShellView([MarshalAs(UnmanagedType.Interface)]out object view);
    }
    [ComImport,Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface FolderView{
        void GetCurrentViewMode(out uint mode);void SetCurrentViewMode(uint mode);void GetFolder(ref Guid iid,out IntPtr obj);void Item(int index,out IntPtr pidl);void ItemCount(uint flags,out int count);void Items(uint flags,ref Guid iid,out IntPtr items);void GetSelectionMarkedItem(out int i);void GetFocusedItem(out int i);void GetItemPosition(IntPtr pidl,out Point point);void GetSpacing(out Point point);void GetDefaultSpacing(out Point point);[PreserveSig]int GetAutoArrange();void SelectItem(int i,uint flags);void SelectAndPositionItems(uint count,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]IntPtr[] pidls,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]Point[] points,uint flags);
    }
    [ComImport,Guid("000214E3-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface OleWindow{void GetWindow(out IntPtr window);void ContextSensitiveHelp(bool help);}
    [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h,ref Point p);
    [DllImport("shell32.dll")] static extern uint ILGetSize(IntPtr p);
    public class Move {public string Id;public int X,Y,ToX,ToY;}
    readonly string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"desktop-layout-backup.json");
    List<Move> moves=new List<Move>();
    internal string Status="";
    internal DesktopIcons(){try{if(File.Exists(path))moves=new JavaScriptSerializer().Deserialize<List<Move>>(File.ReadAllText(path))??new List<Move>();}catch{Status="桌面位置备份读取失败";}}
    static FolderView View(){
        object windows=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")));object desktop=null,browser=null;
        try{dynamic sw=windows;object location=0,root=0;int hwnd;desktop=sw.FindWindowSW(ref location,ref root,8,out hwnd,1);Guid sid=new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"),iid=typeof(Browser).GUID;Marshal.ThrowExceptionForHR(((Services)desktop).QueryService(ref sid,ref iid,out browser));object view;((Browser)browser).QueryActiveShellView(out view);return (FolderView)view;}
        finally{if(browser!=null)Marshal.ReleaseComObject(browser);if(desktop!=null)Marshal.ReleaseComObject(desktop);Marshal.ReleaseComObject(windows);}
    }
    sealed class Entry {internal IntPtr Pidl;internal string Id;internal Point Point;}
    static List<Entry> Entries(FolderView view){var list=new List<Entry>();int count;view.ItemCount(2,out count);try{for(int i=0;i<count;i++){IntPtr p;view.Item(i,out p);var entry=new Entry{Pidl=p};list.Add(entry);var bytes=new byte[ILGetSize(p)];Marshal.Copy(p,bytes,0,bytes.Length);entry.Id=Convert.ToBase64String(bytes);view.GetItemPosition(p,out entry.Point);}return list;}catch{Free(list);throw;}}
    static void Free(List<Entry> entries){foreach(var e in entries)Marshal.FreeCoTaskMem(e.Pidl);}
    void Save(){File.WriteAllText(path,new JavaScriptSerializer().Serialize(moves));}
    internal static Point? FreePosition(Point original,Size spacing,Rectangle work,IEnumerable<Point> occupied){
        var points=occupied.ToArray();int dx=Math.Max(40,spacing.Width),dy=Math.Max(40,spacing.Height);
        for(int c=0;c<Math.Max(1,work.Width/dx);c++){int x=c==0?original.X:work.Left+8+(c-1)*dx;for(int y=original.Y+Math.Max(0,(int)Math.Ceiling((work.Top+8-original.Y)/(double)dy))*dy;y+dy<=work.Bottom;y+=dy){var p=new Point(x,y);if(x<work.Left||x+dx>work.Right)continue;if(points.All(q=>Math.Abs(q.X-x)>=dx-4||Math.Abs(q.Y-y)>=dy-4))return p;}}return null;
    }
    internal void Avoid(Rectangle strip,Rectangle work){
        FolderView view=null;List<Entry> entries=null;
        try{view=View();entries=Entries(view);IntPtr h;((OleWindow)view).GetWindow(out h);Point origin=Point.Empty;ClientToScreen(h,ref origin);Point space;view.GetSpacing(out space);
            var area=work;area.Offset(-origin.X,-origin.Y);var top=strip;top.Offset(-origin.X,-origin.Y);
            foreach(var e in entries.Where(e=>e.Point.X>=top.Left&&e.Point.X<top.Right&&e.Point.Y<top.Bottom&&e.Point.Y+space.Y>top.Top).ToArray()){
                if(moves.Any(existing=>existing.Id==e.Id))continue;
                Point? target=FreePosition(e.Point,new Size(space.X,space.Y),area,entries.Where(v=>v!=e).Select(v=>v.Point));if(!target.HasValue){Status="桌面空间不足，部分图标无法避让";continue;}
                var m=new Move{Id=e.Id,X=e.Point.X,Y=e.Point.Y,ToX=target.Value.X,ToY=target.Value.Y};moves.Add(m);Save();view.SelectAndPositionItems(1,new[]{e.Pidl},new[]{target.Value},0x80);Point actual;view.GetItemPosition(e.Pidl,out actual);m.ToX=actual.X;m.ToY=actual.Y;e.Point=actual;Save();
                if(actual.Y<top.Bottom)Status="桌面自动排列限制了避让，请关闭自动排列后重试";
            }
        }catch(Exception e){Status="桌面图标避让未完成："+e.Message;}finally{if(entries!=null)Free(entries);if(view!=null)Marshal.ReleaseComObject(view);}
    }
    internal void Restore(){if(moves.Count==0)return;FolderView view=null;List<Entry> entries=null;try{view=View();entries=Entries(view);foreach(var m in moves.ToArray()){var e=entries.FirstOrDefault(x=>x.Id==m.Id);if(e!=null&&e.Point==new Point(m.ToX,m.ToY))view.SelectAndPositionItems(1,new[]{e.Pidl},new[]{new Point(m.X,m.Y)},0x80);moves.Remove(m);Save();}Status="";}catch(Exception e){Status="桌面位置恢复待重试："+e.Message;}finally{if(entries!=null)Free(entries);if(view!=null)Marshal.ReleaseComObject(view);}}
    internal int CountCovered(Rectangle strip){FolderView v=null;List<Entry> entries=null;try{v=View();entries=Entries(v);IntPtr h;((OleWindow)v).GetWindow(out h);Point origin=Point.Empty;ClientToScreen(h,ref origin);strip.Offset(-origin.X,-origin.Y);return entries.Count(e=>strip.Contains(e.Point));}finally{if(entries!=null)Free(entries);if(v!=null)Marshal.ReleaseComObject(v);}}
    internal string Inspect(){FolderView v=null;List<Entry> entries=null;try{v=View();entries=Entries(v);return "Desktop icons: "+entries.Count+"; auto arrange="+(v.GetAutoArrange()==0)+"; layout="+string.Join("|",entries.OrderBy(e=>e.Id).Select(e=>e.Id+":"+e.Point.X+","+e.Point.Y));}finally{if(entries!=null)Free(entries);if(v!=null)Marshal.ReleaseComObject(v);}}
}
}



