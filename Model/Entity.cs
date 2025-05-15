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
   public virtual List<CadPoint> Vertices => mVertices ??= [mStartPt, mEndPt];
   public CadPoint StartPoint { get => mStartPt; protected set => mStartPt = value; }
   public CadPoint EndPoint { get => mEndPt; protected set => mEndPt = value; }
   #endregion

   #region Methods --------------------------------------------------
   public abstract Entity Clone ();
   public abstract Entity Transformed (CadMatrix xfm);
   #endregion

   #region Private Data ---------------------------------------------
   protected CadPoint mStartPt, mEndPt;
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
      (mStartPt, mEndPt) = (p1, p2);
   }
   #endregion

   #region Properties -----------------------------------------------
   public double Length => mLength;
   public double Angle => mAngle;
   #endregion

   #region Methods --------------------------------------------------
   public override Entity Clone () => new Line (mStartPt, mEndPt);

   public override Entity Transformed (CadMatrix xfm) => new Line (mStartPt * xfm, mEndPt * xfm);
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
      (mStartPt, mEndPt) = (firstCorner, secondCorner);
      mBound = new Bound (mStartPt, mEndPt);
      (mWidth, mHeight) = (mEndPt - mStartPt).Cords ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public override List<CadPoint> Vertices => mVertices ??=
   [mStartPt, mEndPt, new CadPoint (mStartPt.X + mEndPt.X, mStartPt.Y + mEndPt.Y) * 0.5,
    new CadPoint(mStartPt.X, mEndPt.Y), new CadPoint(mEndPt.X, mStartPt.Y)];
   public double Height => mHeight;
   public double Width => mWidth;
   #endregion

   #region Methods --------------------------------------------------
   public override Entity Clone () => new Rectangle (mStartPt, mEndPt);
   public override Entity Transformed (CadMatrix xfm) => new Rectangle (mStartPt * xfm, mEndPt * xfm);
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mHeight, mWidth;
   #endregion
}
#endregion

public class Square : Rectangle {
   public Square (CadPoint firstCorner, CadPoint secondCorner) : base (firstCorner, secondCorner) { }
}

#region class Circle ------------------------------------------------------------------------------
public class Circle : Entity {
   #region Constructors ---------------------------------------------
   public Circle (CadPoint center, CadPoint tangent, double radius) {
      (mStartPt, mEndPt) = (center, tangent);
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
   public override Entity Clone () => new Circle (mStartPt, mEndPt, mRadius);
   public override Entity Transformed (CadMatrix xfm) => new Circle (mStartPt * xfm, mEndPt * xfm, mRadius);
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mRadius;
   readonly CadPoint mCenter;
   #endregion
}
#endregion

#region class Plane -------------------------------------------------------------------------------
public class Plane : Entity {
   #region Constructor ----------------------------------------------
   public Plane (CadPoint startPt, CadPoint endPt) {
      (mStartPt, mEndPt, mPlaneAngle) = (startPt, endPt, 45.0);
      var dist = mStartPt.DistanceTo (mEndPt);
      (mV1,  mV2) = (mStartPt.RadialMove (dist, mPlaneAngle), mEndPt.RadialMove (dist, mPlaneAngle));
      mVertices = [mStartPt, mEndPt, mV1, mV2];
      mBound = new Bound (mVertices);
   }
   #endregion

   #region Methods --------------------------------------------------
   public override Entity Clone () => new Plane (mStartPt, mEndPt);
   public override Entity Transformed (CadMatrix xfm) => new Plane (mStartPt * xfm, mEndPt * xfm);
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mPlaneAngle;
   readonly CadPoint mV1, mV2;
   #endregion
}
#endregion