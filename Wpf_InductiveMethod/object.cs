using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using Point = System.Drawing.Point;
using System.Diagnostics;

namespace Wpf_InductiveMethod
{
    public class DemoTask
    {
        public List<Generation> generations = new List<Generation>();
        public List<InteractObject> DemoObject { get; } = new List<InteractObject>();
        public DemoTask(List<InteractObject> demoObject)
        {
            DemoObject = demoObject;
            generations = new List<Generation>();
            generations.Add(new Generation());
        }
        //object function
        public void drawObjectOn(IInputOutputArray mat)
        {
            foreach (InteractObject obj in DemoObject)
                obj.drawOn(mat);
        }
        public void ConfirmGeneration()
        {
            // ConfirmSegment();
            generations.Add(new Generation());
        }
        public void ConfirmSegment()
        {
            Segment addin = null;
            int interactObjectIndex = -1;
            for (int i = 0; i < DemoObject.Count(); i++)
            {
                if (DemoObject[i].thisRoundObjectIndex != -1)
                {
                    addin = new Segment(DemoObject[i].thisRoundObjectIndex, DemoObject.Count());
                    interactObjectIndex = DemoObject[i].thisRoundObjectIndex;
                    break;
                }
            }

            if (addin == null || interactObjectIndex == -1)
                Trace.WriteLine("Segment 裡面的 thisRoundObjectIndex 不該都為-1");

            //絕對路徑 各加各的
            for (int i = 0; i < DemoObject.Count(); i++)
            {
                for (int p = 0; p < DemoObject[i].thisRoundPath.Count(); p++)
                {
                    addin.objectTrajectoryPacks[i].AbsoluteTrajectory.AddPoint(DemoObject[i].thisRoundPath[p]);
                }
            }

            //相對路徑 都只管interact object 的路徑 只是以不同物件為原點 
            for (int i = 0; i < DemoObject.Count(); i++)
            {
                for (int p = 0; p < DemoObject[interactObjectIndex].thisRoundPath.Count(); p++)
                {
                    //要以自己起始點為原點  不能是自己的終點
                    Point Pr = new Point(DemoObject[interactObjectIndex].thisRoundPath[p].X - DemoObject[i].thisRoundPath[0].X, DemoObject[interactObjectIndex].thisRoundPath[p].Y - DemoObject[i].thisRoundPath[0].Y);
                    addin.objectTrajectoryPacks[i].RelativeTrajectory.AddPoint(Pr);
                }
            }

            //清除 thisRoundPath
            for (int i = 0; i < DemoObject.Count(); i++)
                DemoObject[i].thisRoundPath.Clear();//提供新的給 下次demo用

            generations.Last().segments.Add(addin);

        }

        public void ZeroAllKeys()
        {
            foreach (Generation G in generations)
                foreach (Segment S in G.segments)
                    foreach (ObjectTrajectoryPack OTP in S.objectTrajectoryPacks)
                    {
                        OTP.AbsoluteTrajectory.ZeroKey();
                        OTP.RelativeTrajectory.ZeroKey();
                    }
        }


    }
    public class Generation
    {
        public List<Segment> segments = new List<Segment>();

    }


    public class Segment
    {
        public List<ObjectTrajectoryPack> objectTrajectoryPacks = new List<ObjectTrajectoryPack>();
        public Segment(int interactObjectIndex, int objectCount)
        {
            for (int i = 0; i < objectCount; i++)
                objectTrajectoryPacks.Add(new ObjectTrajectoryPack(i, interactObjectIndex));
        }
    }


    public class InteractObject
    {
        public enum Type
        {
            circle = 0,
            square = 1
        }
        public int index = -1;
        public Type Shape = Type.circle;
        public Point Position = new Point();
        public int Radius = 30;
        public MCvScalar Color { get; set; } = new MCvScalar(150, 150, 150);

        private bool _isPick = false;

        public InteractObject(int i)
        {
            index = i;
        }
        public bool isPick
        {
            get { return _isPick; }
        }

