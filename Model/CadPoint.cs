using System.Numerics;
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
   public static CadPoint operator - (CadPoint pt, double f) => new (pt.X - f, pt.Y - f);
   public static CadPoint operator / (CadPoint pt, double f) => new (pt.X / f, pt.Y / f);
   public static CadPoint operator + (CadPoint pt, (double dx, double dy) del) => new (pt.X + del.dx, pt.Y + del.dy);
   public static CadPoint operator - (CadPoint p1, CadPoint p2) => new (p1.X - p2.X, p1.Y - p2.Y);
   public static CadPoint operator * (CadPoint pt, double f) => new (pt.X * f, pt.Y * f);
   #endregion

   #region Methods --------------------------------------------------
   public double AngleTo (CadPoint b) {
      var angle = Round (Atan2 (b.Y - Y, b.X - X) * (180 / PI), 2);
      return angle < 0 ? 360 + angle : angle;
   }

   public (double X, double Y) Cords () => (X, Y);

   public (double dx, double dy) Delta (CadPoint p) => (p.X - X, p.Y - Y);

   public double DistanceTo (CadPoint p) => Round (Sqrt (Pow (p.X - X, 2) + Pow (p.Y - Y, 2)), 2);

   public bool HasNearestPoint (IEnumerable<CadPoint> pts, double delta, out CadPoint nearestPoint) {
      var pt = this;
      nearestPoint = pts.Any (p => pt.DistanceTo (p) < delta) ? pts.ToList ().Find (p => pt.DistanceTo (p) < delta) : Default;
      return !nearestPoint.Equals (Default);
   }

   public CadPoint RadialMove (double r, double th) {
      //th *= (PI / 180);
      return new (X + r * Cos (th), Y + r * Sin (th));
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

#region struct Bound ------------------------------------------------------------------------------
public readonly record struct Bound {
   #region Constructors ---------------------------------------------
   public Bound (CadPoint cornerA, CadPoint cornerB) {
      MinX = Min (cornerA.X, cornerB.X);
      MaxX = Max (cornerA.X, cornerB.X);
      MinY = Min (cornerA.Y, cornerB.Y);
      MaxY = Max (cornerA.Y, cornerB.Y);
      mMid = new CadPoint (MaxX + MinX, MaxY + MinY) / 2.0;
      (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
   }

   public Bound (IEnumerable<CadPoint> pts) {
      MinX = pts.Min (p => p.X);
      MaxX = pts.Max (p => p.X);
      MinY = pts.Min (p => p.Y);
      MaxY = pts.Max (p => p.Y);
      mMid = new CadPoint (MaxX + MinX, MaxY + MinY) / 2.0;
      (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
   }

   public Bound (IEnumerable<Bound> bounds) {
      MinX = bounds.Min (b => b.MinX);
      MaxX = bounds.Max (b => b.MaxX);
      MinY = bounds.Min (b => b.MinY);
      MaxY = bounds.Max (b => b.MaxY);
      mMid = new CadPoint (MaxX + MinX, MaxY + MinY) / 2.0;
      (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
   }

   public Bound (double minX, double minY, double maxX, double maxY) {
      MinX = minX;
      MinY = minY;
      MaxX = maxX;
      MaxY = maxY;
      mMid = new CadPoint (MaxX + MinX, MaxY + MinY) / 2.0;
      (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
   }

   public Bound () => this = Empty;
   #endregion

   #region Properties -----------------------------------------------
   public bool IsEmpty => MinX > MaxX || MinY > MaxY;
   public double MinX { get; init; }
   public double MaxX { get; init; }
   public double MinY { get; init; }
   public double MaxY { get; init; }
   public double Width => mWidth;
   public double Height => mHeight;
   public CadPoint Mid => mMid;

   public static readonly Bound Empty = new () { MinX = double.MaxValue, MinY = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue };
   #endregion

   #region Methods --------------------------------------------------
   public Bound Inflated (CadPoint ptAt, double factor) {
      if (IsEmpty) return this;
      var minX = ptAt.X - (ptAt.X - MinX) * factor;
      var maxX = ptAt.X + (MaxX - ptAt.X) * factor;
      var minY = ptAt.Y - (ptAt.Y - MinY) * factor;
      var maxY = ptAt.Y + (MaxY - ptAt.Y) * factor;
      return new (minX, minY, maxX, maxY);
   }

   public Bound Cloned () => new (MinX, MinY, MaxX, MaxY);

   public bool IsInside (Bound b) => MinX > b.MinX && MinY > b.MinY && MaxX < b.MaxX && MaxY < b.MaxY;

   public override string ToString () => $"( {MaxX}, {MaxY} )";
   #endregion

   #region Private Data ---------------------------------------------
   readonly CadPoint mMid;
   readonly double mHeight, mWidth;
   #endregion
}
#endregion

#region struct CadVector --------------------------------------------------------------------------
public readonly record struct CadVector (double X, double Y) {
   public static CadVector operator + (CadVector v1, CadVector v2) => new (v1.X + v2.X, v1.Y + v2.Y);
   public static CadVector operator * (CadVector v, double f) => new (v.X * f, v.Y * f);
   public static CadVector operator - (CadVector v) => new (-v.X, -v.Y);
}
#endregion

#region struct CadMatrix --------------------------------------------------------------------------
public struct CadMatrix {
   public CadMatrix (double m11, double m12, double m21, double m22, double dx, double dy)
      => (M11, M12, M21, M22, DX, DY) = (m11, m12, m21, m22, dx, dy);

   public static CadMatrix Translate (CadVector v) => new (1, 0, 0, 1, v.X, v.Y);
   public static CadMatrix Scale (double scaleX, double scaleY) => new (scaleX, 0, 0, scaleY, 0, 0);
   public static CadMatrix Rotate (double theta) {
      var (s, c) = (Sin (theta), Cos (theta));
      return new (c, s, -s, c, 0, 0);
   }

   public static CadPoint operator * (CadPoint p, CadMatrix m)
      => new (p.X * m.M11 + p.Y * m.M21 + m.DX, p.X * m.M12 + p.Y * m.M22 + m.DY);

   public static CadMatrix operator * (CadMatrix a, CadMatrix b)
      => new (a.M11 * b.M11 + a.M12 * b.M21, a.M11 * b.M12 + a.M12 * b.M22,
              a.M21 * b.M11 + a.M22 * b.M21, a.M21 * b.M12 + a.M22 * b.M22,
              a.DX * b.M11 + a.DY * b.M21 + b.DX, a.DX * b.M12 + a.DY * b.M22 + b.DY);

   public readonly double M11, M12, M21, M22, DX, DY;
}
#endregion