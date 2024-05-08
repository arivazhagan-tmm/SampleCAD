using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using Model;
using ViewModel;
using System.Windows.Documents;

namespace View;

#region class Viewport ----------------------------------------------------------------------------
internal sealed class Viewport : Canvas {
   #region Constructors ---------------------------------------------
   public Viewport () => Loaded += OnLoaded;
   #endregion

   #region Properties -----------------------------------------------
   public List<Entity> Entities => mEntities ??= [];
   #endregion

   #region Methods --------------------------------------------------
   public void AddEntity (Entity entity) {
      if (entity is null || mEntities is null || mEntities.Contains (entity)) return;
      mEntities.Add (entity);
   }

   public void Clear () => mEntities?.Clear ();

   public void Refresh () {
      mStartPt.Reset ();
      mCurrentMousePt.Reset ();
      mWidget?.Initialize ();
      mEntities?.ForEach (e => e.IsSelected = false);
      InvalidateVisual ();
   }

   public void SetWidget (Widget widget) {
      mWidget = widget;
      mStartPt.Reset ();
      mCurrentMousePt.Reset ();
   }

   public void UpdateAxis () {
      if (MainWindow.It is null) return;
      mHAxis = new (new (0.0, mViewportHeight / 2), new (mViewportWidth, mViewportHeight / 2));
      mVAxis = new (new (mViewportWidth / 2, 0.0), new (mViewportWidth / 2, mViewportHeight));
      InvalidateVisual ();
   }

   public void ZoomExtends () {
      if (mEntities is null || mEntities.Count is 0) return;
      var bound = new Bound (mEntities.Select (e => e.Bound));
      UpdateProjXform (bound);
      InvalidateVisual ();
   }
   #endregion

   #region Implementation -------------------------------------------
   CadPoint ToCadPoint (Point pt) {
      pt = mInvProjXfm.Transform (pt);
      return new CadPoint (pt.X, pt.Y);
   }

   Point ToWindowsPoint (CadPoint pt) => mProjXfm.Transform (new Point (pt.X, pt.Y));

   void OnLoaded (object sender, RoutedEventArgs e) {
      Height = 650;
      Width = 750;
      Background = Brushes.Transparent;

      mEntities = [];
      mStartPt = mCurrentMousePt = CadPoint.Default;
      mDwgLineWeight = 1.0;
      mDwgBrush = Brushes.ForestGreen;
      mAxisPen = new (Brushes.Gray, 0.5);
      mBGPen = new (Brushes.LightGray, 1.5);
      mDwgPen = new (Brushes.Black, mDwgLineWeight);
      mPreviewPen = new (mDwgBrush, mDwgLineWeight);
      mOrthoPen = new Pen (Brushes.Blue, 1.0) { DashStyle = DashStyles.Dash };

      if (MainWindow.It != null)
         mViewportRect = new Rect (new Size (MainWindow.It.ActualWidth - 120, MainWindow.It.ActualHeight - 100));
      (mViewportWidth, mViewportHeight) = (mViewportRect.Width, mViewportRect.Height);
      mViewportBound = new Bound (new (0.0, 0.0), new (mViewportWidth, mViewportHeight));
      mViewportCenter = new Point (mViewportBound.Mid.X, mViewportBound.Mid.Y);
      UpdateProjXform (mViewportBound);

      #region Attaching events --------------------------------------
      MouseMove += OnMouseMove;
      MouseWheel += OnMouseWheel; ;
      MouseLeftButtonDown += OnMouseLeftButtonDown;
      MouseDown += (s, e) => {
         mPanStartPt.Reset ();
         if (e.MiddleButton is MouseButtonState.Pressed) mPanStartPt = ToCadPoint (e.GetPosition (this));
      };
      #endregion

      mCords = new TextBlock () { Background = Brushes.Transparent };
      Children.Add (mCords);
   }

