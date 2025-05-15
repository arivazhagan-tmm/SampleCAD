using System.Windows;
using static System.Math;
using Model;
using System.Windows.Media;

namespace View;

internal static class CadUtils {
   /// <summary>Returns horizontal and vertical offset of p1 from p2 in a tuple</summary>
   public static (double DX, double DY) Diff (this Point p1, Point p2) => (p1.X - p2.X, p1.Y - p2.Y);

   public static double DistanceTo (this Point p0, Point p1) =>
   Round (Sqrt (Pow (p1.X - p0.X, 2) + Pow (p1.Y - p0.Y, 2)), 2);

   /// <summary>Converts cad point to windows point</summary>
   public static CadPoint Convert (this Point pt) => new (pt.X, pt.Y);

   /// <summary>Returns a new cad point with applied transformation</summary>
   public static CadPoint Transform (this CadPoint pt, Matrix xfm) => xfm.Transform (pt.Convert ()).Convert ();

   /// <summary>Returns a new point with applied transformation</summary>
   public static Point Transform (this Point pt, Matrix xfm) => xfm.Transform (pt);

   /// <summary>Converts windows point to cad point</summary>
   public static Point Convert (this CadPoint pt) => new (pt.X, pt.Y);

   /// <summary>Returns a new bound with applied transformation</summary>
   public static Bound Transform (this Bound b, Matrix xfm) {
      var (min, max) = (new Point (b.MinX, b.MinY), new Point (b.MaxX, b.MaxY));
      min = xfm.Transform (min);
      max = xfm.Transform (max);
      return new Bound (min.Convert (), max.Convert ());
   }

   public static Point RadialMove (this Point pt, double r, double th) {
      th = th.ToRadians ();
      return new (pt.X + r * Cos (th), pt.Y + r * Sin (th));
   }

   public static double ToRadians (this double theta) => theta * PI / 180;

   public static (double dx, double dy) Delta (this Point p1, Point p2) => (p2.X - p1.X, p2.Y - p1.Y);

   /// <summary>Returns the quadrant of the point compared with the reference point</summary>
   public static EQuadrant Quadrant (this Point p, Point refPoint) {
      var (dx, dy) = p.Diff (refPoint);
      var quadrant = EQuadrant.I;
      if (dx < 0 && dy > 0) quadrant = EQuadrant.II;
      else if (dx < 0 && dy < 0) quadrant = EQuadrant.III;
      else if (dx > 0 && dy < 0) quadrant = EQuadrant.IV;
      return quadrant;
   }
}
