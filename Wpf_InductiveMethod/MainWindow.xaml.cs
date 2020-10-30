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
using InductiveMethod;

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
        List<int> objectLayerKey = new List<int>();
        private void ImageUpdate()
        {
            mat.SetTo(new byte[600 * 600 * 3]);
            demoTask.drawObjectOn(mat, objectLayerKey);
            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }

        int thisSegmentObj = -1;
        int handingObject = -1;

        ObservableCollection<TrajectoryInfoData> TrajectoryInfoDataCollection = new ObservableCollection<TrajectoryInfoData>();
        //List<string[]> LVinfo = new List<string[]>();//我不知道怎麼取得 select listView text，所以用這個存

        Mat mat = new Mat(600, 600, DepthType.Cv8U, 3);
        int nowGeneration = 0;
        int nowSegment = 0;
        DemoTask demoTask;
        private void Button_start_Click(object sender, RoutedEventArgs e)
        {//start new task

            //initial
            List<InteractObject> demoObject = new List<InteractObject>();
            demoObject.Add(new InteractObject(0));
            demoObject[0].Radius = 40;
            demoObject[0].Shape = InteractObject.Type.square;
            demoObject[0].Color = new MCvScalar(200, 50, 50);
            objectLayerKey.Add(0);

            demoObject.Add(new InteractObject(1));
            demoObject[1].Radius = 40;
            demoObject[1].Shape = InteractObject.Type.circle;
            demoObject[1].Color = new MCvScalar(50, 200, 50);
            objectLayerKey.Add(1);

            demoTask = new DemoTask(demoObject);

            TrajectoryInfoDataCollection.Clear();
            //  LVinfo.Clear();

            nowGeneration = 0;
            nowSegment = 0;
            tb_GenNum.Text = "Now Gen : " + nowGeneration.ToString();
            tb_SegNum.Text = "Now Seg : " + nowSegment.ToString();

            rndObject();
            ImageUpdate();
        }

        #region //---Environment---\\
        void bringUpIndex(ref List<int> list, int target)
        {
            int Ti;
            for (Ti = 0; Ti < list.Count(); Ti++)
            {
                if (list[Ti] == target)
                    break;
            }
            for (int i = 0; i < Ti; i++)
            {
                list[Ti - i] = list[Ti - i - 1];
            }
            list[0] = target;
        }

        private void Image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            for (int i = 0; i < objectLayerKey.Count(); i++)
            {
                if (demoTask.environment.DemoObject[objectLayerKey[i]].isInArea(e.GetPosition(image1)))
                {
                    handingObject = objectLayerKey[i];
                    break;//debug 避免同時兩個一起按到(有重疊)
                }
            }
            Console.WriteLine("pick:"+handingObject.ToString());

            if (handingObject == -1)//亂點
                return;

            if (thisSegmentObj != -1 && thisSegmentObj != demoTask.environment.DemoObject[handingObject].index)
            {
                Btn_nextSegment_Click(null, null);
            }

            demoTask.environment.DemoObject[handingObject].pick();
            demoTask.environment.DemoObject[handingObject].thisRoundPath.Add(e.GetPosition(image1).toDraw());

            bringUpIndex(ref objectLayerKey, demoTask.environment.DemoObject[handingObject].index);

            ImageUpdate();
        }
        private void Image1_MouseMove(object sender, MouseEventArgs e)
        {
            var P = e.GetPosition(image1);
            foreach (InteractObject obj in demoTask.environment.DemoObject)
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
            foreach (InteractObject obj in demoTask.environment.DemoObject)
            {
                if (obj.isInArea(e.GetPosition(image1)))
                {
                    obj.place();
                    obj.thisRoundPath.Add(e.GetPosition(image1).toDraw());
                }
            }

            thisSegmentObj = handingObject;
            handingObject = -1;
            ImageUpdate();
        }
        private void Btn_abortThisDemoPath_Click(object sender, RoutedEventArgs e)
        {
            foreach (InteractObject obj in demoTask.environment.DemoObject)
            {
                obj.place();
                obj.thisRoundPath.Clear();
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
            foreach (InteractObject obj in demoTask.environment.DemoObject)
            {
                obj.Position = new Point(rnd.Next(50, 550), rnd.Next(50, 550));
                obj.place();
                obj.thisRoundPath.Clear();
                //obj.thisRoundObjectIndex = -1;
                //初始點 也要算一個，因為有可能初始點就剛好是我要的絕對位置
                obj.thisRoundPath.Add(obj.Position);
            }
        }

        #endregion \\---Environment---//

        #region //---nextStage---\\
        private void Btn_nextSegment_Click(object sender, RoutedEventArgs e)
        {
            demoTask.ConfirmSegment(thisSegmentObj);

            //add listView UI
            MCvScalar black = new MCvScalar(0, 0, 0);
            int thisSegObj = thisSegmentObj;// demoTask.generations.Last().segments.Last().InteractObjectIndex;

            TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, thisSegObj, "Abs", black, black, demoTask.environment.DemoObject[thisSegObj].Color, black));
            for (int o = 0; o < demoTask.environment.DemoObject.Count(); o++)
            {
                TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, o, "Rel", black, black, demoTask.environment.DemoObject[o].Color, black));
            }

            //new segment
            thisSegmentObj = -1;
            nowSegment++;
            tb_SegNum.Text = "Now Seg : " + nowSegment.ToString();

            ImageUpdate();
        }
        private void Btn_nextGeneration_Click(object sender, RoutedEventArgs e)
        {
            demoTask.ConfirmSegment(thisSegmentObj);

            //add listView UI
            MCvScalar black = new MCvScalar(0, 0, 0);
            int thisSegObj = thisSegmentObj;//demoTask.generations.Last().segments.Last().InteractObjectIndex;

            TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, thisSegObj, "Abs", black, black, demoTask.environment.DemoObject[thisSegObj].Color, black));
            for (int o = 0; o < demoTask.environment.DemoObject.Count(); o++)
            {
                TrajectoryInfoDataCollection.Add(new TrajectoryInfoData(nowGeneration, nowSegment, o, "Rel", black, black, demoTask.environment.DemoObject[o].Color, black));
            }

            demoTask.ConfirmGeneration();

            thisSegmentObj = -1;
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
                demoTask.generations[Igen].segments[Iseg].AbsoluteTrajectory.DrawOn(mat, new Point(0, 0), demoTask.environment.DemoObject[Iobj].Color);
            else if (Itra == "Rel")
                demoTask.generations[Igen].segments[Iseg].RelativeTrajectory[Iobj].DrawOn(mat, demoTask.environment.DemoObject[Iobj].Position, demoTask.environment.DemoObject[Iobj].Color, 4, 1);

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
                        demoTask.generations[Igen].segments[Iseg].AbsoluteTrajectory.DrawOn(mat, new Point(0, 0), demoTask.environment.DemoObject[Iobj].Color);
                    else if (Itra == "Rel")//relative才會有 誰為原點的問題
                    {
                        int interactObject = demoTask.generations[Igen].segments[Iseg].InteractObjectIndex;
                        //這裡的color要注意，因為可能是別人在動，只是以自己為原點，所以不能畫 自己顏色的路徑，會誤會。
                        demoTask.generations[Igen].segments[Iseg].RelativeTrajectory[Iobj].DrawOn(mat, demoTask.environment.DemoObject[Iobj].Position, demoTask.environment.DemoObject[interactObject].Color, 4, 1);
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
            demoTask.ZeroAllKeys();
            int segmentIndex = 0;
            for (; segmentIndex < demoTask.generations[0].segments.Count(); segmentIndex++)
            {
                for (int G = 1; G < demoTask.generations.Count() - 1; G++)//generations 會多一 ，因為示範結束就++，但還沒示範就進到這裡了
                {
                    //ABS 比較
                    CompareTwoTrajectory(
                        demoTask.generations[0].segments[segmentIndex].AbsoluteTrajectory,
                        demoTask.generations[G].segments[segmentIndex].AbsoluteTrajectory,
                        20);
                    //Rel 比較
                    for (int O = 0; O < demoTask.environment.DemoObject.Count(); O++)
                    {
                        CompareTwoTrajectory(
                           demoTask.generations[0].segments[segmentIndex].RelativeTrajectory[O],
                           demoTask.generations[G].segments[segmentIndex].RelativeTrajectory[O],
                           20);
                    }
                    //注意!! key path 只存到 generations[0]
                }

                //draw keyPath on UI
                int threshold = demoTask.generations.Count() - 2;// 示範完會++ 所以少一，互相比較，所以若N代又要全部都是key 會需要N-1個(比喻:雙循環賽 全贏)
                demoTask.generations[0].segments[segmentIndex].AbsoluteTrajectory.DrawKeyOn(mat, new Point(0, 0), threshold, new MCvScalar(55, 5, 200));
                for (int O = 0; O < demoTask.environment.DemoObject.Count(); O++)
                    demoTask.generations[0].segments[segmentIndex].RelativeTrajectory[O].DrawKeyOn(mat, demoTask.environment.DemoObject[O].Position, threshold, new MCvScalar(55, 5, 200), 4, 1);

                int interactObject = demoTask.generations[0].segments[segmentIndex].InteractObjectIndex;

                List<Point> path = new List<Point>();
                for (int k = 0; k < demoTask.generations[0].segments[segmentIndex].AbsoluteTrajectory.PathKeyMatch.Count(); k++)//PathKeyCount.Count() 都會一樣
                {
                    if (demoTask.generations[0].segments[segmentIndex].AbsoluteTrajectory.PathKeyMatch[k] >= threshold)
                    {
                        path.Add(demoTask.generations[0].segments[segmentIndex].AbsoluteTrajectory.PathList[k]);
                    }
                    else
                    {    //RelativeTrajectory 有多個物件
                        for (int O = 0; O < demoTask.environment.DemoObject.Count(); O++)
                        {
                            if (demoTask.generations[0].segments[segmentIndex].RelativeTrajectory[O].PathKeyMatch[k] >= threshold)
                            {
                                Point Pr = new Point(demoTask.generations[0].segments[segmentIndex].RelativeTrajectory[O].PathList[k].X + demoTask.environment.DemoObject[O].Position.X,
                                                               demoTask.generations[0].segments[segmentIndex].RelativeTrajectory[O].PathList[k].Y + demoTask.environment.DemoObject[O].Position.Y);
                                path.Add(Pr);
                                break;
                            }
                        }
                    }
                }

                //draw
                CvInvoke.Line(mat, demoTask.environment.DemoObject[interactObject].Position, path[0], demoTask.environment.DemoObject[interactObject].Color, 2);
                for (int i = 1; i < path.Count(); i++)
                {
                    CvInvoke.Line(mat, path[i - 1], path[i], demoTask.environment.DemoObject[interactObject].Color, 2);
                }
            }

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
                        demoTask.generations[Igen].segments[S].AbsoluteTrajectory,
                        demoTask.generations[G].segments[S].AbsoluteTrajectory,
                        20);
                    }
                    else if (Itra == "Rel")
                    {
                        CompareTwoTrajectory(
                        demoTask.generations[Igen].segments[S].RelativeTrajectory[Iobj],
                        demoTask.generations[G].segments[S].RelativeTrajectory[Iobj],
                        20);
                    }
            }

            //draw
            int threshold = demoTask.generations.Count() - 2;// 示範完會++ 所以少一，互相比較，所以若N代又要全部都是key 會需要N-1個(比喻:雙循環賽 全贏)
            if (Itra == "Abs")
                demoTask.generations[Igen].segments[0].AbsoluteTrajectory.DrawKeyOn(mat, new Point(0, 0), threshold, new MCvScalar(55, 5, 200));
            else if (Itra == "Rel")
                demoTask.generations[Igen].segments[0].RelativeTrajectory[Iobj].DrawKeyOn(mat, demoTask.environment.DemoObject[Iobj].Position, threshold, new MCvScalar(55, 5, 200), 4, 1);

            image1.Source = BitmapSourceConvert.ToBitmapSource(mat);
        }
        private void CompareTwoTrajectory(Trajectory T0, Trajectory Tc, int distance)
        {
            for (int P = 0; P < T0.PathList.Count(); P++)
            {
                for (int Pc = 0; Pc < Tc.PathList.Count(); Pc++)//compare with T1
                    if (Ex.Distanse(T0.PathList[P], Tc.PathList[Pc]) < distance)//如果T0P0 與 T1Pc0 距離小於   則
                    {
                        T0.PathKeyMatch[P]++;
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
