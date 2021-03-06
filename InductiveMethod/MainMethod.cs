﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using Point = System.Drawing.Point;
using System.Diagnostics;

namespace InductiveMethod
{
    public class DemoTask
    {
        /// <summary>environment of Task, including InteractObject</summary>
        public Environment environment;// = new Environment();
        /// <summary>Generations of Task</summary>
        public List<Generation> generations = new List<Generation>();

        public DemoTask(List<InteractObject> demoObject, bool isPointer = true)
        {
            environment = new Environment(demoObject, isPointer);
            generations = new List<Generation>();
            generations.Add(new Generation());
        }

        //Draw
        public void drawObjectOn(IInputOutputArray mat)
        {
            foreach (InteractObject obj in environment.DemoObject)
                obj.drawOn(mat);
        }
        public void drawObjectOn(IInputOutputArray mat, List<int> layerList)
        {
            for (int i = layerList.Count() - 1; i >= 0; i--)
            {
                environment.DemoObject[layerList[i]].drawOn(mat);
            }
        }

        public void ConfirmGeneration()
        {
            if (environment.DemoObject == null)
                throw new Exception("Genearation沒有初始化");
            generations.Add(new Generation());
        }
        public void ConfirmSegment(int index)
        {
            Console.WriteLine("ConfirmSegment with object:" + index.ToString());
            Segment addin = null;
            int interactObjectIndex = -1;

            addin = new Segment(index, environment.DemoObject.Count());
            interactObjectIndex = index;

            if (addin == null || interactObjectIndex == -1)
                throw new Exception("Segment 裡面的 thisRoundObjectIndex 不該都為-1");

            //絕對路徑 各加各的//應該只需要加interact object就可以了//另一個不管，因為只能有一個物件移動
            for (int p = 0; p < environment.thisSegPath.Count(); p++)
            {
                //addin.objectTrajectoryPacks[interactObjectIndex].AbsoluteTrajectory.AddPoint(DemoObject[interactObjectIndex].thisRoundPath[p]);
                addin.AbsoluteTrajectory.AddPoint(environment.thisSegPath[p]);
            }

            //相對路徑 都只管interact object 的路徑 只是以不同物件為原點
            for (int i = 0; i < environment.DemoObject.Count(); i++)
            {
                if (i == environment.thisSegObjectIndex)//如果是自己就跟 自己的原點比
                    for (int p = 0; p < environment.thisSegPath.Count(); p++)
                    {
                        //要以自己起始點為原點  不能是自己的終點，所以不能是environment.DemoObject[i].Position
                        Point Pr = new Point(environment.thisSegPath[p].X - environment.thisSegPath[0].X, environment.thisSegPath[p].Y - environment.thisSegPath[0].Y);
                        addin.RelativeTrajectory[i].AddPoint(Pr);
                    }
                else//其他的 因為不會動，所以就跟目前位置做比較就可以了
                    for (int p = 0; p < environment.thisSegPath.Count(); p++)
                    {
                        Point Pr = new Point(environment.thisSegPath[p].X - environment.DemoObject[i].Position.X, environment.thisSegPath[p].Y - environment.DemoObject[i].Position.Y);
                        addin.RelativeTrajectory[i].AddPoint(Pr);
                    }
            }

            //清除 thisRoundPath
            environment.thisSegPath.Clear();
            //for (int i = 0; i < environment.DemoObject.Count(); i++)
            //{
            //    environment.DemoObject[i].thisRoundPath.Clear();//提供新的給 下次demo用
            //    environment.DemoObject[i].thisRoundPath.Add(environment.DemoObject[i].Position);//給第一個初始點
            //}

            generations.Last().segments.Add(addin);//UNDONE 如果要換方法，addin
        }

        public void ZeroAllKeys()
        {
            foreach (Generation G in generations)
                foreach (Segment S in G.segments)
                {
                    S.AbsoluteTrajectory.ZeroKey();
                    foreach (Trajectory T in S.RelativeTrajectory)
                    {
                        T.ZeroKey();
                    }
                }
        }
    }

