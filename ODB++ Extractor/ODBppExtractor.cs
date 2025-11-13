using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Lzw;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Core;

namespace ODB___Extractor
{
    /// <summary>
    /// Orchestrates the ODB++ extraction workflow: unpacking the job, reading metadata,
    /// and emitting structured job/component reports for downstream tools.
    /// </summary>
    public static class ODBppExtractor
    {
        #region Logging

        [Conditional("DEBUG")]
        private static void LogInfo(string message)
        {
            Debug.WriteLine(message);
        }

        #endregion

        #region Data Contracts

        /// <summary>
        /// Represents a single CMP record and keeps the raw string values together with the detected unit.
        /// </summary>
        public sealed class ComponentRecord
        {
            public ComponentRecord(string pkgRef, string x, string y, string rot, string mirror, string name, string part, string unit)
            {
                PkgRef = pkgRef;
                X = x;
                Y = y;
                Rot = rot;
                Mirror = mirror;
                ComponentName = name;
                PartName = part;
                Unit = unit;
            }

            public string PkgRef { get; }
            public string X { get; }
            public string Y { get; }
            public string Rot { get; }
            public string Mirror { get; }
            public string ComponentName { get; }
            public string PartName { get; }
            public string Unit { get; }
        }
        /// <summary>
        /// Describes a footprint entry (PKG) alongside its computed extents and outlines.
        /// </summary>
        public sealed class PkgRecord
        {
            public PkgRecord(int index, string name, double pitch, double xmin, double ymin, double xmax, double ymax, IReadOnlyList<OutlineRecord> outlines, string unit)
            {
                Index = index;
                Name = name;
                Pitch = pitch;
                XMin = xmin;
                YMin = ymin;
                XMax = xmax;
                YMax = ymax;
                Outlines = outlines;
                Unit = unit;
            }

            public int Index { get; }
            public string Name { get; }
            public double Pitch { get; }
            public double XMin { get; }
            public double YMin { get; }
            public double XMax { get; }
            public double YMax { get; }
            public IReadOnlyList<OutlineRecord> Outlines { get; }
            public string Unit { get; }
        }
        /// <summary>
        /// Captures the geometry definition (rectangle, circle, square) for a package outline.
        /// </summary>
        public sealed class OutlineRecord
        {
            public OutlineRecord(string type, IReadOnlyDictionary<string, double> parameters, IReadOnlyList<IReadOnlyList<double>> polygon, string raw)
            {
                Type = type;
                Parameters = parameters;
                Polygon = polygon;
                Raw = raw;
            }

            public string Type { get; }
            public IReadOnlyDictionary<string, double> Parameters { get; }
            public IReadOnlyList<IReadOnlyList<double>> Polygon { get; }
            public string Raw { get; }
        }
        /// <summary>
        /// Container for all components parsed out of one component layer file.
        /// </summary>
        public sealed class ComponentData
        {
            public ComponentData(string unit, IReadOnlyList<ComponentRecord> records)
            {
                Unit = unit;
                Records = records;
            }

            public string Unit { get; }
            public IReadOnlyList<ComponentRecord> Records { get; }
        }
        /// <summary>
        /// Holds the parsed EDA package information for a step, including the raw data file path.
        /// </summary>
        public sealed class EdaData
        {
            public EdaData(string unit, string dataPath, IReadOnlyList<PkgRecord> records)
            {
                Unit = unit;
                DataPath = dataPath;
                Records = records;
            }

            public string Unit { get; }
            public string DataPath { get; }
            public IReadOnlyList<PkgRecord> Records { get; }
        }
        /// <summary>
        /// Tracks discovery state for a layer within a step (component files, parsed representations, etc.).
        /// </summary>
        public sealed class LayerReport
        {
            public LayerReport(string name, string path, bool exists)
            {
                Name = name;
                Path = path;
                Exists = exists;
            }

            public string Name { get; }
            public string Path { get; }
            public bool Exists { get; }
            public string ComponentsPath { get; set; }
            public ComponentData Components { get; set; }
            public EdaData Eda { get; set; }
        }
        /// <summary>
        /// Aggregates per-step artifacts such as the profile, layers, and package data.
        /// </summary>
        public sealed class StepReport
        {
            public StepReport(string name, string path, bool exists)
            {
                Name = name;
                Path = path;
                Exists = exists;
                Layers = new List<LayerReport>();
            }

            public string Name { get; }
            public string Path { get; }
            public bool Exists { get; }
            public string Unit { get; set; }
            public StepProfileData ProfileData { get; set; }
            public BoundingBoxData? ProfileBoundingBox { get; set; }
            public List<LayerReport> Layers { get; }
            public EdaData Eda { get; set; }
        }
        /// <summary>
        /// Stores the parsed PCB outline (profile) along with the original unit of measurement.
        /// </summary>
        public sealed class StepProfileData
        {
            public StepProfileData(string unit, IReadOnlyList<StepProfileSurface> surfaces)
            {
                Unit = unit;
                Surfaces = surfaces;
            }

            public string Unit { get; }
            public IReadOnlyList<StepProfileSurface> Surfaces { get; }
        }
        /// <summary>
        /// Represents an SP surface section and the paths declared inside it.
        /// </summary>
        public sealed class StepProfileSurface
        {
            public StepProfileSurface(int? surfaceId)
            {
                SurfaceId = surfaceId;
                Paths = new List<StepProfilePath>();
            }

            public int? SurfaceId { get; }
            public List<StepProfilePath> Paths { get; }
        }
        /// <summary>
        /// Represents an OB/OS/OC path inside a surface and the ordered records it contains.
        /// </summary>
        public sealed class StepProfilePath
        {
            public StepProfilePath(char kind)
            {
                Kind = kind;
                Records = new List<StepProfileRecord>();
            }

