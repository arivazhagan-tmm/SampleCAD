using System.ComponentModel;
using Model;

namespace ViewModel;

#region class Widget ------------------------------------------------------------------------------
public abstract class Widget : INotifyPropertyChanged {
   #region Properties -----------------------------------------------
   public CadPoint StartPoint => mStartPoint;
   public Entity? Entity { get => mEntity; protected set => mEntity = value; }
   public string? Prompt { get => $"{this}: {mPrompt}"; set { mPrompt = value; OnPropertyChanged (nameof (Prompt)); } }
   public abstract string[]? Params { get; protected set; }

   public event PropertyChangedEventHandler? PropertyChanged;

   public List<Entity> OriginalEntities { get => mOriginalEntities; protected set => mOriginalEntities = value; }
   public List<Entity> TransformedEntities { get => mTransformedEntities; protected set => mTransformedEntities = value; }
   #endregion

   #region Methods --------------------------------------------------
   public virtual void ReceiveInput (object obj) {
      if (obj is CadPoint pt && mEntity is null)
         if (mStartPoint.IsSet) {
            mEndPoint = pt;
            CreateEntity ();
            mStartPoint.Reset ();
            mEndPoint.Reset ();
            mPromptIndex = 0;
         } else {
            mPromptIndex++;
            mStartPoint = pt;
            mEndPoint.Reset ();
            UpdateParameters ();
         }
      if (mPrompts != null && mPromptIndex < mPrompts.Length) Prompt = mPrompts[mPromptIndex];
   }

   public virtual void Initialize () {
      mEntity = null;
      mStartPoint = mEndPoint = CadPoint.Default;
      mPromptIndex = 0;
      if (mPrompts != null && mPrompts.Length > 0) Prompt = mPrompts[mPromptIndex];
   }
   #endregion

   #region Implementation -------------------------------------------
   protected abstract void CreateEntity ();
   protected abstract void UpdateParameters ();
   protected void OnPropertyChanged (string propertyName) => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
   #endregion

   #region Private Data ---------------------------------------------
   protected int mPromptIndex;
   protected string? mPrompt;
   protected string[]? mPrompts;
   protected string[]? mParams;
   protected Entity? mEntity;
   protected List<Entity> mOriginalEntities = [], mTransformedEntities = [];
   protected CadPoint mStartPoint, mEndPoint;
   #endregion
}
#endregion

