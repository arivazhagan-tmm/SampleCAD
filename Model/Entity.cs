namespace Model;

#region class Entity ------------------------------------------------------------------------------
public abstract class Entity {
   #region Properties -----------------------------------------------
   public virtual List<CadPoint> Vertices => mVertices ??= [mStartPoint, mEndPoint];
   public CadPoint StartPoint { get => mStartPoint; protected set => mStartPoint = value; }
   public CadPoint EndPoint { get => mEndPoint; protected set => mEndPoint = value; }
   #endregion

   #region Private Data ---------------------------------------------
   protected CadPoint mStartPoint, mEndPoint;
   protected List<CadPoint>? mVertices;
   #endregion
}
#endregion

#region class Line --------------------------------------------------------------------------------
public class Line : Entity {
   #region Constructors ---------------------------------------------
   public Line (CadPoint p1, CadPoint p2) {
      mAngle = p1.AngleTo (p2);
      mLength = p1.DistanceTo (p2);
      (mStartPoint, mEndPoint) = (p1, p2);
   }
   #endregion

   #region Properties -----------------------------------------------
   public double Length => mLength;
   public double Angle => mAngle;
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mLength, mAngle;
   #endregion
}
#endregion

#region class Rectangle ---------------------------------------------------------------------------
public class Rectangle : Entity {
   #region Constructors ---------------------------------------------
   public Rectangle (CadPoint firstCorner, CadPoint secondCorner) {
      (mStartPoint, mEndPoint) = (firstCorner, secondCorner);
      (mWidth, mHeight) = (mEndPoint - mStartPoint).Cords ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public override List<CadPoint> Vertices => mVertices ??= 
   [mStartPoint, mEndPoint, new CadPoint (mStartPoint.X + mEndPoint.X, mStartPoint.Y + mEndPoint.Y) * 0.5, 
    new CadPoint(mStartPoint.X, mEndPoint.Y), new CadPoint(mEndPoint.X, mStartPoint.Y)];
   public double Height => mHeight;
   public double Width => mWidth;
   #endregion

   #region Private Data ------------------------------------------------
   readonly double mHeight, mWidth;
   #endregion
}
#endregion

#region class Circle ------------------------------------------------------------------------------
public class Circle : Entity {
   #region Constructors ---------------------------------------------
   public Circle (CadPoint center, double radius) {
      (mStartPoint, mEndPoint) = (center, center + radius);
      (mCenter, mRadius) = (center, radius);
   }
   #endregion

   #region Properties -----------------------------------------------
   public CadPoint Center => mCenter;
   public double Radius => mRadius;
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mRadius;
   readonly CadPoint mCenter;
   #endregion
}
#endregion