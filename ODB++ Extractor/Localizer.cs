using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using ODB___Extractor.Properties;

namespace ODB___Extractor
{
    internal static class Localizer
    {
        private const string DefaultCultureCode = "en";
        private static readonly Dictionary<string, Dictionary<string, string>> Store =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Common_ErrorTitle"] = "Error",
                    ["Common_InfoTitle"] = "Info",
                    ["Extractor_ArchiveFilter"] = "Archives (*.tgz;*.zip;*.tar;*.tar.gz)|*.tgz;*.zip;*.tar;*.tar.gz",
                    ["Extractor_DefaultStatisticText"] = "No component data loaded (top-left).",
                    ["Extractor_DefaultStatusText"] = "Waiting for an ODB++ archive or directory.",
                    ["Extractor_DimensionFormat"] = "Dimension: {0} x {1}",
                    ["Extractor_DimensionUnavailable"] = "Dimension unavailable",
                    ["Extractor_Error_ExportFailed"] = "Export failed: {0}",
                    ["Extractor_Error_FailedLoad"] = "Failed to load ODB++ job.",
                    ["Extractor_Error_InvalidLayerName"] = "Selected layer does not have a valid name.",
                    ["Extractor_Error_LoadBeforeExport"] = "Load an ODB++ job before exporting.",
                    ["Extractor_Error_LoadBeforePreview"] = "Load an ODB++ job before previewing.",
                    ["Extractor_Error_OpenViewerFailed"] = "Failed to open viewer: {0}",
                    ["Extractor_Error_SelectLayerBeforeExport"] = "Select a layer before exporting.",
                    ["Extractor_Error_SelectStepBeforePreview"] = "Select a step before previewing.",
                    ["Extractor_Error_UnexpectedLoad"] = "Unexpected error while loading ODB++ job: {0}",
                    ["Extractor_Error_UnknownProcessing"] = "An unknown error occurred while processing the ODB++ data.",
                    ["Extractor_Export_CompleteMessage"] = "Component reports exported to:\n{0}",
                    ["Extractor_Export_CompleteTitle"] = "Export complete",
                    ["Extractor_Export_NoReports"] = "No component reports were generated.",
                    ["Extractor_Export_ResultTitle"] = "Export result",
                    ["Extractor_FriendlyJobName"] = "ODB++ job",
                    ["Extractor_Grid_CenterX"] = "Center X",
                    ["Extractor_Grid_CenterY"] = "Center Y",
                    ["Extractor_Grid_Component"] = "Component",
                    ["Extractor_Grid_Length"] = "Length",
                    ["Extractor_Grid_Package"] = "Package",
                    ["Extractor_Grid_Rotation"] = "Rotation",
                    ["Extractor_Grid_Width"] = "Width",
                    ["Extractor_InvalidFileMessage"] = "Only .tgz, .zip, .tar, or .tar.gz files are supported.",
                    ["Extractor_InvalidFileTitle"] = "Invalid file",
                    ["Extractor_Language_Chinese"] = "Simplified Chinese",
                    ["Extractor_Language_English"] = "English",
                    ["Extractor_Origin_BottomLeft"] = "Bottom-left",
                    ["Extractor_Origin_TopLeft"] = "Top-left",
                    ["Extractor_OriginLabel_BottomLeft"] = "bottom-left",
                    ["Extractor_OriginLabel_TopLeft"] = "top-left",
                    ["Extractor_SelectExportFolderTitle"] = "Select export folder",
                    ["Extractor_SelectFileTitle"] = "Select file",
                    ["Extractor_SelectFolderTitle"] = "Select folder",
                    ["Extractor_Status_Components"] = "Layer '{0}' ({1}) contains {2} components.",
                    ["Extractor_Status_LayerNotFound"] = "Layer '{0}' was not found on disk.",
                    ["Extractor_Status_LoadedSteps"] = "Loaded {0} step(s).",
                    ["Extractor_Status_Loading"] = "Loading '{0}'...",
                    ["Extractor_Status_NoComponentLayers"] = "Step '{0}' contains no component layers.",
                    ["Extractor_Status_NoComponents"] = "Layer '{0}' ({1}) contains no components.",
                    ["Extractor_Status_NoStepEntries"] = "No step entries available.",
                    ["Extractor_Status_NoStepsFound"] = "No steps were found in the ODB++ job.",
                    ["Extractor_Status_SelectLayer"] = "Select a layer to view data.",
                    ["Extractor_Status_StepSelected"] = "Step '{0}' selected ({1} layer(s)).",
                    ["Extractor_Status_LanguageChanged"] = "Current language: {0}.",
                    ["Extractor_Statistic_Format"] = "{0} • Unit: {1} • Components count: {2}",
                    ["Extractor_UnitUnknown"] = "unit unknown",
                    ["Viewer_BackgroundImageFilter"] = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif",
                    ["Viewer_BackgroundImageTitle"] = "Select background image",
                    ["Viewer_BackgroundLoadFailed"] = "Failed to load image: {0}",
                    ["Viewer_BackgroundTitle"] = "Background",
                    ["Viewer_BoardDataNullMessage"] = "Error: Board data is null. XML may not have been parsed correctly.",
                    ["Viewer_ComponentInfo_Length"] = "Length: {0:F2}",
                    ["Viewer_ComponentInfo_Package"] = "Package: {0}",
                    ["Viewer_ComponentInfo_Position"] = "Position: {0:F2}, {1:F2}",
                    ["Viewer_ComponentInfo_Rotation"] = "Rotation: {0:F2}°",
                    ["Viewer_ComponentInfo_Width"] = "Width: {0:F2}",
                    ["Viewer_DefaultComponentName"] = "Unknown",
                    ["Viewer_DefaultLayerName"] = "Unknown",
                    ["Viewer_DefaultPackageName"] = "N/A",
                    ["Viewer_DefaultStepName"] = "Step",
                    ["Viewer_NoLayersMessage"] = "No layers found in the ODB++ data.\n\nXML Content Length: {0}",
                    ["Viewer_ParseErrorMessage"] = "Error parsing XML: {0}",
                    ["Viewer_ParseErrorTitle"] = "Parse Error",
                    ["Viewer_SearchPlaceholder"] = "Search components…",
                    ["ExtractorForm.$this.Text"] = "ODB++ Extractor",
                    ["ExtractorForm.lbl_Path.Text"] = "Path:",
                    ["ExtractorForm.btn_BrowseDir.Text"] = "Browse folder",
                    ["ExtractorForm.btn_BrowseFile.Text"] = "Browse file",
                    ["ExtractorForm.gb_ImportODBpp.Text"] = "Import ODB++ Design",
                    ["ExtractorForm.lbl_Step.Text"] = "Step:",
                    ["ExtractorForm.lbl_Layer.Text"] = "Layer:",
                    ["ExtractorForm.lbl_Statistic.Text"] = "Statistics",
                    ["ExtractorForm.chk_FlipYAxis.Text"] = "Flip Y-axis",
                    ["ExtractorForm.chk_FlipXAxis.Text"] = "Flip X-axis",
                    ["ExtractorForm.lbl_Origin.Text"] = "Origin:",
                    ["ExtractorForm.btn_RefreshData.Text"] = "Refresh",
                    ["ExtractorForm.gb_Data.Text"] = "Component Data",
                    ["ExtractorForm.btn_PreviewData.Text"] = "Preview",
                    ["ExtractorForm.lbl_Status.Text"] = "Status",
                    ["ExtractorForm.btn_ExportLayer.Text"] = "Export Current Layer",
                    ["ExtractorForm.btn_ExportAllLayer.Text"] = "Export All Layers",
                    ["ExtractorForm.gb_Export.Text"] = "Export",
                    ["ViewerForm.$this.Text"] = "ODB++ Component Viewer",
                    ["ViewerForm.txt_Search.Text"] = " ",
                    ["ViewerForm.resetBackgroundTransformMenuItem.Text"] = "Reset background alignment",
                    ["ViewerForm.resetView.Text"] = "Reset View",
                    ["ViewerForm.loadBackgroundMenuItem.Text"] = "Load background image...",
                    ["ViewerForm.clearBackgroundMenuItem.Text"] = "Clear background",
                    ["ViewerForm.toggleBackgroundLockMenuItem.Text"] = "Lock background"
                },
                ["zh-CHS"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Common_ErrorTitle"] = "错误",
                    ["Common_InfoTitle"] = "信息",
                    ["Extractor_ArchiveFilter"] = "归档 (*.tgz;*.zip;*.tar;*.tar.gz)|*.tgz;*.zip;*.tar;*.tar.gz",
                    ["Extractor_DefaultStatisticText"] = "未加载组件数据（左上角）。",
                    ["Extractor_DefaultStatusText"] = "等待 ODB++ 压缩包或目录。",
                    ["Extractor_DimensionFormat"] = "尺寸：{0} x {1}",
                    ["Extractor_DimensionUnavailable"] = "尺寸不可用",
                    ["Extractor_Error_ExportFailed"] = "导出失败：{0}",
                    ["Extractor_Error_FailedLoad"] = "加载 ODB++ 作业失败。",
                    ["Extractor_Error_InvalidLayerName"] = "所选层没有有效名称。",
                    ["Extractor_Error_LoadBeforeExport"] = "请先加载 ODB++ 作业再导出。",
                    ["Extractor_Error_LoadBeforePreview"] = "请先加载 ODB++ 作业再预览。",
                    ["Extractor_Error_OpenViewerFailed"] = "打开查看器失败：{0}",
                    ["Extractor_Error_SelectLayerBeforeExport"] = "请先选择层再导出。",
                    ["Extractor_Error_SelectStepBeforePreview"] = "请先选择步骤再预览。",
                    ["Extractor_Error_UnexpectedLoad"] = "加载 ODB++ 作业时发生意外错误：{0}",
                    ["Extractor_Error_UnknownProcessing"] = "处理 ODB++ 数据时发生未知错误。",
                    ["Extractor_Export_CompleteMessage"] = "组件报告已导出到：\n{0}",
                    ["Extractor_Export_CompleteTitle"] = "导出完成",
                    ["Extractor_Export_NoReports"] = "未生成任何组件报告。",
                    ["Extractor_Export_ResultTitle"] = "导出结果",
                    ["Extractor_FriendlyJobName"] = "ODB++ 作业",
                    ["Extractor_Grid_CenterX"] = "中心 X",
                    ["Extractor_Grid_CenterY"] = "中心 Y",
                    ["Extractor_Grid_Component"] = "元件",
                    ["Extractor_Grid_Length"] = "长度",
                    ["Extractor_Grid_Package"] = "封装",
                    ["Extractor_Grid_Rotation"] = "旋转",
                    ["Extractor_Grid_Width"] = "宽度",
                    ["Extractor_InvalidFileMessage"] = "仅支持 .tgz、.zip、.tar 或 .tar.gz 文件。",
                    ["Extractor_InvalidFileTitle"] = "无效文件",
                    ["Extractor_Language_Chinese"] = "简体中文",
                    ["Extractor_Language_English"] = "英语",
                    ["Extractor_Origin_BottomLeft"] = "左下",
                    ["Extractor_Origin_TopLeft"] = "左上",
                    ["Extractor_OriginLabel_BottomLeft"] = "左下",
                    ["Extractor_OriginLabel_TopLeft"] = "左上",
                    ["Extractor_SelectExportFolderTitle"] = "选择导出文件夹",
                    ["Extractor_SelectFileTitle"] = "选择文件",
                    ["Extractor_SelectFolderTitle"] = "选择文件夹",
                    ["Extractor_Status_Components"] = "层 '{0}'（{1}）包含 {2} 个元件。",
                    ["Extractor_Status_LayerNotFound"] = "在磁盘上未找到层 '{0}'。",
                    ["Extractor_Status_LoadedSteps"] = "已加载 {0} 个步骤。",
                    ["Extractor_Status_Loading"] = "正在加载 '{0}'...",
                    ["Extractor_Status_NoComponentLayers"] = "步骤 '{0}' 不包含元件层。",
                    ["Extractor_Status_NoComponents"] = "层 '{0}'（{1}）不包含元件。",
                    ["Extractor_Status_NoStepEntries"] = "没有可用的步骤条目。",
                    ["Extractor_Status_NoStepsFound"] = "ODB++ 作业中未找到步骤。",
                    ["Extractor_Status_SelectLayer"] = "请选择一个层以查看数据。",
                    ["Extractor_Status_StepSelected"] = "已选择步骤 '{0}'（{1} 个层）。",
                    ["Extractor_Status_LanguageChanged"] = "当前语言：{0}。",
                    ["Extractor_Statistic_Format"] = "{0} • 单位：{1} • 元件数量：{2}",
                    ["Extractor_UnitUnknown"] = "单位未知",
                    ["Viewer_BackgroundImageFilter"] = "图像|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif",
                    ["Viewer_BackgroundImageTitle"] = "选择背景图像",
                    ["Viewer_BackgroundLoadFailed"] = "加载图像失败：{0}",
                    ["Viewer_BackgroundTitle"] = "背景",
                    ["Viewer_BoardDataNullMessage"] = "错误：板数据为空。XML 可能未正确解析。",
                    ["Viewer_ComponentInfo_Length"] = "长度：{0:F2}",
                    ["Viewer_ComponentInfo_Package"] = "封装：{0}",
                    ["Viewer_ComponentInfo_Position"] = "位置：{0:F2}, {1:F2}",
                    ["Viewer_ComponentInfo_Rotation"] = "旋转：{0:F2}°",
                    ["Viewer_ComponentInfo_Width"] = "宽度：{0:F2}",
                    ["Viewer_DefaultComponentName"] = "未知",
                    ["Viewer_DefaultLayerName"] = "未知",
                    ["Viewer_DefaultPackageName"] = "N/A",
                    ["Viewer_DefaultStepName"] = "步骤",
                    ["Viewer_NoLayersMessage"] = "ODB++ 数据中未找到层。\n\nXML 内容长度：{0}",
                    ["Viewer_ParseErrorMessage"] = "解析 XML 出错：{0}",
                    ["Viewer_ParseErrorTitle"] = "解析错误",
                    ["Viewer_SearchPlaceholder"] = "搜索元件…",
                    ["ExtractorForm.$this.Text"] = "ODB++ 提取器",
                    ["ExtractorForm.lbl_Path.Text"] = "路径：",
                    ["ExtractorForm.btn_BrowseDir.Text"] = "浏览文件夹",
                    ["ExtractorForm.btn_BrowseFile.Text"] = "浏览文件",
                    ["ExtractorForm.gb_ImportODBpp.Text"] = "导入ODB++设计",
                    ["ExtractorForm.lbl_Step.Text"] = "步骤：",
                    ["ExtractorForm.lbl_Layer.Text"] = "图层：",
                    ["ExtractorForm.lbl_Statistic.Text"] = "统计",
                    ["ExtractorForm.chk_FlipYAxis.Text"] = "反转Y轴",
                    ["ExtractorForm.chk_FlipXAxis.Text"] = "反转X轴",
                    ["ExtractorForm.lbl_Origin.Text"] = "原点：",
                    ["ExtractorForm.btn_RefreshData.Text"] = "刷新",
                    ["ExtractorForm.gb_Data.Text"] = "元器件数据",
                    ["ExtractorForm.btn_PreviewData.Text"] = "预览",
                    ["ExtractorForm.lbl_Status.Text"] = "状态",
                    ["ExtractorForm.btn_ExportLayer.Text"] = "导出当前图层",
                    ["ExtractorForm.btn_ExportAllLayer.Text"] = "导出全部图层",
                    ["ExtractorForm.gb_Export.Text"] = "导出",
                    ["ViewerForm.$this.Text"] = "ODB++ 元器件预览",
                    ["ViewerForm.txt_Search.Text"] = " ",
                    ["ViewerForm.resetBackgroundTransformMenuItem.Text"] = "重置背景对齐",
                    ["ViewerForm.resetView.Text"] = "重置视图",
                    ["ViewerForm.loadBackgroundMenuItem.Text"] = "加载背景图像...",
                    ["ViewerForm.clearBackgroundMenuItem.Text"] = "清除背景",
                    ["ViewerForm.toggleBackgroundLockMenuItem.Text"] = "锁定背景"
                }
            };

        private static Dictionary<string, string> _strings = Store[DefaultCultureCode];
        public static string CurrentCultureCode { get; private set; } = DefaultCultureCode;

        public static void Initialize()
        {
            var cultureCode = string.IsNullOrWhiteSpace(Settings.Default.UICulture)
                ? DefaultCultureCode
                : Settings.Default.UICulture;
            SetCulture(cultureCode, applyToOpenForms: false);
        }

        public static void SetCulture(string cultureCode, bool applyToOpenForms)
        {
            var normalized = NormalizeCultureCode(cultureCode);
            if (!Store.TryGetValue(normalized, out var map))
            {
                map = Store[DefaultCultureCode];
                normalized = DefaultCultureCode;
            }

            _strings = map;
            CurrentCultureCode = normalized;
            Settings.Default.UICulture = normalized;
            Settings.Default.Save();

            TryApplyThreadCulture(normalized);

            if (applyToOpenForms)
            {
                ApplyOpenForms();
            }
        }

        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (_strings.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
        }

        public static void Apply(Form form)
        {
            if (form == null)
            {
                return;
            }

            ApplyText(form, $"{form.Name}.$this.Text", form.Text);
            ApplyControlTexts(form, form.Name);
            ApplyToolStripTexts(form, form.Name);
        }

        private static void ApplyOpenForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form is ExtractorForm extractor)
                {
                    extractor.ApplyLocalization();
                }
                else if (form is ViewerForm viewer)
                {
                    viewer.ApplyLocalization();
                }
                else
                {
                    Apply(form);
                }
            }
        }

        private static void ApplyControlTexts(Control parent, string formName)
        {
            foreach (Control control in parent.Controls)
            {
                ApplyText(control, $"{formName}.{control.Name}.Text", control.Text);
                if (control.HasChildren)
                {
                    ApplyControlTexts(control, formName);
                }
            }
        }

        private static void ApplyToolStripTexts(Form form, string formName)
        {
            foreach (var menu in FindToolStrips(form))
            {
                ApplyToolStripItems(menu.Items, formName);
            }

            foreach (var contextMenu in FindContextMenus(form))
            {
                ApplyToolStripItems(contextMenu.Items, formName);
            }
        }

        private static IEnumerable<ToolStrip> FindToolStrips(Control root)
        {
            if (root is ToolStrip strip)
            {
                yield return strip;
            }

            foreach (Control child in root.Controls)
            {
                foreach (var nested in FindToolStrips(child))
                {
                    yield return nested;
                }
            }
        }

        private static IEnumerable<ContextMenuStrip> FindContextMenus(Control root)
        {
            if (root.ContextMenuStrip != null)
            {
                yield return root.ContextMenuStrip;
            }

            foreach (Control child in root.Controls)
            {
                foreach (var nested in FindContextMenus(child))
                {
                    yield return nested;
                }
            }
        }

        private static void ApplyToolStripItems(ToolStripItemCollection items, string formName)
        {
            foreach (ToolStripItem item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    ApplyText(item, $"{formName}.{item.Name}.Text", item.Text);
                }

                if (item is ToolStripDropDownItem dropDown && dropDown.HasDropDownItems)
                {
                    ApplyToolStripItems(dropDown.DropDownItems, formName);
                }
            }
        }

        private static void ApplyText(object target, string key, string current)
        {
            var value = Get(key);
            if (string.IsNullOrEmpty(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            switch (target)
            {
                case Control control:
                    control.Text = value;
                    break;
                case ToolStripItem item:
                    item.Text = value;
                    break;
            }
        }

        private static string NormalizeCultureCode(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return DefaultCultureCode;
            }

            var trimmed = cultureCode.Trim();
            if (trimmed.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultCultureCode;
            }

            if (trimmed.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CHS";
            }

            return trimmed;
        }

        private static void TryApplyThreadCulture(string cultureCode)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureCode);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // Ignore invalid culture codes.
            }
        }
    }
}
