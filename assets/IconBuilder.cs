using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using CornerBar;
class IconBuilder {
    static void Main(string[] args){var sizes=new[]{16,20,24,32,48,64,128,256};var frames=new List<byte[]>();foreach(int size in sizes){using(var bmp=new Bitmap(size,size)){using(var g=Graphics.FromImage(bmp)){g.Clear(Color.Transparent);Brand.Draw(g,new Rectangle(0,0,size,size),size<=24);}using(var ms=new MemoryStream()){bmp.Save(ms,ImageFormat.Png);frames.Add(ms.ToArray());}if(size==256)bmp.Save(Path.Combine(args[0],"ROW.png"),ImageFormat.Png);}}
        using(var stream=File.Create(Path.Combine(args[0],"ROW.ico")))using(var w=new BinaryWriter(stream)){w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(frames[i].Length);w.Write(offset);offset+=frames[i].Length;}foreach(var bytes in frames)w.Write(bytes);}
    }
}
