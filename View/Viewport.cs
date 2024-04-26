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
      mEndPt.Reset ();
      mWidget?.Initialize ();
      InvalidateVisual ();
   }

   public void SetWidget (Widget widget) {
      mWidget = widget;
      mStartPt.Reset ();
      mEndPt.Reset ();
   }
   #endregion

   #region Implementation -------------------------------------------
   CadPoint ToCadPoint (Point pt) => new (pt.X - mViewportCenter.X, mViewportCenter.Y - pt.Y);

   Point ToWindowsPoint (CadPoint pt) => new (pt.X + mViewportCenter.X, mViewportCenter.Y - pt.Y);

   void OnLoaded (object sender, RoutedEventArgs e) {
      mEntities = [];
      Height = 650;
      Width = 750;
      mDwgLineWeight = 1.0;
      mStartPt = mEndPt = CadPoint.Default;
      Background = Brushes.Transparent;
      mDwgBrush = Brushes.ForestGreen;
      mAxisPen = new (Brushes.Gray, 0.5);
      mBGPen = new (Brushes.LightGray, 1.5);
      mDwgPen = new (Brushes.Black, mDwgLineWeight);
      mPreviewPen = new (mDwgBrush, mDwgLineWeight);
      MouseMove += OnMouseMove;
      MouseUp += (s, e) => { };
      MouseLeftButtonDown += OnMouseLeftButtonDown;
      mCords = new TextBlock () { Background = Brushes.Transparent };
      Children.Add (mCords);
   }

   void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
      var pt = ToCadPoint (e.GetPosition (this));
      if (!mStartPt.IsSet) mStartPt = pt;
      if (mWidget != null) {
         mWidget.ReceiveInput (pt);
         if (mWidget.Entity != null) {
            mEntities?.Add (mWidget.Entity);
            mWidget.Initialize ();
            mStartPt.Reset ();
            mEndPt.Reset ();
         }
      }
      InvalidateVisual ();
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      mEndPt = ToCadPoint (e.GetPosition (this));
      if (mCords != null) mCords.Text = $"X : {mEndPt.X}  Y : {mEndPt.Y}";
      InvalidateVisual ();
   }

   public void UpdateAxis () {
      if (MainWindow.It is null) return;
      mViewportRect = new Rect (new Size (MainWindow.It.ActualWidth - 120, MainWindow.It.ActualHeight - 100));
      var (w, h) = (mViewportRect.Width, mViewportRect.Height);
      mHAxis = new (new (0.0, h / 2), new (w, h / 2));
      mVAxis = new (new (w / 2, 0.0), new (w / 2, h));
      mViewportCenter = new CadPoint (w, h) * 0.5;
      InvalidateVisual ();
   }

   protected override void OnRender (DrawingContext dc) {
      if (mStartPt.IsSet && mEndPt.IsSet) {
         switch (mWidget) {
            case LineWidget:
               dc.DrawLine (mPreviewPen, ToWindowsPoint (mStartPt), ToWindowsPoint (mEndPt));
               break;
            case RectWidget:
               dc.DrawRectangle (Brushes.Transparent, mPreviewPen, new Rect (ToWindowsPoint (mStartPt), ToWindowsPoint (mEndPt)));
               break;
            case CircleWidget:
               var radius = mStartPt.DistanceTo (mEndPt);
               dc.DrawEllipse (Brushes.Transparent, mPreviewPen, ToWindowsPoint (mStartPt), radius, radius);
               break;
         }
      }
      if (mEntities != null) {
         foreach (var entity in mEntities) {
            switch (entity) {
               case Line line:
                  dc.DrawLine (mDwgPen, ToWindowsPoint (line.StartPoint), ToWindowsPoint (line.EndPoint));
                  break;
               case Rectangle rect:
                  dc.DrawRectangle (Brushes.Transparent, mDwgPen, new Rect (ToWindowsPoint (rect.StartPoint), ToWindowsPoint (rect.EndPoint)));
                  break;
               case Circle circle:
                  dc.DrawEllipse (Brushes.Transparent, mDwgPen, ToWindowsPoint (circle.Center), circle.Radius, circle.Radius);
                  break;
            }
         }
      }
      dc.DrawRectangle (Background, mBGPen, mViewportRect);
      dc.DrawLine (mAxisPen, mHAxis.P1, mHAxis.P2);
      dc.DrawLine (mAxisPen, mVAxis.P1, mVAxis.P2);
      base.OnRender (dc);
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mDwgLineWeight;
   Rect mViewportRect;
   Pen? mAxisPen, mBGPen, mDwgPen, mPreviewPen;
   Brush? mDwgBrush;
   CadPoint mEndPt, mStartPt, mViewportCenter;
   List<Entity>? mEntities;
   Widget? mWidget;
   TextBlock? mCords;
   Axis mHAxis, mVAxis;
   #endregion
}
#endregion

#region struct Axis -------------------------------------------------------------------------------
internal readonly record struct Axis (Point P1, Point P2);
#endregion