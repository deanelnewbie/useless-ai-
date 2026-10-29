using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace UselessAI;
public partial class MainWindow : Window
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private readonly SpeechSynthesizer _speaker = new();
    private PetWindow? _pet;
    private SpeechRecognitionEngine? _recognizer;
    private bool _expanded = true, _busy, _closing, _speakEnabled = true;
    private const string OllamaUrl = "http://localhost:11434/api/chat";
    private const int HOTKEY_ID = 8128;
    private const uint MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, WM_HOTKEY = 0x0312;
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    private HwndSource? _source;

    public MainWindow() { InitializeComponent(); SourceInitialized += (_, _) => RegisterGlobalHotkey(); }
    private void RegisterGlobalHotkey()
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle); _source?.AddHook(WndProc);
        RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)KeyInterop.VirtualKeyFromKey(Key.U));
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID) { Dispatcher.Invoke(() => { if (_expanded) MinimizeIsland(); else ExpandIsland(); }); handled = true; }
        return IntPtr.Zero;
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PlaceTopRight(); _pet = new PetWindow(ExpandIsland); _pet.Show();
        StatusText.Text = "Hai! Aku siap membantu";
        try { _speaker.SetOutputToDefaultAudioDevice(); _speaker.Volume = 80; _speaker.Rate = 0; } catch { }
        _ = SpeakGreetingAsync();
    }
    private async Task SpeakGreetingAsync()
    {
        await Task.Delay(700);
        try { if (_speakEnabled) _speaker.SpeakAsync("Hai, aku Useless AI. Mau dibantu apa hari ini?"); }
        catch { }
        ListeningText.Text = "Ketik pesan atau tekan mikrofon";
    }
    private void PlaceTopRight() { var work = SystemParameters.WorkArea; Left = work.Right - Width - 16; Top = work.Top + 12; }
    private void Window_Deactivated(object? sender, EventArgs e) { if (_expanded && IsLoaded && !_closing) MinimizeIsland(); }
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) { _closing = true; }
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) { if (_expanded) MinimizeIsland(); else ExpandIsland(); } else if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void MascotButton_Click(object sender, RoutedEventArgs e) => ExpandIsland();
    private void Minimize_Click(object sender, RoutedEventArgs e) => MinimizeIsland();
    private void ExpandIsland()
    {
        if (!IsLoaded) return;
        _expanded = true; _pet?.Hide();
        Width = 390; Height = 510; Shell.CornerRadius = new CornerRadius(27); Body.Visibility = Visibility.Visible;
        PlaceTopRight(); Show(); Activate(); SetForegroundWindow(new WindowInteropHelper(this).Handle);
        StatusText.Text = "Hai! Ada yang bisa kubantu?"; ListeningText.Text = "Siap mendengarkan";
        Opacity = 0; BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        _speaker.SpeakAsyncCancelAll(); try { _speaker.SpeakAsync("Hai! Ada yang bisa kubantu?"); } catch { }
        PromptBox.Focus();
    }
    private void MinimizeIsland()
    {
        if (!IsLoaded) return;
        _expanded = false; Body.Visibility = Visibility.Collapsed; Width = 74; Height = 74; Shell.CornerRadius = new CornerRadius(37);
        var work = SystemParameters.WorkArea; Left = work.Right - Width - 16; Top = work.Top + 12; Show();
        if (_pet is null) _pet = new PetWindow(ExpandIsland);
        _pet.PositionNearIsland(); _pet.Show(); StatusText.Text = "Klik untuk membuka";
    }
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(); settings.Owner = this;
        if (settings.ShowDialog() == true)
        {
            _speakEnabled = settings.SpeakEnabled; _speaker.Volume = _speakEnabled ? 80 : 0;
            _speaker.SpeakAsyncCancelAll();
            ListeningText.Text = settings.SpeakEnabled ? "Suara aktif" : "Suara dimatikan";
        }
    }
    private async void Scan_Click(object sender, RoutedEventArgs e) => await CaptureAndAskAsync();
    private async Task CaptureAndAskAsync()
    {
        MinimizeForCapture();
        await Task.Delay(250);
        var picker = new ScreenCaptureWindow();
        var result = picker.ShowDialog();
        if (result != true || picker.SelectedRect.Width < 8) { ExpandIsland(); return; }
        try
        {
            var rect = picker.SelectedRect;
            var left = (int)rect.Left; var top = (int)rect.Top; var width = (int)rect.Width; var height = (int)rect.Height;
            using var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
            var path = Path.Combine(Path.GetTempPath(), "useless-ai-screen.png"); bmp.Save(path, ImageFormat.Png);
            var model = SelectedModel();
            if (!model.Contains("llava", StringComparison.OrdinalIgnoreCase))
            {
                ReplyText.Text = $"Area layar tersimpan sementara di {path}. Untuk analisis gambar, pilih model vision (misalnya llava:7b) lalu coba lagi. Model coding biasa tidak dapat memahami gambar.";
                ExpandIsland(); return;
            }
            var image64 = Convert.ToBase64String(File.ReadAllBytes(path));
            var body = new { model, stream = false, messages = new[] { new { role = "user", content = "Jelaskan isi area layar ini dalam bahasa Indonesia. Jika berupa soal atau kode, bantu pahami dan jelaskan.", images = new[] { image64 } } } };
            var response = await _http.PostAsJsonAsync(OllamaUrl, body); response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            ReplyText.Text = json.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "Tidak ada respons.";
        }
        catch (Exception ex) { ReplyText.Text = "Gagal memproses area layar: " + ex.Message; }
        finally { ExpandIsland(); }
    }
    private void MinimizeForCapture() { _pet?.Hide(); Hide(); }
    private void Coding_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = "Pilih file kode untuk ditinjau", Filter = "File kode|*.cs;*.js;*.ts;*.py;*.html;*.css;*.json;*.xaml;*.cpp;*.h;*.java;*.go;*.rs;*.php;*.sql;*.md|Semua file|*.*", CheckFileExists = true };
        if (dlg.ShowDialog() != true) { PromptBox.Text = "Bantu aku meninjau kode ini. Cari bug dan sarankan perbaikan."; return; }
        try
        {
            var code = File.ReadAllText(dlg.FileName);
            if (code.Length > 18000) code = code[..18000] + "\n\n[Dipangkas agar sesuai batas konteks starter]";
            PromptBox.Text = $"Tinjau file `{dlg.FileName}`. Temukan bug, jelaskan alasannya, dan berikan kode perbaikan yang disarankan. Jangan mengubah file otomatis.\n\n```text\n{code}\n```";
            PromptBox.Focus(); PromptBox.CaretIndex = PromptBox.Text.Length;
            ListeningText.Text = "File dibaca; kirim untuk analisis AI";
        }
        catch (Exception ex) { ReplyText.Text = "Tidak bisa membaca file: " + ex.Message; }
    }
    private async void Send_Click(object sender, RoutedEventArgs e) => await SubmitPromptAsync();
    private async void PromptBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift) { e.Handled = true; await SubmitPromptAsync(); } }
    private string SelectedModel() => (ModelChoice.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "qwen2.5-coder:3b";
    private async Task SubmitPromptAsync()
    {
        var prompt = PromptBox.Text.Trim(); if (string.IsNullOrWhiteSpace(prompt) || _busy) return;
        PromptBox.Clear(); ReplyText.Text = "Sedang berpikir…"; StatusText.Text = "Sedang berpikir"; ListeningText.Text = "AI memproses permintaan"; _busy = true;
        try
        {
            if (await TryLaunchAppAsync(prompt)) { ReplyText.Text = "Perintah aplikasi diproses."; if (SpeakEnabled) _speaker.SpeakAsync("Baik, aku membuka aplikasi."); return; }
            var model = SelectedModel();
            var body = new { model, stream = false, messages = new object[] { new { role = "system", content = "Kamu adalah Useless AI, asisten desktop berbahasa Indonesia. Untuk coding, jelaskan temuan dan berikan patch/kode usulan yang aman. Jangan mengaku telah mengubah file atau menjalankan tindakan yang belum dilakukan. Jawab ringkas tetapi jelas." }, new { role = "user", content = prompt } } };
            using var result = await _http.PostAsJsonAsync(OllamaUrl, body); result.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            var response = json.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "(Tidak ada respons)";
            ReplyText.Text = response;
            if (SpeakEnabled) _speaker.SpeakAsync(response.Length > 500 ? response[..500] : response);
        }
        catch (HttpRequestException) { ReplyText.Text = "Tidak dapat terhubung ke Ollama. Pastikan Ollama berjalan dan model terunduh. Contoh: ollama pull qwen2.5-coder:3b"; }
        catch (TaskCanceledException) { ReplyText.Text = "Permintaan terlalu lama. Coba model lebih ringan atau periksa Ollama."; }
        catch (Exception ex) { ReplyText.Text = "Terjadi kesalahan: " + ex.Message; }
        finally { _busy = false; StatusText.Text = "Siap membantu kamu"; ListeningText.Text = "AI lokal · Ctrl+Shift+U"; }
    }
    private bool SpeakEnabled => _speakEnabled;
    private async Task<bool> TryLaunchAppAsync(string prompt)
    {
        var p = prompt.ToLowerInvariant(); string? command = null;
        if (p.Contains("buka notepad") || p.Contains("open notepad")) command = "notepad.exe";
        else if (p.Contains("buka kalkulator") || p.Contains("buka calculator") || p.Contains("open calculator")) command = "calc.exe";
        else if (p.Contains("buka paint") || p.Contains("open paint")) command = "mspaint.exe";
        else if (p.Contains("buka file explorer") || p.Contains("buka explorer") || p.Contains("open file explorer")) command = "explorer.exe";
        else if (p.Contains("buka vs code") || p.Contains("buka vscode") || p.Contains("open vs code")) command = "code";
        if (command is null) return false;
        await Task.Run(() => Process.Start(new ProcessStartInfo(command) { UseShellExecute = true })); return true;
    }
    private void Listen_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _recognizer?.Dispose(); _recognizer = new SpeechRecognitionEngine();
            _recognizer.LoadGrammar(new DictationGrammar()); _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += (_, args) => Dispatcher.Invoke(() => { PromptBox.Text = args.Result.Text; ListeningText.Text = "Terdengar: " + args.Result.Text; _ = SubmitPromptAsync(); });
            _recognizer.SpeechHypothesized += (_, args) => Dispatcher.Invoke(() => { StatusText.Text = "Mendengarkan…"; ListeningText.Text = "♫ " + args.Result.Text; });
            _recognizer.RecognizeCompleted += (_, _) => Dispatcher.Invoke(() => StatusText.Text = "Siap membantu kamu");
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            StatusText.Text = "Mendengarkan…"; ListeningText.Text = "♫ Mendengarkan · ucapkan perintah";
            if (SpeakEnabled) _speaker.SpeakAsync("Aku mendengarkan.");
        }
        catch (Exception ex) { MessageBox.Show("Tidak dapat memulai pengenalan suara. Periksa mikrofon dan speech pack Windows.\n\n" + ex.Message, "Mikrofon", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    protected override void OnClosed(EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle; UnregisterHotKey(handle, HOTKEY_ID); _source?.RemoveHook(WndProc);
        _recognizer?.Dispose(); _speaker.Dispose(); _pet?.Close(); _http.Dispose(); base.OnClosed(e);
    }
}
