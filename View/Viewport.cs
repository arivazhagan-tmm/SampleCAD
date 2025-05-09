using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using Model;
using ViewModel;
using System.Security.Cryptography.Xml;

namespace View;

#region class Viewport ----------------------------------------------------------------------------
internal sealed class Viewport : Canvas {
   #region Constructors ---------------------------------------------
   public Viewport () => Loaded += OnLoaded;
   #endregion

   #region Properties -----------------------------------------------
   public List<Entity> Entities => mEntities ??= [];
   public IEnumerable<Entity> SelectedEntities => mEntities?.Where (e => e.IsSelected)!;
   #endregion

   #region Methods --------------------------------------------------
   public void Add (Entity entity) {
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

   public void Remove (Entity entity) {
      if (entity is null || mEntities is null || !mEntities.Contains (entity)) return;
      mEntities.Remove (entity);
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
      var bound = mEntities is null || mEntities.Count is 0 ? mViewportBound : new Bound (mEntities.Select (e => e.Bound));
      UpdateProjXform (bound);
      InvalidateVisual ();
   }
   #endregion

   #region Implementation -------------------------------------------
   Point Convert (CadPoint pt) => mProjXfm.Transform (pt.Convert ());

   void OnLoaded (object sender, RoutedEventArgs e) {
      Height = 500;
      Width = 500;
      Background = Brushes.Transparent;

      mEntities = [];
      mStartPt = mCurrentMousePt = CadPoint.Default;
      mDwgLineWeight = 1.0;
      mDwgBrush = Brushes.ForestGreen;
      mAxisPen = new (Brushes.SlateGray, 0.5);
      mBGPen = new (Brushes.LightCyan, 0.5);
      mDwgPen = new (Brushes.Black, mDwgLineWeight);
      mPreviewPen = new (mDwgBrush, mDwgLineWeight);
      mOrthoPen = new Pen (Brushes.Blue, 1.0) { DashStyle = DashStyles.Dash };

      if (MainWindow.It != null)
         mViewportRect = new Rect (new Size (MainWindow.It.ActualWidth - 120, MainWindow.It.ActualHeight - 100));
      //mViewportRect = new Rect (new Size (500.0, 500.0));
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
         foreach (var entity in mEntities.FindAll (ent => ent.Bound.IsInside (bound))) entity.IsSelected = true;
         InvalidateVisual ();
      }
      if (mIsClip && mWidget is null) mStartPt.Reset ();
   }

   void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
      var pt = e.GetPosition (this).Transform (mInvProjXfm).Convert ();
      if (mSnapPoint.IsSet) pt = mSnapPoint;
      if (!mStartPt.IsSet) mStartPt = pt;
      if (mWidget != null) {
         mWidget.ReceiveInput (pt);
         if (mWidget is not ITransform && mWidget.Entity != null) {
            var entity = mWidget.Entity;
            mEntities?.Add (entity);
            ResetValues ();
         } else if (mWidget.TransformedEntities != null && mWidget.TransformedEntities.Any ()) {
            mWidget.OriginalEntities.ToList ().ForEach (Remove);
            mWidget.TransformedEntities.ToList ().ForEach (Add);
            ResetValues ();
         }
      }
      InvalidateVisual ();

