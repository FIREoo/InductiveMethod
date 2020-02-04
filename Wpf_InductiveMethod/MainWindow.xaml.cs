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
            demoTask.drawObjectOn(mat);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }


        int handingObj = -1;
        List<string[]> LVinfo = new List<string[]>();//我不知道怎麼取得 select listView text，所以用這個存

        Mat mat = new Mat(600, 600, DepthType.Cv8U, 3);
        int nowGeneration = 0;
        int nowSegment = 0;
        DemoTask demoTask;
        private void Button_start_Click(object sender, RoutedEventArgs e)
        {//start new task

            List<InteractObject> demoObject = new List<InteractObject>();
            demoObject.Add(new InteractObject(0));
            demoObject[0].Radius = 40;
            demoObject[0].Shape = InteractObject.Type.square;
            demoObject[0].Color = new MCvScalar(200, 50, 50);


            demoObject.Add(new InteractObject(1));
            demoObject[1].Radius = 40;
            demoObject[1].Shape = InteractObject.Type.circle;
            demoObject[1].Color = new MCvScalar(50, 200, 50);


            demoTask = new DemoTask(demoObject);


            this.LV_trajectoryInfo.Items.Clear();
            LVinfo.Clear();

            nowGeneration = 0;
            nowSegment = 0;
            tb_GenNum.Text = "Now Gen : " + nowGeneration.ToString();
            tb_SegNum.Text = "Now Gen : " + nowSegment.ToString();

            rndObject();
            ImageUpdate();
        }

        #region //---Environment---\\
        private void Image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (InteractObject obj in demoTask.DemoObject)
            {
                if (obj.isInArea(e.GetPosition(image1)))
                {
                    handingObj = obj.index;
                    obj.pick();
                    obj.thisRoundPath.Add(e.GetPosition(image1).toDraw());
                    break;//debug 避免同時兩個一起按到
                }
                else//debug
                    obj.place();
            }
            ImageUpdate();
        }

        private void Image1_MouseMove(object sender, MouseEventArgs e)
        {
            var P = e.GetPosition(image1);
            foreach (InteractObject obj in demoTask.DemoObject)
            {
                if (obj.isPick)
                {
                    obj.Position = new Point((int)P.X, (int)P.Y);
                    obj.thisRoundPath.Add(e.GetPosition(image1).toDraw());
                    ImageUpdate();//放這裡，拖拉時浪費效能，但沒事時省效能
                }
            }

        }

        private void Image1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (InteractObject obj in demoTask.DemoObject)
            {
                if (obj.isInArea(e.GetPosition(image1)))
                {
                    obj.place();
                    obj.thisRoundPath.Add(e.GetPosition(image1).toDraw());
                }
            }
            ImageUpdate();



            handingObj = -1;
        }
        private void Btn_abortThisDemoPath_Click(object sender, RoutedEventArgs e)
        {
            foreach (InteractObject obj in demoTask.DemoObject)
            {
                obj.place();
                obj.thisRoundPath.Clear();
            }
        }
        private void Btn_resetPos_Click(object sender, RoutedEventArgs e)
        {
            rndObject();
            ImageUpdate();
        }
        private void rndObject()
        {
            Random rnd = new Random();
            foreach (InteractObject obj in demoTask.DemoObject)
            {
                obj.Position = new Point(rnd.Next(50, 550), rnd.Next(50, 550));
                //初始點 也要算一個，因為有可能初始點就剛好是我要的絕對位置
                obj.thisRoundPath.Clear();
                obj.thisRoundPath.Add(obj.Position);
            }
        }

        #endregion \\---Environment---//
        private void Btn_nextSegment_Click(object sender, RoutedEventArgs e)
        {
            demoTask.ConfirmSegment();
            for (int o = 0; o < demoTask.DemoObject.Count(); o++)
            {
                ListViewAdd(new TrajectoryInfoAdder(nowGeneration.ToString(), nowSegment.ToString(), o.ToString(), "Abs"));
                ListViewAdd(new TrajectoryInfoAdder(nowGeneration.ToString(), nowSegment.ToString(), o.ToString(), "Rel"));
            }

            //new segment
            nowSegment++;
            tb_SegNum.Text = "Now Gen : " + nowSegment.ToString();
            rndObject();
            ImageUpdate();
        }
        private void Btn_nextGeneration_Click(object sender, RoutedEventArgs e)
        {
            demoTask.ConfirmGeneration();
            for (int o = 0; o < demoTask.DemoObject.Count(); o++)
            {
                ListViewAdd(new TrajectoryInfoAdder(nowGeneration.ToString(), nowSegment.ToString(), o.ToString(), "Abs"));
                ListViewAdd(new TrajectoryInfoAdder(nowGeneration.ToString(), nowSegment.ToString(), o.ToString(), "Rel"));
            }
            nowGeneration++;
            tb_GenNum.Text = "Now Gen : " + nowGeneration.ToString();
        }

        #region //---draw replat Path---\\
        private void Btn_DrawSelect_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = LV_trajectoryInfo.SelectedIndex;
            if (LV_trajectoryInfo.SelectedIndex < 0)
            {
                MessageBox.Show("please select items");
                return;
            }

            string[] text = ListViewText(selectIndex);
            int Igen = text[0].toInt();
            int Iseg = text[1].toInt();
            int Iobj = text[2].toInt();
            string Itra = text[3];

            if (Itra == "Abs")
                demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].AbsoluteTrajectory.DrawOn(mat, new Point(0, 0), demoTask.DemoObject[Iobj].Color);
            else if (Itra == "Rel")
                demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].RelativeTrajectory.DrawOn(mat, demoTask.DemoObject[Iobj].Position, demoTask.DemoObject[Iobj].Color,4,1);

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);

        }



        private void Btn_replay_all_Click(object sender, RoutedEventArgs e)
        {
            //mat.SetTo(new byte[600 * 600 * 3]);
            //foreach (InteractObject o in demoObject)
            //    foreach (Trajectory t in o.trajectory)
            //        t.drawOn(mat, o.Position, o.Color);

            //image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void Btn_clearImage_Click(object sender, RoutedEventArgs e)
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void But_clearPath_Click(object sender, RoutedEventArgs e)
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            ImageUpdate();
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        #endregion \\---draw replat Path---//

        private void Btn_check_Click(object sender, RoutedEventArgs e)
        {

        }



        private void Btn_countKeyPoint_Click(object sender, RoutedEventArgs e)
        {
            ////compare T0 with Tc
            //for (int Tc = 1; Tc < demoObject[0].trajectory.Count; Tc++)
            //    CompareTwoTrajectory(demoObject[0].trajectory[0], demoObject[0].trajectory[Tc], 20);

            //mat.SetTo(new byte[600 * 600 * 3]);

            ////draw path
            //foreach (InteractObject o in demoObject)
            //    foreach (Trajectory t in o.trajectory)
            //        t.drawOn(mat, o.Position, o.Color);


            ////draw key point
            //demoObject[0].trajectory[0].drawKeyOn(1, mat, demoObject[0].Position, new MCvScalar(30, 30, 200));
            ////obj[1].trajectory[0].drawKeyOn(1, mat, obj[1].Center, new MCvScalar(30, 30, 200));

            //image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void CompareTwoTrajectory(Trajectory T0, Trajectory Tc, int distance)
        {
            //for (int P = 0; P < T0.Absolute.Count; P++)
            //{
            //    for (int Pc = 0; Pc < Tc.Absolute.Count; Pc++)//compare with T1
            //        if (Ex.Distanse(T0.Absolute[P], Tc.Absolute[Pc]) < distance)//如果T0P0 與 T1Pc0 距離小於   則
            //        {
            //            T0.Abs_keyCount[P]++;
            //            break;//若比對正確，一條路徑只會算一次(each Tc counts once)
            //        }
            //}


            //for (int P = 0; P < T0.Relative.Count; P++)
            //{
            //    for (int Pc = 0; Pc < Tc.Relative.Count; Pc++)//compare with T1
            //        if (Ex.Distanse(T0.Relative[P], Tc.Relative[Pc]) < distance)//如果T0P0 與 T1Pc0 距離小於   則
            //        {
            //            T0.Rel_keyCount[P]++;
            //            break;//若比對正確，一條路徑只會算一次(each Tc counts once)
            //        }
            //}


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


        private void ListViewAdd(TrajectoryInfoAdder add)
        {
            string[] rtn = new string[4];
            rtn[0] = TrajectoryInfoAdder.Gen;
            rtn[1] = TrajectoryInfoAdder.Segment;
            rtn[2] = TrajectoryInfoAdder.Object;
            rtn[3] = TrajectoryInfoAdder.Trajectory;
            LVinfo.Add(rtn);
            this.LV_trajectoryInfo.Items.Add(add);
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


    }

    public class TrajectoryInfoAdder
    {
        public TrajectoryInfoAdder(string gen, string segment, string obj, string trajectory)
        {
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;
            Color1 = new SolidColorBrush(Colors.Black);
            Color2 = new SolidColorBrush(Colors.Black);
            Color3 = new SolidColorBrush(Colors.Black);
            Color4 = new SolidColorBrush(Colors.Black);
        }
        public TrajectoryInfoAdder(string gen, string segment, string obj, string trajectory, SolidColorBrush C1, SolidColorBrush C2, SolidColorBrush C3, SolidColorBrush C4)
        {
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;
            Color1 = (C1);
            Color2 = (C2);
            Color3 = (C3);
            Color4 = (C4);
        }
        public TrajectoryInfoAdder(string gen, string segment, string obj, string trajectory, Color C1, Color C2, Color C3, Color C4)
        {
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;

            Color1 = new SolidColorBrush(C1);
            Color2 = new SolidColorBrush(C2);
            Color3 = new SolidColorBrush(C3);
            Color4 = new SolidColorBrush(C4);
        }

        public static string Gen { get; set; }
        public static string Segment { get; set; }
        public static string Object { get; set; }
        public static string Trajectory { get; set; }

        public static SolidColorBrush Color1 { get; set; } = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush Color2 { get; set; } = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush Color3 { get; set; } = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush Color4 { get; set; } = new SolidColorBrush(Colors.Black);
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
