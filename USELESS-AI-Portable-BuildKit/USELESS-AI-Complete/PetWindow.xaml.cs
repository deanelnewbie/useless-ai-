using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace UselessAI;
public partial class PetWindow : Window
{
    private readonly Action _open;
    private readonly DispatcherTimer _idleTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly DispatcherTimer _eyeTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly string[] _phrases = { "Hai! Klik aku", "Mau dibantu?", "Aku di sini ☁", "Yuk, ngoding!", "Zzz… eh, halo!" };
    private int _phraseIndex;
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);

    public PetWindow(Action open) { InitializeComponent(); _open = open; }
    private void Pet_Loaded(object sender, RoutedEventArgs e)
    {
        PositionNearIsland();
        _idleTimer.Tick += (_, _) => { _phraseIndex = (_phraseIndex + 1) % _phrases.Length; Caption.Text = _phrases[_phraseIndex]; };
        _idleTimer.Start();
        _eyeTimer.Tick += (_, _) =>
        {
            if (GetCursorPos(out var pt))
            {
                var screenPoint = PointFromScreen(new Point(pt.X, pt.Y));
                var x = Math.Clamp((screenPoint.X - 56) / 18, -1, 1);
                var y = Math.Clamp((screenPoint.Y - 38) / 30, -1, 1);
                Eye1.RenderTransform = new TranslateTransform(x * 2, y * 1.5);
                Eye2.RenderTransform = new TranslateTransform(x * 2, y * 1.5);
            }
        };
        _eyeTimer.Start();
        PetStack.RenderTransform = new TranslateTransform();
        PetStack.RenderTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-4, 4, TimeSpan.FromSeconds(1.7)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
    }
    public void PositionNearIsland()
    {
        var work = SystemParameters.WorkArea;
        Left = work.Right - Width - 8; Top = work.Top + 75;
    }
    private void Pet_Click(object sender, MouseButtonEventArgs e) { Caption.Text = "Yay! Ayo ngobrol"; _open(); }
    protected override void OnClosed(EventArgs e) { _idleTimer.Stop(); _eyeTimer.Stop(); base.OnClosed(e); }
}
