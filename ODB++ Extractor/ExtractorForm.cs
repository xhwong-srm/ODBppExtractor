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
using ODB___Extractor.Properties;
using WK.Libraries.BetterFolderBrowserNS;

namespace ODB___Extractor
{
    public partial class ExtractorForm : Form
    {
        private string DefaultStatusText => Localizer.Get("Extractor_DefaultStatusText");
        private string DefaultStatisticText => Localizer.Get("Extractor_DefaultStatisticText");

        private bool _suspendSelection;
        private bool _isLoading;
        private ODBppExtractor.JobReport _currentJobReport;
        private readonly string _workingDirectoryRoot;
        private ViewerForm _viewerForm;
        private static readonly string[] SupportedCultures = { "en", "zh-CHS" };

        public ExtractorForm()
        {
            InitializeComponent();
            InitializeDataGrid();
            ResetUi();
            InitializeOriginCombo();
            InitializeUnitCombo();
            _workingDirectoryRoot = EnsureWorkingDirectory();
            KeyPreview = true;
            KeyDown += ExtractorForm_KeyDown;
            cbo_Unit.SelectedIndexChanged += cbo_Unit_SelectedIndexChanged;
            ApplyLocalization();
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
                dialog.Title = Localizer.Get("Extractor_SelectFileTitle");
                dialog.Filter = Localizer.Get("Extractor_ArchiveFilter");
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;

                TrySetInitialDirectory(dialog, txt_Path.Text);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (!IsAllowedArchive(dialog.FileName))
                    {
                        MessageBox.Show(this, Localizer.Get("Extractor_InvalidFileMessage"), Localizer.Get("Extractor_InvalidFileTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                Title = Localizer.Get("Extractor_SelectFolderTitle"),
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

            lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_StepSelected"), step.Name, step.Layers.Count);
            lbl_Statistic.Text = BuildStepDimensionText(step, GetTargetUnit());
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
            var componentColumn = CreateTextColumn("ComponentName", Localizer.Get("Extractor_Grid_Component"), DataGridViewAutoSizeColumnMode.Fill);
            componentColumn.MinimumWidth = 200;
            componentColumn.FillWeight = 2f;
            dgv_Data.Columns.Add(componentColumn);
            dgv_Data.Columns.Add(CreateTextColumn("PackageName", Localizer.Get("Extractor_Grid_Package"), DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("CenterX", Localizer.Get("Extractor_Grid_CenterX"), DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("CenterY", Localizer.Get("Extractor_Grid_CenterY"), DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Rotation", Localizer.Get("Extractor_Grid_Rotation"), DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Width", Localizer.Get("Extractor_Grid_Width"), DataGridViewAutoSizeColumnMode.AllCells));
            dgv_Data.Columns.Add(CreateTextColumn("Length", Localizer.Get("Extractor_Grid_Length"), DataGridViewAutoSizeColumnMode.AllCells));
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

            var friendlyName = string.IsNullOrWhiteSpace(path) ? Localizer.Get("Extractor_FriendlyJobName") : Path.GetFileName(path);
            lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_Loading"), friendlyName);
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
                    ShowError(result.ErrorMessage ?? Localizer.Get("Extractor_Error_FailedLoad"));
                    ResetUi(Localizer.Get("Extractor_Error_FailedLoad"));
                    return;
                }

                _currentJobReport = result.JobReport;
                if (_currentJobReport?.Steps == null || _currentJobReport.Steps.Count == 0)
                {
                    ResetUi(Localizer.Get("Extractor_Status_NoStepsFound"));
                    return;
                }

                SelectDefaultUnitFromJob(_currentJobReport);
                PopulateSteps();
                lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_LoadedSteps"), _currentJobReport.Steps.Count);

                RefreshViewer(autoFit: true);
            }
            catch (Exception ex)
            {
                ShowError(string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Error_UnexpectedLoad"), ex.Message));
                ResetUi(Localizer.Get("Extractor_Error_FailedLoad"));
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
                lbl_Status.Text = Localizer.Get("Extractor_Status_NoStepEntries");
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
                lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_NoComponentLayers"), stepReport.Name);
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
            var stepText = BuildStepDimensionText(step, GetTargetUnit());
            var unitText = DetermineLayerUnit(step, layer);
            var originLabel = GetCurrentOriginLabel();

            if (step == null || layer == null)
            {
                lbl_Statistic.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Statistic_Format"), stepText, unitText, 0);
                lbl_Status.Text = Localizer.Get("Extractor_Status_SelectLayer");
                return;
            }

            if (!layer.Exists)
            {
                lbl_Statistic.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Statistic_Format"), stepText, unitText, 0);
                lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_LayerNotFound"), layer.Name);
                return;
            }

            var placements = (GetCurrentPlacementData() ?? Array.Empty<ODBppExtractor.ComponentPlacementInfo>())
                .Where(info =>
                    string.Equals(info.Step, step.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(info.Layer, layer.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (placements.Count == 0)
            {
                lbl_Statistic.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Statistic_Format"), stepText, unitText, 0);
                lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_NoComponents"), layer.Name, originLabel);
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

            lbl_Statistic.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Statistic_Format"), stepText, unitText, placements.Count);
            lbl_Status.Text = string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Status_Components"), layer.Name, originLabel, placements.Count);
        }

        private void ShowError(string message)
        {
            var text = string.IsNullOrWhiteSpace(message)
                ? Localizer.Get("Extractor_Error_UnknownProcessing")
                : message;

            MessageBox.Show(this, text, Localizer.Get("Common_ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ResetUi(string statusMessage = null)
        {
            _currentJobReport = null;
            CloseViewer();
            ClearVisuals();
            lbl_Status.Text = statusMessage ?? DefaultStatusText;
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
            cbo_Unit.Enabled = enabled;
        }

        private void ExportComponents(bool exportAllLayers)
        {
            if (_currentJobReport == null)
            {
                ShowError(Localizer.Get("Extractor_Error_LoadBeforeExport"));
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
                    ShowError(Localizer.Get("Extractor_Error_SelectLayerBeforeExport"));
                    return;
                }

                var layerName = layerReport.Name;
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    ShowError(Localizer.Get("Extractor_Error_InvalidLayerName"));
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
                    targetUnit: GetTargetUnit(),
                    flipOptions: flipOptions);
            }
            catch (Exception ex)
            {
                ShowError(string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Error_ExportFailed"), ex.Message));
                return;
            }

            if (exportedPaths == null || exportedPaths.Count == 0)
            {
                MessageBox.Show(this, Localizer.Get("Extractor_Export_NoReports"), Localizer.Get("Extractor_Export_ResultTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Export_CompleteMessage"), targetDirectory), Localizer.Get("Extractor_Export_CompleteTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool TrySelectExportDirectory(out string exportDirectory)
        {
            exportDirectory = null;
            using (var browser = new BetterFolderBrowser
            {
                Title = Localizer.Get("Extractor_SelectExportFolderTitle"),
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

        private static string BuildStepDimensionText(ODBppExtractor.StepReport step, string targetUnit)
        {
            if (step == null)
            {
                return Localizer.Get("Extractor_DimensionUnavailable");
            }

            if (step.ProfileBoundingBox.HasValue)
            {
                var bbox = step.ProfileBoundingBox.Value;
                var sourceUnit = step.Unit ?? "MM";
                var widthValue = bbox.MaxX - bbox.MinX;
                var lengthValue = bbox.MaxY - bbox.MinY;
                if (!string.IsNullOrWhiteSpace(targetUnit))
                {
                    widthValue = ODBppExtractor.ConvertUnitValue(widthValue, sourceUnit, targetUnit);
                    lengthValue = ODBppExtractor.ConvertUnitValue(lengthValue, sourceUnit, targetUnit);
                }

                var width = FormatDimension(widthValue);
                var length = FormatDimension(lengthValue);
                return string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_DimensionFormat"), width, length);
            }

            return Localizer.Get("Extractor_DimensionUnavailable");
        }

        private string DetermineLayerUnit(ODBppExtractor.StepReport step, ODBppExtractor.LayerReport layer)
        {
            var selected = GetSelectedUnitDisplay();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return selected;
            }

            var unit = layer?.Components?.Unit;
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = step?.Unit;
            }

            return string.IsNullOrWhiteSpace(unit) ? Localizer.Get("Extractor_UnitUnknown") : unit;
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
            cbo_Origin.Items.Add(Localizer.Get("Extractor_Origin_TopLeft"));
            cbo_Origin.Items.Add(Localizer.Get("Extractor_Origin_BottomLeft"));
            cbo_Origin.SelectedIndex = 0;
            _suspendSelection = false;
        }

        private void InitializeUnitCombo()
        {
            cbo_Unit.DisplayMember = nameof(UnitChoice.Display);
            UpdateUnitCombo();
        }

        private void UpdateUnitCombo()
        {
            var selectedUnit = GetSelectedUnit();
            _suspendSelection = true;
            cbo_Unit.Items.Clear();
            cbo_Unit.Items.Add(new UnitChoice("MM", Localizer.Get("Extractor_Unit_MM")));
            cbo_Unit.Items.Add(new UnitChoice("INCH", Localizer.Get("Extractor_Unit_INCH")));
            SetUnitSelection(selectedUnit);
            _suspendSelection = false;
        }

        private void SetUnitSelection(string unit)
        {
            if (cbo_Unit.Items.Count == 0)
            {
                return;
            }

            var normalized = NormalizeUnit(unit);
            if (!string.IsNullOrEmpty(normalized))
            {
                for (var i = 0; i < cbo_Unit.Items.Count; i++)
                {
                    if (cbo_Unit.Items[i] is UnitChoice choice &&
                        string.Equals(choice.Unit, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        cbo_Unit.SelectedIndex = i;
                        return;
                    }
                }
            }

            cbo_Unit.SelectedIndex = 0;
        }

        private void SelectDefaultUnitFromJob(ODBppExtractor.JobReport report)
        {
            if (report?.Steps == null || report.Steps.Count == 0)
            {
                return;
            }

            var step = report.Steps.FirstOrDefault();
            var layer = step?.Layers?.FirstOrDefault();
            var unit = layer?.Components?.Unit ?? step?.Unit;
            _suspendSelection = true;
            SetUnitSelection(unit);
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
                flipOptions: flipOptions,
                targetUnit: GetTargetUnit());
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
            IsOriginBottomLeftSelected() ? Localizer.Get("Extractor_OriginLabel_BottomLeft") : Localizer.Get("Extractor_OriginLabel_TopLeft");

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

        private void cbo_Unit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendSelection)
            {
                return;
            }

            HandleLayerSelectionChange();
        }

        private void chk_FlipXAxis_CheckedChanged(object sender, EventArgs e) => HandleLayerSelectionChange();

        private void chk_FlipYAxis_CheckedChanged(object sender, EventArgs e) => HandleLayerSelectionChange();

        private void ExtractorForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.L)
            {
                RotateCulture();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void RotateCulture()
        {
            var current = Settings.Default.UICulture ?? string.Empty;
            var index = Array.FindIndex(SupportedCultures, code =>
                string.Equals(code, current, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                index = 0;
            }

            var next = SupportedCultures[(index + 1) % SupportedCultures.Length];
            Localizer.SetCulture(next, applyToOpenForms: true);

            var languageName = GetLanguageDisplayName(next);
            lbl_Status.Text = string.Format(
                CultureInfo.CurrentCulture,
                Localizer.Get("Extractor_Status_LanguageChanged"),
                languageName);
        }

        private string GetLanguageDisplayName(string cultureCode)
        {
            if (string.Equals(cultureCode, "zh-CHS", StringComparison.OrdinalIgnoreCase))
            {
                return Localizer.Get("Extractor_Language_Chinese");
            }

            return Localizer.Get("Extractor_Language_English");
        }

        public void ApplyLocalization()
        {
            Localizer.Apply(this);
            UpdateGridHeaders();
            UpdateOriginCombo();
            UpdateUnitCombo();
            if (_currentJobReport == null)
            {
                lbl_Status.Text = DefaultStatusText;
                lbl_Statistic.Text = DefaultStatisticText;
            }
            else
            {
                HandleLayerSelectionChange();
            }
        }

        private void UpdateGridHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                ["ComponentName"] = Localizer.Get("Extractor_Grid_Component"),
                ["PackageName"] = Localizer.Get("Extractor_Grid_Package"),
                ["CenterX"] = Localizer.Get("Extractor_Grid_CenterX"),
                ["CenterY"] = Localizer.Get("Extractor_Grid_CenterY"),
                ["Rotation"] = Localizer.Get("Extractor_Grid_Rotation"),
                ["Width"] = Localizer.Get("Extractor_Grid_Width"),
                ["Length"] = Localizer.Get("Extractor_Grid_Length")
            };

            foreach (DataGridViewColumn column in dgv_Data.Columns)
            {
                if (headers.TryGetValue(column.Name, out var header))
                {
                    column.HeaderText = header;
                }
            }
        }

        private void UpdateOriginCombo()
        {
            var selected = cbo_Origin.SelectedIndex;
            InitializeOriginCombo();
            if (selected >= 0 && selected < cbo_Origin.Items.Count)
            {
                cbo_Origin.SelectedIndex = selected;
            }
        }

        private void ShowViewer()
        {
            if (_currentJobReport == null)
            {
                ShowError(Localizer.Get("Extractor_Error_LoadBeforePreview"));
                return;
            }

            var step = cbo_Step.SelectedItem as ODBppExtractor.StepReport;
            if (step == null)
            {
                ShowError(Localizer.Get("Extractor_Error_SelectStepBeforePreview"));
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
                ShowError(string.Format(CultureInfo.CurrentCulture, Localizer.Get("Extractor_Error_OpenViewerFailed"), ex.Message));
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

            return GenerateViewerXml(_currentJobReport, origin, layerFilter, flipOptions, GetTargetUnit());
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
            ODBppExtractor.ComponentPlacementFlipOptions flipOptions,
            string targetUnit)
        {
            var placements = ODBppExtractor.GetComponentPlacements(
                report,
                topLeft: origin == ODBppExtractor.CoordinateOrigin.TopLeft,
                flipOptions: flipOptions,
                targetUnit: targetUnit);

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
                        new XAttribute("unit", targetUnit ?? layerReport.Components?.Unit ?? stepReport.Unit ?? "MM"),
                        componentElements);

                    layerElements.Add(layerElement);
                }

                var stepUnit = stepReport.Unit ?? "MM";
                var width = stepReport.ProfileBoundingBox?.MaxX - stepReport.ProfileBoundingBox?.MinX ?? 0;
                var length = stepReport.ProfileBoundingBox?.MaxY - stepReport.ProfileBoundingBox?.MinY ?? 0;
                if (!string.IsNullOrWhiteSpace(targetUnit))
                {
                    width = ODBppExtractor.ConvertUnitValue(width, stepUnit, targetUnit);
                    length = ODBppExtractor.ConvertUnitValue(length, stepUnit, targetUnit);
                    stepUnit = targetUnit;
                }

                var stepElement = new XElement("step",
                    new XAttribute("name", stepName),
                    new XAttribute("unit", stepUnit),
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

        private string GetTargetUnit()
        {
            var selectedUnit = GetSelectedUnit();
            return NormalizeUnit(selectedUnit);
        }

        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                return null;
            }

            var normalized = unit.Trim().ToUpperInvariant();
            switch (normalized)
            {
                case "IN":
                case "INCH":
                    return "INCH";
                case "MM":
                case "MILLIMETER":
                case "MILLIMETERS":
                    return "MM";
                default:
                    return normalized;
            }
        }

        private string GetSelectedUnit()
        {
            return cbo_Unit.SelectedItem is UnitChoice choice ? choice.Unit : null;
        }

        private string GetSelectedUnitDisplay()
        {
            return cbo_Unit.SelectedItem is UnitChoice choice ? choice.Display : null;
        }

        private sealed class UnitChoice
        {
            public UnitChoice(string unit, string display)
            {
                Unit = unit;
                Display = display;
            }

            public string Unit { get; }
            public string Display { get; }

            public override string ToString() => Display ?? base.ToString();
        }
    }
}
