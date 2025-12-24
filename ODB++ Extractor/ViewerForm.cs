using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Xml.Linq;

namespace ODB___Extractor
{
    public partial class ViewerForm : Form
    {
        private string _xmlContent;

        private BoardData boardData;
        private string selectedLayer;
        private ComponentData selectedComponent;
        private ComponentData hoveredComponent;

        private float scale = 100f;
        private float offsetX = 0f;
        private float offsetY = 0f;
        private bool isDragging = false;
        private int dragStartX = 0;
        private int dragStartY = 0;
        private bool needsInitialFit = false;
        private bool suppressCenterOnListSelection;

        public ViewerForm(string xmlContent = "")
        {
            _xmlContent = xmlContent;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
            this.UpdateStyles();

            InitializeComponent();
            EnableCanvasDoubleBuffering();
            this.Shown += ViewerForm_Shown;
            canvasPanel.SizeChanged += CanvasPanel_SizeChanged;
            ParseXmlData();
            SetupUI();
        }
        private void ParseXmlData()
        {
            try
            {
                boardData = new BoardData();
                var doc = XDocument.Parse(_xmlContent);
                var root = doc.Root;

                if (root == null) return;

                // Parse board info
                boardData.Count = root.Attribute("count")?.Value ?? "0";
                boardData.Origin = root.Attribute("origin")?.Value ?? "top-left";

                // Parse steps and layers
                foreach (var stepEl in root.Elements("step"))
                {
                    var stepName = stepEl.Attribute("name")?.Value ?? "Step";
                    boardData.Unit = stepEl.Attribute("unit")?.Value ?? "MM";
                    boardData.Width = double.TryParse(stepEl.Attribute("width")?.Value, out var w) ? w : 100;
                    boardData.Length = double.TryParse(stepEl.Attribute("length")?.Value, out var l) ? l : 100;

                    foreach (var layerEl in stepEl.Elements("layer"))
                    {
                        var layerName = layerEl.Attribute("name")?.Value ?? "Unknown";
                        var layerData = new LayerData { Name = layerName };

                        foreach (var compEl in layerEl.Elements("component"))
                        {
                            var comp = new ComponentData
                            {
                                Name = compEl.Attribute("name")?.Value ?? "Unknown",
                                PackageName = compEl.Attribute("packageName")?.Value ?? "N/A",
                                CenterX = double.TryParse(compEl.Attribute("centerX")?.Value, out var cx) ? cx : 0,
                                CenterY = double.TryParse(compEl.Attribute("centerY")?.Value, out var cy) ? cy : 0,
                                Width = double.TryParse(compEl.Attribute("width")?.Value, out var wd) ? wd : 5,
                                Length = double.TryParse(compEl.Attribute("length")?.Value, out var ln) ? ln : 5,
                                Rotation = double.TryParse(compEl.Attribute("rotation")?.Value, out var r) ? r : 0,
                                Shape = compEl.Attribute("shape")?.Value ?? "rect",
                                Layer = layerName
                            };
                            layerData.Components.Add(comp);
                            boardData.AllComponents.Add(comp);
                        }

                        boardData.Layers.Add(layerData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing XML: {ex.Message}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupUI()
        {
            needsInitialFit = false;
            if (boardData == null)
            {
                MessageBox.Show("Error: Board data is null. XML may not have been parsed correctly.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (boardData.Layers.Count == 0)
            {
                MessageBox.Show("No layers found in the ODB++ data.\n\nXML Content Length: " + _xmlContent?.Length, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Set first layer as default
            selectedLayer = boardData.Layers[0].Name;
            
            // Populate component list for first layer
            UpdateComponentList();
            needsInitialFit = true;
        }

        private void UpdateComponentList()
        {
            lstComponents.Items.Clear();
            
            if (boardData == null || string.IsNullOrEmpty(selectedLayer))
                return;

            var layer = boardData.Layers.FirstOrDefault(l => l.Name == selectedLayer);
            if (layer == null)
                return;

            var filter = (txt_Search.Text ?? string.Empty).Trim().ToLowerInvariant();
            var filtered = layer.Components.Where(c => c.Name.ToLower().Contains(filter)).ToList();

            foreach (var comp in filtered)
            {
                lstComponents.Items.Add(comp.Name);
            }
        }

        private void SelectComponent()
        {
            if (lstComponents.SelectedItem is string compName)
            {
                selectedComponent = boardData.AllComponents.FirstOrDefault(c => c.Name == compName);
            }
            else
            {
                selectedComponent = null;
            }

            if (!suppressCenterOnListSelection)
            {
                CenterOnComponent(selectedComponent);
            }
            canvasPanel.Invalidate();
        }

        private void CenterOnComponent(ComponentData component)
        {
            if (component == null)
                return;

            var clientSize = canvasPanel.ClientSize;
            if (clientSize.Width <= 0 || clientSize.Height <= 0)
                return;

            var sidebarWidth = (sidebarPanel.Visible && sidebarPanel.Dock == DockStyle.Right) ? sidebarPanel.Width : 0;
            var visibleWidth = Math.Max(1f, clientSize.Width - sidebarWidth);
            offsetX = visibleWidth / 2f - (float)(component.CenterX * scale);
            offsetY = clientSize.Height / 2f - (float)(component.CenterY * scale);
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(10, 10, 10));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (boardData == null || boardData.Layers.Count == 0) return;

            // Draw board outline
            DrawBoard(e.Graphics);

            // Draw grid
            DrawGrid(e.Graphics);

            // Draw components
            var layer = boardData.Layers.FirstOrDefault(l => l.Name == selectedLayer);
            if (layer != null)
            {
                foreach (var comp in layer.Components)
                {
                    DrawComponent(e.Graphics, comp);
                }
            }

            DrawSelectedComponentInfo(e.Graphics);
        }

        private void DrawBoard(Graphics g)
        {
            var x = WorldToScreenX(0);
            var y = WorldToScreenY(0);
            var w = (float)(boardData.Width * scale);
            var h = (float)(boardData.Length * scale);

            using (var pen = new Pen(Color.FromArgb(255, 68, 68), 2))
            {
                g.DrawRectangle(pen, x, y, w, h);
            }
        }

        private void DrawGrid(Graphics g)
        {
            var gridSize = boardData.Unit == "MM" ? 10 : 1;
            using (var pen = new Pen(Color.FromArgb(26, 26, 26), 1))
            {
                for (double x = 0; x <= boardData.Width; x += gridSize)
                {
                    var sx = WorldToScreenX(x);
                    g.DrawLine(pen, sx, 0, sx, canvasPanel.Height);
                }

                for (double y = 0; y <= boardData.Length; y += gridSize)
                {
                    var sy = WorldToScreenY(y);
                    g.DrawLine(pen, 0, sy, canvasPanel.Width, sy);
                }
            }
        }

        private void DrawComponent(Graphics g, ComponentData comp)
        {
            var x = WorldToScreenX(comp.CenterX);
            var y = WorldToScreenY(comp.CenterY);
            var w = (float)(comp.Width * scale);
            var h = (float)(comp.Length * scale);

            bool isSelected = comp == selectedComponent;
            bool isHovered = comp == hoveredComponent;

            Color color = isSelected ? Color.FromArgb(255, 170, 0) : isHovered ? Color.Lime : Color.FromArgb(74, 158, 255);

            using (var pen = new Pen(color, isSelected ? 3 : isHovered ? 2 : 1.5f))
            {
                g.DrawRectangle(pen, x - w / 2, y - h / 2, w, h);
            }

            // Center point
            using (var brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, x - 2, y - 2, 4, 4);
            }

            // Draw name if selected or hovered
            if (isSelected || isHovered)
            {
                using (var font = new Font("Arial", 10, FontStyle.Bold))
                using (var brush = new SolidBrush(color))
                {
                    g.DrawString(comp.Name, font, brush, x + 5, y - 15);
                }
            }
        }

        private float WorldToScreenX(double x) => (float)(x * scale + offsetX);
        private float WorldToScreenY(double y) => (float)(y * scale + offsetY);
        private double ScreenToWorldX(float x) => (x - offsetX) / scale;
        private double ScreenToWorldY(float y) => (y - offsetY) / scale;

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            var layer = boardData.Layers.FirstOrDefault(l => l.Name == selectedLayer);
            var hitComponent = false;

            if (layer != null)
            {
                foreach (var comp in layer.Components)
                {
                    if (!IsComponentAtPoint(comp, e.X, e.Y))
                        continue;

                    hitComponent = true;

                    if (e.Clicks > 1 && selectedComponent == comp)
                    {
                        selectedComponent = null;
                        lstComponents.ClearSelected();
                    }
                    else
                    {
                        selectedComponent = comp;
                        suppressCenterOnListSelection = true;
                        try
                        {
                            if (lstComponents.Items.Contains(comp.Name))
                            {
                                lstComponents.SelectedItem = comp.Name;
                            }
                            else
                            {
                                lstComponents.ClearSelected();
                            }
                        }
                        finally
                        {
                            suppressCenterOnListSelection = false;
                        }
                    }

                    canvasPanel.Invalidate();
                    break;
                }
            }

            isDragging = true;
            dragStartX = e.X;
            dragStartY = e.Y;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.Button == MouseButtons.Left)
            {
                offsetX += e.X - dragStartX;
                offsetY += e.Y - dragStartY;
                dragStartX = e.X;
                dragStartY = e.Y;
                canvasPanel.Invalidate();
            }
            else
            {
                // Check hover - only redraw if hover state changes
                var oldHovered = hoveredComponent;
                hoveredComponent = null;
                var layer = boardData.Layers.FirstOrDefault(l => l.Name == selectedLayer);
                if (layer != null)
                {
                    foreach (var comp in layer.Components)
                    {
                        if (IsComponentAtPoint(comp, e.X, e.Y))
                        {
                            hoveredComponent = comp;
                            canvasPanel.Cursor = Cursors.Hand;
                            break;
                        }
                    }
                }

                if (hoveredComponent == null)
                {
                    canvasPanel.Cursor = Cursors.Default;
                }

                // Only invalidate if hover state changed
                if (hoveredComponent != oldHovered)
                {
                    canvasPanel.Invalidate();
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void Canvas_MouseLeave(object sender, EventArgs e)
        {
            hoveredComponent = null;
            canvasPanel.Invalidate();
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            var oldScale = scale;
            scale *= e.Delta > 0 ? 1.1f : 0.9f;
            scale = Math.Max(10, Math.Min(500, scale));

            // Zoom towards mouse position
            var mx = e.X;
            var my = e.Y;
            offsetX = mx - (mx - offsetX) * (scale / oldScale);
            offsetY = my - (my - offsetY) * (scale / oldScale);

            canvasPanel.Invalidate();
        }

        private bool IsComponentAtPoint(ComponentData comp, int sx, int sy)
        {
            var x = WorldToScreenX(comp.CenterX);
            var y = WorldToScreenY(comp.CenterY);
            var w = (float)(comp.Width * scale);
            var h = (float)(comp.Length * scale);

            return sx >= x - w / 2 && sx <= x + w / 2 && sy >= y - h / 2 && sy <= y + h / 2;
        }

        // Designer event handlers
        private void txt_Search_TextChanged(object sender, EventArgs e)
        {
            UpdateComponentList();
        }

        private void lstComponents_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectComponent();
        }

        private void resetView_Click(object sender, EventArgs e)
        {
            FitToView();
        }

        private void ViewerForm_Shown(object sender, EventArgs e)
        {
            if (!needsInitialFit)
                return;

            needsInitialFit = false;
            BeginInvoke(new Action(FitToView));
        }

        private void EnableCanvasDoubleBuffering()
        {
            var doubleBufferProperty = typeof(Panel).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferProperty?.SetValue(canvasPanel, true);
            //canvasPanel.ResizeRedraw = true;
        }

        private void CanvasPanel_SizeChanged(object sender, EventArgs e)
        {
            FitToView();
        }

        private void FitToView()
        {
            if (boardData == null)
                return;

            var clientSize = canvasPanel.ClientSize;
            if (clientSize.Width <= 0 || clientSize.Height <= 0)
                return;

            var padding = 20f;
            var sidebarWidth = (sidebarPanel.Visible && sidebarPanel.Dock == DockStyle.Right) ? sidebarPanel.Width : 0;
            var visibleWidth = Math.Max(1f, clientSize.Width - sidebarWidth);
            var availWidth = Math.Max(1f, visibleWidth - padding * 2);
            var availHeight = Math.Max(1f, clientSize.Height - padding * 2);

            var bounds = CalculateBoardBounds();
            var boardWidth = Math.Max(1f, (float)(bounds.MaxX - bounds.MinX));
            var boardHeight = Math.Max(1f, (float)(bounds.MaxY - bounds.MinY));

            var scaleX = availWidth / boardWidth;
            var scaleY = availHeight / boardHeight;

            scale = Math.Min(scaleX, scaleY);
            scale = Math.Max(10, Math.Min(500, scale));

            var boardWidthScaled = boardWidth * scale;
            var boardHeightScaled = boardHeight * scale;

            var extraX = Math.Max(0f, availWidth - boardWidthScaled) / 2f;
            var extraY = Math.Max(0f, availHeight - boardHeightScaled) / 2f;

            offsetX = padding + extraX - (float)(bounds.MinX * scale);
            offsetY = padding + extraY - (float)(bounds.MinY * scale);

            canvasPanel.Invalidate();
        }

        private void DrawSelectedComponentInfo(Graphics g)
        {
            if (selectedComponent == null)
                return;

            var clientSize = canvasPanel.ClientSize;
            var lines = new[]
            {
                $"Package: {selectedComponent.PackageName}",
                $"Position: {selectedComponent.CenterX:F2}, {selectedComponent.CenterY:F2}",
                $"Width: {selectedComponent.Width:F2}",
                $"Length: {selectedComponent.Length:F2}",
                $"Rotation: {selectedComponent.Rotation:F2}°"
            };

            using (var font = new Font("Segoe UI", 9, FontStyle.Regular, GraphicsUnit.Point))
            using (var textBrush = new SolidBrush(Color.White))
            using (var backgroundBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 20)))
            using (var borderPen = new Pen(Color.FromArgb(210, 255, 255, 255), 1))
            {
                float padding = 8f;
                float yOffset = padding;
                float textWidth = 0f;
                float totalHeight = 0f;

                foreach (var line in lines)
                {
                    var textSize = g.MeasureString(line, font);
                    textWidth = Math.Max(textWidth, textSize.Width);
                    totalHeight += textSize.Height;
                }

                var rectWidth = textWidth + padding * 2f;
                var rectHeight = totalHeight + padding * 2f;
                var rectX = padding;
                var rectY = Math.Max(padding, clientSize.Height - rectHeight - padding);

                g.FillRectangle(backgroundBrush, rectX, rectY, rectWidth, rectHeight);
                g.DrawRectangle(borderPen, rectX, rectY, rectWidth, rectHeight);

                var textY = rectY + padding;
                foreach (var line in lines)
                {
                    g.DrawString(line, font, textBrush, rectX + padding, textY);
                    textY += g.MeasureString(line, font).Height;
                }
            }
        }

        private (double MinX, double MinY, double MaxX, double MaxY) CalculateBoardBounds()
        {
            if (boardData == null)
            {
                return (0, 0, 0, 0);
            }

            var components = boardData.AllComponents;
            if (components.Count == 0)
            {
                return (0, 0, boardData.Width, boardData.Length);
            }

            var minX = components.Min(c => c.CenterX - c.Width / 2.0);
            var maxX = components.Max(c => c.CenterX + c.Width / 2.0);
            var minY = components.Min(c => c.CenterY - c.Length / 2.0);
            var maxY = components.Max(c => c.CenterY + c.Length / 2.0);

            return (minX, minY, maxX, maxY);
        }
    }

    public class BoardData
    {
        public string Count { get; set; }
        public string Origin { get; set; }
        public string Unit { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public List<LayerData> Layers { get; set; } = new List<LayerData>();
        public List<ComponentData> AllComponents { get; set; } = new List<ComponentData>();
    }

    public class LayerData
    {
        public string Name { get; set; }
        public List<ComponentData> Components { get; set; } = new List<ComponentData>();
    }

    public class ComponentData
    {
        public string Name { get; set; }
        public string PackageName { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double Rotation { get; set; }
        public string Shape { get; set; }
        public string Layer { get; set; }
    }
}
