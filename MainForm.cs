using System.Windows.Forms;

sealed class MainForm : Form
{
    readonly DataCatalog _catalog;

    readonly TextBox _inputPath = new() { ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    readonly TextBox _outputPath = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    readonly ComboBox _sigils = new() { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    readonly ComboBox _sigilLevels = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    readonly Label _primaryTrait = new() { AutoSize = true };
    readonly ComboBox _primaryTraitLevels = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    readonly ComboBox _secondaryTraits = new() { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    readonly ComboBox _secondaryTraitLevels = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    readonly NumericUpDown _quantity = new() { Minimum = 1, Maximum = 999, Value = 1, Width = 120 };
    readonly Button _addToQueue = new() { Text = "Add", Width = 120 };
    readonly Button _clearQueue = new() { Text = "Clear", Width = 120, Enabled = false };
    readonly ListBox _queue = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, IntegralHeight = false };
    readonly Button _apply = new() { Text = "Apply", Enabled = false, Width = 120 };
    readonly Button _removeAllSigils = new() { Text = "Remove all", Enabled = false, Width = 120 };
    readonly Label _status = new() { AutoSize = false, Height = 48, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    readonly List<QueuedEdit> _queuedEdits = [];

    public MainForm()
    {
        Text = "GBFR sigil generator";
        MinimumSize = new Size(720, 560);
        StartPosition = FormStartPosition.CenterScreen;

        _catalog = DataCatalog.LoadDefault();
        BuildLayout();
        LoadData();
        UpdateSelection();
    }

    void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 12,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        AddRow(root, 0, "Input save", _inputPath, Button("Browse", BrowseInput));
        AddRow(root, 1, "Output save", _outputPath, Button("Output", BrowseOutput));
        AddRow(root, 2, "Sigil", _sigils, null);
        AddRow(root, 3, "Sigil level", _sigilLevels, null);
        AddRow(root, 4, "First trait", _primaryTrait, null);
        AddRow(root, 5, "First trait level", _primaryTraitLevels, null);
        AddRow(root, 6, "Second trait", _secondaryTraits, null);
        AddRow(root, 7, "Second trait level", _secondaryTraitLevels, null);
        AddRow(root, 8, "Quantity", _quantity, _addToQueue);

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.Controls.Add(new Label { Text = "Queue", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 9);
        root.Controls.Add(_queue, 1, 9);
        root.Controls.Add(_clearQueue, 2, 9);

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(_status, 0, 10);
        root.SetColumnSpan(_status, 2);
        root.Controls.Add(_apply, 2, 10);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.Controls.Add(_removeAllSigils, 2, 11);

        _sigils.SelectedIndexChanged += (_, _) => UpdateSelection();
        _secondaryTraits.SelectedIndexChanged += (_, _) => UpdateSecondaryLevels();
        _inputPath.TextChanged += (_, _) => UpdateApplyState();
        _outputPath.TextChanged += (_, _) => UpdateApplyState();
        _sigilLevels.SelectedIndexChanged += (_, _) => UpdateApplyState();
        _primaryTraitLevels.SelectedIndexChanged += (_, _) => UpdateApplyState();
        _secondaryTraitLevels.SelectedIndexChanged += (_, _) => UpdateApplyState();
        _quantity.ValueChanged += (_, _) => UpdateApplyState();
        _addToQueue.Click += (_, _) => AddToQueue();
        _clearQueue.Click += (_, _) => ClearQueue();
        _queue.SelectedIndexChanged += (_, _) => UpdateClearQueueState();
        _apply.Click += (_, _) => ApplyEdit();
        _removeAllSigils.Click += (_, _) => ApplyRemoveAllSigils();

        Controls.Add(root);
    }

    static Button Button(string text, EventHandler handler)
    {
        var button = new Button { Text = text, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        button.Click += handler;
        return button;
    }

    static void AddRow(TableLayoutPanel root, int row, string label, Control editor, Control? button)
    {
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        root.Controls.Add(editor, 1, row);
        if (button is not null)
            root.Controls.Add(button, 2, row);
    }

    void LoadData()
    {
        _sigils.DataSource = _catalog.Sigils
            .OrderBy(GetSigilMajorGroup)
            .ThenBy(s => s.DisplayName)
            .ToList();
    }

    static int GetSigilMajorGroup(SigilData sigil)
    {
        string id = sigil.InternalId;
        return id.Length >= 8 &&
            id.StartsWith("GEEN_", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(id.AsSpan(5, 3), out int group)
                ? group
                : int.MaxValue;
    }

    void BrowseInput(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "GBFR save (*.dat)|*.dat|All files (*.*)|*.*",
            Title = "Select GBFR save file",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _inputPath.Text = dialog.FileName;
        string directory = Path.GetDirectoryName(dialog.FileName) ?? Directory.GetCurrentDirectory();
        string name = Path.GetFileNameWithoutExtension(dialog.FileName);
        string extension = Path.GetExtension(dialog.FileName);
        _outputPath.Text = Path.Combine(directory, $"{name}_modified{extension}");
    }

    void BrowseOutput(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "GBFR save (*.dat)|*.dat|All files (*.*)|*.*",
            Title = "Choose output save file",
            FileName = string.IsNullOrWhiteSpace(_outputPath.Text) ? "SaveData1_modified.dat" : Path.GetFileName(_outputPath.Text),
            InitialDirectory = string.IsNullOrWhiteSpace(_outputPath.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(_outputPath.Text),
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _outputPath.Text = dialog.FileName;
    }

    void UpdateSelection()
    {
        if (_sigils.SelectedItem is not SigilData sigil)
            return;

        SetCombo(_sigilLevels, TryLevels(() => _catalog.RequireSigilLevels(sigil)));

        try
        {
            TraitData primary = _catalog.RequireTrait(sigil.PrimaryTraitId);
            _primaryTrait.Text = primary.DisplayName;
            SetCombo(_primaryTraitLevels, TryLevels(() => _catalog.RequirePrimaryTraitLevels(sigil)));
        }
        catch (ToolError ex)
        {
            _primaryTrait.Text = "TODO: primary trait not verified";
            SetCombo(_primaryTraitLevels, []);
            _status.Text = ex.Message;
        }

        var secondaries = _catalog.GetAllowedSecondaryTraits(sigil).ToList();
        _secondaryTraits.Enabled = secondaries.Count > 0;
        _secondaryTraits.DataSource = secondaries;
        if (!string.IsNullOrWhiteSpace(sigil.DefaultSecondaryTraitId))
        {
            TraitData? defaultSecondary = secondaries.FirstOrDefault(t =>
                t.InternalId.Equals(sigil.DefaultSecondaryTraitId, StringComparison.OrdinalIgnoreCase));
            if (defaultSecondary is not null)
                _secondaryTraits.SelectedItem = defaultSecondary;
        }

        UpdateSecondaryLevels();
    }

    void UpdateSecondaryLevels()
    {
        if (_sigils.SelectedItem is not SigilData sigil || _secondaryTraits.SelectedItem is not TraitData secondary)
        {
            _secondaryTraitLevels.DataSource = Array.Empty<int>();
            _secondaryTraitLevels.Enabled = false;
            UpdateApplyState();
            return;
        }

        SetCombo(_secondaryTraitLevels, TryLevels(() => _catalog.RequireSecondaryTraitLevels(sigil, secondary)));
        UpdateApplyState();
    }

    IReadOnlyList<int> TryLevels(Func<IReadOnlyList<int>> read)
    {
        try
        {
            _status.Text = "";
            return read();
        }
        catch (ToolError ex)
        {
            _status.Text = ex.Message;
            return [];
        }
    }

    static void SetCombo(ComboBox combo, IReadOnlyList<int> values)
    {
        combo.Enabled = values.Count > 0;
        combo.DataSource = values.ToArray();
    }

    void UpdateApplyState()
    {
        try
        {
            var request = BuildBatchRequest();
            SaveEditorService.ValidateBatch(request, _catalog);
            _status.Text = $"Ready to create {TotalQuantity(request.Entries)} sigil(s). The input save will not be changed.";
            _apply.Enabled = true;
        }
        catch (Exception ex) when (ex is ToolError or InvalidOperationException)
        {
            _status.Text = ex.Message;
            _apply.Enabled = false;
        }

        UpdateRemoveAllState();
    }

    EditBatchRequest BuildBatchRequest()
    {
        if (string.IsNullOrWhiteSpace(_inputPath.Text))
            throw new ToolError("Choose an input save file.");
        if (string.IsNullOrWhiteSpace(_outputPath.Text))
            throw new ToolError("Choose an output save file.");

        IReadOnlyList<EditBatchEntry> entries = _queuedEdits.Count > 0
            ? _queuedEdits.Select(x => x.Entry).ToArray()
            : [BuildCurrentEntry()];

        return new EditBatchRequest(_inputPath.Text, _outputPath.Text, entries);
    }

    EditBatchEntry BuildCurrentEntry()
    {
        if (_sigils.SelectedItem is not SigilData sigil)
            throw new ToolError("Choose a sigil.");
        if (_sigilLevels.SelectedItem is not int sigilLevel)
            throw new ToolError("Choose a sigil level.");
        if (_primaryTraitLevels.SelectedItem is not int primaryLevel)
            throw new ToolError("Choose a first trait level.");

        int quantity = checked((int)_quantity.Value);
        if (quantity <= 0)
            throw new ToolError("Quantity must be at least 1.");

        string? secondaryTraitId = null;
        int? secondaryLevel = null;
        if (sigil.SupportsSecondaryTrait == true)
        {
            if (_secondaryTraits.SelectedItem is not TraitData secondary)
                throw new ToolError("Choose a valid second trait.");
            if (_secondaryTraitLevels.SelectedItem is not int secondaryTraitLevel)
                throw new ToolError("Choose a second trait level.");
            secondaryTraitId = secondary.InternalId;
            secondaryLevel = secondaryTraitLevel;
        }

        return new EditBatchEntry(
            sigil.InternalId,
            sigilLevel,
            primaryLevel,
            secondaryTraitId,
            secondaryLevel,
            quantity);
    }

    static int TotalQuantity(IReadOnlyList<EditBatchEntry> entries)
    {
        return entries.Sum(x => x.Quantity);
    }

    void AddToQueue()
    {
        try
        {
            EditBatchEntry entry = BuildCurrentEntry();
            SaveEditorService.ValidateEntry(entry, _catalog);
            _queuedEdits.Add(new QueuedEdit(entry, DescribeEntry(entry)));
            RefreshQueue(_queuedEdits.Count - 1);
            UpdateApplyState();
        }
        catch (Exception ex) when (ex is ToolError or InvalidOperationException)
        {
            _status.Text = ex.Message;
        }
    }

    void ClearQueue()
    {
        int index = _queue.SelectedIndex;
        if (index < 0 || index >= _queuedEdits.Count)
        {
            _status.Text = "Select a queued sigil to clear.";
            return;
        }

        _queuedEdits.RemoveAt(index);
        int nextIndex = Math.Min(index, _queuedEdits.Count - 1);
        RefreshQueue(nextIndex);
        UpdateApplyState();
    }

    void RefreshQueue(int selectedIndex = -1)
    {
        _queue.DataSource = null;
        _queue.DataSource = _queuedEdits.Select(x => x.Description).ToArray();
        if (selectedIndex >= 0 && selectedIndex < _queuedEdits.Count)
            _queue.SelectedIndex = selectedIndex;

        UpdateClearQueueState();
    }

    void UpdateClearQueueState()
    {
        _clearQueue.Enabled = _queue.SelectedIndex >= 0 && _queue.SelectedIndex < _queuedEdits.Count;
    }

    string DescribeEntry(EditBatchEntry entry)
    {
        SigilData sigil = _catalog.RequireSigil(entry.SigilId);
        TraitData primary = _catalog.RequireTrait(sigil.PrimaryTraitId);
        string secondary = entry.SecondaryTraitId is null
            ? "No secondary"
            : $"{_catalog.RequireTrait(entry.SecondaryTraitId).DisplayName} Lv {entry.SecondaryTraitLevel}";

        return $"{sigil.DisplayName} x{entry.Quantity} | {primary.DisplayName} Lv {entry.PrimaryTraitLevel} / {secondary}";
    }

    void ApplyEdit()
    {
        try
        {
            EditBatchRequest request = BuildBatchRequest();
            if (File.Exists(request.OutputPath))
            {
                DialogResult overwrite = MessageBox.Show(
                    this,
                    "The output file already exists. Overwrite it?",
                    "Confirm overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (overwrite != DialogResult.Yes)
                    return;
            }

            EditBatchResult result = SaveEditorService.ApplyBatch(request, _catalog);
            MessageBox.Show(
                this,
                $"Created {result.CreatedSigils.Count} sigil(s).\nVerified {result.VerifiedSigils} sigil(s).\n\nWrote output save:\n{result.OutputPath}",
                "Save written",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            _queuedEdits.Clear();
            RefreshQueue();
            _status.Text = $"Wrote and verified {Path.GetFileName(result.OutputPath)} with {result.CreatedSigils.Count} sigil(s).";
        }
        catch (Exception ex) when (ex is ToolError or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Could not apply edit", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = ex.Message;
        }
    }

    void ApplyRemoveAllSigils()
    {
        try
        {
            if (!CanRemoveAllSigils())
                throw new ToolError("Choose an input save and a different output save path first.");

            DialogResult confirm = MessageBox.Show(
                this,
                "This will create an output save with all existing sigils cleared. The player will lose all sigils in that output save. This is an extreme option for saves with more sigils than you can manage.\n\nContinue?",
                "Remove all sigils",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;

            if (File.Exists(_outputPath.Text))
            {
                DialogResult overwrite = MessageBox.Show(
                    this,
                    "The output file already exists. Overwrite it?",
                    "Confirm overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (overwrite != DialogResult.Yes)
                    return;
            }

            RemoveAllSigilsResult result = SaveEditorService.RemoveAllSigils(_inputPath.Text, _outputPath.Text);
            MessageBox.Show(
                this,
                $"Removed {result.RemovedSigils} sigil(s).\nVerified {result.RemainingSigils} occupied sigil slot(s) remain.\n\nWrote output save:\n{result.OutputPath}",
                "Save written",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            _status.Text = $"Wrote and verified {Path.GetFileName(result.OutputPath)} with all sigils cleared.";
        }
        catch (Exception ex) when (ex is ToolError or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Could not remove sigils", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = ex.Message;
        }
    }

    void UpdateRemoveAllState()
    {
        _removeAllSigils.Enabled = CanRemoveAllSigils();
    }

    bool CanRemoveAllSigils()
    {
        if (!File.Exists(_inputPath.Text) || string.IsNullOrWhiteSpace(_outputPath.Text))
            return false;

        try
        {
            string inputFullPath = Path.GetFullPath(_inputPath.Text);
            string outputFullPath = Path.GetFullPath(_outputPath.Text);
            return !string.Equals(inputFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

sealed record QueuedEdit(EditBatchEntry Entry, string Description);
