using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UselessAI;
public partial class ScreenCaptureWindow : Window
{
    private Point _start;
    private bool _dragging;
    public Rect SelectedRect { get; private set; }
    public ScreenCaptureWindow() { InitializeComponent(); }
    private void Capture_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(CanvasLayer); _dragging = true; Selection.Visibility = Visibility.Visible;
        Canvas.SetLeft(Selection, _start.X); Canvas.SetTop(Selection, _start.Y); Selection.Width = 1; Selection.Height = 1; CaptureMouse();
    }
    private void Capture_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(CanvasLayer);
        var x = Math.Min(p.X, _start.X); var y = Math.Min(p.Y, _start.Y);
        Canvas.SetLeft(Selection, x); Canvas.SetTop(Selection, y);
        Selection.Width = Math.Abs(p.X - _start.X); Selection.Height = Math.Abs(p.Y - _start.Y);
    }
    private void Capture_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false; ReleaseMouseCapture();
        var p = e.GetPosition(CanvasLayer);
        SelectedRect = new Rect(Math.Min(p.X, _start.X), Math.Min(p.Y, _start.Y), Math.Abs(p.X - _start.X), Math.Abs(p.Y - _start.Y));
        DialogResult = SelectedRect.Width > 8 && SelectedRect.Height > 8;
        Close();
    }
    private void Capture_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { DialogResult = false; Close(); } }
}
