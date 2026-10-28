namespace WlyTunnel;

sealed partial class TunnelForm
{
    void ShowSetupGuide()
    {
        using var stream = typeof(TunnelForm).Assembly.GetManifestResourceStream("WlyTunnel.PASSO-A-PASSO.txt")!;
        using var reader = new StreamReader(stream);
        using var dialog = new Form
        {
            Text = "WLY Tunnel — Passo a passo",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(800, 720),
            MinimumSize = new Size(560, 440),
            BackColor = Background,
            ForeColor = ForeColor,
            Icon = Icon,
            MinimizeBox = false,
            MaximizeBox = true,
            Padding = new Padding(20)
        };
        var text = new TextBox
        {
            Text = reader.ReadToEnd().ReplaceLineEndings(Environment.NewLine),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ForeColor = ForeColor,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11),
            AccessibleName = "Guia de configuração em ordem cronológica",
            SelectionStart = 0,
            SelectionLength = 0
        };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(0, 12, 0, 0), FlowDirection = FlowDirection.RightToLeft };
        var close = new Button { Text = "Fechar", DialogResult = DialogResult.Cancel };
        StyleButton(close, true);
        var copyGuide = new Button { Text = "Copiar guia" };
        StyleButton(copyGuide, false);
        copyGuide.Width = 150;
        copyGuide.Click += (_, _) =>
        {
            try { Clipboard.SetText(text.Text); }
            catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Não foi possível copiar"); }
        };
        footer.Controls.AddRange([close, copyGuide]);
        dialog.Controls.Add(text);
        dialog.Controls.Add(footer);
        dialog.CancelButton = close;
        dialog.ShowDialog(this);
    }
}
