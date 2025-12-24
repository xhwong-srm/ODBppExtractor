using Microsoft.VisualStudio.TestTools.UnitTesting;
using ODB___Extractor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using JobReport = ODB___Extractor.ODBppExtractor.JobReport;
using CoordinateOrigin = ODB___Extractor.ODBppExtractor.CoordinateOrigin;
using AxisFlip = ODB___Extractor.ODBppExtractor.AxisFlip;
using ComponentPlacementFlipOptions = ODB___Extractor.ODBppExtractor.ComponentPlacementFlipOptions;

namespace ODBppExtractor.Tests
{
    [TestClass]
    public class ODBppExtractorTests
    {
        private const string MirrorScenario = "pcb1-mirror";
        private const string NonMirrorScenario = "pcb1-non-mirror";
        private const double CoordinateTolerance = 1e-3;

        [TestMethod]
        public void ExtractorProducesExpectedComponentPlacements()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "ODBppExtractor.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var jobDirectory = PrepareJobDirectory(tempRoot);
                var workingDirectory = Path.Combine(tempRoot, "work");
                Directory.CreateDirectory(workingDirectory);

                var extractionResult = ODB___Extractor.ODBppExtractor.Extract(jobDirectory, workingDirectory);
                Assert.IsTrue(extractionResult.IsSuccessful, $"Extraction failed: {extractionResult.ErrorMessage}");
                Assert.IsNotNull(extractionResult.JobReport);