      void ResetValues () {
         mWidget.Initialize ();
         mStartPt.Reset ();
         mCurrentMousePt.Reset ();
      }
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      mSnapPoint.Reset ();
      mCurrentMousePt = e.GetPosition (this).Transform (mInvProjXfm).Convert ();
      foreach (var entity in mEntities!) {
         if (mCurrentMousePt.HasNearestPoint (entity.Vertices, mSnapTolerance, out var nearestPoint)) {
            mSnapPoint = nearestPoint;
            mCurrentMousePt = mSnapPoint;
            break;
         }
      }
      var center = mViewportCenter.Convert ();
      if (!mSnapPoint.IsSet && mCurrentMousePt.DistanceTo (center) <= mSnapTolerance) mCurrentMousePt = mSnapPoint = center;
      if (mCords != null) mCords.Text = $"X : {mCurrentMousePt.X}  Y : {mCurrentMousePt.Y}";
      mIsClip = e.LeftButton is MouseButtonState.Pressed && mWidget is null;
      if (mWidget != null) {
         if (mWidget.Entity != null) {
            Add (mWidget.Entity);
            mStartPt.Reset ();
         } else if (mWidget.StartPoint.IsSet) mStartPt = mWidget.StartPoint;
         InvalidateVisual ();
      }
      InvalidateVisual ();
   }

   double mSnapTolerance;

   void OnMouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      UpdateProjXform (mViewportBound.Transform (mInvProjXfm).Inflated (mCurrentMousePt, zoomFactor));
      InvalidateVisual ();
   }
   
   void UpdateProjXform (Bound b) {
      var margin = 0.0;
      double scaleX = (mViewportWidth - margin) / b.Width,
             scaleY = (mViewportHeight - margin) / b.Height;
      double scale = Math.Min (scaleX, scaleY);
      var xfm = Matrix.Identity;
      xfm.Scale (scale, -scale);
      Point projectedMidPt = xfm.Transform (new Point (b.Mid.X, b.Mid.Y));
      var (dx, dy) = (mViewportCenter.X - projectedMidPt.X, mViewportCenter.Y - projectedMidPt.Y);
      xfm.Translate (dx, dy);
      mProjXfm = xfm;
      mInvProjXfm = mProjXfm;
      mInvProjXfm.Invert ();
      mSnapTolerance = b.MaxX * 0.01;
   }

   protected override void OnRender (DrawingContext dc) {
      #region Showing Grids ---------------------
      var (pen1, pen2) = (new Pen (Brushes.DimGray, 0.15), new Pen (Brushes.DimGray, 0.3));
      var (w, h) = (mViewportWidth, mViewportHeight);
      for (int i = 0; i < w; i += 30) {
         var j = i * 5;
         dc.DrawLine (pen1, new (0, i), new (w, i));
         dc.DrawLine (pen2, new (0, j), new (w, j));
         dc.DrawLine (pen1, new (i, 0), new (i, h));
         dc.DrawLine (pen2, new (j, 0), new (j, h));
      }
      #endregion

      var (startPt, endPt) = (Convert (mStartPt), Convert (mCurrentMousePt));
      if (mSnapPoint.IsSet) {
         dc.DrawLine (mOrthoPen, new (0, endPt.Y), new (mViewportWidth, endPt.Y));
         dc.DrawLine (mOrthoPen, new (endPt.X, 0), new (endPt.X, mViewportHeight));
      }
      var angle = mStartPt.AngleTo (mCurrentMousePt);
      if (angle is < 2.0 and > -2 or > 178.0 and < 182.0) {
         dc.DrawLine (mOrthoPen, new (0, startPt.Y), new (mViewportRect.Width, startPt.Y));
         mSnapPoint = new (mCurrentMousePt.X, mStartPt.Y);
      }
      if (angle is > 88.0 and < 92.0 or > 268 and < 272) {
         dc.DrawLine (mOrthoPen, new (startPt.X, 0), new (startPt.X, mViewportRect.Height));
         mSnapPoint = new (mStartPt.X, mCurrentMousePt.Y);
      }

      #region Commented --------------------------------------------------
      //if (mCurrentMousePt.Y is < delta and > -delta) {
      //   dc.DrawLine (mOrthoPen, new (0, mViewportCenter.Y), new (mViewportRect.Width, mViewportCenter.Y));
      //   mSnapPoint = new (mCurrentMousePt.X, 0);
      //}
      //if (mCurrentMousePt.X is < delta and > -delta) {
      //   dc.DrawLine (mOrthoPen, new (mViewportCenter.X, 0), new (mViewportCenter.X, mViewportRect.Height));
      //   mSnapPoint = new (0, mCurrentMousePt.Y);
      //}
      //if (mEntities != null && mEntities.Any (e => e.Vertices.Any (v => v.X < mCurrentMousePt.X + delta && v.X > mCurrentMousePt.X - delta))) {
      //   dc.DrawLine (mOrthoPen, new (endPt.X, 0), new (endPt.X, mViewportRect.Height));
      //   mSnapPoint = mCurrentMousePt;
      //}
      //if (mEntities != null && mEntities.Any (e => e.Vertices.Any (v => v.Y < mCurrentMousePt.Y + delta && v.Y > mCurrentMousePt.Y - delta))) {
      //   dc.DrawLine (mOrthoPen, new (0, endPt.Y), new (mViewportRect.Width, endPt.Y));
      //   mSnapPoint = mCurrentMousePt;
      //}
      #endregion

      if (mIsClip && mWidget is null)
         dc.DrawRectangle (Brushes.LightSteelBlue, mOrthoPen, new Rect (startPt, endPt));
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
            case PlaneWidget:
               var dist = startPt.DistanceTo (endPt);
               var (v1, v2) = (startPt.RadialMove (dist, -45.0), endPt.RadialMove (dist, -45.0));
               dc.DrawLine (mPreviewPen, startPt, endPt);
               dc.DrawLine (mPreviewPen, startPt, v1);
               dc.DrawLine (mPreviewPen, endPt, v2);
               dc.DrawLine (mPreviewPen, v1, v2);
               break;
            case TranslateWidget:
               var (dx, dy) = startPt.Delta (endPt);
               var v = new Vector (dx, dy);
               foreach (var entity in SelectedEntities) {
                  switch (entity) {
                     case Line line:
                        dc.DrawLine (mPreviewPen, Convert (line.StartPoint) + v, Convert (line.EndPoint) + v);
                        break;
                     case Rectangle rect:
                        dc.DrawRectangle (Brushes.Transparent, mPreviewPen, new Rect (Convert (rect.StartPoint) + v, Convert (rect.EndPoint) + v));
                        break;
                  }
               }
               break;
            case ScaleWidget:
               var scale = startPt.DistanceTo (endPt);
               var xfm = CadMatrix.Scale (scale, scale);
               foreach (var entity in SelectedEntities) {
                  switch (entity) {
                     case Line line:
                        dc.DrawLine (mPreviewPen, Convert (line.StartPoint * xfm), Convert (line.EndPoint * xfm));
                        break;
                     case Rectangle rect:
                        dc.DrawRectangle (Brushes.Transparent, mPreviewPen,
                        new Rect (Convert (rect.StartPoint * xfm), Convert (rect.EndPoint * xfm)));
                        break;
                  }
               }
               break;
         }
      }
      if (mEntities != null) {
         foreach (var entity in mEntities) {
            var clr = entity.Layer;
            var pen = new Pen (new SolidColorBrush (Color.FromRgb (clr.R, clr.G, clr.B)), entity.LineWeight);
            switch (entity) {
               case Line line:
                  dc.DrawLine (pen, Convert (line.StartPoint), Convert (line.EndPoint));
                  break;
               case Rectangle rect:
                  dc.DrawRectangle (Brushes.Transparent, pen, new Rect (Convert (rect.StartPoint), Convert (rect.EndPoint)));
                  break;
               case Circle circle:
                  var (center, tangent) = (Convert (circle.Center), Convert (circle.EndPoint));
                  var radius = center.DistanceTo (tangent);
                  dc.DrawEllipse (Brushes.Transparent, pen, Convert (circle.Center), radius, radius);
                  break;
               case Plane p:
                  var v = p.Vertices.Select (Convert).ToArray ();
                  dc.DrawLine (pen, v[0], v[1]);
                  dc.DrawLine (pen, v[0], v[2]);
                  dc.DrawLine (pen, v[1], v[3]);
                  dc.DrawLine (pen, v[2], v[3]);
                  break;
            }
         }
      }
      if (mSnapPoint.IsSet) {
         var snapSize = 5;
         var snapPt = Convert (mSnapPoint);
         var v = new Vector (snapSize, snapSize);
         dc.DrawRectangle (Brushes.White, mDwgPen, new (snapPt - v, snapPt + v));
      }
      var pt = Convert (mViewportCenter.Convert ());
      dc.DrawEllipse (Brushes.White, mDwgPen, pt, 5.0, 5.0);
      mVAxis = new (new (pt.X, 0), new (pt.X, mViewportHeight));
      mHAxis = new (new (0, pt.Y), new (mViewportWidth, pt.Y));
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
   CadPoint mCurrentMousePt, mStartPt, mSnapPoint;
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