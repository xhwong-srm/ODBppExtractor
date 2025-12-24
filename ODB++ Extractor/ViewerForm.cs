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
using System.Drawing.Imaging;

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
        private float minScale = 10f;
        private float maxScale = 500f;

        private Image backgroundImage;
        private float backgroundWorldPerPixel = 1f;
        private float backgroundScaleMultiplier = 1f;
        private double backgroundOriginWorldX;
        private double backgroundOriginWorldY;
        private float backgroundOpacity = 0.55f;
        private bool backgroundLocked = true;
        private bool backgroundNeedsInit;
        private bool isBackgroundDragging;
        private int backgroundDragStartX;
        private int backgroundDragStartY;

        public ViewerForm(string xmlContent = "")
        {
            _xmlContent = xmlContent;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
            this.UpdateStyles();

            InitializeComponent();
            // Set placeholder (hint) text for search box
            SetCueBanner(txt_Search, "Search components…");
            this.KeyPreview = true;
            this.KeyDown += ViewerForm_KeyDown;
            txt_Search.KeyDown += Txt_Search_KeyDown;
            EnableCanvasDoubleBuffering();
            this.Shown += ViewerForm_Shown;
            canvasPanel.SizeChanged += CanvasPanel_SizeChanged;

            LoadFromXml(xmlContent, autoFit: true, clearSearch: true);
            UpdateBackgroundMenuItems();
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

        private void LoadFromXml(string xmlContent, bool autoFit, bool clearSearch)
        {
            _xmlContent = xmlContent ?? string.Empty;
            selectedComponent = null;
            hoveredComponent = null;

            if (clearSearch)
            {
                txt_Search.TextChanged -= txt_Search_TextChanged;
                txt_Search.Text = string.Empty;
                txt_Search.TextChanged += txt_Search_TextChanged;
            }

            ParseXmlData();
            SetupUI();
            backgroundNeedsInit = true;

            if (autoFit)
            {
                FitToView();
            }
            else
            {
                canvasPanel.Invalidate();
            }
        }

        public void UpdateFromXml(string xmlContent, bool autoFit = true, bool clearSearch = true)
        {
            LoadFromXml(xmlContent, autoFit, clearSearch);
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
            var compY = string.Equals(boardData?.Origin, "bottom-left", StringComparison.OrdinalIgnoreCase)
                ? (boardData?.Length ?? 0) - component.CenterY
                : component.CenterY;
            offsetY = clientSize.Height / 2f - (float)(compY * scale);
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(10, 10, 10));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            TryInitializeBackgroundTransform();

            if (boardData == null || boardData.Layers.Count == 0) return;

            DrawBackgroundImage(e.Graphics);

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
            var y = string.Equals(boardData?.Origin, "bottom-left", StringComparison.OrdinalIgnoreCase)
                ? WorldToScreenY(boardData.Length)
                : WorldToScreenY(0);
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

        private void DrawBackgroundImage(Graphics g)
        {
            if (backgroundImage == null)
                return;

            var worldPerPixel = Math.Max(0.000001f, backgroundWorldPerPixel * backgroundScaleMultiplier);
            var destWorldWidth = backgroundImage.Width * worldPerPixel;
            var destWorldHeight = backgroundImage.Height * worldPerPixel;

            var destX = WorldToScreenX(backgroundOriginWorldX);
            var destY = WorldToScreenY(backgroundOriginWorldY);
            var destW = destWorldWidth * scale;
            var destH = destWorldHeight * scale;

            using (var attributes = new ImageAttributes())
            {
                var matrix = new ColorMatrix
                {
                    Matrix33 = Math.Max(0f, Math.Min(1f, backgroundOpacity))
                };
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(
                    backgroundImage,
                    new Rectangle((int)Math.Round(destX), (int)Math.Round(destY), (int)Math.Round(destW), (int)Math.Round(destH)),
                    0,
                    0,
                    backgroundImage.Width,
                    backgroundImage.Height,
                    GraphicsUnit.Pixel,
                    attributes);
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
        private float WorldToScreenY(double y)
        {
            var originBottomLeft = string.Equals(boardData?.Origin, "bottom-left", StringComparison.OrdinalIgnoreCase);
            var worldY = originBottomLeft ? (boardData?.Length ?? 0) - y : y;
            return (float)(worldY * scale + offsetY);
        }
        private double ScreenToWorldX(float x) => (x - offsetX) / scale;
        private double ScreenToWorldY(float y) => (y - offsetY) / scale;
        private double ScreenDeltaToWorldX(float dx) => dx / scale;
        private double ScreenDeltaToWorldY(float dy)
        {
            var originBottomLeft = string.Equals(boardData?.Origin, "bottom-left", StringComparison.OrdinalIgnoreCase);
            return originBottomLeft ? -dy / scale : dy / scale;
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (backgroundImage != null && !backgroundLocked && Control.ModifierKeys.HasFlag(Keys.Control) && e.Button == MouseButtons.Left)
            {
                isBackgroundDragging = true;
                backgroundDragStartX = e.X;
                backgroundDragStartY = e.Y;
                return;
            }

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
            if (isBackgroundDragging)
            {
                var dxWorld = ScreenDeltaToWorldX(e.X - backgroundDragStartX);
                var dyWorld = ScreenDeltaToWorldY(e.Y - backgroundDragStartY);

                backgroundOriginWorldX += dxWorld;
                backgroundOriginWorldY += dyWorld;

                backgroundDragStartX = e.X;
                backgroundDragStartY = e.Y;
                canvasPanel.Invalidate();
                return;
            }

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
            isBackgroundDragging = false;
            isDragging = false;
        }

        private void Canvas_MouseLeave(object sender, EventArgs e)
        {
            isBackgroundDragging = false;
            hoveredComponent = null;
            canvasPanel.Invalidate();
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (backgroundImage != null && !backgroundLocked && Control.ModifierKeys.HasFlag(Keys.Control))
            {
                var factor = (float)Math.Pow(1.02, e.Delta / 120.0); // finer 2% steps per wheel tick
                AdjustBackgroundZoom(factor);
                return;
            }

            var oldScale = scale;
            scale *= e.Delta > 0 ? 1.1f : 0.9f;
            scale = Math.Max(minScale, Math.Min(maxScale, scale));

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

        private void AdjustBackgroundZoom(float factor)
        {
            backgroundScaleMultiplier = Math.Max(0.001f, Math.Min(1000f, backgroundScaleMultiplier * factor));
            canvasPanel.Invalidate();
        }

        private void Txt_Search_KeyDown(object sender, KeyEventArgs e)
        {
            // Prevent search box from receiving the ctrl+plus/minus keystrokes used for background zoom
            if (backgroundImage != null && !backgroundLocked && e.Control &&
                (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add || e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        // Designer event handlers
        private void txt_Search_TextChanged(object sender, EventArgs e)
        {
            // Remove any invisible/control characters that may have been input
            var text = txt_Search.Text;
            var filtered = new string(text.Where(c => !char.IsControl(c) || c == '\b').ToArray());
            if (filtered != text)
            {
                txt_Search.TextChanged -= txt_Search_TextChanged;
                txt_Search.Text = filtered;
                txt_Search.TextChanged += txt_Search_TextChanged;
            }

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

        private void ViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (backgroundImage != null && !backgroundLocked && e.Control)
            {
                if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                {
                    AdjustBackgroundZoom(e.Shift ? 1.005f : 1.02f); // Shift for ultra-fine steps
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                {
                    AdjustBackgroundZoom(e.Shift ? 0.995f : 0.98f);
                    e.Handled = true;
                }
            }
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

            // Always fit to the physical board size (0..Width, 0..Length)
            var boardMinX = 0.0;
            var boardMinY = 0.0;
            var boardWidth = Math.Max(1f, (float)boardData.Width);
            var boardHeight = Math.Max(1f, (float)boardData.Length);

            var scaleX = availWidth / boardWidth;
            var scaleY = availHeight / boardHeight;
            var boardFitScale = Math.Min(scaleX, scaleY);

            // Allow zooming out to show the board at 25% of the view
            minScale = Math.Max(0.0001f, boardFitScale * 0.25f);
            maxScale = ComputeMaxScaleForSmallestComponent(availWidth, availHeight);

            // Start zoomed to the board fit while staying within the new limit
            scale = Math.Max(minScale, Math.Min(boardFitScale, maxScale));

            var boardWidthScaled = boardWidth * scale;
            var boardHeightScaled = boardHeight * scale;

            var extraX = Math.Max(0f, availWidth - boardWidthScaled) / 2f;
            var extraY = Math.Max(0f, availHeight - boardHeightScaled) / 2f;

            offsetX = padding + extraX - (float)(boardMinX * scale);
            offsetY = padding + extraY - (float)(boardMinY * scale);

            canvasPanel.Invalidate();
        }

        private void TryInitializeBackgroundTransform()
        {
            if (!backgroundNeedsInit || backgroundImage == null || boardData == null)
                return;

            var baseScaleX = boardData.Width > 0 ? (float)(boardData.Width / Math.Max(1, backgroundImage.Width)) : 1f;
            var baseScaleY = boardData.Length > 0 ? (float)(boardData.Length / Math.Max(1, backgroundImage.Height)) : 1f;
            backgroundWorldPerPixel = Math.Max(0.000001f, Math.Min(baseScaleX, baseScaleY));
            backgroundScaleMultiplier = 1f;
            backgroundOriginWorldX = 0;
            backgroundOriginWorldY = string.Equals(boardData.Origin, "bottom-left", StringComparison.OrdinalIgnoreCase)
                ? boardData.Length
                : 0;

            backgroundNeedsInit = false;
        }

        private float ComputeMaxScaleForSmallestComponent(float availWidth, float availHeight)
        {
            if (boardData == null || boardData.AllComponents == null || boardData.AllComponents.Count == 0)
            {
                // Fallback if no components: allow a reasonable zoom-in headroom
                return Math.Max(minScale * 10f, minScale);
            }

            // Determine the smallest component by area
            var smallest = boardData.AllComponents
                .OrderBy(c => Math.Max(0.000001, c.Width) * Math.Max(0.000001, c.Length))
                .First();

            var compW = (float)Math.Max(0.000001, smallest.Width);
            var compH = (float)Math.Max(0.000001, smallest.Length);

            // Max scale where the smallest component just fits the viewport
            var fitScaleX = availWidth / compW;
            var fitScaleY = availHeight / compH;
            var componentFitScale = Math.Min(fitScaleX, fitScaleY);

            // Ensure maxScale is at least minScale
            var computed = Math.Max(minScale, componentFitScale);

            // Provide a safety upper bound to avoid pathological values
            return Math.Min(computed, minScale * 1000f);
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

        private void LoadBackgroundImageFromFile()
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Select background image",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    using (var loaded = Image.FromFile(dialog.FileName))
                    {
                        backgroundImage?.Dispose();
                        backgroundImage = new Bitmap(loaded);
                    }

                    backgroundNeedsInit = true;
                    SetBackgroundLocked(false);
                    canvasPanel.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to load image: {ex.Message}", "Background", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ClearBackgroundImage()
        {
            backgroundImage?.Dispose();
            backgroundImage = null;
            backgroundNeedsInit = false;
            canvasPanel.Invalidate();
        }

        private void ResetBackgroundTransform()
        {
            backgroundNeedsInit = true;
            TryInitializeBackgroundTransform();
            canvasPanel.Invalidate();
        }

        private void SetBackgroundLocked(bool locked)
        {
            backgroundLocked = locked;
            UpdateBackgroundMenuItems();
        }

        private void UpdateBackgroundMenuItems()
        {
            if (toggleBackgroundLockMenuItem != null)
            {
                toggleBackgroundLockMenuItem.Checked = backgroundLocked;
            }

            if (clearBackgroundMenuItem != null)
            {
                clearBackgroundMenuItem.Enabled = backgroundImage != null;
            }

            if (resetBackgroundTransformMenuItem != null)
            {
                resetBackgroundTransformMenuItem.Enabled = backgroundImage != null;
            }
        }

        private void loadBackgroundMenuItem_Click(object sender, EventArgs e)
        {
            LoadBackgroundImageFromFile();
            UpdateBackgroundMenuItems();
        }

        private void clearBackgroundMenuItem_Click(object sender, EventArgs e)
        {
            ClearBackgroundImage();
            UpdateBackgroundMenuItems();
        }

        private void toggleBackgroundLockMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem != null)
            {
                SetBackgroundLocked(menuItem.Checked);
            }
            else
            {
                SetBackgroundLocked(!backgroundLocked);
            }
        }

        private void resetBackgroundTransformMenuItem_Click(object sender, EventArgs e)
        {
            ResetBackgroundTransform();
        }

        // Native cue banner (placeholder) support for TextBox on Windows Vista+
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        private void SetCueBanner(TextBox textBox, string hint, bool showWhenFocused = true)
        {
            void Apply()
            {
                try
                {
                    SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)(showWhenFocused ? 1 : 0), hint ?? string.Empty);
                }
                catch { /* ignore if OS does not support */ }
            }

            if (textBox.IsHandleCreated)
            {
                Apply();
            }
            else
            {
                textBox.HandleCreated += (s, e) => Apply();
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
