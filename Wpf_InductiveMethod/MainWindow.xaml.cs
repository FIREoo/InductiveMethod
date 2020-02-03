using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using Point = System.Drawing.Point;
using System.Windows.Threading;
using System.Data;

namespace Wpf_InductiveMethod
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void ImageUpdate()
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            foreach (InteractObject o in obj)
                o.drawOn(mat);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }

        InteractObject[] obj = new InteractObject[2];
        int handingObj = -1;
        List<string[]> LVinfo = new List<string[]>();//我不知道怎麼取得 select listView text，所以用這個存

        Mat mat = new Mat(600, 600, DepthType.Cv8U, 3);
        int Generation = 0;
        private void Button_start_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            obj[0] = new InteractObject(0);
            obj[0].Center = new Point(rnd.Next(50, 550), rnd.Next(50, 550));
            obj[0].Radius = 40;
            obj[0].Shape = InteractObject.Type.square;
            obj[0].Color = new MCvScalar(200, 50, 50);
            obj[0].trajectory.Clear();
            obj[0].trajectory.Add(new Trajectory());

            obj[1] = new InteractObject(1);
            obj[1].Center = new Point(rnd.Next(50, 550), rnd.Next(50, 550));
            obj[1].Radius = 40;
            obj[1].Shape = InteractObject.Type.circle;
            obj[1].Color = new MCvScalar(50, 200, 50);
            obj[1].trajectory.Clear();
            obj[1].trajectory.Add(new Trajectory());


            this.LV_trajectoryInfo.Items.Clear();
            LVinfo.Clear();
            Generation = 0;
            tb_checkNum.Text = "Gen : " + Generation.ToString();
            ImageUpdate();
        }

        #region //---Environment---\\
        private void Image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (InteractObject o in obj)
            {
                if (o.isInArea(e.GetPosition(image1)))
                {
                    handingObj = o.index;
                    o.pick();
                    o.trajectory[Generation].AddPoint((e.GetPosition(image1).toDraw()));
                }
                else//debug
                    o.place();
            }
            ImageUpdate();
        }

        private void Image1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (InteractObject o in obj)
            {
                if (o.isInArea(e.GetPosition(image1)))
                {
                    o.place();
                    o.trajectory[Generation].AddPoint((e.GetPosition(image1).toDraw()));
                }
            }
            ImageUpdate();

            Btn_check_Click(null, null);
            handingObj = -1;
        }

        private void Image1_MouseMove(object sender, MouseEventArgs e)
        {
            var P = e.GetPosition(image1);                                          
            foreach (InteractObject o in obj)
            {
                if (o.isPick)
                {
                    o.Center = new Point((int)P.X, (int)P.Y);
                    o.trajectory[Generation].AddPoint((e.GetPosition(image1).toDraw()));
                    ImageUpdate();
                }
            }
        }
        #endregion \\---Environment---//

        #region //---draw replat Path---\\
        private void Btn_replay_Click(object sender, RoutedEventArgs e)
        {
            int removeIndex = LV_trajectoryInfo.SelectedIndex;
            if (LV_trajectoryInfo.SelectedIndex < 0)
            {
                MessageBox.Show("please select items");
                return;
            }

            string[] text = ListViewText(removeIndex);
            int Io = text[0].toInt();
            int Ig = text[1].toInt();
            obj[Io].trajectory[Ig].drawOn(mat, obj[Io].Center, obj[Io].Color);

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);


        }
        private void Btn_replay_all_Click(object sender, RoutedEventArgs e)
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            foreach (InteractObject o in obj)
                foreach (Trajectory t in o.trajectory)
                    t.drawOn(mat, o.Center, o.Color);

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void Btn_clearImage_Click(object sender, RoutedEventArgs e)
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void But_clearPath_Click(object sender, RoutedEventArgs e)
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        #endregion \\---draw replat Path---//

        private void Btn_check_Click(object sender, RoutedEventArgs e)
        {
            //add in listView 
            if (handingObj < 0)
            {
                MessageBox.Show("noting handing.");
                return;
            }
            //only the handing object need to be added
            obj[handingObj].trajectory[Generation].AddPointDone(obj[handingObj].Center);
            ListViewAdd(new TrajectoryInfoAdder(handingObj.ToString(), Generation.ToString(), Colors.Black, Colors.Black));

            //next gen
            resetObject();
            Generation++;

            //顯示
            tb_checkNum.Text = "Now Gen : " + Generation.ToString();

            ImageUpdate();
        }
        private void resetObject()
        {
            Random rnd = new Random();
            foreach (InteractObject o in obj)
            {
                o.Center = new Point(rnd.Next(50, 550), rnd.Next(50, 550));
                o.trajectory.Add(new Trajectory());
            }
        }


        private void Btn_countKeyPoint_Click(object sender, RoutedEventArgs e)
        {
            //compare T0 with Tc
            for (int Tc = 1; Tc < obj[0].trajectory.Count; Tc++)
                CompareTwoTrajectory(obj[0].trajectory[0], obj[0].trajectory[Tc], 20);

            mat.SetTo(new byte[600 * 600 * 3]);

            //draw path
            foreach (InteractObject o in obj)
                foreach (Trajectory t in o.trajectory)
                    t.drawOn(mat, o.Center, o.Color);


            //draw key point
            obj[0].trajectory[0].drawKeyOn(1, mat, obj[0].Center, new MCvScalar(30, 30, 200));
            //obj[1].trajectory[0].drawKeyOn(1, mat, obj[1].Center, new MCvScalar(30, 30, 200));

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void CompareTwoTrajectory(Trajectory T0, Trajectory Tc, int distance)
        {
            for (int P = 0; P < T0.Absolute.Count; P++)
            {
                for (int Pc = 0; Pc < Tc.Absolute.Count; Pc++)//compare with T1
                    if (Ex.Distanse(T0.Absolute[P], Tc.Absolute[Pc]) < distance)//如果T0P0 與 T1Pc0 距離小於   則
                    {
                        T0.Abs_keyCount[P]++;
                        break;//若比對正確，一條路徑只會算一次(each Tc counts once)
                    }
            }


            for (int P = 0; P < T0.Relative.Count; P++)
            {
                for (int Pc = 0; Pc < Tc.Relative.Count; Pc++)//compare with T1
                    if (Ex.Distanse(T0.Relative[P], Tc.Relative[Pc]) < distance)//如果T0P0 與 T1Pc0 距離小於   則
                    {
                        T0.Rel_keyCount[P]++;
                        break;//若比對正確，一條路徑只會算一次(each Tc counts once)
                    }
            }


        }


        private void Btn_remove_Click(object sender, RoutedEventArgs e)
        {
            int removeIndex = LV_trajectoryInfo.SelectedIndex;
            if (LV_trajectoryInfo.SelectedIndex < 0)
            {
                MessageBox.Show("please select items");
                return;
            }
            ListViewRemove(removeIndex);
        }

        private void ListViewAdd(TrajectoryInfoAdder adder)
        {
            string[] rtn = new string[2];
            rtn[0] = adder.getObject();
            rtn[1] = adder.getGen();
            LVinfo.Add(rtn);
            this.LV_trajectoryInfo.Items.Add(adder);
            Action action = delegate { };
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Input, action);//不加這個會造成兩個list view item都一樣(怪bug)
        }
        private void ListViewRemove(int index)
        {
            LVinfo.RemoveAt(index);
            this.LV_trajectoryInfo.Items.Remove(LV_trajectoryInfo.Items[index]);
        }
        private string[] ListViewText(int index)
        {
            return LVinfo[index];
        }

        private void Btn_resetPos_Click(object sender, RoutedEventArgs e)
        {
            resetObject();
            ImageUpdate();
        }
    }

    public class TrajectoryInfoAdder
    {
        public TrajectoryInfoAdder(string obj, string gen, SolidColorBrush C1, SolidColorBrush C2)
        {
            Object = obj;
            Gen = gen;
            Color1 = (C1);
            Color2 = (C2);
        }
        public TrajectoryInfoAdder(string obj, string gen, Color C1, Color C2)
        {
            Object = obj;
            Gen = gen;
            Color1 = new SolidColorBrush(C1);
            Color2 = new SolidColorBrush(C2);
        }
        public string getObject()
        {
            return Object;
        }
        public string getGen()
        {
            return Gen;
        }
        public static string Object { get; set; }
        public static string Gen { get; set; }
        public static SolidColorBrush Color1 { get; set; } = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush Color2 { get; set; } = new SolidColorBrush(Colors.Black);
    }

    public static class BitmapSourceConvert
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }
    }
}
