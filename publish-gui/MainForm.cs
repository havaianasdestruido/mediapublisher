using System.ComponentModel;
using PublishStudio.Core;

namespace PublishStudio.Gui;

/// <summary>
/// Thin WinForms client for WLXMediaPublishSubscribe.dll.
/// Target list, auth status, publish flow, and honest stub badges.
/// </summary>
public sealed class MainForm : Form
{
    private readonly DataGridView _grid;
    private readonly BindingList<TargetRow> _rows = new();

    private readonly ComboBox _targetCombo;
    private readonly TextBox _fileBox;
    private readonly TextBox _titleBox;
    private readonly TextBox _descBox;
    private readonly CheckBox _privateBox;
    private readonly Button _browseBtn;
    private readonly Button _publishBtn;
    private readonly ProgressBar _progress;
    private readonly Label _statusLabel;
    private readonly Label _stubLine;
    private readonly RichTextBox _legendBox;

    public MainForm()
    {
        Text = "Publish Studio -- WLXMediaPublishSubscribe.dll client";
        Width = 920;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 560,
        };

        // --- left: targets + auth ---
        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _rows,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TargetRow.Id), HeaderText = "ID", Width = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TargetRow.Name), HeaderText = "Target", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TargetRow.AvailableText), HeaderText = "Available", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TargetRow.AuthText), HeaderText = "Authenticated", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TargetRow.AccountText), HeaderText = "Account", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        left.Controls.Add(_grid, 0, 0);

        var authBar = new Panel { Dock = DockStyle.Fill };
        var refreshBtn = new Button { Text = "Refresh", Width = 80 };
        refreshBtn.Click += (_, _) => RefreshTargets();
        var authBtn = new Button { Text = "Authenticate", Width = 110 };
        authBtn.Click += (_, _) => AuthenticateSelected();
        var signoutBtn = new Button { Text = "Sign Out", Width = 90 };
        signoutBtn.Click += (_, _) => SignOutSelected();
        authBar.Controls.Add(refreshBtn);
        authBar.Controls.Add(authBtn);
        authBar.Controls.Add(signoutBtn);
        authBtn.Location = new Point(refreshBtn.Right + 6, 6);
        signoutBtn.Location = new Point(authBtn.Right + 6, 6);
        left.Controls.Add(authBar, 0, 2);

        // --- right: publish + stub awareness ---
        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        right.Controls.Add(BuildPublishPanel(), 0, 0);

        _legendBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 8.5f),
        };
        right.Controls.Add(_legendBox, 0, 1);

        _stubLine = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(150, 80, 0),
            AutoEllipsis = true,
        };
        right.Controls.Add(_stubLine, 0, 2);

        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(right);
        Controls.Add(split);

        Load += (_, _) =>
        {
            LoadLegend();
            RefreshTargets();
        };

        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].DataBoundItem is TargetRow row)
            {
                _targetCombo.SelectedItem = _targetCombo.Items.Cast<ComboItem>()
                    .FirstOrDefault(c => c.Id == row.Id);
            }
        };
    }

    private Panel BuildPublishPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var g = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9 };
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var fileRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        _fileBox = new TextBox { Dock = DockStyle.Fill };
        _browseBtn = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        _browseBtn.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.wmv;*.avi;*.mov;*.mpg;*.mpeg;*.mts;*.m2ts;*.3gp|All files|*.*",
                Title = "Select video to publish",
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                _fileBox.Text = ofd.FileName;
                _titleBox.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
            }
        };
        fileRow.Controls.Add(_fileBox, 0, 0);
        fileRow.Controls.Add(_browseBtn, 1, 0);

        _targetCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        for (int i = 0; i <= 4; i++)
            _targetCombo.Items.Add(new ComboItem(i, PublishManager.GetTargetName(i).Value));

        _titleBox = new TextBox { Dock = DockStyle.Fill };
        _descBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 44 };
        _privateBox = new CheckBox { Text = "Private upload", Dock = DockStyle.Fill };
        _publishBtn = new Button { Text = "Publish", Dock = DockStyle.Fill };
        _publishBtn.Click += async (_, _) => await PublishAsync();
        _progress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };
        _statusLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };

        AddRow(g, "Video file", fileRow, 0);
        AddRow(g, "Target", _targetCombo, 1);
        AddRow(g, "Title", _titleBox, 2);
        AddRow(g, "Description", _descBox, 3);
        AddRow(g, "Options", _privateBox, 4);
        AddRow(g, "", _publishBtn, 5);
        AddRow(g, "Progress", _progress, 6);
        AddRow(g, "Status", _statusLabel, 7);

        p.Controls.Add(g);
        return p;
    }

    private static void AddRow(TableLayoutPanel g, string label, Control c, int row)
    {
        var l = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        g.Controls.Add(l, 0, row);
        g.Controls.Add(c, 1, row);
    }

    private void LoadLegend()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Engine surface: real vs stub (WLXMediaPublishSubscribe.dll)");
        sb.AppendLine();
        foreach (string line in StubAware.TableLines())
            sb.AppendLine(line);
        _legendBox.Text = sb.ToString();
    }

    private void RefreshTargets()
    {
        try
        {
            _rows.Clear();
            using var pm = PublishManager.Create();
            var ids = pm.EnumerateTargets();
            foreach (int id in ids.Value)
            {
                var name = PublishManager.GetTargetName(id);
                var svc = PublishManager.GetServiceStatus(id);
                var auth = pm.IsAuthenticated(id);
                var acct = pm.GetAccountInfo(id);
                _rows.Add(new TargetRow(id, name.Value, svc.Value, auth.Value, acct.Value));
            }
            SetStub(null);
        }
        catch (Exception ex)
        {
            ShowStubLine($"ERROR: {ex.Message}");
        }
    }

    private PublishManager? NewManager()
    {
        try
        {
            return PublishManager.Create();
        }
        catch (Exception ex)
        {
            ShowStubLine($"ERROR: {ex.Message}");
            return null;
        }
    }

    private int SelectedId()
    {
        if (_targetCombo.SelectedItem is ComboItem ci)
            return ci.Id;
        return 0;
    }

    private void AuthenticateSelected()
    {
        using var pm = NewManager();
        if (pm is null) return;
        int id = SelectedId();
        var auth = pm.Authenticate(id);
        var isAuth = pm.IsAuthenticated(id);
        SetStub(StubAware.Annotate("Authenticate", auth.Hr));
        RefreshTargets();
        _statusLabel.Text = $"Authenticate -> {Hr.Fmt(auth.Hr)}; authenticated={isAuth.Value}";
    }

    private void SignOutSelected()
    {
        using var pm = NewManager();
        if (pm is null) return;
        int id = SelectedId();
        var so = pm.SignOut(id);
        SetStub(StubAware.Annotate("SignOut", so.Hr));
        RefreshTargets();
        _statusLabel.Text = $"SignOut -> {Hr.Fmt(so.Hr)}";
    }

    private async Task PublishAsync()
    {
        string file = _fileBox.Text.Trim();
        if (!File.Exists(file))
        {
            _statusLabel.Text = "Select an existing video file first.";
            return;
        }

        _publishBtn.Enabled = false;
        _progress.Value = 0;
        _statusLabel.Text = "Publishing...";
        SetStub(null);

        try
        {
            var cfg = PublishConfig.DefaultFor(SelectedId());
            cfg.Title = string.IsNullOrWhiteSpace(_titleBox.Text) ? Path.GetFileName(file) : _titleBox.Text;
            cfg.Description = _descBox.Text;
            cfg.Private = _privateBox.Checked ? 1 : 0;

            int id = SelectedId();
            using var pm = PublishManager.Create();

            var auth = await Task.Run(() => pm.Authenticate(id));
            var start = await Task.Run(() => pm.StartPublish(id, file, cfg));
            var st = await Task.Run(() => start.Value == IntPtr.Zero ? default : pm.QueryStatus(start.Value));

            _progress.Value = 100;

            var lines = new List<string>
            {
                StubAware.Annotate("Authenticate", auth.Hr),
                StubAware.Annotate("StartPublish", start.Hr) +
                    (start.Value != IntPtr.Zero ? $" (session 0x{start.Value.ToInt64():X})" : " (no session returned)"),
            };
            if (start.Value != IntPtr.Zero)
            {
                lines.Add(StubAware.Annotate("GetStatus", st.Hr) +
                    (st.Hr == Hr.E_NOTIMPL ? "" : $" status={st.Status}%={st.Percent}"));
            }
            SetStub(string.Join(Environment.NewLine, lines));
            _statusLabel.Text = start.Value != IntPtr.Zero
                ? "Session created via real API. Upload/status/result are engine stubs (E_NOTIMPL)."
                : "StartPublish returned no session.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"ERROR: {ex.Message}";
        }
        finally
        {
            _publishBtn.Enabled = true;
        }
    }

    private void SetStub(string? text) =>
        _stubLine.Text = text ?? "engine stubs are reported honestly (E_NOTIMPL) -- no faked progress";

    private void ShowStubLine(string text) => _stubLine.Text = text;

    private sealed record TargetRow(int Id, string Name, bool Available, bool Authenticated, string Account)
    {
        public string AvailableText => Available ? "TRUE" : "FALSE";
        public string AuthText => Authenticated ? "TRUE" : "FALSE";
        public string AccountText => Account ?? "";
    }

    private sealed record ComboItem(int Id, string Name)
    {
        public override string ToString() => $"{Id} - {Name}";
    }
}