    /// <summary>
    /// Environment
    /// including demo objects status, moving path,  
    /// temporary saving parameter until next segment
    /// </summary>
    public class Environment
    {
        public List<InteractObject> DemoObject { get; } = new List<InteractObject>();
        public Environment(List<InteractObject> demoObject, bool isPointer = true)
        {

            if (isPointer)
                DemoObject = demoObject;
            else
                DemoObject = demoObject.ToList();
        }
        /// <summary>temporal path in this segment</summary>
        public List<Point> thisSegPath = new List<Point>();
        /// <summary>temporal object index in this segment</summary>
        public int thisSegObjectIndex = -1;
        /// <summary>The Object witch is on hand (null = -1)</summary>
        public int handingObjectIndex = -1;

        public void AbortThisSeg()
        {
            thisSegPath.Clear();
            PlaceDown();
        }
        public void RandomPos(int x0,int x1,int y0,int y1)
        {
            Random rnd = new Random();
            foreach (InteractObject obj in DemoObject)
            {
                obj.Position = new Point(rnd.Next(x0, x1), rnd.Next(y0, y1));
            }
        }

        public void PickUp(int objectIndex)
        {
            DemoObject[objectIndex].pick();
            handingObjectIndex = objectIndex;
            thisSegObjectIndex = objectIndex;
        }
        public void PlaceDown(int objectIndex)
        {
            DemoObject[objectIndex].place();
            handingObjectIndex = -1;
        }
        public void PlaceDown()
        {
            if (handingObjectIndex == -1)//本來就沒拿東西。
                return;
            foreach (InteractObject io in DemoObject)
                io.place();
            handingObjectIndex = -1;

        }


    }

    public class Generation
    {
        public List<Segment> segments = new List<Segment>();

    }

    /// <summary>Segment 用於儲存絕對、相對路徑跟"一個"互動物件</summary>
    public class Segment
    {
        public int InteractObjectIndex = -1;
        // public List<ObjectTrajectoryPack> objectTrajectoryPacks = new List<ObjectTrajectoryPack>();
        public Segment(int interactObjectIndex, int objectCount)
        {
            InteractObjectIndex = interactObjectIndex;

            for (int i = 0; i < objectCount; i++)
            {
                RelativeTrajectory.Add(new Trajectory());
            }
        }
        public Trajectory AbsoluteTrajectory = new Trajectory();
        public List<Trajectory> RelativeTrajectory = new List<Trajectory>();

        public int pathCount()
        {
            return AbsoluteTrajectory.PathList.Count();
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

        internal void pick()
        {
            _isPick = true;
        }
        internal void place()
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
                if (result <= Radius)
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
        /// <summary>用於儲存目前圖上的路徑</summary>
        //public List<Point> thisRoundPath = new List<Point>();

    }

    public class Trajectory
    {
        public List<Point> PathList = new List<Point>();
        public List<int> PathKeyMatch = new List<int>();

        public void AddPoint(Point point)
        {
            PathList.Add(point);
            PathKeyMatch.Add(0);
        }
        public void Clear()
        {
            PathList.Clear();
            PathKeyMatch.Clear();
        }
        public void ClearKey()
        {
            PathKeyMatch.Clear();
        }
        public void ZeroKey()
        {
            for (int i = 0; i < PathKeyMatch.Count(); i++)
                PathKeyMatch[i] = 0;
        }

        public void DrawOn(IInputOutputArray img, Point RelPoint, MCvScalar Color, int Radius = 3, int thickness = -1)
        {
            foreach (Point P in PathList)
            {
                Point Pr = new Point(P.X + RelPoint.X, P.Y + RelPoint.Y);
                CvInvoke.Circle(img, Pr, Radius, Color, thickness);
            }
        }

        public void DrawKeyOn(IInputOutputArray img, Point RelPoint, int thres, MCvScalar Color, int Radius = 3, int thickness = -1)
        {
            for (int i = 0; i < PathKeyMatch.Count; i++)
            {
                if (PathKeyMatch[i] >= thres)
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
 