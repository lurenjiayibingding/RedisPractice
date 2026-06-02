using SimpleRedis.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleRedis.Wpf;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private RedisClient? _client;
    private ServerCommands? _server;
    private StringCommands? _strings;
    private KeyCommands? _keys;

    public MainWindow()
    {
        InitializeComponent();
        CmdBox.Focus();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string prop = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    // ═══════════════════════════════════════════
    //  连接管理
    // ═══════════════════════════════════════════

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        SetConnectionUIEnabled(false);

        var host = HostBox.Text.Trim();
        if (!int.TryParse(PortBox.Text.Trim(), out var port)) port = 6379;
        var user = UserBox.Text.Trim();
        var pass = PassBox.Password;
        if (!int.TryParse(DbBox.Text.Trim(), out var db)) db = 0;

        try
        {
            _client = RedisClient.CreateClient(host, port, user, pass, db);
            await _client.ConnectAsync();

            _server = new ServerCommands(_client);
            _strings = new StringCommands(_client);
            _keys = new KeyCommands(_client);

            StatusIcon.Text = "●";
            StatusIcon.Foreground = System.Windows.Media.Brushes.LimeGreen;
            StatusText.Text = $"已连接 {host}:{port}";
            StatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;

            DisconnectBtn.IsEnabled = true;
            SendBtn.IsEnabled = true;

            AppendOutput($"[系统] 连接成功 → {host}:{port}");

            var ping = await _server.PingAsync();
            AppendOutput($"[系统] PING → {ping}");
        }
        catch (Exception ex)
        {
            StatusIcon.Text = "●";
            StatusIcon.Foreground = System.Windows.Media.Brushes.Red;
            StatusText.Text = "连接失败";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            AppendOutput($"[错误] {ex.Message}");
            SetConnectionUIEnabled(true);
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_client != null)
            {
                await _client.CloseAsync();
                _client.Dispose();
                _client = null;
                _server = null;
                _strings = null;
                _keys = null;
            }
        }
        catch { }

        StatusIcon.Text = "○";
        StatusIcon.Foreground = System.Windows.Media.Brushes.Gray;
        StatusText.Text = "未连接";
        StatusText.Foreground = System.Windows.Media.Brushes.Gray;
        DisconnectBtn.IsEnabled = false;
        SendBtn.IsEnabled = false;
        AppendOutput("[系统] 已断开连接");
        SetConnectionUIEnabled(true);
    }

    // ═══════════════════════════════════════════
    //  命令发送
    // ═══════════════════════════════════════════

    private async void Send_Click(object sender, RoutedEventArgs e) => await ExecuteCommand();

    private async void CmdBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await ExecuteCommand();
    }

    private async Task ExecuteCommand()
    {
        var raw = CmdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw) || _client == null) return;

        CmdBox.Clear();
        AppendOutput($">>> {raw}");

        try
        {
            var resp = await _client.SendCommandAsync(RESPEncoder.Encode(raw));
            if (resp.IsNull())
                AppendOutput("(nil)");
            else if (resp.Type == Enum.ResultTypeEnum.Error)
                AppendOutput($"(error) {resp.ErrorMessage}");
            else if (resp.Type == Enum.ResultTypeEnum.Array)
            {
                var arr = resp.AsArray();
                if (arr.Length == 0)
                    AppendOutput("(empty array)");
                else
                {
                    for (var i = 0; i < arr.Length; i++)
                        AppendOutput($"{i + 1}) {arr[i].AsString() ?? "(nil)"}");
                }
            }
            else
                AppendOutput(resp.AsString() ?? "(nil)");
        }
        catch (Exception ex)
        {
            AppendOutput($"[错误] {ex.Message}");
        }

        CmdBox.Focus();
    }

    private async void QuickCmd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            CmdBox.Text = cmd;
            CmdBox.CaretIndex = cmd.Length;
            CmdBox.Focus();
            await ExecuteCommand();
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Clear();
    }

    // ═══════════════════════════════════════════
    //  辅助
    // ═══════════════════════════════════════════

    private void AppendOutput(string text)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
        OutputBox.AppendText(line + Environment.NewLine);
        OutputScroll.ScrollToBottom();
    }

    private void SetConnectionUIEnabled(bool enabled)
    {
        HostBox.IsEnabled = enabled;
        PortBox.IsEnabled = enabled;
        UserBox.IsEnabled = enabled;
        PassBox.IsEnabled = enabled;
        DbBox.IsEnabled = enabled;
        ConnectBtn.IsEnabled = enabled;
    }
}
