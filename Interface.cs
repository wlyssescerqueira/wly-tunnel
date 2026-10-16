using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WlyTunnel;

sealed partial class TunnelForm
{
    static readonly Color Background = Color.FromArgb(14, 21, 33);
    static readonly Color Surface = Color.FromArgb(22, 32, 47);
    static readonly Color Muted = Color.FromArgb(158, 178, 199);
    static readonly Color Accent = Color.FromArgb(44, 220, 181);
    readonly Label endpoint = new() { AutoSize = true };

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    void BuildInterface(out Button copy)
    {
        Text = "WLY Tunnel";
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 830);
        MinimumSize = new Size(820, 830);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Color.FromArgb(235, 242, 250);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(28, 20, 28, 16) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int height in new[] { 80, 148, 304, 40 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        Controls.Add(root);
        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(new PictureBox { Image = Icon?.ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(0, 4, 48, 48) });
        header.Controls.Add(TextLabel("WLY Tunnel", 24, FontStyle.Bold, new Point(64, 0)));
        header.Controls.Add(TextLabel("Seu banco remoto, ao alcance de uma conexão.", 10, FontStyle.Regular, new Point(66, 45), Muted));
        root.Controls.Add(header, 0, 0);

        var connection = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 16) };
        var connectionGrid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 14, 20, 14), ColumnCount = 2, RowCount = 3 };
        connectionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        connectionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        connectionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        connectionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        connectionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        connectionGrid.BackColor = Surface;
        connection.Controls.Add(connectionGrid);
        connectionGrid.Controls.Add(new Label { Text = "CONEXÃO POSTGRESQL", AutoSize = true, ForeColor = Muted, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) }, 0, 0);
        status.Font = new Font(Font.FontFamily, 16, FontStyle.Bold);
        status.ForeColor = Muted;
        status.Margin = Padding.Empty;
        connectionGrid.Controls.Add(status, 0, 1);
        endpoint.Font = new Font("Consolas", 12);
        endpoint.ForeColor = Accent;
        endpoint.Margin = new Padding(0, 6, 0, 0);
        connectionGrid.Controls.Add(endpoint, 0, 2);
        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 13, 0, 0) };
        StyleButton(connect, true); StyleButton(disconnect, false);
        actions.Controls.AddRange([connect, disconnect]);
        connectionGrid.Controls.Add(actions, 1, 0); connectionGrid.SetRowSpan(actions, 3);
        root.Controls.Add(connection, 0, 1);

        var config = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14) };
        var configGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, Padding = new Padding(18, 12, 18, 12) };
        foreach (int unused in Enumerable.Range(0, 3)) configGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        configGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        for (int i = 0; i < 3; i++) configGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3));
        var title = TextLabel("Configuração do túnel", 12, FontStyle.Bold, Point.Empty);
        configGrid.Controls.Add(title, 0, 0); configGrid.SetColumnSpan(title, 3);
        configGrid.Controls.Add(Field("Servidor SSH", host), 0, 1);
        configGrid.Controls.Add(Field("Usuário SSH", user), 1, 1);
        configGrid.Controls.Add(Field("Porta SSH", sshPort), 2, 1);
        var keyField = Field("Chave privada SSH", key);
        configGrid.Controls.Add(keyField, 0, 2); configGrid.SetColumnSpan(keyField, 3);
        configGrid.Controls.Add(Field("Porta neste computador", localPort), 0, 3);
        configGrid.Controls.Add(Field("Destino no servidor", remoteHost), 1, 3);
        configGrid.Controls.Add(Field("Porta de destino", remotePort), 2, 3);
        configGrid.BackColor = Surface;
        config.Controls.Add(configGrid);
        root.Controls.Add(config, 0, 2);

        var help = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        help.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); help.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        usage.Dock = DockStyle.Fill; usage.ForeColor = Muted; usage.Font = new Font(Font.FontFamily, 9); usage.AutoSize = false;
        help.Controls.Add(usage, 0, 0);
        copy = new Button { Text = "Copiar endereço", Dock = DockStyle.Fill };
        StyleButton(copy, false); copy.Height = 30;
        help.Controls.Add(copy, 1, 0);
        root.Controls.Add(help, 0, 3);

        var diagnostic = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12), Margin = new Padding(0, 8, 0, 0) };
        var logTitle = new Label { Text = "ATIVIDADE DA CONEXÃO", Dock = DockStyle.Top, Height = 28, ForeColor = Muted, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) };
        log.BackColor = Surface; log.ForeColor = Color.FromArgb(181, 204, 221); log.BorderStyle = BorderStyle.None;
        log.Font = new Font("Consolas", 9); log.WordWrap = true;
        diagnostic.Controls.Add(log); diagnostic.Controls.Add(logTitle);
        root.Controls.Add(diagnostic, 0, 4);
        root.Controls.Add(new Label { Text = "O X envia para a bandeja e mantém o túnel. Para encerrar, use Sair e desconectar no ícone perto do relógio.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Muted, Font = new Font(Font.FontFamily, 9) }, 0, 5);
        HandleCreated += (_, _) => { int dark = 1; DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int)); };
    }

    static Label TextLabel(string text, float size, FontStyle style, Point location, Color? color = null) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, style), Location = location, ForeColor = color ?? Color.FromArgb(235, 242, 250) };

    static Control Field(string label, TextBox input)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0, 3, 12, 5) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 9) }, 0, 0);
        var frame = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.FromArgb(32, 46, 65), Padding = new Padding(10, 7, 10, 5) };
        input.BorderStyle = BorderStyle.None; input.BackColor = frame.BackColor; input.ForeColor = Color.FromArgb(237, 244, 250);
        input.Font = new Font("Segoe UI", 10); input.Dock = DockStyle.Fill; input.AccessibleName = label;
        frame.Controls.Add(input); layout.Controls.Add(frame, 0, 1);
        return layout;
    }

    static void StyleButton(Button button, bool primary)
    {
        button.AutoSize = false; button.Size = new Size(124, 42); button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0;
        button.BackColor = primary ? Accent : Color.FromArgb(39, 54, 74);
        button.ForeColor = primary ? Background : Color.FromArgb(225, 235, 246);
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Margin = new Padding(6, 0, 0, 0);
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(81, 238, 204) : Color.FromArgb(53, 72, 96);
    }

    sealed class CardPanel : Panel
    {
        public CardPanel() { DoubleBuffered = true; BackColor = Background; Padding = new Padding(1); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            int w = Width - 1, h = Height - 1, d = 20;
            path.AddArc(0, 0, d, d, 180, 90); path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90); path.AddArc(0, h - d, d, d, 90, 90); path.CloseFigure();
            using var brush = new SolidBrush(Surface); using var pen = new Pen(Color.FromArgb(40, 55, 75));
            e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path);
        }
    }
}