            public char Kind { get; }
            public List<StepProfileRecord> Records { get; }
        }
        /// <summary>
        /// Raw record line from the profile file (OB, OS, OC, or OE).
        /// </summary>
        public sealed class StepProfileRecord
        {
            public string Type { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Xc { get; set; }
            public double Yc { get; set; }
            public bool Cw { get; set; }
        }
        /// <summary>
        /// Immutable axis-aligned bounding box helper for the extracted outline data.
        /// </summary>
        public readonly struct BoundingBoxData
        {
            public BoundingBoxData(double minX, double minY, double maxX, double maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public double MinX { get; }
            public double MinY { get; }
            public double MaxX { get; }
            public double MaxY { get; }
        }
        /// <summary>
        /// Top-level artifact returned to callers so they can inspect steps, layers, and generated paths.
        /// </summary>
        public sealed class JobReport
        {
            public JobReport(string sourceArchive, string sourcePath, string extractDir, string matrixPath)
            {
                SourceArchive = sourceArchive;
                SourcePath = sourcePath;
                ExtractDir = extractDir;
                MatrixPath = matrixPath;
                ExtractedAt = DateTime.UtcNow;
                Steps = new List<StepReport>();
            }

            public string SourceArchive { get; }
            public string SourcePath { get; }
            public string ExtractDir { get; }
            public string MatrixPath { get; }
            public DateTime ExtractedAt { get; }
            public List<StepReport> Steps { get; }
        }

        #endregion

        #region Regex Patterns

        private static readonly Regex ComponentRegex = new Regex(
            @"^\s*CMP\s+(?<pkg_ref>\d+)\s+(?<x>-?\d+(?:\.\d+)?)\s+(?<y>-?\d+(?:\.\d+)?)\s+(?<rot>-?\d+(?:\.\d+)?)\s+(?<mirror>[NM])\s+(?<comp_name>\S+)\s+(?<part_name>\S+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private const string NumericPattern = @"[+-]?(?:\d+(?:\.\d*)?|\.\d+)";
        private static readonly Regex PkgRegex = new Regex(
            $@"(?m)^[ \t]*PKG[ \t]+(?<name>\S+)[ \t]+(?<pitch>{NumericPattern})[ \t]+(?<xmin>{NumericPattern})[ \t]+(?<ymin>{NumericPattern})[ \t]+(?<xmax>{NumericPattern})[ \t]+(?<ymax>{NumericPattern})",
            RegexOptions.Compiled);
        private static readonly Regex OutlineLineRegex = new Regex(
            @"(?m)^[ \t]*(RC|CR|SQ)[^\n]*$",
            RegexOptions.Compiled);
        private static readonly Regex RcLineRegex = new Regex(
            $@"^\s*RC\s+(?<llx>{NumericPattern})\s+(?<lly>{NumericPattern})\s+(?<w>{NumericPattern})\s+(?<h>{NumericPattern})\s*$",
            RegexOptions.Compiled);
        private static readonly Regex CrLineRegex = new Regex(
            $@"^\s*CR\s+(?<xc>{NumericPattern})\s+(?<yc>{NumericPattern})\s+(?<r>{NumericPattern})\s*$",
            RegexOptions.Compiled);
        private static readonly Regex SqLineRegex = new Regex(
            $@"^\s*SQ\s+(?<xc>{NumericPattern})\s+(?<yc>{NumericPattern})\s+(?<half>{NumericPattern})\s*$",
            RegexOptions.Compiled);
        private static readonly Regex NextStopRegex = new Regex(
            @"(?m)^[ \t]*(?:PKG|PIN)\b",
            RegexOptions.Compiled);
        private static readonly Regex UnitRegex = new Regex(
            @"(?mi)^[ \t]*U(?:NITS)?[ \t]*=[ \t]*(INCH|MM)\b",
            RegexOptions.Compiled);
        private static readonly Regex StepProfileSurfStartRegex = new Regex(
            @"^\s*S\s+P\s+0(?:\s*;;\s*ID\s*=\s*(?<sid>\d+))?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StepProfileObRegex = new Regex(
            $@"^\s*OB\s+(?<x>{NumericPattern})\s+(?<y>{NumericPattern})\s+(?<kind>[IH])\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StepProfileOsRegex = new Regex(
            $@"^\s*OS\s+(?<x>{NumericPattern})\s+(?<y>{NumericPattern})\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StepProfileOcRegex = new Regex(
            $@"^\s*OC\s+(?<x>{NumericPattern})\s+(?<y>{NumericPattern})\s+(?<xc>{NumericPattern})\s+(?<yc>{NumericPattern})\s+(?<cw>[YN])\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StepProfileOeRegex = new Regex(
            @"^\s*OE\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StepProfileSeRegex = new Regex(
            @"^\s*SE\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region Extraction Workflow

        #region Entry Point

        /// <summary>
        /// Entry point for consumers: extracts an ODB++ archive/directory and builds the job/component reports.
        /// </summary>
        /// <param name="inputPath">Path to the .tgz/.tar.gz archive or already-extracted job directory.</param>
        /// <returns>Success metadata plus references to the generated XML reports, or an error payload.</returns>
        public static ExtractionResult Extract(string inputPath, ExportPreferences exportPreferences = null)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return ExtractionResult.Failure("No path provided.");
            }

            if (exportPreferences == null)
            {
                exportPreferences = ExportPreferences.Default;
            }

            string extractDir = string.Empty;
            var tempRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            Directory.CreateDirectory(tempRoot);
            var extracted = false;
            var shouldCleanTempDir = false;

            try
            {
                var normalizedInputPath = Environment.ExpandEnvironmentVariables(inputPath.Trim().Trim('"'));

                var isArchiveFile = File.Exists(normalizedInputPath) && IsGZipTarFile(normalizedInputPath);
                var isDirectory = Directory.Exists(normalizedInputPath);

                if (!isArchiveFile && !isDirectory)
                {
                    var message = $"File or directory not found: {normalizedInputPath}";
                    LogInfo(message);
                    return ExtractionResult.Failure(message, normalizedInputPath, null);
                }

                if (isArchiveFile)
                {
                    var archiveBaseName = Path.GetFileNameWithoutExtension(normalizedInputPath);
                    if (normalizedInputPath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                    {
                        archiveBaseName = Path.GetFileNameWithoutExtension(archiveBaseName);
                    }

                    extractDir = Path.Combine(tempRoot, $"{archiveBaseName}_{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.CreateDirectory(extractDir);
                    shouldCleanTempDir = true;

                    try
                    {
                        ExtractTarGz(normalizedInputPath, extractDir);
                        extracted = true;
                        LogInfo($"Archive extracted to: {extractDir}");
                    }
                    catch (Exception ex)
                    {
                        LogInfo("Extraction failed:");
                        LogInfo(ex.Message);
                        return ExtractionResult.Failure($"Extraction failed: {ex.Message}", normalizedInputPath, extractDir);
                    }
                }
                else
                {
                    extractDir = Path.GetFullPath(normalizedInputPath);
                    extracted = true;
                    LogInfo($"Using existing directory: {extractDir}");
                }

                if (!extracted)
                {
                    return ExtractionResult.Failure("Extraction did not complete.", normalizedInputPath, null);
                }

                var (matrixFound, jobReport) = TryBuildJobReport(extractDir, normalizedInputPath);
                if (!matrixFound || jobReport == null)
                {
                    var message = "Matrix file not found within the provided folders.";
                    LogInfo(message);
                    return ExtractionResult.Failure(message, normalizedInputPath, extractDir);
                }

                if (exportPreferences.LayerSelectionProvider != null && exportPreferences.ComponentLayerFilter == null)
                {
                    var selectedLayers = exportPreferences.LayerSelectionProvider(jobReport);
                    exportPreferences.ComponentLayerFilter = selectedLayers ?? Array.Empty<string>();
                }

                var jobReportPath = exportPreferences.ExportJobReport ? SaveJobReport(jobReport) : string.Empty;
                var componentReportPaths = new List<string>();
                var topLeftComponentReportPaths = new List<string>();
                HashSet<string> layerFilter = null;
                if (exportPreferences.ComponentLayerFilter != null && exportPreferences.ComponentLayerFilter.Count > 0)
                {
                    layerFilter = new HashSet<string>(
                        exportPreferences.ComponentLayerFilter
                            .Where(name => name != null)
                            .Select(name => string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim()),
                        StringComparer.OrdinalIgnoreCase);
                }

                switch (exportPreferences.ComponentMode)
                {
                    case ComponentExportMode.BottomLeft:
                        componentReportPaths.AddRange(SaveComponentPlacementReport(jobReport, CoordinateOrigin.BottomLeft, exportPreferences.SeparateComponentFilesByLayer, layerFilter));
                        break;
                    case ComponentExportMode.TopLeft:
                        topLeftComponentReportPaths.AddRange(SaveComponentPlacementReport(jobReport, CoordinateOrigin.TopLeft, exportPreferences.SeparateComponentFilesByLayer, layerFilter));
                        break;
                    case ComponentExportMode.Both:
                        componentReportPaths.AddRange(SaveComponentPlacementReport(jobReport, CoordinateOrigin.BottomLeft, exportPreferences.SeparateComponentFilesByLayer, layerFilter));
                        topLeftComponentReportPaths.AddRange(SaveComponentPlacementReport(jobReport, CoordinateOrigin.TopLeft, exportPreferences.SeparateComponentFilesByLayer, layerFilter));
                        break;
                    case ComponentExportMode.None:
                        break;
                }

                return ExtractionResult.Success(
                    normalizedInputPath,
                    extractDir,
                    jobReport,
                    jobReportPath,
                    componentReportPaths,
                    topLeftComponentReportPaths);
            }
            catch (Exception ex)
            {
                LogInfo("Unexpected error while processing the archive:");
                LogInfo(ex.ToString());
                return ExtractionResult.Failure(ex.Message, inputPath, null);
            }
            finally
            {
                if (shouldCleanTempDir && !string.IsNullOrEmpty(extractDir) && Directory.Exists(extractDir))
                {
                    try
                    {
                        Directory.Delete(extractDir, true);
                        LogInfo($"Deleted temporary extraction folder: {extractDir}");
                    }
                    catch (Exception cleanupEx)
                    {
                        LogInfo($"Failed to delete temporary extraction folder {extractDir}: {cleanupEx.Message}");
                    }
                }
            }
        }

        #endregion

        #region Archive Handling

        private static bool IsGZipTarFile(string path)
        {
            return path.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
                   || path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase);
        }

        private static void ExtractTarGz(string archivePath, string extractDirectory)
        {
            var normalizedExtractDir = Path.GetFullPath(extractDirectory);
            using (var fileStream = File.OpenRead(archivePath))
            using (var gzipStream = new GZipInputStream(fileStream))
            using (var tarStream = new TarInputStream(gzipStream, Encoding.UTF8))
            {
                TarEntry entry;
                while ((entry = tarStream.GetNextEntry()) != null)
                {
                    var entryName = entry.Name.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                    if (string.IsNullOrWhiteSpace(entryName))
                    {
                        continue;
                    }

                    var targetPath = Path.GetFullPath(Path.Combine(normalizedExtractDir, entryName));
                    if (!targetPath.StartsWith(normalizedExtractDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(targetPath, normalizedExtractDir, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Archive contains entries outside the target extraction path.");
                    }

                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(targetPath);
                        continue;
                    }

                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    using (var outputStream = File.Create(targetPath))
                    {
                        tarStream.CopyEntryContents(outputStream);
                    }
                }
            }
        }

        #region Matrix & Layer Discovery

        /// <summary>
        /// Walks the extracted job folder and assembles the in-memory <see cref="JobReport"/> representation.
        /// </summary>
        private static (bool Success, JobReport Report) TryBuildJobReport(string extractDirectory, string sourceArchive)
        {
            var matrixDir = Directory
                .EnumerateDirectories(extractDirectory, "*", SearchOption.AllDirectories)
                .FirstOrDefault(d => string.Equals(Path.GetFileName(d), "matrix", StringComparison.OrdinalIgnoreCase));

            if (matrixDir == null)
            {
                return (false, null);
            }

            var matrixFile = Directory
                .EnumerateFiles(matrixDir, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "matrix", StringComparison.OrdinalIgnoreCase));

            if (matrixFile == null)
            {
                return (false, null);
            }

            LogInfo($"Matrix contents from {matrixFile}:");
            var matrixText = File.ReadAllText(matrixFile, new UTF8Encoding(false, false));
            var (steps, componentLayers) = ParseMatrixStepsAndComponentLayers(matrixText);

            LogInfo("Steps:");
            foreach (var step in steps)
            {
                LogInfo($" - {step}");
            }

            LogInfo("\nComponent layers (CONTEXT=BOARD & TYPE=COMPONENT):");
            foreach (var layer in componentLayers)
            {
                LogInfo($" - {layer}");
            }

            var jobRoot = Path.GetDirectoryName(matrixDir);
            var stepsRoot = jobRoot == null ? null : Path.Combine(jobRoot, "steps");
            var jobReport = new JobReport(Path.GetFileName(sourceArchive) ?? string.Empty, sourceArchive, extractDirectory, matrixFile);
            var stepReports = steps.Select(step =>
            {
                var stepDir = string.IsNullOrEmpty(stepsRoot) ? string.Empty : Path.Combine(stepsRoot, step);
                var stepExists = !string.IsNullOrEmpty(stepDir) && Directory.Exists(stepDir);
                return new StepReport(step, stepDir, stepExists);
            }).ToList();
            var overallProfileBoundingBox = (BoundingBoxData?)null;

            if (string.IsNullOrEmpty(stepsRoot) || !Directory.Exists(stepsRoot))
            {
                LogInfo("\nStep directory not found adjacent to the matrix folder.");
            }
            else if (steps.Count == 0)
            {
                LogInfo($"\nNo step names extracted to validate under {stepsRoot}.");
            }
            else
            {
                LogInfo($"\nChecking step directories under {stepsRoot}:");
                for (var idx = 0; idx < steps.Count; idx++)
                {
                    var step = steps[idx];
                    var stepReport = stepReports[idx];
                    var stepExists = stepReport.Exists;
                    var stepDir = stepReport.Path;
                    LogInfo($" - Step '{step}': {(stepExists ? "found" : "missing")}");

                    if (stepExists)
                    {
                        var profilePath = Path.Combine(stepDir, "profile");
                        var profileExists = File.Exists(profilePath);
                        LogInfo($"   - Step profile: {profilePath}: {(profileExists ? "found" : "missing")}");
                        if (profileExists)
                        {
                            var profileData = ParseStepProfile(profilePath);
                            if (profileData != null)
                            {
                                if (!string.IsNullOrEmpty(profileData.Unit))
                                {
                                    LogInfo($"     profile unit: {profileData.Unit}");
                                }

                                stepReport.Unit = profileData.Unit;
                                stepReport.ProfileData = profileData;
                                PrintStepProfileData(profileData);
                                if (TryGetStepProfileBoundingBox(profileData, out var bbox))
                                {
                                    stepReport.ProfileBoundingBox = bbox;
                                    LogInfo(
                                        $"     profile bounding box: x=[{FormatDouble(bbox.MinX)}..{FormatDouble(bbox.MaxX)}], y=[{FormatDouble(bbox.MinY)}..{FormatDouble(bbox.MaxY)}]");
                                    ReportBoundingBoxMinZero(bbox, "step");
                                    overallProfileBoundingBox = overallProfileBoundingBox.HasValue
                                        ? MergeBoundingBoxes(overallProfileBoundingBox.Value, bbox)
                                        : (BoundingBoxData?)bbox;
                                }
                            }
                        }
                    }
                    else
                    {
                        LogInfo("   - Step profile check skipped because step directory is missing.");
                    }

                    if (componentLayers.Count > 0)
                    {
                        foreach (var layer in componentLayers)
                        {
                            var layerDir = Path.Combine(stepDir, "layers", layer.ToLowerInvariant());
                            var layerExists = Directory.Exists(layerDir);
                            LogInfo($"   - Layer '{layer}' ({layerDir}): {(layerExists ? "found" : "missing")}");
                            var layerReport = new LayerReport(layer, layerDir, layerExists);
                            stepReport.Layers.Add(layerReport);

                            if (layerExists)
                            {
                                var componentsPath = EnsureComponentsFile(layerDir);
                                layerReport.ComponentsPath = componentsPath;
                                if (componentsPath != null)
                                {
                                    var (componentUnit, componentRecords) = ParseComponentsFile(componentsPath);
                                    layerReport.Components = new ComponentData(componentUnit, componentRecords);
                                    PrintComponentRecords(componentsPath, componentUnit, componentRecords);
                                }
                            }
                        }
                    }

                    var edaDir = Path.Combine(stepDir, "eda");
                    if (stepExists && Directory.Exists(edaDir))
                    {
                        LogInfo($"   - EDA directory: {edaDir}");
                        var dataPath = EnsureEdaDataFile(edaDir);
                        if (dataPath != null)
                        {
                            LogInfo($"     data file ready at {dataPath}");
                            var (dataUnit, pkgRecords) = ParseDataFile(dataPath);
                            stepReport.Eda = new EdaData(dataUnit, dataPath, pkgRecords);
                            PrintDataRecords(dataPath, dataUnit, pkgRecords);
                        }
                    }
                    else
                    {
                        LogInfo("   - EDA directory not found.");
                    }

                }
            }

            if (overallProfileBoundingBox.HasValue)
            {
                var bb = overallProfileBoundingBox.Value;
                LogInfo(
                    $"Overall profile bounding box: x=[{FormatDouble(bb.MinX)}..{FormatDouble(bb.MaxX)}], y=[{FormatDouble(bb.MinY)}..{FormatDouble(bb.MaxY)}]");
                ReportBoundingBoxMinZero(bb, "overall");
            }

            jobReport.Steps.AddRange(stepReports);
            return (true, jobReport);
        }
        private static string EnsureComponentsFile(string layerDir)
        {
            var componentsFile = Path.Combine(layerDir, "components");
            if (File.Exists(componentsFile))
            {
                LogInfo("     components file already present.");
                return componentsFile;
            }

            var compressedFile = Path.Combine(layerDir, "components.Z");
            if (!File.Exists(compressedFile))
            {
                LogInfo("     no components or components.Z file found in this layer.");
                return null;
            }

            LogInfo("     components file missing, decompressing components.Z...");
            try
            {
                using (var inputStream = File.OpenRead(compressedFile))
                using (var lzwStream = new LzwInputStream(inputStream))
                using (var outputStream = File.Create(componentsFile))
                {
                    StreamUtils.Copy(lzwStream, outputStream, new byte[32 * 1024]);
                }

                if (File.Exists(componentsFile))
                {
                    LogInfo("     decompression succeeded.");
                    return componentsFile;
                }
                else
                {
                    LogInfo("     decompression completed but components file still missing.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogInfo($"     failed to decompress components.Z: {ex.Message}");
                return null;
            }
        }

        private static (string Unit, IReadOnlyList<ComponentRecord> Records) ParseComponentsFile(string path)
        {
            var text = ReadTextMaybeGZip(path);
            var unit = DetectUnit(text);
            var records = new List<ComponentRecord>();
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = ComponentRegex.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    records.Add(new ComponentRecord(
                        match.Groups["pkg_ref"].Value,
                        match.Groups["x"].Value,
                        match.Groups["y"].Value,
                        match.Groups["rot"].Value,
                        match.Groups["mirror"].Value,
                        match.Groups["comp_name"].Value,
                        match.Groups["part_name"].Value,
                        unit));
                }
            }

            return (unit, records);
        }

        private static (string Unit, IReadOnlyList<PkgRecord> Records) ParseDataFile(string path)
        {
            var text = ReadTextMaybeGZip(path);
            var unit = DetectUnit(text);
            var records = new List<PkgRecord>();
            var matches = PkgRegex.Matches(text).Cast<Match>().ToList();
            for (var idx = 0; idx < matches.Count; idx++)
            {
                var match = matches[idx];
                var pitch = ParseDouble(match.Groups["pitch"].Value);
                var xmin = ParseDouble(match.Groups["xmin"].Value);
                var ymin = ParseDouble(match.Groups["ymin"].Value);
                var xmax = ParseDouble(match.Groups["xmax"].Value);
                var ymax = ParseDouble(match.Groups["ymax"].Value);

                var sectionStart = match.Index + match.Length;
                var stopMatch = NextStopRegex.Match(text, sectionStart);
                var sectionEnd = stopMatch.Success ? stopMatch.Index : text.Length;
                var outlines = ExtractOutlineRecords(text, sectionStart, sectionEnd);

                records.Add(new PkgRecord(idx, match.Groups["name"].Value, pitch, xmin, ymin, xmax, ymax, outlines, unit));
            }

            return (unit, records);
        }

        private static IReadOnlyList<OutlineRecord> ExtractOutlineRecords(string text, int start, int end)
        {
            var records = new List<OutlineRecord>();
            foreach (Match match in OutlineLineRegex.Matches(text, start))
            {
                if (match.Index >= end)
                {
                    break;
                }

                var outline = ParseOutlineLine(match.Value.Trim());
                if (outline != null)
                {
                    records.Add(outline);
                }
            }

            return records;
        }

        private static OutlineRecord ParseOutlineLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var typeToken = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (typeToken == "RC")
            {
                var match = RcLineRegex.Match(line);
                if (!match.Success)
                {
                    return null;
                }

                var llx = ParseDouble(match.Groups["llx"].Value);
                var lly = ParseDouble(match.Groups["lly"].Value);
                var w = ParseDouble(match.Groups["w"].Value);
                var h = ParseDouble(match.Groups["h"].Value);
                var poly = new List<IReadOnlyList<double>>
                {
                    new List<double> { llx, lly },
                    new List<double> { llx + w, lly },
                    new List<double> { llx + w, lly + h },
                    new List<double> { llx, lly + h },
                };
                return new OutlineRecord(
                    "RC",
                    new Dictionary<string, double>
                    {
                        ["llx"] = llx,
                        ["lly"] = lly,
                        ["w"] = w,
                        ["h"] = h,
                    },
                    poly,
                    line);
            }

            if (typeToken == "CR")
            {
                var match = CrLineRegex.Match(line);
                if (!match.Success)
                {
                }
                else
                {
                    var xc = ParseDouble(match.Groups["xc"].Value);
                    var yc = ParseDouble(match.Groups["yc"].Value);
                    var r = ParseDouble(match.Groups["r"].Value);
                    return new OutlineRecord(
                        "CR",
                        new Dictionary<string, double>
                        {
                            ["xc"] = xc,
                            ["yc"] = yc,
                            ["r"] = r,
                        },
                        new List<IReadOnlyList<double>>(),
                        line);
                }
            }

            if (typeToken == "SQ")
            {
                var match = SqLineRegex.Match(line);
                if (!match.Success)
                {
                    return null;
                }

                var xc = ParseDouble(match.Groups["xc"].Value);
                var yc = ParseDouble(match.Groups["yc"].Value);
                var half = ParseDouble(match.Groups["half"].Value);
                var poly = new List<IReadOnlyList<double>>
                {
                    new List<double> { xc - half, yc - half },
                    new List<double> { xc + half, yc - half },
                    new List<double> { xc + half, yc + half },
                    new List<double> { xc - half, yc + half },
                };
                return new OutlineRecord(
                    "SQ",
                    new Dictionary<string, double>
                    {
                        ["xc"] = xc,
                        ["yc"] = yc,
                        ["half"] = half,
                    },
                    poly,
                    line);
            }

            return null;
        }

        #endregion

        #region Step Profile Parsing

        private static string ReadTextMaybeGZip(string path)
        {
            var data = File.ReadAllBytes(path);
            try
            {
                using (var memory = new MemoryStream(data))
                using (var gzipStream = new GZipInputStream(memory))
                using (var reader = new StreamReader(gzipStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return Encoding.UTF8.GetString(data);
            }
        }

        private static StepProfileData ParseStepProfile(string profilePath)
        {
            try
            {
                var profileText = ReadTextMaybeGZip(profilePath);
                var surfaces = ParseStepProfileSurfaces(profileText);
                var unit = DetectUnit(profileText);
                return new StepProfileData(unit, surfaces);
            }
            catch (Exception ex)
            {
                LogInfo($"     failed to read profile file: {ex.Message}");
                return null;
            }
        }

        private static IReadOnlyList<StepProfileSurface> ParseStepProfileSurfaces(string text)
        {
            var surfaces = new List<StepProfileSurface>();
            if (string.IsNullOrEmpty(text))
            {
                return surfaces;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var lineIndex = 0;
            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex];
                var trimmedLine = line?.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    lineIndex++;
                    continue;
                }

                var surfMatch = StepProfileSurfStartRegex.Match(line);
                if (!surfMatch.Success)
                {
                    lineIndex++;
                    continue;
                }

                int? surfaceId = null;
                var surfaceIdValue = surfMatch.Groups["sid"].Value;
                if (int.TryParse(surfaceIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    surfaceId = parsed;
                }

                var surface = new StepProfileSurface(surfaceId);
                surfaces.Add(surface);
                var currentPath = (StepProfilePath)null;
                var endedBySe = false;
                lineIndex++;

                while (lineIndex < lines.Length)
                {
                    line = lines[lineIndex];
                    trimmedLine = line?.Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        lineIndex++;
                        continue;
                    }

                    if (trimmedLine.StartsWith("#"))
                    {
                        lineIndex++;
                        continue;
                    }

                    if (StepProfileSeRegex.IsMatch(line))
                    {
                        if (currentPath != null)
                        {
                            surface.Paths.Add(currentPath);
                            currentPath = null;
                        }

                        lineIndex++;
                        endedBySe = true;
                        break;
                    }

                    var obMatch = StepProfileObRegex.Match(line);
                    if (obMatch.Success)
                    {
                        if (currentPath != null)
                        {
                            surface.Paths.Add(currentPath);
                        }

                        var kindGroup = obMatch.Groups["kind"].Value;
                        var kindChar = !string.IsNullOrEmpty(kindGroup)
                            ? char.ToUpperInvariant(kindGroup[0])
                            : 'I';
                        currentPath = new StepProfilePath(kindChar);
                        currentPath.Records.Add(new StepProfileRecord
                        {
                            Type = "OB",
                            X = ParseDouble(obMatch.Groups["x"].Value),
                            Y = ParseDouble(obMatch.Groups["y"].Value)
                        });

                        lineIndex++;
                        continue;
                    }

                    var osMatch = StepProfileOsRegex.Match(line);
                    if (osMatch.Success && currentPath != null)
                    {
                        currentPath.Records.Add(new StepProfileRecord
                        {
                            Type = "OS",
                            X = ParseDouble(osMatch.Groups["x"].Value),
                            Y = ParseDouble(osMatch.Groups["y"].Value)
                        });

                        lineIndex++;
                        continue;
                    }

                    var ocMatch = StepProfileOcRegex.Match(line);
                    if (ocMatch.Success && currentPath != null)
                    {
                        currentPath.Records.Add(new StepProfileRecord
                        {
                            Type = "OC",
                            X = ParseDouble(ocMatch.Groups["x"].Value),
                            Y = ParseDouble(ocMatch.Groups["y"].Value),
                            Xc = ParseDouble(ocMatch.Groups["xc"].Value),
                            Yc = ParseDouble(ocMatch.Groups["yc"].Value),
                            Cw = string.Equals(ocMatch.Groups["cw"].Value, "Y", StringComparison.OrdinalIgnoreCase)
                        });

                        lineIndex++;
                        continue;
                    }

                    var oeMatch = StepProfileOeRegex.Match(line);
                    if (oeMatch.Success && currentPath != null)
                    {
                        currentPath.Records.Add(new StepProfileRecord
                        {
                            Type = "OE"
                        });

                        surface.Paths.Add(currentPath);
                        currentPath = null;
                        lineIndex++;
                        continue;
                    }

                    lineIndex++;
                }

                if (!endedBySe && currentPath != null)
                {
                    surface.Paths.Add(currentPath);
                    currentPath = null;
                }
            }

            return surfaces;
        }

        private static void PrintStepProfileData(StepProfileData profileData)
        {
            if (profileData == null)
            {
                return;
            }

            var surfaces = profileData.Surfaces ?? Array.Empty<StepProfileSurface>();
            LogInfo($"     Profile surfaces parsed: {surfaces.Count}");
            for (var surfaceIndex = 0; surfaceIndex < surfaces.Count; surfaceIndex++)
            {
                var surface = surfaces[surfaceIndex];
                var idText = surface.SurfaceId.HasValue ? $" id={surface.SurfaceId.Value}" : string.Empty;
                LogInfo($"       Surface {surfaceIndex + 1}{idText}: {surface.Paths.Count} path(s)");
                for (var pathIndex = 0; pathIndex < surface.Paths.Count; pathIndex++)
                {
                    var path = surface.Paths[pathIndex];
                    LogInfo($"         Path {pathIndex + 1} kind={path.Kind} records={path.Records.Count}");
                    foreach (var record in path.Records)
                    {
                        LogInfo($"           {record.Type}: {DescribeProfileRecord(record)}");
                    }
                }
            }
        }

        private static bool TryGetStepProfileBoundingBox(StepProfileData profileData, out BoundingBoxData boundingBox)
        {
            boundingBox = default;
            if (profileData?.Surfaces == null)
            {
                return false;
            }

            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var foundPoint = false;

            foreach (var surface in profileData.Surfaces)
            {
                foreach (var path in surface.Paths)
                {
                    foreach (var record in path.Records)
                    {
                        if (string.IsNullOrEmpty(record?.Type))
                        {
                            continue;
                        }

                        double x = 0, y = 0;
                        var hasPoint = true;
                        switch (record.Type)
                        {
                            case "OB":
                            case "OS":
                                x = record.X;
                                y = record.Y;
                                break;
                            case "OC":
                                x = record.X;
                                y = record.Y;
                                break;
                            default:
                                hasPoint = false;
                                break;
                        }

                        if (!hasPoint)
                        {
                            continue;
                        }

                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                        foundPoint = true;
                    }
                }
            }

            if (!foundPoint)
            {
                return false;
            }

            boundingBox = new BoundingBoxData(minX, minY, maxX, maxY);
            return true;
        }

        private static BoundingBoxData MergeBoundingBoxes(BoundingBoxData left, BoundingBoxData right)
        {
            return new BoundingBoxData(
                Math.Min(left.MinX, right.MinX),
                Math.Min(left.MinY, right.MinY),
                Math.Max(left.MaxX, right.MaxX),
                Math.Max(left.MaxY, right.MaxY));
        }

        private static void ReportBoundingBoxMinZero(BoundingBoxData bbox, string context)
        {
            if (!IsZero(bbox.MinX) || !IsZero(bbox.MinY))
            {
                LogInfo(
                    $"     Warning: {context} bounding box min values expected at 0,0 but found x={FormatDouble(bbox.MinX)}, y={FormatDouble(bbox.MinY)}");
            }
        }

        private static bool IsZero(double value)
        {
            const double Tolerance = 1e-6;
            return Math.Abs(value) <= Tolerance;
        }

        private static string DescribeProfileRecord(StepProfileRecord record)
        {
            switch (record.Type)
            {
                case "OB":
                case "OS":
                    return $"x={FormatDouble(record.X)} y={FormatDouble(record.Y)}";
                case "OC":
                    return $"x={FormatDouble(record.X)} y={FormatDouble(record.Y)} xc={FormatDouble(record.Xc)} yc={FormatDouble(record.Yc)} cw={(record.Cw ? "Y" : "N")}";
                case "OE":
                    return "end";
                default:
                    return string.Empty;
            }
        }

        #endregion

        #region Reporting & Serialization

        private static string DetectUnit(string text)
        {
            var match = UnitRegex.Match(text);
            if (!match.Success)
            {
                return "INCH";
            }

            return match.Groups[1].Value.ToUpperInvariant();
        }

        private static void PrintDataRecords(string path, string unit, IReadOnlyList<PkgRecord> records)
        {
            if (records.Count == 0)
            {
                LogInfo($"     no PKG entries detected in {Path.GetFileName(path)}.");
                return;
            }

            LogInfo($"     Parsed {records.Count} PKG records from {Path.GetFileName(path)} (unit={unit}):");
            foreach (var record in records)
            {
                LogInfo(
                    $"       index={record.Index}, name={record.Name}, pitch={record.Pitch}, xmin={record.XMin}, ymin={record.YMin}, xmax={record.XMax}, ymax={record.YMax}");
                for (var idx = 0; idx < record.Outlines.Count; idx++)
                {
                    var outline = record.Outlines[idx];
                    LogInfo($"         outline[{idx}]: type={outline.Type}, params={FormatParameters(outline.Parameters)}, polygon={FormatPolygon(outline.Polygon)}");
                    if (!string.IsNullOrWhiteSpace(outline.Raw))
                    {
                        LogInfo($"           raw: {outline.Raw}");
                    }
                }
            }
        }

        private static string FormatParameters(IReadOnlyDictionary<string, double> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "{}";
            }

            return "{" + string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}")) + "}";
        }

