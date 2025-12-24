using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using WK.Libraries.BetterFolderBrowserNS;

namespace ODB___Extractor
{
    public partial class ExtractorForm : Form
    {
        private const string DefaultStatusText = "Waiting for an ODB++ archive or directory.";
        private const string DefaultStatisticText = "No component data loaded (top-left).";

        private bool _suspendSelection;
        private bool _isLoading;
        private ODBppExtractor.JobReport _currentJobReport;
        private readonly string _workingDirectoryRoot;
        private ViewerForm _viewerForm;

        public ExtractorForm()
        {
            InitializeComponent();
            InitializeDataGrid();
            ResetUi();
            InitializeOriginCombo();
            _workingDirectoryRoot = EnsureWorkingDirectory();
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
            var initialPath = ResolveInitialFolder(txt_Path.Text);
            using (var browser = new BetterFolderBrowser
            {
                Title = "Select folder",
                Multiselect = false,
                RootFolder = string.IsNullOrWhiteSpace(initialPath) ? null : initialPath
            })
            {
                if (browser.ShowDialog(this) == DialogResult.OK)
                {
                    txt_Path.Text = browser.SelectedPath;
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

        private static string ResolveInitialFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                var candidate = Directory.Exists(path)
                    ? path
                    : Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // ignore invalid paths
            }

            return string.Empty;
        }

        private void cbo_Step_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleStepSelectionChange();
        }

        private void cbo_Layer_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleLayerSelectionChange();
        }

        private void btn_ExportAllLayer_Click(object sender, EventArgs e) => ExportComponents(true);

        private void btn_ExportLayer_Click(object sender, EventArgs e) => ExportComponents(false);

        private void btn_PreviewData_Click(object sender, EventArgs e) => ShowViewer();

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

            RefreshViewer(autoFit: true);
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

            RefreshViewer(autoFit: true);
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
                var exportPreferences = new ODBppExtractor.ExportPreferences
                {
                    ExportJobReport = false,
                    ComponentMode = ODBppExtractor.ComponentExportMode.None
                };

                var result = await Task.Run(() => ODBppExtractor.Extract(path, _workingDirectoryRoot, exportPreferences));
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

                RefreshViewer(autoFit: true);
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
            var originLabel = GetCurrentOriginLabel();

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

