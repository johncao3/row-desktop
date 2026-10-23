using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CornerBar {
internal static class DesktopDock {
    [StructLayout(LayoutKind.Sequential)] internal struct Rect {public int Left,Top,Right,Bottom;public Rect(Rectangle r){Left=r.Left;Top=r.Top;Right=r.Right;Bottom=r.Bottom;}public Rectangle Bounds{get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
    [StructLayout(LayoutKind.Sequential)] internal struct AppData {public uint cbSize;public IntPtr hWnd;public uint callback,edge;public Rect rc;public IntPtr param;}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo {public int Size;public Rect Monitor,Work;public uint Flags;}
    [DllImport("shell32.dll")] static extern UIntPtr SHAppBarMessage(uint message,ref AppData data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string text);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromRect(ref Rect rect,uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr window,int index);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int cx,int cy,uint flags);
    delegate bool EnumProc(IntPtr window,IntPtr param);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc,IntPtr param);
    [DllImport("user32.dll")] static extern bool GetGUIThreadInfo(uint thread,ref GuiInfo info);
    [StructLayout(LayoutKind.Sequential)] struct GuiInfo {public uint Size,Flags;public IntPtr Active,Focus,Capture,MenuOwner,MoveSize,Caret;public Rect CaretRect;}
    internal static void EnableDpi(){try{if(SetProcessDpiAwarenessContext(new IntPtr(-4)))return;}catch(EntryPointNotFoundException){}SetProcessDPIAware();}
    internal static float Scale(IntPtr h){try{return Math.Max(1,GetDpiForWindow(h)/96f);}catch(EntryPointNotFoundException){return 1;}}
    static AppData Data(IntPtr h){return new AppData{cbSize=(uint)Marshal.SizeOf(typeof(AppData)),hWnd=h};}
    internal static bool Register(IntPtr h,uint callback){AppData a=Data(h);a.callback=callback;return SHAppBarMessage(0,ref a)!=UIntPtr.Zero;}
    internal static void Remove(IntPtr h){AppData a=Data(h);SHAppBarMessage(1,ref a);}
    internal static Rectangle Position(IntPtr h,Rectangle screen,int height){
        AppData a=Data(h);a.edge=1;a.rc=new Rect(screen);SHAppBarMessage(2,ref a);a.rc.Bottom=a.rc.Top+height;SHAppBarMessage(3,ref a);return a.rc.Bounds;
    }
    internal static void Notify(IntPtr h,uint message,IntPtr param){AppData a=Data(h);a.param=param;SHAppBarMessage(message,ref a);}
    internal static Rectangle WorkArea(Rectangle screen){Rect r=new Rect(screen);IntPtr mon=MonitorFromRect(ref r,2);var info=new MonitorInfo{Size=Marshal.SizeOf(typeof(MonitorInfo))};return GetMonitorInfo(mon,ref info)?info.Work.Bounds:screen;}
    // Only relocate normal, resizable application windows whose title bar is covered.
    // Windows manages maximized/snapped bounds through AppBar's work-area reservation.
    internal static void KeepWindowsBelow(IntPtr bar,Rectangle strip,Rectangle screen,IntPtr only){
        var gui=new GuiInfo{Size=(uint)Marshal.SizeOf(typeof(GuiInfo))};if(GetGUIThreadInfo(0,ref gui)&&gui.MoveSize!=IntPtr.Zero)return;
        Rectangle work=WorkArea(screen);
        EnumProc visit=delegate(IntPtr h,IntPtr p){
            if(h==bar||(only!=IntPtr.Zero&&h!=only)||!IsWindowVisible(h)||IsIconic(h)||IsZoomed(h))return true;
            int style=GetWindowLong(h,-16),ex=GetWindowLong(h,-20);
            if((style&0x00C00000)!=0x00C00000||(style&0x00040000)==0||(ex&0x80)!=0)return true;
            Rect r;if(!GetWindowRect(h,out r))return true;Rectangle b=r.Bounds;
            if(!screen.Contains(new Point(b.Left+b.Width/2,b.Top+Math.Min(20,b.Height/2)))||b.Top>=strip.Bottom||b.Bottom<=strip.Top)return true;
            int height=Math.Min(b.Height,work.Height);SetWindowPos(h,IntPtr.Zero,b.Left,work.Top,b.Width,height,0x4014);return true;
        };
        EnumWindows(visit,IntPtr.Zero);
    }
}
}
