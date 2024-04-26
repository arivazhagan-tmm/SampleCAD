using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VA = System.Windows.VerticalAlignment;
using HA = System.Windows.HorizontalAlignment;
using Model;
using ViewModel;

namespace View;

#region MainWindow --------------------------------------------------------------------------------
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
   #region Constructors ---------------------------------------------
   public MainWindow () {
      InitializeComponent ();
      (Height, Width) = (750, 800);
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      WindowState = WindowState.Maximized;
      WindowStyle = WindowStyle.SingleBorderWindow;
      Loaded += OnLoaded;
   }
   #endregion

   #region Properties -----------------------------------------------
   public static MainWindow? It { get; private set; }
   #endregion

   #region Implementation -------------------------------------------
   void OnButtonClicked (object sender, EventArgs e) {
      if (sender is not ToggleButton btn || mViewport is null) return;
      if (!Enum.TryParse ($"{btn.Content}", out EEntity entity)) return;
      Widget? widget = null;
      switch (entity) {
         case EEntity.Line: widget = new LineWidget (); break;
         case EEntity.Rectangle: widget = new RectWidget (); break;
         case EEntity.Circle: widget = new CircleWidget (); break;
         default: break;
      }
      mViewport.SetWidget (widget!);
      ResetSelection ();
      btn.IsChecked = true;
      mPromptPanel ??= new ContentControl ();
      mPromptPanel.Content = widget != null && widget.Params != null ? new PromptPanel (widget, widget.Params)
                                                                     : new PromptPanel ();
   }

   void OnLoaded (object sender, RoutedEventArgs e) {
      Title = "CAD Demo";
      It = this;
      mPromptPanel ??= new ();
      mBGColor = new SolidColorBrush (Color.FromArgb (255, 200, 200, 200));
      Background = mBGColor;
      var spStyle = new Style ();
      spStyle.Setters.Add (new Setter (HeightProperty, 20.0));
      spStyle.Setters.Add (new Setter (VerticalContentAlignmentProperty, VA.Top));
      spStyle.Setters.Add (new Setter (BackgroundProperty, mBGColor));
      var menuPanel = new StackPanel () { Style = spStyle };
      var menuStyle = new Style ();
      menuStyle.Setters.Add (new Setter (WidthProperty, 50.0));
      menuStyle.Setters.Add (new Setter (HeightProperty, 20.0));
      var menu = new Menu () { Background = mBGColor };
      var fileMenu = new MenuItem () { Style = menuStyle, Header = "_File" };
      fileMenu.Items.Add (new MenuItem () { Header = "_Open...", Command = ApplicationCommands.Open });
      fileMenu.Items.Add (new MenuItem () { Header = "_Save", Command = ApplicationCommands.Save });
      fileMenu.Items.Add (new MenuItem () { Header = "_SaveAs...", Command = ApplicationCommands.SaveAs });
      var editMenu = new MenuItem () { Style = menuStyle, Header = "_Edit" };
      editMenu.Items.Add (new MenuItem () { Header = "_Undo", Command = ApplicationCommands.Undo });
      editMenu.Items.Add (new MenuItem () { Header = "_Redo", Command = ApplicationCommands.Redo });
      editMenu.Items.Add (new MenuItem () { Header = "_Delete", Command = ApplicationCommands.Delete });
      editMenu.Items.Add (new MenuItem () { Header = "_SelectAll", Command = ApplicationCommands.SelectAll });
      menu.Items.Add (fileMenu);
      menu.Items.Add (editMenu);
      menuPanel.Children.Add (menu);

      var optionPanel = new StackPanel () { Name = "CadOptions", HorizontalAlignment = HA.Left, Margin = new Thickness (0, 50, 0, 0) };
      var btnStyle = new Style ();
      btnStyle.Setters.Add (new Setter (WidthProperty, 80.0));
      btnStyle.Setters.Add (new Setter (HeightProperty, 25.0));
      btnStyle.Setters.Add (new Setter (BackgroundProperty, Brushes.White));
      btnStyle.Setters.Add (new Setter (MarginProperty, new Thickness (5.0)));
      btnStyle.Setters.Add (new Setter (HorizontalAlignmentProperty, HA.Left));
      btnStyle.Setters.Add (new Setter (VerticalAlignmentProperty, VA.Center));
      var borderStyle = new Style () { TargetType = typeof (Border) };
      borderStyle.Setters.Add (new Setter (Border.CornerRadiusProperty, new CornerRadius (5.0)));
      borderStyle.Setters.Add (new Setter (Border.BorderThicknessProperty, new Thickness (1.0)));
      btnStyle.Resources = new ResourceDictionary { [typeof (Border)] = borderStyle };
      foreach (var name in Enum.GetNames (typeof (EEntity))) {
         var btn = new ToggleButton () { Content = name, Style = btnStyle };
         btn.Click += OnButtonClicked;
         optionPanel.Children.Add (btn);
      }
      mViewport = new Viewport ();
      var viewportPanel = new WrapPanel ();
      viewportPanel.MouseEnter += (s, e) => Cursor = Cursors.Cross;
      viewportPanel.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
      viewportPanel.MouseDown += (s, e) => {
         if (e.ChangedButton is MouseButton.Middle && e.ButtonState is MouseButtonState.Pressed)
            Cursor = Cursors.Hand;
      };
      viewportPanel.MouseUp += (s, e) => Cursor = Cursors.Cross;
      viewportPanel.SizeChanged += (s, e) => {
         if (mViewport is null) return;
         mViewport.UpdateAxis ();
      };
      KeyDown += (s, e) => {
         if (e.Key is Key.Escape && mViewport != null) {
            mViewport.Refresh ();
         }
      };
      var context = new ContextMenu ();
      var clear = new MenuItem () { Header = "Clear" };
      clear.Click += (s, e) => { mViewport.Entities.Clear (); };
      context.Items.Add (clear);
      viewportPanel.ContextMenu = context;
      viewportPanel.Children.Add (mViewport);
      var sp = new StackPanel ();
      var dp = new DockPanel () { LastChildFill = true };
      dp.Children.Add (menuPanel);
      dp.Children.Add (mPromptPanel);
      dp.Children.Add (optionPanel);
      dp.Children.Add (viewportPanel);
      DockPanel.SetDock (menuPanel, Dock.Top);
      DockPanel.SetDock (mPromptPanel, Dock.Bottom);
      DockPanel.SetDock (optionPanel, Dock.Left);
      mCadUI.Content = dp;
   }

   void ResetSelection () {
      foreach (var control in (mCadUI.Content as DockPanel)?.Children.OfType<StackPanel> ().
      First (c => c.Name is "CadOptions").Children.OfType<ToggleButton> ()!) control.IsChecked = false;
   }
   #endregion

   #region Private Data ---------------------------------------------
   Brush? mBGColor;
   Viewport? mViewport;
   ContentControl? mPromptPanel;
   #endregion
}
#endregion

