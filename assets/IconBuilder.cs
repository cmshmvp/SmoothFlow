using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Original mint mouse-wheel mark. Each ICO size is rendered from geometry.
internal static class IconBuilder {
    static GraphicsPath Round(float x,float y,float w,float h,float r) {
        GraphicsPath p=new GraphicsPath();float d=r*2;
        p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();return p;
    }
    static Bitmap Draw(int size) {
        int factor=4;
        using(Bitmap large=new Bitmap(size*factor,size*factor,PixelFormat.Format32bppArgb)) {
            using(Graphics g=Graphics.FromImage(large)) {
                g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size*factor/64F,size*factor/64F);
                using(GraphicsPath tile=Round(2,2,60,60,14))using(LinearGradientBrush bg=new LinearGradientBrush(new PointF(0,0),new PointF(64,64),Color.FromArgb(31,49,57),Color.FromArgb(13,23,32)))g.FillPath(bg,tile);
                using(GraphicsPath mouse=Round(21,11,22,42,11))using(LinearGradientBrush mint=new LinearGradientBrush(new PointF(21,11),new PointF(43,53),Color.FromArgb(147,247,217),Color.FromArgb(61,207,170)))g.FillPath(mint,mouse);
                using(Pen groove=new Pen(Color.FromArgb(18,53,51),3.4F)) {groove.StartCap=LineCap.Round;groove.EndCap=LineCap.Round;g.DrawLine(groove,32,20,32,29);}
                using(Pen flow=new Pen(Color.FromArgb(104,225,188),3.4F)) {flow.StartCap=LineCap.Round;flow.EndCap=LineCap.Round;g.DrawBezier(flow,15,21,7,28,7,36,15,43);g.DrawBezier(flow,49,21,57,28,57,36,49,43);}
            }
            Bitmap result=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(Graphics g=Graphics.FromImage(result)) {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(large,new Rectangle(0,0,size,size));}
            return result;
        }
    }
    static int Main(string[] args) {
        Directory.CreateDirectory(args[0]);int[] sizes={16,20,24,32,40,48,64,128,256};List<byte[]> images=new List<byte[]>();
        foreach(int size in sizes)using(Bitmap image=Draw(size))using(MemoryStream stream=new MemoryStream()) {image.Save(stream,ImageFormat.Png);images.Add(stream.ToArray());if(size==256)image.Save(Path.Combine(args[0],"SmoothFlow-icon.png"),ImageFormat.Png);}
        using(BinaryWriter output=new BinaryWriter(File.Create(Path.Combine(args[0],"SmoothFlow.ico")))) {
            output.Write((ushort)0);output.Write((ushort)1);output.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++){output.Write((byte)(sizes[i]==256 ? 0 : sizes[i]));output.Write((byte)(sizes[i]==256 ? 0 : sizes[i]));output.Write((byte)0);output.Write((byte)0);output.Write((ushort)1);output.Write((ushort)32);output.Write(images[i].Length);output.Write(offset);offset+=images[i].Length;}
            foreach(byte[] image in images)output.Write(image);
        }
        return 0;
    }
}
