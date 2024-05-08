using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using Model;
using ViewModel;

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
   Point Project (CadPoint pt) => mProjXfm.Transform (pt.Convert ());

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
      MouseUp += OnMouseUp;
      MouseMove += OnMouseMove;
      MouseWheel += OnMouseWheel; ;
      MouseLeftButtonDown += OnMouseLeftButtonDown;
      #endregion

      mCords = new TextBlock () { Background = Brushes.Transparent };
      Children.Add (mCords);
   }

   void OnMouseUp (object sender, MouseButtonEventArgs e) {
      if (mIsClip && mEntities != null && mEntities.Any ()) {
         var bound = new Bound (mStartPt, mCurrentMousePt);
         foreach (var entity in mEntities.FindAll (ent => ent.Bound.IsInside (bound))) {
            entity.IsSelected = true;
         }
         mStartPt.Reset ();
         InvalidateVisual ();
      }
   }

   void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
      var pt = e.GetPosition (this).Transform (mInvProjXfm).Convert ();
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
      mCurrentMousePt = e.GetPosition (this).Transform (mInvProjXfm).Convert ();
      foreach (var entity in mEntities!) {
         if (mCurrentMousePt.HasNearestPoint (entity.Vertices, 0.5, out var nearestPoint)) {
            mSnapPoint = nearestPoint;
            break;
         }
      }
      if (mSnapPoint.IsSet) mCurrentMousePt = mSnapPoint;
      if (mCords != null) mCords.Text = $"X : {mCurrentMousePt.X}  Y : {mCurrentMousePt.Y}";
      mIsClip = e.LeftButton is MouseButtonState.Pressed;
      InvalidateVisual ();
   }

   void OnMouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      UpdateProjXform (mViewportBound.Transform (mInvProjXfm).Inflated (mCurrentMousePt, zoomFactor));
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
      var (startPt, endPt) = (Project (mStartPt), Project (mCurrentMousePt));
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
      if (mIsClip) {
         dc.DrawRectangle (Brushes.LightSteelBlue, mDwgPen, new Rect (startPt, endPt));
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
               var radius = startPt.DistanceTo (endPt);
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
                  dc.DrawLine (pen, Project (line.StartPoint), Project (line.EndPoint));
                  break;
               case Rectangle rect:
                  dc.DrawRectangle (Brushes.Transparent, pen, new Rect (Project (rect.StartPoint), Project (rect.EndPoint)));
                  break;
               case Circle circle:
                  var (center, tangent) = (Project (circle.Center), Project (circle.EndPoint));
                  var radius = center.DistanceTo (tangent);
                  dc.DrawEllipse (Brushes.Transparent, pen, Project (circle.Center), radius, radius);
                  break;
            }
         }
      }
      if (mSnapPoint.IsSet) {
         var snapSize = 5;
         var snapPt = Project (mSnapPoint);
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
   bool mIsClip;
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