   void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
      var pt = ToCadPoint (e.GetPosition (this));
      if (mSnapPoint.IsSet) pt = mSnapPoint;
      if (!mStartPt.IsSet) mStartPt = pt;
      if (mWidget != null) {
         mWidget.ReceiveInput (pt);
         if (mWidget.Entity != null) {
            mEntities?.Add (mWidget.Entity);
            mWidget.Initialize ();
            mStartPt.Reset ();
            mCurrentMousePt.Reset ();
         }
      }
      InvalidateVisual ();
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      mSnapPoint.Reset ();
      mCurrentMousePt = ToCadPoint (e.GetPosition (this));
      foreach (var entity in mEntities!) {
         if (mCurrentMousePt.HasNearestPoint (entity.Vertices, 0.5, out var nearestPoint)) {
            mSnapPoint = nearestPoint;
            break;
         }
      }
      if (mSnapPoint.IsSet) mCurrentMousePt = mSnapPoint;
      if (mCords != null) mCords.Text = $"X : {mCurrentMousePt.X}  Y : {mCurrentMousePt.Y}";
      InvalidateVisual ();
   }

   void OnMouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      CadPoint cornerA = ToCadPoint (new Point ()),
               cornerB = ToCadPoint (new Point (mViewportBound.Width, mViewportBound.Height));
      var b = new Bound (cornerA, cornerB);
      UpdateProjXform (b.Inflated (mCurrentMousePt, zoomFactor));
      InvalidateVisual ();
   }

   void UpdateProjXform (Bound b) {
      var margin = 10.0;
      double scaleX = (mViewportWidth - margin) / b.Width,
             scaleY = (mViewportHeight - margin) / b.Height;
      double scale = Math.Min (scaleX, scaleY);
      var xfm = Matrix.Identity;
      xfm.Scale (scale, -scale);
      Point projectedMidPt = xfm.Transform (new Point (b.Mid.X, b.Mid.Y));
      var (dx, dy) = (mViewportCenter.X - projectedMidPt.X, mViewportCenter.Y - projectedMidPt.Y);
      var translateMatrix = Matrix.Identity;
      translateMatrix.Translate (dx, dy);
      xfm.Append (translateMatrix);
      mProjXfm = xfm;
      mInvProjXfm = mProjXfm;
      mInvProjXfm.Invert ();
   }

   protected override void OnRender (DrawingContext dc) {
      var (startPt, endPt) = (ToWindowsPoint (mStartPt), ToWindowsPoint (mCurrentMousePt));
      var angle = mStartPt.AngleTo (mCurrentMousePt);
      const double delta = 0.5;
      if (angle is < 2.0 and > -2 or > 178.0 and < 182.0) {
         dc.DrawLine (mOrthoPen, new (0, startPt.Y), new (mViewportRect.Width, startPt.Y));
         mSnapPoint = new (mCurrentMousePt.X, mStartPt.Y);
      }
      if (angle is > 88.0 and < 92.0 or > 268 and < 272) {
         dc.DrawLine (mOrthoPen, new (startPt.X, 0), new (startPt.X, mViewportRect.Height));
         mSnapPoint = new (mStartPt.X, mCurrentMousePt.Y);
      }
      if (mCurrentMousePt.Y is < delta and > -delta) {
         dc.DrawLine (mOrthoPen, new (0, mViewportCenter.Y), new (mViewportRect.Width, mViewportCenter.Y));
         mSnapPoint = new (mCurrentMousePt.X, 0);
      }
      if (mCurrentMousePt.X is < delta and > -delta) {
         dc.DrawLine (mOrthoPen, new (mViewportCenter.X, 0), new (mViewportCenter.X, mViewportRect.Height));
         mSnapPoint = new (0, mCurrentMousePt.Y);
      }
      if (mEntities != null && mEntities.Any (e => e.Vertices.Any (v => v.X < mCurrentMousePt.X + delta && v.X > mCurrentMousePt.X - delta))) {
         dc.DrawLine (mOrthoPen, new (endPt.X, 0), new (endPt.X, mViewportRect.Height));
         mSnapPoint = mCurrentMousePt;
      }
      if (mEntities != null && mEntities.Any (e => e.Vertices.Any (v => v.Y < mCurrentMousePt.Y + delta && v.Y > mCurrentMousePt.Y - delta))) {
         dc.DrawLine (mOrthoPen, new (0, endPt.Y), new (mViewportRect.Width, endPt.Y));
         mSnapPoint = mCurrentMousePt;
      }

      if (mStartPt.IsSet && mCurrentMousePt.IsSet) {
         switch (mWidget) {
            case LineWidget:
               dc.DrawLine (mPreviewPen, startPt, endPt);
               break;
            case RectWidget:
               dc.DrawRectangle (Brushes.Transparent, mPreviewPen, new Rect (startPt, endPt));
               break;
            case CircleWidget:
               var radius = endPt.X - startPt.X;
               dc.DrawEllipse (Brushes.Transparent, mPreviewPen, startPt, radius, radius);
               break;
         }
      }
      if (mEntities != null) {
         foreach (var entity in mEntities) {
            var clr = entity.Layer;
            var pen = new Pen (new SolidColorBrush (Color.FromRgb (clr.R, clr.G, clr.B)), entity.LineWeight);
            switch (entity) {
               case Line line:
                  dc.DrawLine (pen, ToWindowsPoint (line.StartPoint), ToWindowsPoint (line.EndPoint));
                  break;
               case Rectangle rect:
                  dc.DrawRectangle (Brushes.Transparent, pen, new Rect (ToWindowsPoint (rect.StartPoint), ToWindowsPoint (rect.EndPoint)));
                  break;
               case Circle circle:
                  var(center, tangent) = (ToWindowsPoint(circle.Center), ToWindowsPoint(circle.EndPoint));
                  var radius = tangent.X - center.X;
                  dc.DrawEllipse (Brushes.Transparent, pen, ToWindowsPoint (circle.Center), radius, radius);
                  break;
            }
         }
      }
      if (mSnapPoint.IsSet) {
         var snapSize = 5;
         var snapPt = ToWindowsPoint (mSnapPoint);
         var v = new Vector (snapSize, snapSize);
         dc.DrawRectangle (Brushes.White, mDwgPen, new (snapPt - v, snapPt + v));
      }
      dc.DrawRectangle (Background, mBGPen, mViewportRect);
      dc.DrawLine (mAxisPen, mHAxis.P1, mHAxis.P2);
      dc.DrawLine (mAxisPen, mVAxis.P1, mVAxis.P2);
      base.OnRender (dc);
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mDwgLineWeight, mViewportWidth, mViewportHeight;
   Rect mViewportRect;
   Axis mHAxis, mVAxis;
   Bound mViewportBound;
   Point mViewportCenter;
   Matrix mProjXfm, mInvProjXfm;
   CadPoint mCurrentMousePt, mStartPt, mSnapPoint, mPanStartPt;
   Pen? mAxisPen, mBGPen, mDwgPen, mPreviewPen, mOrthoPen;
   Brush? mDwgBrush;
   List<Entity>? mEntities;
   Widget? mWidget;
   TextBlock? mCords;
   #endregion
}
#endregion

#region struct Axis -------------------------------------------------------------------------------
internal readonly record struct Axis (Point P1, Point P2);
#endregion