#region PromptPanel -------------------------------------------------------------------------------
internal sealed class PromptPanel : UserControl {
   #region Constructors ---------------------------------------------
   public PromptPanel () { }

   public PromptPanel (Widget widget, params string[] labels) {
      mWidget = widget;
      var width = 50;
      var sp = new StackPanel { Orientation = Orientation.Horizontal };
      var tblock = new TextBlock () {
         Margin = new Thickness (10.0),
         FontWeight = FontWeights.Bold,
      };
      tblock.SetBinding (TextBlock.TextProperty, new Binding (nameof(mWidget.Prompt)) { Source = widget, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
      sp.Children.Add (tblock);
      for (int i = 0, len = labels.Length; i < len; i++) {
         var str = labels[i];
         var label = new Label () { Content = str + ":", Margin = new Thickness (10.0)};
         var tb = new TextBox () {
            Name = str + "TextBox",
            Width = width,
            Height = 20,
         };
         tb.PreviewKeyDown += OnPreviewKeyDown;
         var binding = new Binding (str) { Source = widget, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
         tb.SetBinding (TextBox.TextProperty, binding);
         sp.Children.Add (label);
         sp.Children.Add (tb);
      }
      Content = sp;
   }
   #endregion

   #region Implementation -------------------------------------------
   void OnPreviewKeyDown (object sender, KeyEventArgs e) {
      var key = e.Key;
      e.Handled = !((key is >= Key.D0 and <= Key.D9) ||
                    (key is >= Key.NumPad0 and <= Key.NumPad9) ||
                    (key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Tab));
      if (key is Key.Enter) mWidget?.ReceiveInput (CadPoint.Default);
   }
   #endregion

   #region Private Data ---------------------------------------------
   readonly Widget? mWidget;
   #endregion
}
#endregion