                ValidateComponentPlacements(extractionResult.JobReport, tempRoot, false, NonMirrorScenario);
                ValidateComponentPlacements(extractionResult.JobReport, tempRoot, true, MirrorScenario);
            }
            finally
            {
                TryDeleteDirectory(tempRoot);
            }
        }

        private static void ValidateComponentPlacements(JobReport jobReport, string tempRoot, bool mirrorBottomLayerX, string scenarioName)
        {
            var outputDirectory = Path.Combine(tempRoot, $"{scenarioName}-export");
            Directory.CreateDirectory(outputDirectory);
            var flipOptions = mirrorBottomLayerX
                ? new ComponentPlacementFlipOptions
                {
                    Axes = AxisFlip.X,
                    BottomLayerOnly = true
                }
                : null;
            var exportedPaths = ODB___Extractor.ODBppExtractor.ExportComponentPlacementReports(
                jobReport,
                CoordinateOrigin.TopLeft,
                separateByLayer: true,
                layerFilter: null,
                targetDirectory: outputDirectory,
                targetUnit: "MM",
                flipOptions: flipOptions);

            Assert.IsTrue(exportedPaths.Count > 0, $"No placement reports were generated for {scenarioName}.");

            var actualPlacements = LoadPlacementData(exportedPaths);
            var expectedPlacements = GetFixturePlacements(scenarioName);

            Assert.AreEqual(expectedPlacements.Count, actualPlacements.Count, $"Layer count mismatch for {scenarioName}.");

            foreach (var expectedLayer in expectedPlacements)
            {
                Assert.IsTrue(actualPlacements.TryGetValue(expectedLayer.Key, out var actualComponents),
                    $"Layer '{expectedLayer.Key}' missing in generated output for {scenarioName}.");

                CollectionAssert.AreEquivalent(
                    expectedLayer.Value.Keys.ToList(),
                    actualComponents.Keys.ToList(),
                    $"Component list mismatch in layer '{expectedLayer.Key}' for {scenarioName}.");

                foreach (var expectedComponent in expectedLayer.Value)
                {
                    var actual = actualComponents[expectedComponent.Key];
                    var expected = expectedComponent.Value;

                    Assert.AreEqual(expected.Shape, actual.Shape,
                        $"Shape mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                    Assert.AreEqual(expected.PackageName, actual.PackageName,
                        $"Package mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                    Assert.AreEqual(expected.Rotation, actual.Rotation, CoordinateTolerance,
                        $"Rotation mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                    Assert.AreEqual(expected.CenterX, actual.CenterX, CoordinateTolerance,
                        $"CenterX mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                    Assert.AreEqual(expected.CenterY, actual.CenterY, CoordinateTolerance,
                        $"CenterY mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                    Assert.AreEqual(expected.Width, actual.Width, CoordinateTolerance,
                        $"Width mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                    Assert.AreEqual(expected.Length, actual.Length, CoordinateTolerance,
                        $"Length mismatch for '{expected.Name}' on layer '{expectedLayer.Key}' ({scenarioName}).");
                }
            }
        }

        private static readonly object FixtureLock = new object();
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, ComponentPlacementRecord>>> FixtureCache
            = new Dictionary<string, Dictionary<string, Dictionary<string, ComponentPlacementRecord>>>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, Dictionary<string, ComponentPlacementRecord>> GetFixturePlacements(string scenarioName)
        {
            lock (FixtureLock)
            {
                if (!FixtureCache.TryGetValue(scenarioName, out var cached))
                {
                    cached = LoadFixturePlacements(scenarioName);
                    FixtureCache[scenarioName] = cached;
                }

                return cached;
            }
        }

        private static Dictionary<string, Dictionary<string, ComponentPlacementRecord>> LoadFixturePlacements(string scenarioName)
        {
            var scenarioDir = Path.Combine(TestDataRoot, "extracted-xml", scenarioName);
            if (!Directory.Exists(scenarioDir))
            {
                throw new DirectoryNotFoundException($"Fixture directory not found: {scenarioDir}");
            }

            var fixtures = new Dictionary<string, Dictionary<string, ComponentPlacementRecord>>(StringComparer.OrdinalIgnoreCase);
            foreach (var filePath in Directory.EnumerateFiles(scenarioDir, "*.xml"))
            {
                var document = XDocument.Load(filePath);
                var layers = document.Root?
                    .Elements("step")
                    .SelectMany(step => step.Elements("layer")) ?? Enumerable.Empty<XElement>();

                foreach (var layerElement in layers)
                {
                    var layerName = layerElement.Attribute("name")?.Value ?? string.Empty;
                    var componentData = ParseComponentsFromLayer(layerElement);
                    fixtures[layerName] = componentData;
                }
            }

            return fixtures;
        }

        private static Dictionary<string, Dictionary<string, ComponentPlacementRecord>> LoadPlacementData(IReadOnlyList<string> exportPaths)
        {
            var placements = new Dictionary<string, Dictionary<string, ComponentPlacementRecord>>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in exportPaths)
            {
                var document = XDocument.Load(path);
                var layers = document.Root?
                    .Elements("step")
                    .SelectMany(step => step.Elements("layer")) ?? Enumerable.Empty<XElement>();

                foreach (var layerElement in layers)
                {
                    var layerName = layerElement.Attribute("name")?.Value ?? string.Empty;
                    var componentData = ParseComponentsFromLayer(layerElement);
                    placements[layerName] = componentData;
                }
            }

            return placements;
        }

        private static Dictionary<string, ComponentPlacementRecord> ParseComponentsFromLayer(XElement layerElement)
        {
            var components = new Dictionary<string, ComponentPlacementRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var componentElement in layerElement.Elements("component"))
            {
                var record = ComponentPlacementRecord.FromXElement(componentElement);
                components[record.Name] = record;
            }

            return components;
        }

        private static string PrepareJobDirectory(string tempRoot)
        {
            var jobSource = Path.Combine(TestDataRoot, "pcb1-odb", "odbjob");
            if (!Directory.Exists(jobSource))
            {
                throw new DirectoryNotFoundException($"Job fixture directory missing: {jobSource}");
            }

            var jobDir = Path.Combine(tempRoot, "odbjob");
            Directory.CreateDirectory(jobDir);
            CopyDirectoryContents(jobSource, jobDir);
            return jobDir;
        }

        private static void CopyDirectoryContents(string sourceDirectory, string destinationDirectory)
        {
            var sourceFullPath = Path.GetFullPath(sourceDirectory);
            foreach (var dirPath in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = dirPath.Substring(sourceFullPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
            }

            foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = filePath.Substring(sourceFullPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destinationPath = Path.Combine(destinationDirectory, relativePath);
                var destinationFolder = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                File.Copy(filePath, destinationPath, overwrite: true);
            }
        }

        private static string TestDataRoot
        {
            get
            {
                var projectDir = TestsProjectDirectory;
                return Path.Combine(projectDir, "test-data");
            }
        }

        private static string TestsProjectDirectory
        {
            get
            {
                var assemblyDir = Path.GetDirectoryName(typeof(ODBppExtractorTests).Assembly.Location);
                if (assemblyDir == null)
                {
                    throw new InvalidOperationException("Unable to locate test assembly directory.");
                }

                return Path.GetFullPath(Path.Combine(assemblyDir, "..", ".."));
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch
            {
                // best effort cleanup
            }
        }

        private sealed class ComponentPlacementRecord
        {
            public ComponentPlacementRecord(string name, string shape, string packageName, double rotation, double centerX, double centerY, double width, double length)
            {
                Name = name;
                Shape = shape;
                PackageName = packageName;
                Rotation = rotation;
                CenterX = centerX;
                CenterY = centerY;
                Width = width;
                Length = length;
            }

            public string Name { get; }
            public string Shape { get; }
            public string PackageName { get; }
            public double Rotation { get; }
            public double CenterX { get; }
            public double CenterY { get; }
            public double Width { get; }
            public double Length { get; }

            public static ComponentPlacementRecord FromXElement(XElement element)
            {
                var name = element.Attribute("name")?.Value ?? string.Empty;
                var shape = element.Attribute("shape")?.Value ?? string.Empty;
                var packageName = element.Attribute("packageName")?.Value ?? string.Empty;

                return new ComponentPlacementRecord(
                    name,
                    shape,
                    packageName,
                    ParseDoubleAttribute(element, "rotation"),
                    ParseDoubleAttribute(element, "centerX"),
                    ParseDoubleAttribute(element, "centerY"),
                    ParseDoubleAttribute(element, "width"),
                    ParseDoubleAttribute(element, "length"));
            }

            private static double ParseDoubleAttribute(XElement element, string attributeName)
            {
                var attribute = element.Attribute(attributeName);
                if (attribute == null)
                {
                    throw new InvalidOperationException($"Missing '{attributeName}' attribute on component.");
                }

                return double.Parse(attribute.Value, CultureInfo.InvariantCulture);
            }
        }
    }
}