        public void pick()
        {
            _isPick = true;
        }
        public void place()
        {
            _isPick = false;
        }
        public bool isInArea(Point point)
        {
            int distX = (point.X) - (Position.X);
            int distY = (point.Y) - (Position.Y);

            if (Shape == Type.circle)
            {
                double result = Math.Sqrt(distX * distX + distY * distY);
                if (result <= 30)
                    return true;
                else
                    return false;
            }
            else if (Shape == Type.square)
            {
                if (point.X < Position.X + Radius / 2 && point.X > Position.X - Radius / 2 && point.Y > Position.Y - Radius / 2 && point.Y < Position.Y + Radius / 2)
                    return true;
                else
                    return false;
            }

            return false;
        }
        public bool isInArea(System.Windows.Point wPoint)
        {
            Point point = new Point((int)wPoint.X, (int)wPoint.Y);
            return isInArea(point);
        }
        public void drawOn(IInputOutputArray img)
        {

            if (Shape == Type.circle)
            {
                CvInvoke.Circle(img, Position, Radius, Color, -1);
                if (isPick)
                    CvInvoke.Circle(img, Position, Radius, new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
            }
            else if (Shape == Type.square)
            {
                CvInvoke.Rectangle(img, new System.Drawing.Rectangle(Position.X - Radius / 2, Position.Y - Radius / 2, Radius, Radius), Color, -1);
                if (isPick)
                {
                    CvInvoke.Line(img, new Point(Position.X - Radius / 2, Position.Y - Radius / 2), new Point(Position.X - Radius / 2, Position.Y + Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                    CvInvoke.Line(img, new Point(Position.X + Radius / 2, Position.Y + Radius / 2), new Point(Position.X - Radius / 2, Position.Y + Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                    CvInvoke.Line(img, new Point(Position.X + Radius / 2, Position.Y + Radius / 2), new Point(Position.X + Radius / 2, Position.Y - Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                    CvInvoke.Line(img, new Point(Position.X - Radius / 2, Position.Y - Radius / 2), new Point(Position.X + Radius / 2, Position.Y - Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                }
            }
            CvInvoke.PutText(img, index.ToString(), Position, FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0), 2);

        }

        //--Trajectory--//
        //public List<Trajectory> trajectory = new List<Trajectory>();

        /// <summary>用於儲存目前圖上的路徑</summary>
        public List<Point> thisRoundPath = new List<Point>();
        /// <summary>用於儲存目前圖上的互動物件(注意，一個segment只會有一個互動物件)</summary>
        public int thisRoundObjectIndex = -1;

    }

    public class ObjectTrajectoryPack
    {
        public int OriginObjectIndex = -1;
        public int InteractObjectIndex = -1;

        public ObjectTrajectoryPack(int originIndex, int interactObjectIndex)
        {
            OriginObjectIndex = originIndex;
            InteractObjectIndex = interactObjectIndex;
        }
        public Trajectory AbsoluteTrajectory = new Trajectory();
        public Trajectory RelativeTrajectory = new Trajectory();
    }
    public class Trajectory
    {
        public List<Point> PathList = new List<Point>();
        public List<int> PathKeyCount = new List<int>();

        public void AddPoint(Point point)
        {
            PathList.Add(point);
            PathKeyCount.Add(0);
        }
        public void Clear()
        {
            PathList.Clear();
            PathKeyCount.Clear();
        }
        public void ClearKey()
        {
            PathKeyCount.Clear();
        }
        public void ZeroKey()
        {
            for (int i = 0; i < PathKeyCount.Count(); i++)
                PathKeyCount[i] = 0;
        }

        public void DrawOn(IInputOutputArray img, Point RelPoint, MCvScalar Color, int Radius = 3, int thickness = -1)
        {
            foreach (Point P in PathList)
            {
                Point Pr = new Point(P.X + RelPoint.X, P.Y + RelPoint.Y);
                CvInvoke.Circle(img, Pr, Radius, Color, thickness);
            }
        }
        /// <summary></summary>

        public void DrawKeyOn(IInputOutputArray img, Point RelPoint, int thres, MCvScalar Color, int Radius = 3, int thickness = -1)
        {
            for (int i = 0; i < PathKeyCount.Count; i++)
            {
                if (PathKeyCount[i] >= thres)
                {
                    Point Pr = new Point(PathList[i].X + RelPoint.X, PathList[i].Y + RelPoint.Y);
                    CvInvoke.Circle(img, Pr, Radius, Color, thickness);
                }
            }
        }

    }



    static class Ex
    {
        static public Point toDraw(this System.Windows.Point wPoint)
        {
            return new Point((int)wPoint.X, (int)wPoint.Y);
        }
        static public int toInt(this string str)
        {
            return int.Parse(str);
        }
        static public int Distanse(Point P1, Point P2)
        {
            int dx = P2.X - P1.X;
            int dy = P2.Y - P1.Y;

            double D = Math.Sqrt((dx * dx) + (dy * dy));
            return (int)D;
        }
    }
}