#region class LineWidget --------------------------------------------------------------------------
public class LineWidget : Widget {
   #region Constructors ---------------------------------------------
   public LineWidget () {
      mParams = [nameof (X), nameof (Y), nameof (DX), nameof (DY), nameof (Length), nameof (Angle)];
      mPrompts = ["  Pick the start point", "   Pick the end point"];
      Initialize ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public double X { get => mX; set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => mY; set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public double DX { get => mDX; set { mDX = value; OnPropertyChanged (nameof (DX)); } }
   public double DY { get => mDY; set { mDY = value; OnPropertyChanged (nameof (DY)); } }
   public double Length { get => mLength; set { mLength = value; OnPropertyChanged (nameof (Length)); } }
   public double Angle { get => mAngle; set { mAngle = value; OnPropertyChanged (nameof (Angle)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Methods --------------------------------------------------
   public override void ReceiveInput (object obj) {
      if (obj is CadPoint pt) base.ReceiveInput (pt);
      else if (obj is string str) {
         mEndPoint = new (mX + mDX, mY + mDY);
         switch (str) {
            case nameof (X) or nameof (Y) or nameof (DX) or nameof (DY):
               mStartPoint = new (mX, mY);
               UpdateParameters ();
               break;
            case nameof (Length) or nameof (Angle):
               mStartPoint = new (mX, mY);
               mEndPoint = mStartPoint.RadialMove (mLength, mAngle);
               break;
         }
      } else if (obj is int) {
         mEntity = new Line (mStartPoint, mEndPoint);
         UpdateParameters ();
      }
   }

   public override string ToString () => "Line";
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      mEntity = new Line (mStartPoint, mEndPoint);
      UpdateParameters ();
   }

   protected override void UpdateParameters () {
      if (mStartPoint.IsSet) (X, Y) = mStartPoint.Cords ();
      if (mEndPoint.IsSet) {
         (Angle, Length) = (mStartPoint.AngleTo (mEndPoint), mStartPoint.DistanceTo (mEndPoint));
         (DX, DY) = (mEndPoint - mStartPoint).Cords ();
      }
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY, mDX, mDY, mLength, mAngle;
   #endregion
}
#endregion

#region class RectWidget --------------------------------------------------------------------------
public class RectWidget : Widget {
   #region Constructors ---------------------------------------------
   public RectWidget () {
      mParams = [nameof (X), nameof (Y), nameof (Height), nameof (Width)];
      mPrompts = ["Pick the first corner", "Pick the opposite corner"];
      Initialize ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public double X { get => mX; set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => mY; set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public double Height { get => mHeight; set { mHeight = value; OnPropertyChanged (nameof (Height)); } }
   public double Width { get => mWidth; set { mWidth = value; OnPropertyChanged (nameof (Width)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Methods --------------------------------------------------
   public override void ReceiveInput (object obj) {
      if (obj is CadPoint pt) base.ReceiveInput (pt);
      else if (obj is string parameter) {
         switch (parameter) {
            case nameof (X) or nameof (Y):
               mStartPoint = new (X, Y);
               break;
            case nameof (Height) or nameof (Width):
               if (!mStartPoint.IsSet) mStartPoint = new (X, Y);
               mEndPoint = new (X + Width, Y + Height);
               break;
         }
      } else if (obj is int) mEntity = new Rectangle (mStartPoint, mEndPoint);
   }

   public override string ToString () => "Rectangle";
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      var rect = new Rectangle (mStartPoint, mEndPoint);
      (Height, Width) = (rect.Height, rect.Width);
      mEntity = rect;
   }

   protected override void UpdateParameters () { if (mStartPoint.IsSet) (X, Y) = mStartPoint.Cords (); }
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY, mWidth, mHeight;
   #endregion
}
#endregion

#region class CircleWidget ------------------------------------------------------------------------
public class CircleWidget : Widget {
   #region Constructors ---------------------------------------------
   public CircleWidget () {
      mParams = [nameof (X), nameof (Y), nameof (Radius)];
      mPrompts = ["Pick the center point", "Pick the point on circle"];
      Initialize ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public double X { get => mX; set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => mY; set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public double Radius { get => mRadius; set { mRadius = value; OnPropertyChanged (nameof (Radius)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Methods --------------------------------------------------
   public override string ToString () => "Circle";
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      Radius = mStartPoint.DistanceTo (mEndPoint);
      mEntity = new Circle (mStartPoint, mEndPoint, Radius);
   }

   protected override void UpdateParameters () {
      if (mStartPoint.IsSet) (X, Y) = mStartPoint.Cords ();
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY, mRadius;
   #endregion
}
#endregion

#region class PlaneWidget -------------------------------------------------------------------------
public class PlaneWidget : Widget {
   #region Constructors ---------------------------------------------
   public PlaneWidget () {
      mParams = [nameof (X), nameof (Y)];
      mPrompts = ["Pick the center point", "Pick the point on circle"];
      Initialize ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public double X { get => mX; set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => mY; set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      mEntity = new Plane (mStartPoint, mEndPoint);
   }

   protected override void UpdateParameters () {
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY;
   #endregion
}
#endregion

#region class SquareWidget ------------------------------------------------------------------------
public class SquareWidget : Widget {
   #region Constructors ---------------------------------------------
   public SquareWidget () {
      mParams = [nameof (X), nameof (Y), nameof (Side)];
      mPrompts = ["Pick the first corner", "Pick the opposite corner"];
      Initialize ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public double X { get => mX; set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => mY; set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public double Side { get => mSide; set { mSide = value; OnPropertyChanged (nameof (mSide)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      var sqr = new Square (mStartPoint, mEndPoint);
      mSide = sqr.Bound.MaxX;
      mEntity = sqr;
   }

   protected override void UpdateParameters () { }
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY, mSide;
   #endregion
}
#endregion

#region class TranslateWidget ---------------------------------------------------------------------
public class TranslateWidget : Widget, ITransform {
   #region Constructors ---------------------------------------------
   public TranslateWidget (IEnumerable<Entity> entities) {
      mOriginalEntities = entities.ToList ();
      mParams = [nameof (DX), nameof (DY)];
      mPrompts = ["  Pick the start point", "  Pick the end point"];
      Initialize ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public double DX { get => mDX; set { mDX = value; OnPropertyChanged (nameof (DX)); } }
   public double DY { get => mDY; set { mDY = value; OnPropertyChanged (nameof (DY)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Methods --------------------------------------------------
   public override void ReceiveInput (object obj) {
      if (obj is CadPoint pt) base.ReceiveInput (pt);
      else if (obj is int val && val is 0 && mStartPoint.IsSet) {
         mEndPoint = mStartPoint + (mDX, mDY);
         CreateEntity ();
      }
   }

   public override string ToString () => "Translate";
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      (DX, DY) = mStartPoint.Delta (mEndPoint);
      var xfm = CadMatrix.Translate (new CadVector (DX, DY));
      mTransformedEntities ??= [];
      mOriginalEntities.ForEach (e => mTransformedEntities.Add (e.Transformed (xfm)));
      mStartPoint.Reset ();
   }

   protected override void UpdateParameters () { }
   #endregion

   #region Private Data ---------------------------------------------
   double mDX, mDY;
   #endregion
}
#endregion

public class ScaleWidget : Widget {
   #region Constructors --------------------------------------------
   public ScaleWidget (IEnumerable<Entity> entities) {
      mOriginalEntities = entities.ToList ();
      mParams = [nameof (ScaleX), nameof (ScaleY)];
      mPrompts = [" Pick the start point", " Pick the end point"];
      Initialize ();
   }
   #endregion

   #region Properties ----------------------------------------------
   public double ScaleX { get => mScaleX; set { mScaleX = value; OnPropertyChanged (nameof (ScaleX)); } }
   public double ScaleY { get => mScaleY; set { mScaleY = value; OnPropertyChanged (nameof (ScaleY)); } }
   public override string[]? Params { get => mParams; protected set => mParams = value; }
   #endregion

   #region Methods -------------------------------------------------
   public override void ReceiveInput (object obj) {
      //if (obj is string parameter) {
      //    switch (parameter) {
      //        case nameof ( ScaleX ) or nameof ( ScaleY ):
      //            base.ReceiveInput ( mStartPoint + (ScaleX, ScaleY) );
      //            UpdateParameters ( );
      //            break;
      //    }
      //}
      //else base.ReceiveInput ( obj );
      if (obj is int) CreateEntity ();
   }

   public override string ToString () => "Scale ";
   #endregion

   #region Implementation ------------------------------------------
   protected override void CreateEntity () {
      var xfm = CadMatrix.Scale (mScaleX, mScaleY);
      mTransformedEntities ??= [];
      mOriginalEntities.ForEach (e => mTransformedEntities.Add (e.Transformed (xfm)));
   }

   protected override void UpdateParameters () { }
   #endregion

   #region Private Data --------------------------------------------
   double mScaleX, mScaleY;
   #endregion
}

public interface ITransform { }