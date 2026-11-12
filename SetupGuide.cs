namespace WlyTunnel;

sealed partial class TunnelForm
{
    static string ReadGuide(string name)
    {
        using var stream = typeof(TunnelForm).Assembly.GetManifestResourceStream($"WlyTunnel.{name}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().ReplaceLineEndings(Environment.NewLine);
    }

    TabPage GuidePage(string title, string resource, string accessibleName)
    {
        var page = new TabPage(title) { BackColor = Surface, Padding = new Padding(12) };
        page.Controls.Add(new TextBox
        {
            Text = ReadGuide(resource),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ForeColor = ForeColor,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11),
            AccessibleName = accessibleName
        });
        return page;
    }

    void ShowSetupGuide()
    {
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
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(0, 12, 0, 0), FlowDirection = FlowDirection.RightToLeft };
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
        tabs.TabPages.AddRange([
            GuidePage("Resumo", "RESUMO.txt", "Resumo dos passos no Windows e no Linux"),
            GuidePage("Windows — seu computador", "GUIA-WINDOWS.txt", "Passos no Windows, onde roda o WLY Tunnel"),
            GuidePage("Linux — VPS", "GUIA-LINUX.txt", "Passos na VPS Linux, feitos pelo administrador")
        ]);
        var close = new Button { Text = "Fechar", DialogResult = DialogResult.Cancel };
        StyleButton(close, true);
        var copyGuide = new Button { Text = "Copiar aba" };
        StyleButton(copyGuide, false);
        copyGuide.Width = 150;
        copyGuide.Click += (_, _) =>
        {
            try { Clipboard.SetText(tabs.SelectedTab!.Controls[0].Text); }
            catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Não foi possível copiar"); }
        };
        footer.Controls.AddRange([close, copyGuide]);
        dialog.Controls.Add(tabs);
        dialog.Controls.Add(footer);
        dialog.CancelButton = close;
        dialog.ShowDialog(this);
    }
}
