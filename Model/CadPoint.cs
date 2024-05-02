using System.Drawing;
using static System.Math;

namespace Model;

#region struct CadPoint ---------------------------------------------------------------------------
public struct CadPoint {
   #region Constructors ---------------------------------------------
   public CadPoint (double x, double y) => (mX, mY) = (x, y);
   #endregion

   #region Properties -----------------------------------------------
   public readonly bool IsSet => !double.IsNaN (mX) || !double.IsNaN (mY);
   public readonly double X => Round (mX, 2);
   public readonly double Y => Round (mY, 2);
   public static CadPoint Default => new (double.NaN, double.NaN);
   #endregion

   #region Operators ------------------------------------------------
   public static CadPoint operator + (CadPoint pt, double f) => new (pt.X + f, pt.Y + f);
   public static CadPoint operator + (CadPoint pt, (double dx, double dy) delta) => new (pt.X + delta.dx, pt.Y + delta.dy);
   public static CadPoint operator - (CadPoint p1, CadPoint p2) => new (p1.X - p2.X, p1.Y - p2.Y);
   public static CadPoint operator * (CadPoint pt, double f) => new (pt.X * f, pt.Y * f);
   #endregion

   #region Methods --------------------------------------------------
   public double AngleTo (CadPoint b) {
      var angle = Round (Atan2 (b.Y - Y, b.X - X) * (180 / PI), 2);
      return angle < 0 ? 360 + angle : angle;
   }
   public (double X, double Y) Cords () => (X, Y);
   public double DistanceTo (CadPoint p) => Round (Sqrt (Pow (p.X - X, 2) + Pow (p.Y - Y, 2)), 2);
   public bool HasNearestPoint (IEnumerable<CadPoint> pts, double delta, out CadPoint nearestPoint) {
      var pt = this;
      nearestPoint = pts.Any (p => pt.DistanceTo (p) < delta) ? pts.ToList ().Find (p => pt.DistanceTo (p) < delta) : Default;
      return !nearestPoint.Equals (Default);
   }
   public void Reset () => (mX, mY) = (double.NaN, double.NaN);
   public override string ToString () => $"({X}, {Y})";
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY;
   #endregion
}
#endregion

#region struct Layer ------------------------------------------------------------------------------
public readonly struct Layer {
}
#endregion

public readonly record struct Bound (CadPoint p1, CadPoint p2) {
}