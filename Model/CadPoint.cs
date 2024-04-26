using static System.Math;

namespace Model;

#region struct CadPoint ---------------------------------------------------------------------------
public struct CadPoint {
   #region Constructors ---------------------------------------------
   public CadPoint (double x, double y) => (mX, mY) = (x, y);
   #endregion

   #region Properties -----------------------------------------------
   public readonly bool IsSet => !Equals (Default);
   public readonly double X => Round (mX, 2);
   public readonly double Y => Round (mY, 2);
   public static CadPoint Default => new (double.NaN, double.NaN);
   #endregion

   #region Operators ------------------------------------------------
   public static CadPoint operator + (CadPoint pt, double f) => new (pt.X + f, pt.Y + f);
   public static CadPoint operator - (CadPoint p1, CadPoint p2) => new (p1.X - p2.X, p1.Y - p2.Y);
   public static CadPoint operator * (CadPoint pt, double f) => new (pt.X * f, pt.Y * f);
   #endregion

   #region Methods --------------------------------------------------
   public double AngleTo (CadPoint b) => Round (Atan2 (b.Y - Y, b.X - X) * (180 / PI), 2);
   public (double X, double Y) Cords () => (X, Y);
   public double DistanceTo (CadPoint p) => Round (Sqrt (Pow (p.X - X, 2) + Pow (p.Y - Y, 2)), 2);
   public void Reset () => (mX, mY) = (double.NaN, double.NaN);
   public override string ToString () => $"({X}, {Y})";
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY;
   #endregion
}
#endregion