        private static string FormatPolygon(IReadOnlyList<IReadOnlyList<double>> polygon)
        {
            if (polygon == null || polygon.Count == 0)
            {
                return "[]";
            }

            return "[" + string.Join("; ", polygon.Select(point => $"[{string.Join(",", point)}]")) + "]";
        }

        private static string SaveJobReport(JobReport report)
        {
            var reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
            Directory.CreateDirectory(reportsDir);
            var archiveName = string.IsNullOrEmpty(report.SourceArchive) ? "job" : Path.GetFileNameWithoutExtension(report.SourceArchive);
            var baseName = SanitizeFileName(archiveName);
            var timestamp = report.ExtractedAt.ToString("yyyyMMddHHmmss");
            var filePath = Path.Combine(reportsDir, $"{baseName}_{timestamp}.xml");

            var doc = new XDocument(
                new XElement("job",
                    new XAttribute("sourceArchive", report.SourceArchive ?? string.Empty),
                    new XAttribute("sourcePath", report.SourcePath ?? string.Empty),
                    new XAttribute("extractDir", report.ExtractDir ?? string.Empty),
                    new XAttribute("extractedAt", report.ExtractedAt.ToString("o")),
                    new XElement("matrix", new XAttribute("path", report.MatrixPath ?? string.Empty)),
                    new XElement("steps", report.Steps.Select(BuildStepElement))
                )
            );

            doc.Save(filePath);
            LogInfo($"Job report saved to {filePath}");
            return filePath;
        }

