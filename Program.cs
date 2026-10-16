using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace WlyTunnel;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TunnelForm());
    }
}

record Settings(string Host, string User, string SshPort, string Key, string LocalPort, string RemoteHost, string RemotePort);

sealed partial class TunnelForm : Form
{
    readonly TextBox host = new() { Text = "167.99.1.163" };
    readonly TextBox user = new() { Text = "root" };
    readonly TextBox sshPort = new() { Text = "22" };
    readonly TextBox key = new() { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "id_rsa") };
    readonly TextBox localPort = new() { Text = "15432" };
    readonly TextBox remoteHost = new() { Text = "127.0.0.1" };
    readonly TextBox remotePort = new() { Text = "15432" };
    readonly Label status = new() { Text = "Desconectado", AutoSize = true, ForeColor = Color.DarkRed };
    readonly Label usage = new() { AutoSize = true };
    readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    readonly Button connect = new() { Text = "Conectar", AutoSize = true };
    readonly Button disconnect = new() { Text = "Desconectar", AutoSize = true, Enabled = false };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 2000 };
    readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WlyTunnel", "settings.json");
    Process? tunnel;
    bool busy;
    bool exiting;
    readonly NotifyIcon tray = new();
    readonly ToolStripMenuItem trayConnect = new("Conectar");
    readonly ToolStripMenuItem trayDisconnect = new("Desconectar");

    public TunnelForm()
    {
        BuildInterface(out var copy);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir WLY Tunnel", null, (_, _) => RestoreWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(trayConnect);
        menu.Items.Add(trayDisconnect);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair e desconectar", null, (_, _) =>
        {
            if (busy) { RestoreWindow(); WriteLog("Aguarde a tentativa de conexão terminar antes de sair."); return; }
            exiting = true;
            Close();
        });
        tray.Icon = Icon;
        tray.Text = "WLY Tunnel — desconectado";
        tray.ContextMenuStrip = menu;
        tray.Visible = true;
        tray.DoubleClick += (_, _) => RestoreWindow();
        trayConnect.Click += async (_, _) => await Connect();
        trayDisconnect.Click += (_, _) => Stop();
        status.TextChanged += (_, _) => tray.Text = $"WLY Tunnel — {status.Text}";
        FormClosed += (_, _) => { tray.Visible = false; tray.Dispose(); menu.Dispose(); };
        SetRunning(false);
        LoadSettings();
        foreach (var field in new[] { localPort, remoteHost, remotePort }) field.TextChanged += (_, _) => UpdateUsage();
        UpdateUsage();
        copy.Click += (_, _) => { try { Clipboard.SetText($"127.0.0.1:{localPort.Text.Trim()}"); } catch (Exception ex) { WriteLog(ex.Message); } };
        connect.Click += async (_, _) => await Connect();
        disconnect.Click += (_, _) => Stop();
        timer.Tick += (_, _) => { if (!busy && tunnel is { HasExited: true }) { WriteLog($"SSH encerrado (código {tunnel.ExitCode}). Consulte o diagnóstico acima e conecte novamente."); Release(); } };
        timer.Start();
        FormClosing += (_, e) =>
        {
            if (!exiting && e.CloseReason is CloseReason.UserClosing or CloseReason.TaskManagerClosing)
            {
                e.Cancel = true;
                Hide();
                tray.ShowBalloonTip(3000, "WLY Tunnel está na bandeja", "O túnel continua no estado atual. Dê dois cliques no ícone para abrir ou use o botão direito para sair.", ToolTipIcon.Info);
                return;
            }
            timer.Stop();
            Stop();
        };
        WriteLog("Pronto. Os dados ficam salvos neste usuário do Windows. A chave é usada pelo OpenSSH; seu conteúdo não é copiado.");
    }

    void RestoreWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    void UpdateUsage()
    {
        endpoint.Text = $"127.0.0.1:{localPort.Text}  →  {remoteHost.Text}:{remotePort.Text}";
        usage.Text = "Use o endereço local nos aplicativos. Mantenha usuário, senha e nome do banco existentes.";
    }
    void WriteLog(string message)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired) { try { BeginInvoke(() => WriteLog(message)); } catch (InvalidOperationException) { } return; }
        log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
    void SetRunning(bool running)
    {
        foreach (var field in new[] { host, user, sshPort, key, localPort, remoteHost, remotePort }) field.Enabled = !running;
        connect.Enabled = !running;
        disconnect.Enabled = running && !busy;
        trayConnect.Enabled = connect.Enabled;
        trayDisconnect.Enabled = disconnect.Enabled;
    }
    async Task Connect()
    {
        if (busy || tunnel != null) return;
        try
        {
            int Port(TextBox field) => int.TryParse(field.Text, out int port) && port is > 0 and <= 65535 ? port : throw new Exception("As portas devem estar entre 1 e 65535.");
            int lp = Port(localPort), sp = Port(sshPort), rp = Port(remotePort);
            foreach (var field in new[] { host, user, remoteHost })
                if (string.IsNullOrWhiteSpace(field.Text) || field.Text.Trim().StartsWith('-') || field.Text.Any(char.IsWhiteSpace)) throw new Exception("Preencha servidor, usuário e destino sem espaços.");
            if (!File.Exists(key.Text.Trim())) throw new Exception("Arquivo de chave SSH não encontrado. Informe o caminho completo.");
            var listener = new TcpListener(IPAddress.Loopback, lp);
            try { listener.Start(); } catch (SocketException) { throw new Exception($"A porta {lp} já está em uso por outro processo ou túnel. Ele não será encerrado por este app."); } finally { listener.Stop(); }
            busy = true;
            SetRunning(true);
            status.Text = "Conectando…";
            status.ForeColor = Color.FromArgb(255, 202, 100);
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(new Settings(host.Text.Trim(), user.Text.Trim(), sp.ToString(), key.Text.Trim(), lp.ToString(), remoteHost.Text.Trim(), rp.ToString()), new JsonSerializerOptions { WriteIndented = true }));
            var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "OpenSSH", "ssh.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var arg in new[] { "-N", "-T", "-L", $"127.0.0.1:{lp}:{remoteHost.Text.Trim()}:{rp}", "-p", sp.ToString(), "-i", key.Text.Trim(), "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes", "-o", "ExitOnForwardFailure=yes", "-o", "ConnectTimeout=10", "-o", "ServerAliveInterval=30", "-o", "ServerAliveCountMax=3", $"{user.Text.Trim()}@{host.Text.Trim()}" }) start.ArgumentList.Add(arg);
            tunnel = new Process { StartInfo = start };
            tunnel.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) WriteLog(e.Data); };
            tunnel.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) WriteLog(e.Data); };
            tunnel.Start();
            tunnel.BeginErrorReadLine(); tunnel.BeginOutputReadLine();
            WriteLog($"Conectando a {user.Text}@{host.Text}:{sp}. Processo SSH: {tunnel.Id}.");
            bool ready = false;
            for (int i = 0; i < 60 && !tunnel.HasExited; i++)
            {
                using var client = new TcpClient();
                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, lp).WaitAsync(TimeSpan.FromMilliseconds(250));
                    // PostgreSQL SSLRequest: uma resposta S/N confirma que o encaminhamento alcança o banco.
                    await client.GetStream().WriteAsync(new byte[] { 0, 0, 0, 8, 4, 210, 22, 47 });
                    byte[] response = new byte[1];
                    int count = await client.GetStream().ReadAsync(response).AsTask().WaitAsync(TimeSpan.FromSeconds(2));
                    if (count == 1 && response[0] is (byte)'S' or (byte)'N') { ready = true; break; }
                }
                catch (Exception ex) when (ex is SocketException or IOException or TimeoutException) { }
                await Task.Delay(200);
            }
            if (!ready) throw new Exception("Não foi possível confirmar o PostgreSQL no destino. Confira o diagnóstico SSH. Para servidor novo, faça a primeira conexão pelo terminal e confirme a identidade do servidor.");
            status.Text = "Conectado ao PostgreSQL";
            status.ForeColor = Accent;
            WriteLog($"Túnel confirmado: 127.0.0.1:{lp} → {remoteHost.Text}:{rp} no servidor.");
        }
        catch (Exception ex) { WriteLog(ex.Message); Stop(); }
        finally { busy = false; SetRunning(tunnel != null); }
    }
    void Stop()
    {
        if (tunnel != null)
        {
            try { if (!tunnel.HasExited) { tunnel.Kill(); tunnel.WaitForExit(3000); } } catch (InvalidOperationException) { }
            WriteLog("Túnel deste app desconectado.");
        }
        Release();
    }
    void Release()
    {
        tunnel?.Dispose(); tunnel = null;
        status.Text = "Desconectado"; status.ForeColor = Muted; SetRunning(false);
    }
    void LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsPath)) return;
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
            if (s == null) return;
            host.Text = s.Host; user.Text = s.User; sshPort.Text = s.SshPort; key.Text = s.Key;
            localPort.Text = s.LocalPort; remoteHost.Text = s.RemoteHost; remotePort.Text = s.RemotePort;
        }
        catch (Exception ex) { WriteLog($"Não foi possível carregar configuração: {ex.Message}"); }
    }
}
