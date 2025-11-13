using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        private const string DefaultStatisticText = "No component data loaded.";

        private bool _suspendSelection;
        private ODBppExtractor.JobReport _currentJobReport;

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

        private void txt_Path_TextChanged(object sender, EventArgs e)
        {
            LoadCurrentPath();
        }

        private void btn_RefreshData_Click(object sender, EventArgs e)
        {
            LoadCurrentPath();
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
            PopulateLayerCombo(step);
        }

        private void HandleLayerSelectionChange()
        {
            if (_suspendSelection)
            {
                return;
            }

            var layer = cbo_Layer.SelectedItem as ODBppExtractor.LayerReport;
            DisplayLayerComponents(layer);
        }

        private void InitializeDataGrid()
        {
            dgv_Data.AutoGenerateColumns = false;
            dgv_Data.ReadOnly = true;
            dgv_Data.Columns.Clear();
            dgv_Data.Columns.Add(CreateTextColumn("ComponentName", "Component", DataGridViewAutoSizeColumnMode.Fill));
            dgv_Data.Columns.Add(CreateTextColumn("PartName", "Part", DataGridViewAutoSizeColumnMode.Fill));
            dgv_Data.Columns.Add(CreateTextColumn("PkgRef", "Pkg Ref", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("X", "X", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Y", "Y", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Rot", "Rot", DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Mirror", "Mirror", DataGridViewAutoSizeColumnMode.AllCells));
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

        private void LoadCurrentPath()
        {
            var path = txt_Path.Text?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                ResetUi();
                return;
            }

            lbl_Status.Text = "Loading ODB++ job...";
            ClearVisuals();

            try
            {
                var result = ODBppExtractor.Extract(path);
                if (!result.IsSuccessful)
                {
                    ShowError(result.ErrorMessage ?? "Failed to load ODB++ job.");
                    ResetUi("Failed to load ODB++ job.");
                    return;
                }

                _currentJobReport = result.JobReport;
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
            lbl_Statistic.Text = DefaultStatisticText;
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

        private void DisplayLayerComponents(ODBppExtractor.LayerReport layer)
        {
            dgv_Data.Rows.Clear();

            if (layer == null)
            {
                lbl_Statistic.Text = DefaultStatisticText;
                lbl_Status.Text = "Select a layer to view data.";
                return;
            }

            if (!layer.Exists)
            {
                lbl_Statistic.Text = "Layer folder is missing.";
                lbl_Status.Text = $"Layer '{layer.Name}' was not found on disk.";
                return;
            }

            var records = layer.Components?.Records ?? Array.Empty<ODBppExtractor.ComponentRecord>();
            if (records.Count == 0)
            {
                lbl_Statistic.Text = layer.Components == null
                    ? "Component data unavailable for this layer."
                    : "No component placements were parsed for this layer.";
                lbl_Status.Text = $"Layer '{layer.Name}' contains no components.";
                return;
            }

            foreach (var record in records)
            {
                dgv_Data.Rows.Add(
                    record.ComponentName,
                    record.PartName,
                    record.PkgRef,
                    record.X,
                    record.Y,
                    record.Rot,
                    record.Mirror);
            }

            var unitDisplay = string.IsNullOrWhiteSpace(layer.Components.Unit) ? "unknown unit" : layer.Components.Unit;
            lbl_Statistic.Text = $"{records.Count} components • Unit: {unitDisplay}";
            lbl_Status.Text = $"Layer '{layer.Name}' contains {records.Count} components.";
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
            ClearVisuals();
            lbl_Status.Text = statusMessage;
        }

        private void ClearVisuals()
        {
            _suspendSelection = true;
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