        private static IReadOnlyList<string> SaveComponentPlacementReport(JobReport report, CoordinateOrigin origin, bool separateByLayer, HashSet<string> layerFilter)
        {
            if (!separateByLayer)
            {
                var layerElements = BuildComponentPlacementLayerElements(report, origin, layerFilter);
                if (layerElements.Count == 0)
                {
                    LogInfo($"Component placement report ({FormatOrigin(origin)}) skipped (no components with package data).");
                    return Array.Empty<string>();
                }

                var filePath = SaveComponentPlacementDocument(report, origin, "_components", layerElements);
                return new[] { filePath };
            }

            var entries = BuildComponentPlacementEntries(report, origin, layerFilter);
            if (entries.Count == 0)
            {
                LogInfo($"Component placement report ({FormatOrigin(origin)}) skipped (no components with package data).");
                return Array.Empty<string>();
            }

            var filePaths = new List<string>();
            var usedSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var layerGroup in entries.GroupBy(entry => entry.Layer ?? string.Empty))
            {
                var layerName = string.IsNullOrWhiteSpace(layerGroup.Key) ? "layer" : layerGroup.Key;
                var sanitizedLayer = SanitizeFileName(layerName);
                if (string.IsNullOrEmpty(sanitizedLayer))
                {
                    sanitizedLayer = "layer";
                }

                var suffix = $"_components_{sanitizedLayer}";
                var uniqueSuffix = suffix;
                var suffixIndex = 1;
                while (usedSuffixes.Contains(uniqueSuffix))
                {
                    uniqueSuffix = $"{suffix}_{suffixIndex++}";
                }

                usedSuffixes.Add(uniqueSuffix);

                var layerElements = BuildStepElementsFromEntries(layerGroup);
                if (layerElements.Count == 0)
                {
                    continue;
                }

                var filePath = SaveComponentPlacementDocument(report, origin, uniqueSuffix, layerElements, layerName);
                filePaths.Add(filePath);
            }

