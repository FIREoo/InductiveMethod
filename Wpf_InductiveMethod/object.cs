using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using Point = System.Drawing.Point;
namespace Wpf_InductiveMethod
{
    class InteractObject
    {
        public enum Type
        {
            circle = 0,
            square = 1
        }

        public Type Shape = Type.circle;
        public Point Center = new Point();
        public int Radius = 30;
       public MCvScalar Color { get; set; } = new MCvScalar(150, 150, 150);

        private bool _isPick = false;
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
            int distX = (point.X) - (Center.X);
            int distY = (point.Y) - (Center.Y);

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
                if (point.X < Center.X + Radius / 2 && point.X > Center.X - Radius / 2 && point.Y > Center.Y - Radius / 2 && point.Y < Center.Y + Radius / 2)
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
                CvInvoke.Circle(img, Center, Radius, Color, -1);
                if (isPick)
                    CvInvoke.Circle(img, Center, Radius, new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
            }
            else if (Shape == Type.square)
            {
                CvInvoke.Rectangle(img, new System.Drawing.Rectangle(Center.X - Radius / 2, Center.Y - Radius / 2, Radius, Radius), Color, -1);
                if (isPick)
                {
                    CvInvoke.Line(img, new Point(Center.X - Radius / 2, Center.Y - Radius / 2), new Point(Center.X - Radius / 2, Center.Y + Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                    CvInvoke.Line(img, new Point(Center.X + Radius / 2, Center.Y + Radius / 2), new Point(Center.X - Radius / 2, Center.Y + Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                    CvInvoke.Line(img, new Point(Center.X + Radius / 2, Center.Y + Radius / 2), new Point(Center.X + Radius / 2, Center.Y - Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                    CvInvoke.Line(img, new Point(Center.X - Radius / 2, Center.Y - Radius / 2), new Point(Center.X + Radius / 2, Center.Y - Radius / 2), new MCvScalar(Color.V0 + 50, Color.V1 + 50, Color.V2 + 50), 3);
                }

            }
        }

        //--Trajectory--//
        public List<Trajectory> trajectory = new List<Trajectory>();

        public void IniTrajectory()
        {

        }

    }


    public class Trajectory
    {
        public List<Point> Absolute = new List<Point>();
        public List<int> Abs_keyCount = new List<int>();
        public List<Point> Relative = new List<Point>();
        public List<int> Rel_keyCount = new List<int>();

        public void Clear()
        {
            Absolute.Clear();
            Relative.Clear();
        }

        public void AddPoint(Point P)
        {
            Absolute.Add(P);
            Abs_keyCount.Add(0);
        }
        public void resetKeyCount()
        {
            for (int c = 0; c < Abs_keyCount.Count; c++)
                Abs_keyCount[c] = 0;
        }
        public void drawOn(IInputOutputArray img, MCvScalar Color, int Radius = 3)
        {
            for (int i = 0; i < Absolute.Count(); i++)
            {
                CvInvoke.Circle(img, Absolute[i], Radius, Color, -1);
            }
        }
        /// <summary>
        /// draw if >= threshold
        /// </summary>

        public void drawKeyOn(int threshold, IInputOutputArray img, MCvScalar Color, int Radius = 3)
        {
            for (int i = 0; i < Absolute.Count(); i++)
            {
                if (Abs_keyCount[i] >= threshold)
                    CvInvoke.Circle(img, Absolute[i], Radius, Color, -1);
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
        static public int Distanse(Point P1 ,Point P2)
        {
            int dx = P2.X - P1.X;
            int dy = P2.Y - P1.Y;

            double D = Math.Sqrt((dx * dx) +(dy * dy));
            return (int)D;
        }
    }
}
