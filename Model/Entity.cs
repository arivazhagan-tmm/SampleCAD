using System.Drawing;

namespace Model;

#region class Entity ------------------------------------------------------------------------------
public abstract class Entity {
   #region Properties -----------------------------------------------
   public float LineWeight { get => mLineWeight; protected set => mLineWeight = value; }
   public bool IsSelected {
      get => mIsSelected;
      set {
         mIsSelected = value;
         (mLineWeight, mLayer) = value ? (2.5f, Color.White) : (1.0f, Color.Black);
      }
   }
   public Color Layer { get => mLayer; protected set => mLayer = value; }
   public Bound Bound => mBound;
   public virtual List<CadPoint> Vertices => mVertices ??= [mStartPoint, mEndPoint];
   public CadPoint StartPoint { get => mStartPoint; protected set => mStartPoint = value; }
   public CadPoint EndPoint { get => mEndPoint; protected set => mEndPoint = value; }
   #endregion

   #region Methods --------------------------------------------------
   public abstract Entity Clone ();
   public abstract Entity Transformed (CadMatrix xfm);
   #endregion

   #region Private Data ---------------------------------------------
   protected CadPoint mStartPoint, mEndPoint;
   protected List<CadPoint>? mVertices;
   protected Bound mBound;
   protected bool mIsSelected;
   protected float mLineWeight = 1.0f;
   protected Color mLayer = Color.Black;
   #endregion
}
#endregion

#region class Line --------------------------------------------------------------------------------
public class Line : Entity {
   #region Constructors ---------------------------------------------
   public Line (CadPoint p1, CadPoint p2) {
      mAngle = p1.AngleTo (p2);
      mBound = new Bound (p1, p2);
      mLength = p1.DistanceTo (p2);
      (mStartPoint, mEndPoint) = (p1, p2);
   }
   #endregion

   #region Properties -----------------------------------------------
   public double Length => mLength;
   public double Angle => mAngle;
   #endregion

   #region Methods --------------------------------------------------
   public override Entity Clone () => new Line (mStartPoint, mEndPoint);

   public override Entity Transformed (CadMatrix xfm) => new Line (mStartPoint * xfm, mEndPoint * xfm);
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
      mBound = new Bound (mStartPoint, mEndPoint);
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

   #region Methods --------------------------------------------------
   public override Entity Clone () => new Rectangle (mStartPoint, mEndPoint);
   public override Entity Transformed (CadMatrix xfm) => new Rectangle (mStartPoint * xfm, mEndPoint * xfm);
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mHeight, mWidth;
   #endregion
}
#endregion

#region class Circle ------------------------------------------------------------------------------
public class Circle : Entity {
   #region Constructors ---------------------------------------------
   public Circle (CadPoint center, double radius) {
      (mStartPoint, mEndPoint) = (center, center + radius);
      mVertices = [center + (radius, 0), center + (0, radius), center + (-radius, 0), center + (0, -radius)];
      mBound = new Bound (mVertices);
      (mCenter, mRadius) = (center, radius);
   }
   #endregion

   #region Properties -----------------------------------------------
   public CadPoint Center => mCenter;
   public double Radius => mRadius;
   #endregion

   #region Methods --------------------------------------------------
   public override Entity Clone () => new Circle (mStartPoint, mRadius);
   public override Entity Transformed (CadMatrix xfm) => new Circle (mStartPoint * xfm, mRadius);
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mRadius;
   readonly CadPoint mCenter;
   #endregion
}
#endregion