            return filePaths;
        }

        private static string SaveComponentPlacementDocument(
            JobReport report,
            CoordinateOrigin origin,
            string suffix,
            IReadOnlyList<XElement> stepElements,
            string layerName = null)
        {
            var reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
            Directory.CreateDirectory(reportsDir);
            var archiveName = string.IsNullOrEmpty(report.SourceArchive) ? "job" : Path.GetFileNameWithoutExtension(report.SourceArchive);
            var baseName = SanitizeFileName(archiveName);
            var timestamp = report.ExtractedAt.ToString("yyyyMMddHHmmss");
            var filePath = Path.Combine(reportsDir, $"{baseName}_{timestamp}{suffix}.xml");

            var componentCount = stepElements.Sum(step => step.Elements("layer").Sum(layer => layer.Elements("component").Count()));
            var boardsElement = new XElement("boards",
                new XAttribute("generatedAt", report.ExtractedAt.ToString("o")),
                new XAttribute("origin", FormatOrigin(origin)),
                new XAttribute("count", componentCount),
                stepElements);

            if (!string.IsNullOrWhiteSpace(layerName))
            {
                boardsElement.Add(new XAttribute("layer", layerName));
            }

            var doc = new XDocument(boardsElement);
            doc.Save(filePath);
            LogInfo($"Component placement report ({FormatOrigin(origin)}{(string.IsNullOrWhiteSpace(layerName) ? string.Empty : $" layer={layerName}")}) saved to {filePath}");
            return filePath;
        }

        private static string FormatOrigin(CoordinateOrigin origin) =>
            origin == CoordinateOrigin.TopLeft ? "top-left" : "bottom-left";

        private static List<XElement> BuildComponentPlacementLayerElements(JobReport report, CoordinateOrigin origin, HashSet<string> layerFilter)
        {
            var entries = BuildComponentPlacementEntries(report, origin, layerFilter);
            return BuildStepElementsFromEntries(entries);
        }

        private static List<LayerComponentEntry> BuildComponentPlacementEntries(JobReport report, CoordinateOrigin origin, HashSet<string> layerFilter)
        {
            var entries = new List<LayerComponentEntry>();
            foreach (var step in report.Steps)
            {
                if (step?.Layers == null || step.Layers.Count == 0)
                {
                    continue;
                }

                var eda = step.Eda;
                if (eda?.Records == null || eda.Records.Count == 0)
                {
                    continue;
                }

                var packageByIndex = eda.Records.ToDictionary(pkg => pkg.Index);
                var stepWidth = (double?)null;
                var stepLength = (double?)null;
                if (step.ProfileBoundingBox.HasValue)
                {
                    stepWidth = step.ProfileBoundingBox.Value.MaxX - step.ProfileBoundingBox.Value.MinX;
                    stepLength = step.ProfileBoundingBox.Value.MaxY - step.ProfileBoundingBox.Value.MinY;
                }

                foreach (var layer in step.Layers)
                {
                    var componentData = layer.Components;
                    if (componentData?.Records == null || componentData.Records.Count == 0)
                    {
                        continue;
                    }
                    var layerName = layer.Name ?? string.Empty;
                    if (!IsLayerAllowed(layerName, layerFilter))
                    {
                        continue;
                    }

                    foreach (var component in componentData.Records)
                    {
                        var result = BuildComponentPlacementElement(step, layer, component, eda, packageByIndex, origin);
                        if (result != null)
                        {
                            entries.Add(new LayerComponentEntry(
                                step.Name ?? string.Empty,
                                layer.Name ?? string.Empty,
                                result.Unit ?? string.Empty,
                                result.Component,
                                step.Unit,
                                stepWidth,
                                stepLength));
                        }
                    }
                }
            }

            return entries;
        }

        private static List<XElement> BuildStepElementsFromEntries(IEnumerable<LayerComponentEntry> entries)
        {
            var stepElements = new List<XElement>();
            var groupedSteps = entries.GroupBy(entry => entry.Step ?? string.Empty);

            foreach (var stepGroup in groupedSteps)
            {
                var stepUnit = stepGroup.Select(entry => entry.StepUnit).FirstOrDefault(unit => !string.IsNullOrEmpty(unit));
                var stepWidth = stepGroup.Select(entry => entry.StepWidth).FirstOrDefault(value => value.HasValue);
                var stepLength = stepGroup.Select(entry => entry.StepLength).FirstOrDefault(value => value.HasValue);

                var stepElement = new XElement("step", new XAttribute("name", stepGroup.Key));
                if (!string.IsNullOrEmpty(stepUnit))
                {
                    stepElement.Add(new XAttribute("unit", stepUnit));
                }

                if (stepWidth.HasValue)
                {
                    stepElement.Add(new XAttribute("width", FormatDouble(stepWidth.Value)));
                }

                if (stepLength.HasValue)
                {
                    stepElement.Add(new XAttribute("length", FormatDouble(stepLength.Value)));
                }

                var layerGroups = stepGroup.GroupBy(entry => (Layer: entry.Layer ?? string.Empty, Unit: entry.Unit ?? string.Empty));
                foreach (var layerGroup in layerGroups)
                {
                    var layerElement = new XElement("layer",
                        new XAttribute("name", layerGroup.Key.Layer),
                        new XAttribute("unit", layerGroup.Key.Unit));

                    layerElement.Add(layerGroup.Select(entry => entry.Component));
                    stepElement.Add(layerElement);
                }

                stepElements.Add(stepElement);
            }

            return stepElements;
        }

        private static bool IsLayerAllowed(string layerName, HashSet<string> layerFilter)
        {
            if (layerFilter == null || layerFilter.Count == 0)
            {
                return true;
            }

            var normalizedLayer = string.IsNullOrWhiteSpace(layerName) ? string.Empty : layerName.Trim();
            return layerFilter.Contains(normalizedLayer);
        }

        /// <summary>
        /// Creates the XML element for a component, applying package geometry, mirroring rules, and the
        /// profile-origin shift so that all reported coordinates are relative to (0,0) in the selected origin.
        /// </summary>
        private static ComponentPlacementResult BuildComponentPlacementElement(
            StepReport step,
            LayerReport layer,
            ComponentRecord component,
            EdaData eda,
            IReadOnlyDictionary<int, PkgRecord> packageByIndex,
            CoordinateOrigin origin)
        {
            if (!TryResolvePackage(component.PkgRef, packageByIndex, out var pkg))
            {
                LogInfo($"     pkg_ref {component.PkgRef} not resolved for component {component.ComponentName}.");
                return null;
            }

            var componentUnit = NormalizeUnit(component.Unit)
                                ?? NormalizeUnit(layer.Components?.Unit)
                                ?? NormalizeUnit(eda.Unit)
                                ?? "INCH";

            var packageUnit = NormalizeUnit(pkg.Unit)
                              ?? NormalizeUnit(eda.Unit)
                              ?? componentUnit;

            var bounds = CalculatePackageBounds(pkg, packageUnit, componentUnit);
            var outline = pkg.Outlines?.FirstOrDefault();
            var outlineDimensions = GetOutlineDimensions(outline, packageUnit, componentUnit);
            var shape = ResolveShape(outline?.Type);
            var isBottomLayer = IsBottomLayer(layer);

            var rotationDegrees = ParseDouble(component.Rot);
            var quarterTurns = NormalizeQuarterTurns(rotationDegrees);
            var offset = RotateClockwise((bounds.CenterX, bounds.CenterY), quarterTurns);
            if (IsMirrored(component.Mirror))
            {
                offset = (-offset.x, offset.y);
            }

            var componentX = ParseDouble(component.X);
            var componentY = ParseDouble(component.Y);
            var centerX = componentX + offset.x;
            var centerY = componentY + offset.y;
            if (isBottomLayer)
            {
                if (TryGetStepHorizontalBounds(step, componentUnit, out var stepMinX, out var stepMaxX))
                {
                    // Mirror the component across the PCB width using the rotated offset.
                    var mirroredComponentX = (stepMinX + stepMaxX) - componentX;
                    centerX = mirroredComponentX + offset.x;
                }
                else
                {
                    LogInfo(
                        $"     Warning: Unable to mirror component '{component.ComponentName}' on layer '{layer.Name}' because PCB width is unavailable.");
                }
            }

            if (TryGetStepOriginOffset(step, componentUnit, out var originOffsetX, out var originOffsetY)
                && (!IsZero(originOffsetX) || !IsZero(originOffsetY)))
            {
                centerX -= originOffsetX;
                centerY -= originOffsetY;
            }

            if (origin == CoordinateOrigin.TopLeft
                && TryGetStepHeight(step, componentUnit, out var stepHeight))
            {
                centerY = stepHeight - centerY;
            }

            var baseWidth = outlineDimensions?.width ?? bounds.Width;
            var baseLength = outlineDimensions?.length ?? bounds.Length;
            var (width, length) = ApplyRotationToDimensions(baseWidth, baseLength, quarterTurns);

            var componentElement = new XElement("component",
                new XAttribute("name", component.ComponentName ?? string.Empty),
                new XAttribute("rotation", FormatDouble(ConvertClockwiseRotationToCounterClockwise(rotationDegrees))),
                new XAttribute("shape", shape),
                new XAttribute("packageName", pkg.Name ?? string.Empty),
                new XAttribute("centerX", FormatDouble(centerX)),
                new XAttribute("centerY", FormatDouble(centerY)),
                new XAttribute("width", FormatDouble(Math.Abs(width))),
                new XAttribute("length", FormatDouble(Math.Abs(length))));

            return new ComponentPlacementResult(componentUnit, componentElement);
        }

        private enum CoordinateOrigin
        {
            BottomLeft,
            TopLeft
        }

        private sealed class LayerComponentEntry
        {
            public LayerComponentEntry(string step, string layer, string unit, XElement component, string stepUnit, double? stepWidth, double? stepLength)
            {
                Step = step;
                Layer = layer;
                Unit = unit;
                Component = component;
                StepUnit = stepUnit;
                StepWidth = stepWidth;
                StepLength = stepLength;
            }

            public string Step { get; }
            public string Layer { get; }
            public string Unit { get; }
            public XElement Component { get; }
            public string StepUnit { get; }
            public double? StepWidth { get; }
            public double? StepLength { get; }
        }

        private sealed class ComponentPlacementResult
        {
            public ComponentPlacementResult(string unit, XElement component)
            {
                Unit = unit;
                Component = component;
            }

            public string Unit { get; }
            public XElement Component { get; }
        }

        private static bool TryResolvePackage(string pkgRef, IReadOnlyDictionary<int, PkgRecord> packages, out PkgRecord pkg)
        {
            pkg = null;
            if (packages == null || packages.Count == 0 || string.IsNullOrWhiteSpace(pkgRef))
            {
                return false;
            }

            if (!int.TryParse(pkgRef, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            if (packages.TryGetValue(parsed, out pkg))
            {
                return true;
            }

            if (packages.TryGetValue(parsed - 1, out pkg))
            {
                return true;
            }

            return false;
        }

        private static XElement BuildStepElement(StepReport step)
        {
            var stepElement = new XElement("step",
                new XAttribute("name", step.Name ?? string.Empty),
                new XAttribute("exists", step.Exists),
                new XAttribute("path", step.Path ?? string.Empty));

            if (step.ProfileBoundingBox.HasValue)
            {
                stepElement.Add(BuildStepSizeElement(step.ProfileBoundingBox.Value));
            }

            stepElement.Add(new XElement("layers", step.Layers.Select(BuildLayerElement)));

            if (!string.IsNullOrEmpty(step.Unit))
            {
                stepElement.Add(new XAttribute("unit", step.Unit));
            }

            if (step.ProfileData != null)
            {
                stepElement.Add(BuildStepProfileElement(step.ProfileData));
            }

            if (step.Eda != null)
            {
                stepElement.Add(BuildEdaElement(step.Eda));
            }

            return stepElement;
        }

        private static XElement BuildStepProfileElement(StepProfileData profile)
        {
            var profileElement = new XElement("profile");

            if (!string.IsNullOrEmpty(profile.Unit))
            {
                profileElement.Add(new XAttribute("unit", profile.Unit));
            }

            var surfaces = profile.Surfaces ?? Array.Empty<StepProfileSurface>();
            foreach (var surface in surfaces)
            {
                var surfaceElement = new XElement("surface");
                if (surface.SurfaceId.HasValue)
                {
                    surfaceElement.Add(new XAttribute("id", surface.SurfaceId.Value));
                }

                surfaceElement.Add(new XAttribute("paths", surface.Paths.Count));

                foreach (var path in surface.Paths)
                {
                    var pathElement = new XElement("path",
                        new XAttribute("kind", path.Kind),
                        new XAttribute("records", path.Records.Count));

                    foreach (var record in path.Records)
                    {
                        var recordElement = new XElement("record",
                            new XAttribute("type", record.Type ?? string.Empty));

                        switch (record.Type)
                        {
                            case "OB":
                            case "OS":
                                recordElement.Add(
                                    new XAttribute("x", FormatDouble(record.X)),
                                    new XAttribute("y", FormatDouble(record.Y)));
                                break;
                            case "OC":
                                recordElement.Add(
                                    new XAttribute("x", FormatDouble(record.X)),
                                    new XAttribute("y", FormatDouble(record.Y)),
                                    new XAttribute("xc", FormatDouble(record.Xc)),
                                    new XAttribute("yc", FormatDouble(record.Yc)),
                                    new XAttribute("cw", record.Cw ? "Y" : "N"));
                                break;
                            case "OE":
                                break;
                        }

                        pathElement.Add(recordElement);
                    }

                    surfaceElement.Add(pathElement);
                }

                profileElement.Add(surfaceElement);
            }

            return profileElement;
        }

        private static XElement BuildStepSizeElement(BoundingBoxData bbox)
        {
            var width = FormatDouble(bbox.MaxX - bbox.MinX);
            var height = FormatDouble(bbox.MaxY - bbox.MinY);
            return new XElement("size",
                new XAttribute("width", width),
                new XAttribute("height", height),
                new XAttribute("minX", FormatDouble(bbox.MinX)),
                new XAttribute("minY", FormatDouble(bbox.MinY)),
                new XAttribute("maxX", FormatDouble(bbox.MaxX)),
                new XAttribute("maxY", FormatDouble(bbox.MaxY)));
        }

        private static XElement BuildLayerElement(LayerReport layer)
        {
            var layerElement = new XElement("layer",
                new XAttribute("name", layer.Name ?? string.Empty),
                new XAttribute("exists", layer.Exists),
                new XAttribute("path", layer.Path ?? string.Empty));

            layerElement.Add(BuildComponentsElement(layer));
            return layerElement;
        }

        private static XElement BuildComponentsElement(LayerReport layer)
        {
            var element = new XElement("components");

            if (!string.IsNullOrEmpty(layer.ComponentsPath))
            {
                element.Add(new XAttribute("path", layer.ComponentsPath));
            }

            if (layer.Components != null)
            {
                element.Add(new XAttribute("unit", layer.Components.Unit ?? string.Empty));
                foreach (var component in layer.Components.Records)
                {
                    element.Add(new XElement("component",
                        new XAttribute("pkgRef", component.PkgRef ?? string.Empty),
                        new XAttribute("x", component.X ?? string.Empty),
                        new XAttribute("y", component.Y ?? string.Empty),
                        new XAttribute("rot", component.Rot ?? string.Empty),
                        new XAttribute("mirror", component.Mirror ?? string.Empty),
                        new XAttribute("name", component.ComponentName ?? string.Empty),
                        new XAttribute("part", component.PartName ?? string.Empty)));
                }
            }

            return element;
        }

        private static XElement BuildEdaElement(EdaData eda)
        {
            var element = new XElement("eda");
            if (!string.IsNullOrEmpty(eda.DataPath))
            {
                element.Add(new XAttribute("path", eda.DataPath));
            }

            if (!string.IsNullOrEmpty(eda.Unit))
            {
                element.Add(new XAttribute("unit", eda.Unit));
            }

            foreach (var pkg in eda.Records)
            {
                var pkgElement = new XElement("pkg",
                    new XAttribute("index", pkg.Index),
                    new XAttribute("name", pkg.Name ?? string.Empty),
                    new XAttribute("pitch", pkg.Pitch),
                    new XAttribute("xmin", pkg.XMin),
                    new XAttribute("ymin", pkg.YMin),
                    new XAttribute("xmax", pkg.XMax),
                    new XAttribute("ymax", pkg.YMax));

                if (pkg.Outlines != null && pkg.Outlines.Count > 0)
                {
                    var outlinesElement = new XElement("outlines");
                    foreach (var outlineEntry in pkg.Outlines.Select((value, idx) => new { value, idx }))
                    {
                        var outline = outlineEntry.value;
                        var outlineElement = new XElement("outline",
                            new XAttribute("idx", outlineEntry.idx),
                            new XAttribute("type", outline.Type ?? string.Empty));

                        if (outline.Parameters != null && outline.Parameters.Count > 0)
                        {
                            var paramsElement = new XElement("params",
                                outline.Parameters.Select(kv => new XElement("param",
                                    new XAttribute("name", kv.Key),
                                    new XAttribute("value", kv.Value))));
                            outlineElement.Add(paramsElement);
                        }

                        if (outline.Polygon != null && outline.Polygon.Count > 0)
                        {
                            var polygonElement = new XElement("polygon",
                                outline.Polygon.Select(point =>
                                    new XElement("point",
                                        new XAttribute("x", point.ElementAtOrDefault(0)),
                                        new XAttribute("y", point.ElementAtOrDefault(1)))));
                            outlineElement.Add(polygonElement);
                        }

                        if (!string.IsNullOrWhiteSpace(outline.Raw))
                        {
                            outlineElement.Add(new XElement("raw", outline.Raw));
                        }

                        outlinesElement.Add(outlineElement);
                    }

                    pkgElement.Add(outlinesElement);
                }

                element.Add(pkgElement);
            }

            return element;
        }

        #endregion

        #region Geometry & Coordinate Helpers

        private static PackageBounds CalculatePackageBounds(PkgRecord pkg, string pkgUnit, string targetUnit)
        {
            var sourceUnit = NormalizeUnit(pkgUnit);
            var target = NormalizeUnit(targetUnit);
            if (string.IsNullOrEmpty(sourceUnit))
            {
                sourceUnit = target ?? "INCH";
            }

            if (string.IsNullOrEmpty(target))
            {
                target = sourceUnit;
            }

            double Convert(double value) => ConvertUnits(value, sourceUnit, target);

            var convertedXMin = Convert(pkg.XMin);
            var convertedXMax = Convert(pkg.XMax);
            var minX = Math.Min(convertedXMin, convertedXMax);
            var maxX = Math.Max(convertedXMin, convertedXMax);
            var width = maxX - minX;

            var convertedYMin = Convert(pkg.YMin);
            var convertedYMax = Convert(pkg.YMax);
            var minY = Math.Min(convertedYMin, convertedYMax);
            var maxY = Math.Max(convertedYMin, convertedYMax);
            var length = maxY - minY;

            var centerX = minX + width / 2.0;
            var centerY = minY + length / 2.0;

            return new PackageBounds(centerX, centerY, width, length);
        }

        private static (double width, double length)? GetOutlineDimensions(OutlineRecord outline, string pkgUnit, string targetUnit)
        {
            if (outline?.Parameters == null || outline.Parameters.Count == 0)
            {
                return null;
            }

            var sourceUnit = NormalizeUnit(pkgUnit);
            var target = NormalizeUnit(targetUnit);
            if (string.IsNullOrEmpty(sourceUnit))
            {
                sourceUnit = target ?? "INCH";
            }

            if (string.IsNullOrEmpty(target))
            {
                target = sourceUnit;
            }

            var outlineType = outline.Type?.Trim().ToUpperInvariant();
            switch (outlineType)
            {
                case "RC":
                    if (outline.Parameters.TryGetValue("w", out var w) && outline.Parameters.TryGetValue("h", out var h))
                    {
                        return (Math.Abs(ConvertUnits(w, sourceUnit, target)), Math.Abs(ConvertUnits(h, sourceUnit, target)));
                    }

                    break;
                case "SQ":
                    if (outline.Parameters.TryGetValue("half", out var half))
                    {
                        var size = Math.Abs(half) * 2.0;
                        var converted = ConvertUnits(size, sourceUnit, target);
                        return (converted, converted);
                    }

                    break;
                case "CR":
                    if (outline.Parameters.TryGetValue("r", out var radius))
                    {
                        var diameter = Math.Abs(radius) * 2.0;
                        var converted = ConvertUnits(diameter, sourceUnit, target);
                        return (converted, converted);
                    }

                    break;
            }

            return null;
        }

        private static string ResolveShape(string outlineType)
        {
            if (string.IsNullOrWhiteSpace(outlineType))
            {
                return "rect";
            }

            return outlineType.Trim().ToUpperInvariant() == "CR" ? "circle" : "rect";
        }

        private static int NormalizeQuarterTurns(double rotationDegrees)
        {
            if (double.IsNaN(rotationDegrees) || double.IsInfinity(rotationDegrees))
            {
                return 0;
            }

            var turns = (int)Math.Round(rotationDegrees / 90.0, MidpointRounding.AwayFromZero);
            var normalized = turns % 4;
            if (normalized < 0)
            {
                normalized += 4;
            }

            return normalized;
        }

        private static (double x, double y) RotateClockwise((double x, double y) point, int quarterTurns)
        {
            switch (quarterTurns % 4)
            {
                case 0:
                    return point;
                case 1:
                    return (point.y, -point.x);
                case 2:
                    return (-point.x, -point.y);
                case 3:
                    return (-point.y, point.x);
                default:
                    return point;
            }
        }

        private static (double width, double length) ApplyRotationToDimensions(double width, double length, int quarterTurns)
        {
            return quarterTurns % 2 == 0 ? (width, length) : (length, width);
        }

        private static bool IsMirrored(string mirrorValue)
        {
            return !string.IsNullOrWhiteSpace(mirrorValue)
                   && mirrorValue.Trim().Equals("M", StringComparison.OrdinalIgnoreCase);
        }

        private static double ConvertClockwiseRotationToCounterClockwise(double clockwiseDegrees)
        {
            if (double.IsNaN(clockwiseDegrees) || double.IsInfinity(clockwiseDegrees))
            {
                return 0;
            }

            var normalized = clockwiseDegrees % 360.0;
            if (normalized < 0)
            {
                normalized += 360.0;
            }

            var ccw = 360.0 - normalized;
            if (IsZero(ccw) || IsZero(ccw - 360.0))
            {
                return 0;
            }

            return ccw;
        }

        private static bool IsBottomLayer(LayerReport layer)
        {
            return !string.IsNullOrWhiteSpace(layer?.Name)
                   && layer.Name.TrimEnd().EndsWith("_BOT", StringComparison.OrdinalIgnoreCase);
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

        private static double ConvertUnits(double value, string fromUnit, string toUnit)
        {
            var from = NormalizeUnit(fromUnit);
            var to = NormalizeUnit(toUnit);
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to)
            {
                return value;
            }

            if (from == "INCH" && to == "MM")
            {
                return value * 25.4;
            }

            if (from == "MM" && to == "INCH")
            {
                return value / 25.4;
            }

            return value;
        }

        /// <summary>
        /// Converts the profile bounding box minimums into the desired unit so component coordinates can
        /// be rebased to a 0,0 origin even when the ODB++ data uses negative offsets.
        /// </summary>
        private static bool TryGetStepOriginOffset(StepReport step, string targetUnit, out double offsetX, out double offsetY)
        {
            offsetX = 0;
            offsetY = 0;

            if (step?.ProfileBoundingBox == null)
            {
                return false;
            }

            var bbox = step.ProfileBoundingBox.Value;
            offsetX = ConvertUnits(bbox.MinX, step.Unit, targetUnit);
            offsetY = ConvertUnits(bbox.MinY, step.Unit, targetUnit);
            return true;
        }

        private static bool TryGetStepHorizontalBounds(StepReport step, string targetUnit, out double minX, out double maxX)
        {
            minX = 0;
            maxX = 0;

            if (step?.ProfileBoundingBox == null)
            {
                return false;
            }

            var bbox = step.ProfileBoundingBox.Value;
            var convertedMinX = ConvertUnits(bbox.MinX, step.Unit, targetUnit);
            var convertedMaxX = ConvertUnits(bbox.MaxX, step.Unit, targetUnit);

            if (convertedMinX <= convertedMaxX)
            {
                minX = convertedMinX;
                maxX = convertedMaxX;
            }
            else
            {
                minX = convertedMaxX;
                maxX = convertedMinX;
            }

            return true;
        }

        private static bool TryGetStepHeight(StepReport step, string targetUnit, out double height)
        {
            height = 0;
            if (step?.ProfileBoundingBox == null)
            {
                return false;
            }

            var bbox = step.ProfileBoundingBox.Value;
            var convertedMinY = ConvertUnits(bbox.MinY, step.Unit, targetUnit);
            var convertedMaxY = ConvertUnits(bbox.MaxY, step.Unit, targetUnit);
            height = Math.Abs(convertedMaxY - convertedMinY);
            return true;
        }

        #endregion

        #region Utilities

        private static string FormatDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "0";
            }

            var normalized = Math.Abs(value) < 1e-9 ? 0 : value;
            return normalized.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private struct PackageBounds
        {
            public PackageBounds(double centerX, double centerY, double width, double length)
            {
                CenterX = centerX;
                CenterY = centerY;
                Width = width;
                Length = length;
            }

            public double CenterX { get; }
            public double CenterY { get; }
            public double Width { get; }
            public double Length { get; }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "report";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var filtered = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            var sanitized = new string(filtered).Trim();

            return string.IsNullOrEmpty(sanitized) ? "report" : sanitized;
        }

        private static string EnsureEdaDataFile(string edaDir)
        {
            var dataFile = Path.Combine(edaDir, "data");
            if (File.Exists(dataFile))
            {
                LogInfo("     data file already present.");
                return dataFile;
            }

            var compressed = Path.Combine(edaDir, "data.Z");
            if (!File.Exists(compressed))
            {
                LogInfo("     no data or data.Z file found in this EDA folder.");
                return null;
            }

            LogInfo("     data file missing, decompressing data.Z...");
            try
            {
                using (var inputStream = File.OpenRead(compressed))
                using (var lzwStream = new LzwInputStream(inputStream))
                using (var outputStream = File.Create(dataFile))
                {
                    StreamUtils.Copy(lzwStream, outputStream, new byte[32 * 1024]);
                }

                if (File.Exists(dataFile))
                {
                    LogInfo("     data decompression succeeded.");
                    return dataFile;
                }

                LogInfo("     decompress finished but data file is still missing.");
                return null;
            }
            catch (Exception ex)
            {
                LogInfo($"     failed to decompress data.Z: {ex.Message}");
                return null;
            }
        }

        private static void PrintComponentRecords(string path, string unit, IReadOnlyList<ComponentRecord> records)
        {
            if (records.Count == 0)
            {
                LogInfo($"     no CMP entries detected in {Path.GetFileName(path)}.");
                return;
            }

            LogInfo($"     Parsed {records.Count} CMP records from {Path.GetFileName(path)} (unit={unit}):");
            foreach (var record in records)
            {
                LogInfo($"       pkg_ref={record.PkgRef}, x={record.X}, y={record.Y}, rot={record.Rot}, mirror={record.Mirror}, name={record.ComponentName}, part={record.PartName}");
            }
        }
        private static double ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return 0.0;
        }

        private static (List<string> steps, List<string> componentLayers) ParseMatrixStepsAndComponentLayers(string matrixText)
        {
            var options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
            var stepPattern = @"STEP\s*\{\s*(?:[^{}]|\{[^{}]*\})*?\bNAME\s*=\s*([^\r\n}]+)";
            var steps = Regex.Matches(matrixText, stepPattern, options)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var layerPattern = @"LAYER\s*\{\s*(.*?)\s*\}";
            var layerMatches = Regex.Matches(matrixText, layerPattern, options);
            var componentLayers = new List<string>();
            foreach (Match layerMatch in layerMatches)
            {
                var block = layerMatch.Groups[1].Value;
                if (Regex.IsMatch(block, @"\bCONTEXT\s*=\s*BOARD\b", options)
                    && Regex.IsMatch(block, @"\bTYPE\s*=\s*COMPONENT\b", options))
                {
                    var nameMatch = Regex.Match(block, @"\bNAME\s*=\s*([^\r\n}]+)", options);
                    if (nameMatch.Success)
                    {
                        componentLayers.Add(nameMatch.Groups[1].Value.Trim());
                    }
                }
            }

            return (steps, componentLayers);
        }

        #endregion

        #endregion

        /// <summary>
        /// Lightweight DTO returned to callers describing either a successful extraction or the failure reason.
        /// </summary>
        public sealed class ExtractionResult
        {
            private ExtractionResult(
                bool isSuccessful,
                string inputPath,
                string extractDirectory,
                JobReport report,
                string jobReportPath,
                IReadOnlyList<string> componentReportPaths,
                IReadOnlyList<string> topLeftComponentReportPaths,
                string errorMessage)
            {
                IsSuccessful = isSuccessful;
                InputPath = inputPath;
                ExtractDirectory = extractDirectory;
                JobReport = report;
                JobReportPath = jobReportPath;
                ComponentReportPaths = componentReportPaths ?? Array.Empty<string>();
                TopLeftComponentReportPaths = topLeftComponentReportPaths ?? Array.Empty<string>();
                ErrorMessage = errorMessage;
            }

            public bool IsSuccessful { get; }
            public string InputPath { get; }
            public string ExtractDirectory { get; }
            public JobReport JobReport { get; }
            public string JobReportPath { get; }
            public IReadOnlyList<string> ComponentReportPaths { get; }
            public string ComponentReportPath => ComponentReportPaths.FirstOrDefault() ?? string.Empty;
            public IReadOnlyList<string> TopLeftComponentReportPaths { get; }
            public string TopLeftComponentReportPath => TopLeftComponentReportPaths.FirstOrDefault() ?? string.Empty;
            public string ErrorMessage { get; }

            public static ExtractionResult Success(
                string inputPath,
                string extractDirectory,
                JobReport report,
                string jobReportPath,
                IReadOnlyList<string> componentReportPaths,
                IReadOnlyList<string> topLeftComponentReportPaths) =>
                new ExtractionResult(
                    true,
                    inputPath,
                    extractDirectory,
                    report,
                    jobReportPath,
                    componentReportPaths,
                    topLeftComponentReportPaths,
                    string.Empty);

            public static ExtractionResult Failure(string message, string inputPath = null, string extractDirectory = null) =>
                new ExtractionResult(
                    false,
                    inputPath ?? string.Empty,
                    extractDirectory ?? string.Empty,
                    null,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    message);
        }

        public sealed class ExportPreferences
        {
            public ExportPreferences()
            {
                ExportJobReport = true;
                ComponentMode = ComponentExportMode.Both;
                SeparateComponentFilesByLayer = false;
            }

            public bool ExportJobReport { get; set; }
            public ComponentExportMode ComponentMode { get; set; }
            public bool SeparateComponentFilesByLayer { get; set; }
            public IReadOnlyList<string> ComponentLayerFilter { get; set; }
            public Func<JobReport, IReadOnlyList<string>> LayerSelectionProvider { get; set; }

            public static ExportPreferences Default => new ExportPreferences();
        }

        public enum ComponentExportMode
        {
            None,
            BottomLeft,
            TopLeft,
            Both
        }

        #endregion
    }
}