            var placements = (GetCurrentPlacementData() ?? Array.Empty<ODBppExtractor.ComponentPlacementInfo>())
                .Where(info =>
                    string.Equals(info.Step, step.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(info.Layer, layer.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (placements.Count == 0)
            {
                lbl_Statistic.Text = $"{stepText} • Unit: {unitText} • Components count: 0";
                lbl_Status.Text = $"Layer '{layer.Name}' ({originLabel}) contains no components.";
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
            lbl_Status.Text = $"Layer '{layer.Name}' ({originLabel}) contains {placements.Count} components.";
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
            CloseViewer();
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
            btn_PreviewData.Enabled = enabled;
            cbo_Origin.Enabled = enabled;
        }

        private void ExportComponents(bool exportAllLayers)
        {
            if (_currentJobReport == null)
            {
                ShowError("Load an ODB++ job before exporting.");
                return;
            }

            if (!TrySelectExportDirectory(out var targetDirectory))
            {
                return;
            }

            HashSet<string> layerFilter = null;
            if (!exportAllLayers)
            {
                var layerReport = cbo_Layer.SelectedItem as ODBppExtractor.LayerReport;
                if (layerReport == null)
                {
                    ShowError("Select a layer before exporting.");
                    return;
                }

                var layerName = layerReport.Name;
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    ShowError("Selected layer does not have a valid name.");
                    return;
                }

                layerFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { layerName };
            }

            var origin = IsOriginBottomLeftSelected()
                ? ODBppExtractor.CoordinateOrigin.BottomLeft
                : ODBppExtractor.CoordinateOrigin.TopLeft;

            var flipOptions = BuildComponentPlacementFlipOptions();

            IReadOnlyList<string> exportedPaths;
            try
            {
                exportedPaths = ODBppExtractor.ExportComponentPlacementReports(
                    _currentJobReport,
                    origin,
                    separateByLayer: true,
                    layerFilter,
                    targetDirectory,
                    flipOptions: flipOptions);
            }
            catch (Exception ex)
            {
                ShowError($"Export failed: {ex.Message}");
                return;
            }

            if (exportedPaths == null || exportedPaths.Count == 0)
            {
                MessageBox.Show(this, "No component reports were generated.", "Export result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(this, $"Component reports exported to:\n{targetDirectory}", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool TrySelectExportDirectory(out string exportDirectory)
        {
            exportDirectory = null;
            using (var browser = new BetterFolderBrowser
            {
                Title = "Select export folder",
                Multiselect = false
            })
            {
                if (browser.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                var selected = browser.SelectedPath;
                if (string.IsNullOrWhiteSpace(selected))
                {
                    return false;
                }

                exportDirectory = selected;
                return true;
            }
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
            cbo_Step.DataSource = null;
            cbo_Layer.DataSource = null;
            cbo_Step.Enabled = false;
            cbo_Layer.Enabled = false;
            dgv_Data.Rows.Clear();
            lbl_Statistic.Text = DefaultStatisticText;
            _suspendSelection = false;
        }

        private void InitializeOriginCombo()
        {
            _suspendSelection = true;
            cbo_Origin.Items.Clear();
            cbo_Origin.Items.Add("Top-left");
            cbo_Origin.Items.Add("Bottom-left");
            cbo_Origin.SelectedIndex = 0;
            _suspendSelection = false;
        }

        private IReadOnlyList<ODBppExtractor.ComponentPlacementInfo> GetCurrentPlacementData()
        {
            if (_currentJobReport == null)
            {
                return Array.Empty<ODBppExtractor.ComponentPlacementInfo>();
            }

            var origin = IsOriginBottomLeftSelected()
                ? ODBppExtractor.CoordinateOrigin.BottomLeft
                : ODBppExtractor.CoordinateOrigin.TopLeft;

            var flipOptions = BuildComponentPlacementFlipOptions();
            return ODBppExtractor.GetComponentPlacements(
                _currentJobReport,
                topLeft: origin == ODBppExtractor.CoordinateOrigin.TopLeft,
                flipOptions: flipOptions);
        }

        private ODBppExtractor.ComponentPlacementFlipOptions BuildComponentPlacementFlipOptions()
        {
            var axis = ODBppExtractor.AxisFlip.None;
            if (chk_FlipXAxis.Checked)
            {
                axis |= ODBppExtractor.AxisFlip.X;
            }

            if (chk_FlipYAxis.Checked)
            {
                axis |= ODBppExtractor.AxisFlip.Y;
            }

            if (axis == ODBppExtractor.AxisFlip.None)
            {
                return null;
            }

            return new ODBppExtractor.ComponentPlacementFlipOptions
            {
                Axes = axis
            };
        }

        private string GetCurrentOriginLabel() =>
            IsOriginBottomLeftSelected() ? "bottom-left" : "top-left";

        private bool IsOriginBottomLeftSelected() =>
            cbo_Origin.SelectedIndex == 1;

        private static string EnsureWorkingDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ODBppExtractorTemp");
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        private void cbo_Origin_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendSelection)
            {
                return;
            }

            HandleLayerSelectionChange();
        }

        private void chk_FlipXAxis_CheckedChanged(object sender, EventArgs e) => HandleLayerSelectionChange();

        private void chk_FlipYAxis_CheckedChanged(object sender, EventArgs e) => HandleLayerSelectionChange();

        private void ShowViewer()
        {
            if (_currentJobReport == null)
            {
                ShowError("Load an ODB++ job before previewing.");
                return;
            }

            var step = cbo_Step.SelectedItem as ODBppExtractor.StepReport;
            if (step == null)
            {
                ShowError("Select a step before previewing.");
                return;
            }

            try
            {
                var xmlContent = BuildViewerXml();

                if (_viewerForm == null || _viewerForm.IsDisposed)
                {
                    _viewerForm = new ViewerForm(xmlContent);
                    _viewerForm.FormClosed += (s, e) => _viewerForm = null;
                    _viewerForm.Show(this);
                }
                else
                {
                    _viewerForm.UpdateFromXml(xmlContent, autoFit: true);
                    _viewerForm.BringToFront();
                    _viewerForm.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to open viewer: {ex.Message}");
            }
        }

        private string BuildViewerXml()
        {
            var origin = IsOriginBottomLeftSelected()
                ? ODBppExtractor.CoordinateOrigin.BottomLeft
                : ODBppExtractor.CoordinateOrigin.TopLeft;

            var flipOptions = BuildComponentPlacementFlipOptions();

            HashSet<string> layerFilter = null;
            var selectedLayer = cbo_Layer.SelectedItem as ODBppExtractor.LayerReport;
            if (selectedLayer != null && selectedLayer.Exists)
            {
                layerFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { selectedLayer.Name };
            }

            return GenerateViewerXml(_currentJobReport, origin, layerFilter, flipOptions);
        }

        private void RefreshViewer(bool autoFit)
        {
            if (_viewerForm == null || _viewerForm.IsDisposed || _currentJobReport == null)
            {
                return;
            }

            try
            {
                var xmlContent = BuildViewerXml();
                _viewerForm.UpdateFromXml(xmlContent, autoFit: autoFit);
            }
            catch
            {
                // Keep the main workflow responsive even if viewer refresh fails
            }
        }

        private void CloseViewer()
        {
            if (_viewerForm == null)
            {
                return;
            }

            try
            {
                if (!_viewerForm.IsDisposed)
                {
                    _viewerForm.Close();
                }
            }
            catch
            {
                // Ignore viewer closing errors
            }
            finally
            {
                _viewerForm = null;
            }
        }

        private string GenerateViewerXml(
            ODBppExtractor.JobReport report,
            ODBppExtractor.CoordinateOrigin origin,
            HashSet<string> layerFilter,
            ODBppExtractor.ComponentPlacementFlipOptions flipOptions)
        {
            var placements = ODBppExtractor.GetComponentPlacements(
                report,
                topLeft: origin == ODBppExtractor.CoordinateOrigin.TopLeft,
                flipOptions: flipOptions);

            // Group by step and layer
            var stepGroups = placements
                .Where(p => layerFilter == null || layerFilter.Contains(p.Layer))
                .GroupBy(p => p.Step)
                .ToList();

            var stepElements = new List<XElement>();

            foreach (var stepGroup in stepGroups)
            {
                var stepName = stepGroup.Key;
                var stepReport = report.Steps.FirstOrDefault(s => 
                    string.Equals(s.Name, stepName, StringComparison.OrdinalIgnoreCase));

                if (stepReport == null)
                    continue;

                var layerGroups = stepGroup.GroupBy(p => p.Layer).ToList();
                var layerElements = new List<XElement>();

                foreach (var layerGroup in layerGroups)
                {
                    var layerName = layerGroup.Key;
                    var layerReport = stepReport.Layers.FirstOrDefault(l => 
                        string.Equals(l.Name, layerName, StringComparison.OrdinalIgnoreCase));

                    if (layerReport == null)
                        continue;

                    var componentElements = layerGroup.Select(placement => new XElement("component",
                        new XAttribute("name", placement.ComponentName ?? string.Empty),
                        new XAttribute("rotation", placement.Rotation),
                        new XAttribute("shape", "rect"),
                        new XAttribute("packageName", placement.PackageName ?? string.Empty),
                        new XAttribute("centerX", placement.CenterX),
                        new XAttribute("centerY", placement.CenterY),
                        new XAttribute("width", placement.Width),
                        new XAttribute("length", placement.Length)
                    )).ToList();

                    var layerElement = new XElement("layer",
                        new XAttribute("name", layerName),
                        new XAttribute("unit", layerReport.Components?.Unit ?? stepReport.Unit ?? "MM"),
                        componentElements);

                    layerElements.Add(layerElement);
                }

                var width = stepReport.ProfileBoundingBox?.MaxX - stepReport.ProfileBoundingBox?.MinX ?? 0;
                var length = stepReport.ProfileBoundingBox?.MaxY - stepReport.ProfileBoundingBox?.MinY ?? 0;

                var stepElement = new XElement("step",
                    new XAttribute("name", stepName),
                    new XAttribute("unit", stepReport.Unit ?? "MM"),
                    new XAttribute("width", FormatDimension(width)),
                    new XAttribute("length", FormatDimension(length)),
                    layerElements);

                stepElements.Add(stepElement);
            }

            var componentCount = stepElements.Sum(step => 
                step.Elements("layer").Sum(layer => layer.Elements("component").Count()));

            var originText = origin == ODBppExtractor.CoordinateOrigin.TopLeft ? "top-left" : "bottom-left";

            var boardsElement = new XElement("boards",
                new XAttribute("generatedAt", report.ExtractedAt.ToString("o")),
                new XAttribute("origin", originText),
                new XAttribute("count", componentCount),
                stepElements);

            var doc = new XDocument(boardsElement);
            
            using (var stringWriter = new StringWriter())
            {
                doc.Save(stringWriter);
                return stringWriter.ToString();
            }
        }
    }
}
