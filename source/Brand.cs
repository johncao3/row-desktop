using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
[assembly: AssemblyTitle("ROW")]
[assembly: AssemblyProduct("ROW Desktop Bar")]
[assembly: AssemblyVersion("1.6.0.0")]
[assembly: AssemblyFileVersion("1.6.0.0")]
[assembly: AssemblyInformationalVersion("1.06")]
namespace CornerBar {
internal static class Brand {
    internal static readonly Color Orange=Color.FromArgb(255,99,43);
    internal static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    internal static void Fill(Graphics g,RectangleF r,float radius,Color c){using(var p=Round(r,radius))using(var b=new SolidBrush(c))g.FillPath(b,p);}
    internal static void Draw(Graphics g,Rectangle r,bool small){
        var state=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/256f,r.Height/256f);g.SmoothingMode=SmoothingMode.AntiAlias;
        Fill(g,new RectangleF(3,3,250,250),56,Color.White);
        using(var p=Round(new RectangleF(3,3,250,250),56))using(var pen=new Pen(Color.FromArgb(220,224,228),2))g.DrawPath(pen,p);
        var ink=Color.FromArgb(31,36,42);
        if(!small){using(var path=new GraphicsPath())using(var family=new FontFamily("Arial")){path.AddString("ROW",family,(int)FontStyle.Bold,85,new PointF(0,0),StringFormat.GenericTypographic);var b=path.GetBounds();using(var matrix=new Matrix(198/b.Width,0,0,72/b.Height,29-b.X*198/b.Width,80-b.Y*72/b.Height)){path.Transform(matrix);using(var brush=new SolidBrush(ink))g.FillPath(brush,path);}}}
        float y=small?113:171;Fill(g,new RectangleF(30,y,small?38:26,small?38:26),small?11:8,Orange);Fill(g,new RectangleF(small?81:66,y+(small?5:3),small?145:160,small?28:20),10,ink);g.Restore(state);
    }
    internal static Icon LoadIcon(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("ROW.ico")){if(s!=null)return new Icon(s);}return (Icon)SystemIcons.Application.Clone();}
}
internal sealed class Palette {
    internal readonly bool Light;
    internal Palette(bool light){Light=light;}
    internal Color Bg {get{return Light?Color.White:Color.FromArgb(26,28,32);}}
    internal Color Ink {get{return Light?Color.FromArgb(31,36,42):Color.FromArgb(244,245,247);}}
    internal Color Muted {get{return Light?Color.FromArgb(113,119,129):Color.FromArgb(164,170,180);}}
    internal Color Soft {get{return Light?Color.FromArgb(246,247,249):Color.FromArgb(37,40,46);}}
    internal Color Line {get{return Light?Color.FromArgb(229,232,236):Color.FromArgb(55,59,66);}}
}
internal class Surface : Form {
    internal readonly Palette Colors;
    internal Surface(string title,Palette colors,Size size){Colors=colors;Text="ROW · "+title;ClientSize=size;BackColor=colors.Bg;ForeColor=colors.Ink;Font=new Font("Microsoft YaHei UI",9);AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=title=="设置";Icon=Brand.LoadIcon();DoubleBuffered=true;KeyPreview=true;KeyDown+=(o,e)=>{if(e.KeyCode==Keys.Escape)Close();};ButtonAt("×",size.Width-48,18,28,28,Close,false);}
    bool scaled;
    protected override void OnLoad(EventArgs e){base.OnLoad(e);if(scaled)return;scaled=true;float dpi=DesktopDock.Scale(Handle);if(dpi!=1){Scale(new SizeF(dpi,dpi));foreach(Control c in Controls){c.Font=new Font(c.Font.FontFamily,c.Font.Size*dpi,c.Font.Style,c.Font.Unit);var list=c as ListView;if(list!=null)foreach(ColumnHeader col in list.Columns)col.Width=(int)(col.Width*dpi);}}}
    internal Label LabelAt(string text,int x,int y,int w,int h,int size,bool muted){var label=new Label{Text=text,Bounds=new Rectangle(x,y,w,h),ForeColor=muted?Colors.Muted:Colors.Ink,Font=new Font("Microsoft YaHei UI",size,GraphicsUnit.Pixel),AutoEllipsis=true};Controls.Add(label);return label;}
    internal Button ButtonAt(string text,int x,int y,int w,int h,Action action,bool primary){var b=new RowButton{Text=text,Bounds=new Rectangle(x,y,w,h),FlatStyle=FlatStyle.Flat,BackColor=primary?Colors.Ink:Colors.Soft,ForeColor=primary?Colors.Bg:Colors.Ink,Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.Click+=(o,e)=>action();Controls.Add(b);return b;}
    internal void Heading(string title,string subtitle){LabelAt(title,64,22,ClientSize.Width-130,30,21,false);LabelAt(subtitle,24,58,ClientSize.Width-48,24,12,true);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);float dpi=DesktopDock.Scale(Handle);var g=e.Graphics;using(var p=new Pen(Colors.Line))g.DrawRectangle(p,0,0,ClientSize.Width-1,ClientSize.Height-1);g.ScaleTransform(dpi,dpi);Brand.Draw(g,new Rectangle(24,20,28,28),true);using(var p=new Pen(Colors.Line))g.DrawLine(p,24,91,ClientSize.Width/dpi-24,91);}
    protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x84&&m.Result.ToInt32()==1){long v=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(v&0xffff),(short)((v>>16)&0xffff)));float dpi=DesktopDock.Scale(Handle);if(p.Y<85*dpi&&p.X<ClientSize.Width-60*dpi)m.Result=new IntPtr(2);}}
    protected override void Dispose(bool disposing){if(disposing&&Icon!=null)Icon.Dispose();base.Dispose(disposing);}
}
internal sealed class RowButton : Button {
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Brand.Fill(e.Graphics,new Rectangle(0,0,Width-1,Height-1),8*DesktopDock.Scale(Handle),BackColor);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);if(Focused)using(var pen=new Pen(Brand.Orange))using(var path=Brand.Round(new Rectangle(2,2,Width-5,Height-5),6))e.Graphics.DrawPath(pen,path);}
}
internal sealed class MenuRenderer : ToolStripProfessionalRenderer {
    readonly Palette p;internal MenuRenderer(Palette palette){p=palette;}
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(p.Bg);}
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(e.Item.Selected)Brand.Fill(e.Graphics,new Rectangle(4,1,e.Item.Width-8,e.Item.Height-2),6,p.Soft);}
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=p.Ink;base.OnRenderItemText(e);}
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
}
}






