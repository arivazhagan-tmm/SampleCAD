using System.ComponentModel;
using Model;

namespace ViewModel;

#region class Widget ------------------------------------------------------------------------------
public abstract class Widget : INotifyPropertyChanged {
   #region Properties -----------------------------------------------
   public Entity? Entity { get => mEntity; protected set => mEntity = value; }
   public string? Prompt { get => $"{this}: {mPrompt}"; set { mPrompt = value; OnPropertyChanged (nameof (Prompt)); } }
   public abstract string[]? Params { get; protected set; }

   public event PropertyChangedEventHandler? PropertyChanged;
   #endregion

   #region Methods --------------------------------------------------
   public virtual void ReceiveInput (object obj) {
      if (obj is CadPoint pt)
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
            UpdateParams ();
         }
      if (mPrompts != null && mPromptIndex < mPrompts.Length) Prompt = mPrompts[mPromptIndex];
   }

   public virtual void Initialize () {
      mEntity = null;
      mStartPoint = CadPoint.Default;
      mPromptIndex = 0;
      if (mPrompts != null && mPrompts.Length > 0) Prompt = mPrompts[mPromptIndex];
   }
   #endregion

   #region Implementation -------------------------------------------
   protected abstract void CreateEntity ();
   protected abstract void UpdateParams ();
   protected void OnPropertyChanged (string propertyName) => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
   #endregion

   #region Private Data ---------------------------------------------
   protected int mPromptIndex;
   protected string? mPrompt;
   protected string[]? mPrompts;
   protected string[]? mParams;
   protected Entity? mEntity;
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
      if (obj is string parameter) {
         switch (parameter) {
            case nameof (X) or nameof (Y):
               mStartPoint.Reset ();
               base.ReceiveInput (new CadPoint (X, Y));
               break;
               case nameof (DX) or nameof (DY):
               mStartPoint = new (X, Y);
               base.ReceiveInput (mStartPoint + (DX, DY));
               UpdateParams ();
               break;
         }
      }
      else base.ReceiveInput(obj);
   }

   public override string ToString () => "Line";
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      var line = new Line (mStartPoint, mEndPoint);
      mEntity = line;
      (Angle, Length) = (line.Angle, line.Length);
      (DX, DY) = (mEndPoint - mStartPoint).Cords ();
   }

   void SetEndPoint () {
      mEndPoint = mStartPoint + (mDX, mDY);
      UpdateParams ();
   }

   protected override void UpdateParams () {
      if (mStartPoint.IsSet) (X, Y) = mStartPoint.Cords ();
      if (mEndPoint.IsSet) (Angle, Length) = (mStartPoint.AngleTo (mEndPoint), mStartPoint.DistanceTo (mEndPoint));
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
      if (obj is string parameter) {
         switch (parameter) {
            case nameof (X) or nameof (Y):
               mStartPoint.Reset ();
               base.ReceiveInput (new CadPoint (X, Y));
               break;
            case nameof (Height) or nameof (Width):
               mStartPoint = new (X, Y);
               base.ReceiveInput (new CadPoint (X + Width, Y + Height));
               UpdateParams ();
               break;
         }
      } else base.ReceiveInput (obj);
   }

   public override string ToString () => "Rectangle";
   #endregion

   #region Implementation -------------------------------------------
   protected override void CreateEntity () {
      var rect = new Rectangle (mStartPoint, mEndPoint);
      (Height, Width) = (rect.Height, rect.Width);
      mEntity = rect;
   }

   protected override void UpdateParams () { if (mStartPoint.IsSet) (X, Y) = mStartPoint.Cords (); }
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
      mEntity = new Circle (mStartPoint, Radius);
   }

   protected override void UpdateParams () {
      if (mStartPoint.IsSet) (X, Y) = mStartPoint.Cords ();
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mX, mY, mRadius;
   #endregion
}
#endregion