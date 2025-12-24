
namespace ODB___Extractor
{
    partial class ViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewerForm));
            this.canvasPanel = new System.Windows.Forms.Panel();
            this.canvasMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.resetView = new System.Windows.Forms.ToolStripMenuItem();
            this.sidebarPanel = new System.Windows.Forms.Panel();
            this.lstComponents = new System.Windows.Forms.ListBox();
            this.txt_Search = new SRMControl.SRMInputBox();
            this.betterFolderBrowser1 = new WK.Libraries.BetterFolderBrowserNS.BetterFolderBrowser(this.components);
            this.canvasMenu.SuspendLayout();
            this.sidebarPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // canvasPanel
            // 
            this.canvasPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(10)))));
            this.canvasPanel.ContextMenuStrip = this.canvasMenu;
            resources.ApplyResources(this.canvasPanel, "canvasPanel");
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.Canvas_Paint);
            this.canvasPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseDown);
            this.canvasPanel.MouseLeave += new System.EventHandler(this.Canvas_MouseLeave);
            this.canvasPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseMove);
            this.canvasPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseUp);
            this.canvasPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseWheel);
            // 
            // canvasMenu
            // 
            this.canvasMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.canvasMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetView});
            this.canvasMenu.Name = "canvasMenu";
            resources.ApplyResources(this.canvasMenu, "canvasMenu");
            // 
            // resetView
            // 
            this.resetView.Name = "resetView";
            resources.ApplyResources(this.resetView, "resetView");
            this.resetView.Click += new System.EventHandler(this.resetView_Click);
            // 
            // sidebarPanel
            // 
            resources.ApplyResources(this.sidebarPanel, "sidebarPanel");
            this.sidebarPanel.Controls.Add(this.lstComponents);
            this.sidebarPanel.Controls.Add(this.txt_Search);
            this.sidebarPanel.Name = "sidebarPanel";
            // 
            // lstComponents
            // 
            this.lstComponents.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(230)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.lstComponents, "lstComponents");
            this.lstComponents.FormattingEnabled = true;
            this.lstComponents.Name = "lstComponents";
            this.lstComponents.SelectedIndexChanged += new System.EventHandler(this.lstComponents_SelectedIndexChanged);
            // 
            // txt_Search
            // 
            this.txt_Search.BackColor = System.Drawing.Color.White;
            this.txt_Search.DecimalPlaces = 2;
            this.txt_Search.DecMaxValue = new decimal(new int[] {
            -1,
            -1,
            -1,
            0});
            this.txt_Search.DecMinValue = new decimal(new int[] {
            -1,
            -1,
            -1,
            -2147483648});
            resources.ApplyResources(this.txt_Search, "txt_Search");
            this.txt_Search.FocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txt_Search.Name = "txt_Search";
            this.txt_Search.NormalBackColor = System.Drawing.Color.White;
            this.txt_Search.TextChanged += new System.EventHandler(this.txt_Search_TextChanged);
            // 
            // betterFolderBrowser1
            // 
            this.betterFolderBrowser1.Multiselect = false;
            this.betterFolderBrowser1.RootFolder = "C:\\Users\\xhwong\\Desktop";
            this.betterFolderBrowser1.Title = "Please select a folder...";
            // 
            // ViewerForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(230)))), ((int)(((byte)(255)))));
            this.Controls.Add(this.sidebarPanel);
            this.Controls.Add(this.canvasPanel);
            this.DoubleBuffered = true;
            this.Name = "ViewerForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.canvasMenu.ResumeLayout(false);
            this.sidebarPanel.ResumeLayout(false);
            this.sidebarPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel canvasPanel;
        private System.Windows.Forms.Panel sidebarPanel;
        private System.Windows.Forms.ContextMenuStrip canvasMenu;
        private System.Windows.Forms.ToolStripMenuItem resetView;
        private WK.Libraries.BetterFolderBrowserNS.BetterFolderBrowser betterFolderBrowser1;
        private SRMControl.SRMInputBox txt_Search;
        private System.Windows.Forms.ListBox lstComponents;
    }
}