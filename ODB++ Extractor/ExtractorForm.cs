using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ODB___Extractor
{
    public partial class ExtractorForm : Form
    {
        private const string DefaultStatusText = "Waiting for an ODB++ archive or directory.";
        private const string DefaultStatisticText = "No component data loaded (top-left).";

        private bool _suspendSelection;
        private bool _isLoading;
        private ODBppExtractor.JobReport _currentJobReport;
        private IReadOnlyList<ODBppExtractor.ComponentPlacementInfo> _topLeftPlacementData = Array.Empty<ODBppExtractor.ComponentPlacementInfo>();

        public ExtractorForm()
        {
            InitializeComponent();
            InitializeDataGrid();
            ResetUi();
        }

        private void btn_BrowseDir_Click(object sender, EventArgs e)
        {
            BrowseFolder();
        }

        private void btn_BrowseFile_Click(object sender, EventArgs e)
        {
            BrowseFile();
        }

        private void BrowseFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select file";
                dialog.Filter = "Archives (*.tgz;*.zip;*.tar;*.tar.gz)|*.tgz;*.zip;*.tar;*.tar.gz";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;

                TrySetInitialDirectory(dialog, txt_Path.Text);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (!IsAllowedArchive(dialog.FileName))
                    {
                        MessageBox.Show(this, "Only .tgz, .zip, .tar, or .tar.gz files are supported.", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    txt_Path.Text = dialog.FileName;
                }
            }
        }

        private static bool IsAllowedArchive(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var candidate = path.ToLowerInvariant();
            if (candidate.EndsWith(".tar.gz"))
            {
                return true;
            }

            var allowed = new[] { ".tgz", ".zip", ".tar" };
            return allowed.Any(candidate.EndsWith);
        }

        private void BrowseFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                SetFolderBrowserSelectedPath(dialog, txt_Path.Text);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txt_Path.Text = dialog.SelectedPath;
                }
            }
        }

        private static void TrySetInitialDirectory(FileDialog dialog, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var candidate = Directory.Exists(path)
                    ? path
                    : Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                {
                    dialog.InitialDirectory = candidate;
                }
            }
            catch
            {
                // ignore invalid paths
            }
        }

        private static void SetFolderBrowserSelectedPath(FolderBrowserDialog dialog, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var candidate = Directory.Exists(path)
                    ? path
                    : Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                {
                    dialog.SelectedPath = candidate;
                }
            }
            catch
            {
                // ignore invalid paths
            }
        }

        private void cbo_Step_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleStepSelectionChange();
        }

        private void cbo_Layer_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleLayerSelectionChange();
        }

        private void btn_ExportAllLayer_Click(object sender, EventArgs e)
        {

        }

        private void btn_ExportLayer_Click(object sender, EventArgs e)
        {

        }

        private async void txt_Path_TextChanged(object sender, EventArgs e)
        {
            await LoadCurrentPathAsync();
        }

        private async void btn_RefreshData_Click(object sender, EventArgs e)
        {
            await LoadCurrentPathAsync();
        }

        private void HandleStepSelectionChange()
        {
            if (_suspendSelection)
            {
                return;
            }

            var step = cbo_Step.SelectedItem as ODBppExtractor.StepReport;
            if (step == null)
            {
                cbo_Layer.DataSource = null;
                cbo_Layer.Enabled = false;
                lbl_Statistic.Text = DefaultStatisticText;
                lbl_Status.Text = DefaultStatusText;
                dgv_Data.Rows.Clear();
                return;
            }

            lbl_Status.Text = $"Step '{step.Name}' selected ({step.Layers.Count} layer(s)).";
            lbl_Statistic.Text = BuildStepDimensionText(step);
            PopulateLayerCombo(step);
        }

        private void HandleLayerSelectionChange()
        {
            if (_suspendSelection)
            {
                return;
            }

            var step = cbo_Step.SelectedItem as ODBppExtractor.StepReport;
            var layer = cbo_Layer.SelectedItem as ODBppExtractor.LayerReport;
            DisplayLayerComponents(step, layer);
        }

        private void InitializeDataGrid()
        {
            dgv_Data.AutoGenerateColumns = false;
            dgv_Data.ReadOnly = true;
            dgv_Data.Columns.Clear();
            var componentColumn = CreateTextColumn("ComponentName", "Component", DataGridViewAutoSizeColumnMode.Fill);
            componentColumn.MinimumWidth = 200;
            componentColumn.FillWeight = 2f;
            dgv_Data.Columns.Add(componentColumn);
            dgv_Data.Columns.Add(CreateTextColumn("PackageName", "Package", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("CenterX", "Center X", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("CenterY", "Center Y", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Rotation", "Rotation", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Width", "Width", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Length", "Length", DataGridViewAutoSizeColumnMode.AllCells));
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, DataGridViewAutoSizeColumnMode mode)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                AutoSizeMode = mode,
                ReadOnly = true
            };
        }

        private async Task LoadCurrentPathAsync()
        {
            var path = txt_Path.Text?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                ResetUi();
                return;
            }

            if (_isLoading)
            {
                return;
            }

            var friendlyName = string.IsNullOrWhiteSpace(path) ? "ODB++ job" : Path.GetFileName(path);
            lbl_Status.Text = $"Loading '{friendlyName}'...";
            ClearVisuals();
            BeginLoad();

            try
            {
                var result = await Task.Run(() => ODBppExtractor.Extract(path));
                if (!result.IsSuccessful)
                {
                    ShowError(result.ErrorMessage ?? "Failed to load ODB++ job.");
                    ResetUi("Failed to load ODB++ job.");
                    return;
                }

                _currentJobReport = result.JobReport;
                _topLeftPlacementData = ODBppExtractor.GetTopLeftComponentPlacements(_currentJobReport) ?? Array.Empty<ODBppExtractor.ComponentPlacementInfo>();
                if (_currentJobReport?.Steps == null || _currentJobReport.Steps.Count == 0)
                {
                    ResetUi("No steps were found in the ODB++ job.");
                    return;
                }

                PopulateSteps();
                lbl_Status.Text = $"Loaded {_currentJobReport.Steps.Count} step(s).";
            }
            catch (Exception ex)
            {
                ShowError($"Unexpected error while loading ODB++ job: {ex.Message}");
                ResetUi("Failed to load ODB++ job.");
            }
            finally
            {
                EndLoad();
            }
        }

        private void PopulateSteps()
        {
            if (_currentJobReport?.Steps == null || _currentJobReport.Steps.Count == 0)
            {
                cbo_Step.DataSource = null;
                cbo_Step.Enabled = false;
                lbl_Status.Text = "No step entries available.";
                return;
            }

            _suspendSelection = true;
            cbo_Step.DataSource = null;
            cbo_Step.DisplayMember = nameof(ODBppExtractor.StepReport.Name);
            cbo_Step.DataSource = _currentJobReport.Steps;
            cbo_Step.Enabled = true;
            cbo_Step.SelectedIndex = 0;
            _suspendSelection = false;

            HandleStepSelectionChange();
        }

        private void PopulateLayerCombo(ODBppExtractor.StepReport stepReport)
        {
            _suspendSelection = true;
            cbo_Layer.DataSource = null;
            dgv_Data.Rows.Clear();
            cbo_Layer.Enabled = false;
            _suspendSelection = false;

            if (stepReport.Layers == null || stepReport.Layers.Count == 0)
            {
                lbl_Status.Text = $"Step '{stepReport.Name}' contains no component layers.";
                return;
            }

            _suspendSelection = true;
            cbo_Layer.DisplayMember = nameof(ODBppExtractor.LayerReport.Name);
            cbo_Layer.DataSource = stepReport.Layers;
            cbo_Layer.Enabled = true;
            cbo_Layer.SelectedIndex = 0;
            _suspendSelection = false;

            HandleLayerSelectionChange();
        }

        private void DisplayLayerComponents(ODBppExtractor.StepReport step, ODBppExtractor.LayerReport layer)
        {
            dgv_Data.Rows.Clear();
            var stepText = BuildStepDimensionText(step);
            var unitText = DetermineLayerUnit(step, layer);

            if (step == null || layer == null)
            {
                lbl_Statistic.Text = $"{stepText} • Unit: {unitText} • Components count: 0";
                lbl_Status.Text = "Select a layer to view data.";
                return;
            }

            if (!layer.Exists)
            {
                lbl_Statistic.Text = $"{stepText} • Unit: {unitText} • Components count: 0";
                lbl_Status.Text = $"Layer '{layer.Name}' was not found on disk.";
                return;
            }

            var placements = (_topLeftPlacementData ?? Array.Empty<ODBppExtractor.ComponentPlacementInfo>())
                .Where(info =>
                    string.Equals(info.Step, step.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(info.Layer, layer.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (placements.Count == 0)
            {
                lbl_Statistic.Text = $"{stepText} • Unit: {unitText} • Components count: 0";
                lbl_Status.Text = $"Layer '{layer.Name}' contains no components.";
                return;
            }

            foreach (var placement in placements)
            {
                dgv_Data.Rows.Add(
                    placement.ComponentName,
                    placement.PackageName,
                    placement.CenterX,
                    placement.CenterY,
                    placement.Rotation,
                    placement.Width,
                    placement.Length);
            }

            lbl_Statistic.Text = $"{stepText} • Unit: {unitText} • Components count: {placements.Count}";
            lbl_Status.Text = $"Layer '{layer.Name}' (top-left) contains {placements.Count} components.";
        }

        private void ShowError(string message)
        {
            var text = string.IsNullOrWhiteSpace(message)
                ? "An unknown error occurred while processing the ODB++ data."
                : message;

            MessageBox.Show(this, text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ResetUi(string statusMessage = DefaultStatusText)
        {
            _currentJobReport = null;
            _topLeftPlacementData = Array.Empty<ODBppExtractor.ComponentPlacementInfo>();
            ClearVisuals();
            lbl_Status.Text = statusMessage;
        }

        private void BeginLoad()
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            SetActionButtonsEnabled(false);
        }

        private void EndLoad()
        {
            if (!_isLoading)
            {
                return;
            }

            _isLoading = false;
            SetActionButtonsEnabled(true);
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            btn_BrowseDir.Enabled = enabled;
            btn_BrowseFile.Enabled = enabled;
            btn_RefreshData.Enabled = enabled;
            btn_ExportLayer.Enabled = enabled;
            btn_ExportAllLayer.Enabled = enabled;
        }

        private static string BuildStepDimensionText(ODBppExtractor.StepReport step)
        {
            if (step == null)
            {
                return "Dimension unavailable";
            }

            if (step.ProfileBoundingBox.HasValue)
            {
                var bbox = step.ProfileBoundingBox.Value;
                var width = FormatDimension(bbox.MaxX - bbox.MinX);
                var length = FormatDimension(bbox.MaxY - bbox.MinY);
                return $"Dimension: {width} x {length}";
            }

            return "Dimension unavailable";
        }

        private static string DetermineLayerUnit(ODBppExtractor.StepReport step, ODBppExtractor.LayerReport layer)
        {
            var unit = layer?.Components?.Unit;
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = step?.Unit;
            }

            return string.IsNullOrWhiteSpace(unit) ? "unit unknown" : unit;
        }

        private static string FormatDimension(double value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private void ClearVisuals()
        {
            _suspendSelection = true;
            _topLeftPlacementData = Array.Empty<ODBppExtractor.ComponentPlacementInfo>();
            cbo_Step.DataSource = null;
            cbo_Layer.DataSource = null;
            cbo_Step.Enabled = false;
            cbo_Layer.Enabled = false;
            dgv_Data.Rows.Clear();
            lbl_Statistic.Text = DefaultStatisticText;
            _suspendSelection = false;
        }
    }
}
