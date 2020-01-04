using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DllImport;

namespace CombinationColor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ImageSource[] ShowImgsGray=new ImageSource[512];          //存放灰度图的数组
        public static ImageSource[] ShowImgsColour = new ImageSource[512];      //存放伪彩图的数组
        public int imgNum = 128;                                                    //图像张数
        public static byte[] PseudoColor = new byte[256 * 3];                   //存放映射表的数组
        public static int imgLine = 512;                                       //图像宽度
        public static int imgPixel = 885;                                      //图像高度
        public static byte[] ys = new byte[imgLine * imgPixel];                 //读取数据的数组
        FileStream file1 = new FileStream("20191225201705.txt", FileMode.Open);

        public MainWindow()
        {
            FileStream file1 = new FileStream("mycolor_uint8.txt", FileMode.Open);
            file1.Seek(0, SeekOrigin.Begin);
            file1.Read(PseudoColor, 0, 256 * 3);
            file1.Close();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            for(int i=0;i<imgNum;i++)
            {               
                file1.Read(ys, 0, imgLine * imgPixel);
                ShowImgsGray[i] = BecomeImg(ys, imgLine, imgPixel);
                ShowImgsColour[i] = PGrayToPseudoColor(ToGrayBitmap(ys, imgLine, imgPixel), PseudoColor);
            }
            file1.Close();


            ImgsGray.Source = ShowImgsGray[0];
            ImgsColour.Source = ShowImgsColour[0];
        }

        public static Bitmap ToGrayBitmap(byte[] rawValues, int width, int height)
        {
            //// 申请目标位图的变量，并将其内存区域锁定  
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);//u8类型
            BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            //// 获取图像参数  
            int stride = bmpData.Stride;  // 扫描线的宽度  
            int offset = stride - width;  // 显示宽度与扫描线宽度的间隙     
            IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置  

            //int scanBytes = stride * height;// 用stride宽度，表示这是内存区域的大小 
            //创建放入图像的像素数据，使用2，因为它是16bpp  
            int scanBytes = width * height * 1;
            //// 用Marshal的Copy方法，将刚才得到的内存字节数组复制到BitmapData中  
            System.Runtime.InteropServices.Marshal.Copy(rawValues, 0, iptr, scanBytes);
            bmp.UnlockBits(bmpData);  // 解锁内存区域  
            //// 下面的代码是为了修改生成位图的索引表，从伪彩修改为灰度  
            ColorPalette tempPalette;
            using (Bitmap tempBmp = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed))
            {
                tempPalette = tempBmp.Palette;
            }
            for (int i = 0; i < 256; i++)
            {
                tempPalette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
            }
            bmp.Palette = tempPalette;
            return bmp;
        }

        public static ImageSource BecomeImg(byte[] byteImg, int width, int height)
        {
            Bitmap temp = new Bitmap(width, height);
            temp = ToGrayBitmap(byteImg, width, height);
            ColorPalette tempPalette = temp.Palette;
            for (int j = 0; j < 256; j++)
            {
                tempPalette.Entries[j] = System.Drawing.Color.FromArgb(j, j, j);
            }
            temp.Palette = tempPalette;
            IntPtr hBitmap = temp.GetHbitmap();
            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            wpfBitmap.Freeze();
            temp.Dispose();
            return wpfBitmap;
        }

        public static ImageSource PGrayToPseudoColor(Bitmap src, byte[] PseudoColor)
        {

            Bitmap a = new Bitmap(src);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, a.Width, a.Height);
            System.Drawing.Imaging.BitmapData bmpData = a.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int stride = bmpData.Stride;
            unsafe
            {
                byte* pIn = (byte*)bmpData.Scan0.ToPointer();
                int temp;

                for (int y = 0; y < a.Height; y++)
                {
                    for (int x = 0; x < a.Width; x++)
                    {
                        temp = pIn[0];

                        pIn[0] = PseudoColor[temp + 512];            //pIn[0]  B
                        pIn[1] = PseudoColor[temp + 256];            //pIn[0]  G
                        pIn[2] = PseudoColor[temp];            //pIn[0]  R

                        pIn += 3;
                    }
                    pIn += stride - a.Width * 3;
                }
            }
            a.UnlockBits(bmpData);

            IntPtr hBitmap = a.GetHbitmap();
            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            wpfBitmap.Freeze();
            a.Dispose();
            return wpfBitmap;
        }

        public static IntPtr ArrayToIntptr(byte[] source)
        {
            if (source == null)
                return IntPtr.Zero;
            byte[] da = source;
            IntPtr ptr = Marshal.AllocHGlobal(da.Length);
            Marshal.Copy(da, 0, ptr, da.Length);
            return ptr;
        }

        private void ImgRollWidget_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider t = (Slider)sender;
            ImgRollWidget.Maximum = imgNum - 1;
            ImgsGray.Source = ShowImgsGray[(int)t.Value];
            ImgsColour.Source = ShowImgsColour[(int)t.Value];

        }
    }
}
