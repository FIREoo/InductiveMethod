﻿using System;
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
using System.Collections.ObjectModel;
using System.ComponentModel;

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
            LV_trajectoryInfo.ItemsSource = TrajectoryInfoDataCollection;

        }
        private void ImageUpdate()
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            demoTask.drawObjectOn(mat);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }


        int handingObj = -1;


        ObservableCollection<TrajectoryInfoData> TrajectoryInfoDataCollection = new ObservableCollection<TrajectoryInfoData>();
        //List<string[]> LVinfo = new List<string[]>();//我不知道怎麼取得 select listView text，所以用這個存

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


            TrajectoryInfoDataCollection.Clear();
            //  LVinfo.Clear();

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
                    obj.thisRoundObjectIndex = obj.index;
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
                obj.thisRoundObjectIndex = -1;
                obj.thisRoundPath.Add(obj.Position);
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
                obj.place();
                obj.thisRoundPath.Clear();
                obj.thisRoundObjectIndex = -1;
                //初始點 也要算一個，因為有可能初始點就剛好是我要的絕對位置
                obj.thisRoundPath.Add(obj.Position);
            }
        }

        #endregion \\---Environment---//

        #region //---nextStage---\\
        private void Btn_nextSegment_Click(object sender, RoutedEventArgs e)
        {
            demoTask.ConfirmSegment();
            for (int o = 0; o < demoTask.DemoObject.Count(); o++)
            {
                MCvScalar black = new MCvScalar(0, 0, 0);
                TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, o, "Abs", black, black, demoTask.DemoObject[o].Color, black));
                TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, o, "Rel", black, black, demoTask.DemoObject[o].Color, black));
            }

            //new segment
            nowSegment++;
            tb_SegNum.Text = "Now Seg : " + nowSegment.ToString();
            //   rndObject();
            ImageUpdate();
        }
        private void Btn_nextGeneration_Click(object sender, RoutedEventArgs e)
        {
            demoTask.ConfirmSegment();
            demoTask.ConfirmGeneration();
            for (int o = 0; o < demoTask.DemoObject.Count(); o++)
            {
                MCvScalar black = new MCvScalar(0, 0, 0);
                TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, o, "Abs", black, black, demoTask.DemoObject[o].Color, black));
                TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, o, "Rel", black, black, demoTask.DemoObject[o].Color, black));
            }
            nowGeneration++;
            tb_GenNum.Text = "Now Gen : " + nowGeneration.ToString();
            nowSegment = 0;
            tb_SegNum.Text = "Now Seg : " + nowSegment.ToString();
            rndObject();
            ImageUpdate();
        }
        #endregion \\---nextStage---//

        #region //---draw replat Path---\\
        private void Btn_DrawSelect_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = LV_trajectoryInfo.SelectedIndex;
            if (LV_trajectoryInfo.SelectedIndex < 0)
            {
                MessageBox.Show("please select items");
                return;
            }

            int Igen = TrajectoryInfoDataCollection[selectIndex].Gen;
            int Iseg = TrajectoryInfoDataCollection[selectIndex].Segment;
            int Iobj = TrajectoryInfoDataCollection[selectIndex].Object;
            string Itra = TrajectoryInfoDataCollection[selectIndex].Trajectory;

            if (Itra == "Abs")
                demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].AbsoluteTrajectory.DrawOn(mat, new Point(0, 0), demoTask.DemoObject[Iobj].Color);
            else if (Itra == "Rel")
                demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].RelativeTrajectory.DrawOn(mat, demoTask.DemoObject[Iobj].Position, demoTask.DemoObject[Iobj].Color, 4, 1);

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);

        }

        private void Btn_DrawChecked_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < TrajectoryInfoDataCollection.Count(); i++)
            {
                if (TrajectoryInfoDataCollection[i].isChecked == true)
                {
                    int Igen = TrajectoryInfoDataCollection[i].Gen;
                    int Iseg = TrajectoryInfoDataCollection[i].Segment;
                    int Iobj = TrajectoryInfoDataCollection[i].Object;
                    string Itra = TrajectoryInfoDataCollection[i].Trajectory;
                    if (Itra == "Abs")
                        demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].AbsoluteTrajectory.DrawOn(mat, new Point(0, 0), demoTask.DemoObject[Iobj].Color);
                    else if (Itra == "Rel")//relative才會有 誰為原點的問題
                    {
                        int interactObject = demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].InteractObjectIndex;
                        //這裡的color要注意，因為可能是別人在動，只是以自己為原點，所以不能畫 自己顏色的路徑，會誤會。
                        demoTask.generations[Igen].segments[Iseg].objectTrajectoryPacks[Iobj].RelativeTrajectory.DrawOn(mat, demoTask.DemoObject[Iobj].Position, demoTask.DemoObject[interactObject].Color, 4, 1);
                    }
                }
            }
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
            ImageUpdate();
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        #endregion \\---draw replat Path---//

        private void Btn_InductiveSegment_Click(object sender, RoutedEventArgs e)
        {

            int segmentIndex = 0;
            demoTask.ZeroAllKeys();
            for (int O = 0; O < demoTask.DemoObject.Count(); O++)
                for (int G = 1; G < demoTask.generations.Count() - 1; G++)//generations 會多一 ，因為示範結束就++，但還沒示範就進到這裡了
                {
                    CompareTwoTrajectory(
                         demoTask.generations[0].segments[segmentIndex].objectTrajectoryPacks[O].AbsoluteTrajectory,
                         demoTask.generations[G].segments[segmentIndex].objectTrajectoryPacks[O].AbsoluteTrajectory,
                         20);
                    CompareTwoTrajectory(
                       demoTask.generations[0].segments[segmentIndex].objectTrajectoryPacks[O].RelativeTrajectory,
                       demoTask.generations[G].segments[segmentIndex].objectTrajectoryPacks[O].RelativeTrajectory,
                       20);
                }

            int threshold = demoTask.generations.Count() - 2;// 示範完會++ 所以少一，互相比較，所以若N代又要全部都是key 會需要N-1個(比喻:雙循環賽 全贏)
            for (int O = 0; O < demoTask.DemoObject.Count(); O++)
            {
                demoTask.generations[0].segments[0].objectTrajectoryPacks[O].AbsoluteTrajectory.DrawKeyOn(mat, new Point(0, 0), threshold, new MCvScalar(55, 5, 200));
                demoTask.generations[0].segments[0].objectTrajectoryPacks[O].RelativeTrajectory.DrawKeyOn(mat, demoTask.DemoObject[O].Position, threshold, new MCvScalar(55, 5, 200), 4, 1);
            }

            //int interactObject = demoTask.generations[0].segments[0].objectTrajectoryPacks[0].InteractObjectIndex;

            //List<Point> path = new List<Point>();
            //for (int k = 0; k < demoTask.generations[0].segments[0].objectTrajectoryPacks[interactObject].AbsoluteTrajectory.PathKeyCount.Count(); k++)
            //{
            //    if (demoTask.generations[0].segments[0].objectTrajectoryPacks[interactObject].AbsoluteTrajectory.PathKeyCount[k] >= threshold)
            //    {
            //        path.Add(demoTask.generations[0].segments[0].objectTrajectoryPacks[interactObject].AbsoluteTrajectory.PathList[k]);

            //    }
            //    //RelativeTrajectory 可能有兩個物件
            //    else if (demoTask.generations[0].segments[0].objectTrajectoryPacks[0].RelativeTrajectory.PathKeyCount[k] >= threshold)
            //    {
            //        Point Pr = new Point(demoTask.generations[0].segments[0].objectTrajectoryPacks[0].RelativeTrajectory.PathList[k].X+ demoTask.DemoObject[interactObject].Position.X,
            //                                                demoTask.generations[0].segments[0].objectTrajectoryPacks[0].RelativeTrajectory.PathList[k].Y + demoTask.DemoObject[interactObject].Position.Y);
            //        path.Add(Pr);
            //    }
            //    else if (demoTask.generations[0].segments[0].objectTrajectoryPacks[1].RelativeTrajectory.PathKeyCount[k] >= threshold)
            //    {
            //        Point Pr = new Point(demoTask.generations[0].segments[0].objectTrajectoryPacks[1].RelativeTrajectory.PathList[k].X + demoTask.DemoObject[interactObject].Position.X,
            //                                               demoTask.generations[0].segments[0].objectTrajectoryPacks[1].RelativeTrajectory.PathList[k].Y + demoTask.DemoObject[interactObject].Position.Y);
            //        path.Add(Pr);
            //    }
            //}
            //CvInvoke.Line(mat, demoTask.DemoObject[interactObject].Position, path[0], demoTask.DemoObject[interactObject].Color, 3);
            //for (int i = 1; i < path.Count(); i++)
            //{
            //    CvInvoke.Line(mat, path[i-1], path[i], demoTask.DemoObject[interactObject].Color, 3);
            //}


            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }


        private void Btn_countKeyPoint_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = LV_trajectoryInfo.SelectedIndex;
            if (LV_trajectoryInfo.SelectedIndex < 0)
            {
                MessageBox.Show("please select items");
                return;
            }
            int Igen = TrajectoryInfoDataCollection[selectIndex].Gen;
            int Iseg = TrajectoryInfoDataCollection[selectIndex].Segment;
            int Iobj = TrajectoryInfoDataCollection[selectIndex].Object;
            string Itra = TrajectoryInfoDataCollection[selectIndex].Trajectory;

            demoTask.ZeroAllKeys();
            for (int G = 0; G < demoTask.generations.Count() - 1; G++)//generations 會多一 ，因為示範結束就++，但還沒示範就進到這裡了
            {
                if (G == Igen) continue;
                for (int S = 0; S < demoTask.generations[0].segments.Count(); S++)//all generations have same count of segments
                    if (Itra == "Abs")
                    {
                        CompareTwoTrajectory(
                        demoTask.generations[Igen].segments[S].objectTrajectoryPacks[Iobj].AbsoluteTrajectory,
                        demoTask.generations[G].segments[S].objectTrajectoryPacks[Iobj].AbsoluteTrajectory,
                        20);
                    }
                    else if (Itra == "Rel")
                    {
                        CompareTwoTrajectory(
                        demoTask.generations[Igen].segments[S].objectTrajectoryPacks[Iobj].RelativeTrajectory,
                        demoTask.generations[G].segments[S].objectTrajectoryPacks[Iobj].RelativeTrajectory,
                        20);
                    }
            }

            //draw
            int threshold = demoTask.generations.Count() - 2;// 示範完會++ 所以少一，互相比較，所以若N代又要全部都是key 會需要N-1個(比喻:雙循環賽 全贏)
            if (Itra == "Abs")
                demoTask.generations[Igen].segments[0].objectTrajectoryPacks[Iobj].AbsoluteTrajectory.DrawKeyOn(mat, new Point(0, 0), threshold, new MCvScalar(55, 5, 200));
            else if (Itra == "Rel")
                demoTask.generations[Igen].segments[0].objectTrajectoryPacks[Iobj].RelativeTrajectory.DrawKeyOn(mat, demoTask.DemoObject[Iobj].Position, threshold, new MCvScalar(55, 5, 200), 4, 1);

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void CompareTwoTrajectory(Trajectory T0, Trajectory Tc, int distance)
        {
            for (int P = 0; P < T0.PathList.Count(); P++)
            {
                for (int Pc = 0; Pc < Tc.PathList.Count(); Pc++)//compare with T1
                    if (Ex.Distanse(T0.PathList[P], Tc.PathList[Pc]) < distance)//如果T0P0 與 T1Pc0 距離小於   則
                    {
                        T0.PathKeyCount[P]++;
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
            TrajectoryInfoDataCollection.RemoveAt(removeIndex);
        }

    }

    public class TrajectoryInfoData : INotifyPropertyChanged
    {
        bool _check;
        int _gen;
        int _seg;
        int _obj;
        string _tra;

        public TrajectoryInfoData(int gen, int segment, int obj, string trajectory)
        {
            _check = false;
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;
            Color1 = new SolidColorBrush(Colors.Black);
            Color2 = new SolidColorBrush(Colors.Black);
            Color3 = new SolidColorBrush(Colors.Black);
            Color4 = new SolidColorBrush(Colors.Black);
        }
        public TrajectoryInfoData(int gen, int segment, int obj, string trajectory, SolidColorBrush C1, SolidColorBrush C2, SolidColorBrush C3, SolidColorBrush C4)
        {
            _check = false;
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;
            Color1 = (C1);
            Color2 = (C2);
            Color3 = (C3);
            Color4 = (C4);
        }
        public TrajectoryInfoData(int gen, int segment, int obj, string trajectory, Color C1, Color C2, Color C3, Color C4)
        {
            _check = false;
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;

            Color1 = new SolidColorBrush(C1);
            Color2 = new SolidColorBrush(C2);
            Color3 = new SolidColorBrush(C3);
            Color4 = new SolidColorBrush(C4);
        }
        public TrajectoryInfoData(int gen, int segment, int obj, string trajectory, MCvScalar C1, MCvScalar C2, MCvScalar C3, MCvScalar C4)
        {
            _check = false;
            Gen = gen;
            Segment = segment;
            Object = obj;
            Trajectory = trajectory;
            Color1 = new SolidColorBrush(Color.FromRgb((byte)C1.V2, (byte)C1.V1, (byte)C1.V0));
            Color2 = new SolidColorBrush(Color.FromRgb((byte)C2.V2, (byte)C2.V1, (byte)C2.V0));
            Color3 = new SolidColorBrush(Color.FromRgb((byte)C3.V2, (byte)C3.V1, (byte)C3.V0));
            Color4 = new SolidColorBrush(Color.FromRgb((byte)C4.V2, (byte)C4.V1, (byte)C4.V0));
        }
        public bool isChecked
        {
            set
            {
                _check = value;
                NotifyPropertyChanged("isChecked");
            }
            get { return _check; }
        }
        public int Gen
        {
            set
            {
                _gen = value;
                NotifyPropertyChanged("Gen");
            }
            get { return _gen; }
        }
        public int Segment
        {
            set
            {
                _seg = value;
                NotifyPropertyChanged("Segment");
            }
            get { return _seg; }
        }
        public int Object
        {
            set
            {
                _obj = value;
                NotifyPropertyChanged("Object");
            }
            get { return _obj; }
        }
        public string Trajectory
        {
            set
            {
                _tra = value;
                NotifyPropertyChanged("Trajectory");
            }
            get { return _tra; }
        }

        public SolidColorBrush Color1 { get; set; } = new SolidColorBrush(Colors.Black);
        public SolidColorBrush Color2 { get; set; } = new SolidColorBrush(Colors.Black);
        public SolidColorBrush Color3 { get; set; } = new SolidColorBrush(Colors.Black);
        public SolidColorBrush Color4 { get; set; } = new SolidColorBrush(Colors.Black);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }
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
