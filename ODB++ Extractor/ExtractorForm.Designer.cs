
namespace ODB___Extractor
{
    partial class ExtractorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExtractorForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lbl_Path = new SRMControl.SRMLabel();
            this.txt_Path = new SRMControl.SRMInputBox();
            this.btn_BrowseDir = new SRMControl.SRMButton();
            this.btn_BrowseFile = new SRMControl.SRMButton();
            this.gb_ImportODBpp = new SRMControl.SRMGroupBox();
            this.lbl_Step = new SRMControl.SRMLabel();
            this.lbl_Layer = new SRMControl.SRMLabel();
            this.cbo_Step = new SRMControl.SRMComboBox();
            this.cbo_Layer = new SRMControl.SRMComboBox();
            this.dgv_Data = new System.Windows.Forms.DataGridView();
            this.lbl_Statistic = new SRMControl.SRMLabel();
            this.gb_Data = new SRMControl.SRMGroupBox();
            this.chk_FlipYAxis = new SRMControl.SRMCheckBox();
            this.chk_FlipXAxis = new SRMControl.SRMCheckBox();
            this.cbo_Origin = new SRMControl.SRMComboBox();
            this.lbl_Origin = new SRMControl.SRMLabel();
            this.btn_RefreshData = new SRMControl.SRMButton();
            this.btn_PreviewData = new SRMControl.SRMButton();
            this.gb_Export = new SRMControl.SRMGroupBox();
            this.lbl_Status = new SRMControl.SRMLabel();
            this.btn_ExportLayer = new SRMControl.SRMButton();
            this.btn_ExportAllLayer = new SRMControl.SRMButton();
            this.gb_ImportODBpp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Data)).BeginInit();
            this.gb_Data.SuspendLayout();
            this.gb_Export.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbl_Path
            // 
            resources.ApplyResources(this.lbl_Path, "lbl_Path");
            this.lbl_Path.Name = "lbl_Path";
            this.lbl_Path.TextShadowColor = System.Drawing.Color.Gray;
            // 
            // txt_Path
            // 
            this.txt_Path.BackColor = System.Drawing.Color.White;
            this.txt_Path.DecimalPlaces = 2;
            this.txt_Path.DecMaxValue = new decimal(new int[] {
            -1,
            -1,
            -1,
            0});
            this.txt_Path.DecMinValue = new decimal(new int[] {
            -1,
            -1,
            -1,
            -2147483648});
            this.txt_Path.FocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            resources.ApplyResources(this.txt_Path, "txt_Path");
            this.txt_Path.Name = "txt_Path";
            this.txt_Path.NormalBackColor = System.Drawing.Color.White;
            this.txt_Path.ReadOnly = true;
            this.txt_Path.TextChanged += new System.EventHandler(this.txt_Path_TextChanged);
            // 
            // btn_BrowseDir
            // 
            resources.ApplyResources(this.btn_BrowseDir, "btn_BrowseDir");
            this.btn_BrowseDir.Name = "btn_BrowseDir";
            this.btn_BrowseDir.UseVisualStyleBackColor = true;
            this.btn_BrowseDir.Click += new System.EventHandler(this.btn_BrowseDir_Click);
            // 
            // btn_BrowseFile
            // 
            resources.ApplyResources(this.btn_BrowseFile, "btn_BrowseFile");
            this.btn_BrowseFile.Name = "btn_BrowseFile";
            this.btn_BrowseFile.UseVisualStyleBackColor = true;
            this.btn_BrowseFile.Click += new System.EventHandler(this.btn_BrowseFile_Click);
            // 
            // gb_ImportODBpp
            // 
            this.gb_ImportODBpp.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(150)))), ((int)(((byte)(185)))));
            this.gb_ImportODBpp.Controls.Add(this.btn_BrowseFile);
            this.gb_ImportODBpp.Controls.Add(this.lbl_Path);
            this.gb_ImportODBpp.Controls.Add(this.btn_BrowseDir);
            this.gb_ImportODBpp.Controls.Add(this.txt_Path);
            resources.ApplyResources(this.gb_ImportODBpp, "gb_ImportODBpp");
            this.gb_ImportODBpp.Name = "gb_ImportODBpp";
            this.gb_ImportODBpp.TabStop = false;
            // 
            // lbl_Step
            // 
            resources.ApplyResources(this.lbl_Step, "lbl_Step");
            this.lbl_Step.Name = "lbl_Step";
            this.lbl_Step.TextShadowColor = System.Drawing.Color.Gray;
            // 
            // lbl_Layer
            // 
            resources.ApplyResources(this.lbl_Layer, "lbl_Layer");
            this.lbl_Layer.Name = "lbl_Layer";
            this.lbl_Layer.TextShadowColor = System.Drawing.Color.Gray;
            // 
            // cbo_Step
            // 
            this.cbo_Step.BackColor = System.Drawing.Color.White;
            this.cbo_Step.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Step.FocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.cbo_Step.FormattingEnabled = true;
            resources.ApplyResources(this.cbo_Step, "cbo_Step");
            this.cbo_Step.Name = "cbo_Step";
            this.cbo_Step.NormalBackColor = System.Drawing.Color.White;
            this.cbo_Step.SelectedIndexChanged += new System.EventHandler(this.cbo_Step_SelectedIndexChanged);
            // 
            // cbo_Layer
            // 
            this.cbo_Layer.BackColor = System.Drawing.Color.White;
            this.cbo_Layer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Layer.FocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.cbo_Layer.FormattingEnabled = true;
            resources.ApplyResources(this.cbo_Layer, "cbo_Layer");
            this.cbo_Layer.Name = "cbo_Layer";
            this.cbo_Layer.NormalBackColor = System.Drawing.Color.White;
            this.cbo_Layer.SelectedIndexChanged += new System.EventHandler(this.cbo_Layer_SelectedIndexChanged);
            // 
            // dgv_Data
            // 
            this.dgv_Data.AllowUserToAddRows = false;
            this.dgv_Data.AllowUserToDeleteRows = false;
            this.dgv_Data.BackgroundColor = System.Drawing.Color.AliceBlue;
            this.dgv_Data.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.LightSkyBlue;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_Data.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgv_Data.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dgv_Data, "dgv_Data");
            this.dgv_Data.Name = "dgv_Data";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.LightSkyBlue;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_Data.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgv_Data.RowHeadersVisible = false;
            this.dgv_Data.RowTemplate.Height = 24;
            // 
            // lbl_Statistic
            // 
            resources.ApplyResources(this.lbl_Statistic, "lbl_Statistic");
            this.lbl_Statistic.Name = "lbl_Statistic";
            this.lbl_Statistic.TextShadowColor = System.Drawing.Color.Gray;
            // 
            // gb_Data
            // 
            this.gb_Data.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(150)))), ((int)(((byte)(185)))));
            this.gb_Data.Controls.Add(this.chk_FlipYAxis);
            this.gb_Data.Controls.Add(this.chk_FlipXAxis);
            this.gb_Data.Controls.Add(this.cbo_Origin);
            this.gb_Data.Controls.Add(this.lbl_Origin);
            this.gb_Data.Controls.Add(this.btn_RefreshData);
            this.gb_Data.Controls.Add(this.lbl_Statistic);
            this.gb_Data.Controls.Add(this.dgv_Data);
            this.gb_Data.Controls.Add(this.lbl_Step);
            this.gb_Data.Controls.Add(this.cbo_Layer);
            this.gb_Data.Controls.Add(this.lbl_Layer);
            this.gb_Data.Controls.Add(this.cbo_Step);
            resources.ApplyResources(this.gb_Data, "gb_Data");
            this.gb_Data.Name = "gb_Data";
            this.gb_Data.TabStop = false;
            // 
            // chk_FlipYAxis
            // 
            this.chk_FlipYAxis.CheckedColor = System.Drawing.Color.GreenYellow;
            resources.ApplyResources(this.chk_FlipYAxis, "chk_FlipYAxis");
            this.chk_FlipYAxis.Name = "chk_FlipYAxis";
            this.chk_FlipYAxis.Selected = false;
            this.chk_FlipYAxis.SelectedBorderColor = System.Drawing.Color.Red;
            this.chk_FlipYAxis.UnCheckedColor = System.Drawing.Color.Red;
            this.chk_FlipYAxis.UseVisualStyleBackColor = true;
            this.chk_FlipYAxis.CheckedChanged += new System.EventHandler(this.chk_FlipYAxis_CheckedChanged);
            // 
            // chk_FlipXAxis
            // 
            this.chk_FlipXAxis.CheckedColor = System.Drawing.Color.GreenYellow;
            resources.ApplyResources(this.chk_FlipXAxis, "chk_FlipXAxis");
            this.chk_FlipXAxis.Name = "chk_FlipXAxis";
            this.chk_FlipXAxis.Selected = false;
            this.chk_FlipXAxis.SelectedBorderColor = System.Drawing.Color.Red;
            this.chk_FlipXAxis.UnCheckedColor = System.Drawing.Color.Red;
            this.chk_FlipXAxis.UseVisualStyleBackColor = true;
            this.chk_FlipXAxis.CheckedChanged += new System.EventHandler(this.chk_FlipXAxis_CheckedChanged);
            // 
            // cbo_Origin
            // 
            this.cbo_Origin.BackColor = System.Drawing.Color.White;
            this.cbo_Origin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Origin.FocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.cbo_Origin.FormattingEnabled = true;
            resources.ApplyResources(this.cbo_Origin, "cbo_Origin");
            this.cbo_Origin.Name = "cbo_Origin";
            this.cbo_Origin.NormalBackColor = System.Drawing.Color.White;
            this.cbo_Origin.SelectedIndexChanged += new System.EventHandler(this.cbo_Origin_SelectedIndexChanged);
            // 
            // lbl_Origin
            // 
            resources.ApplyResources(this.lbl_Origin, "lbl_Origin");
            this.lbl_Origin.Name = "lbl_Origin";
            this.lbl_Origin.TextShadowColor = System.Drawing.Color.Gray;
            // 
            // btn_RefreshData
            // 
            resources.ApplyResources(this.btn_RefreshData, "btn_RefreshData");
            this.btn_RefreshData.Name = "btn_RefreshData";
            this.btn_RefreshData.UseVisualStyleBackColor = true;
            this.btn_RefreshData.Click += new System.EventHandler(this.btn_RefreshData_Click);
            // 
            // btn_PreviewData
            // 
            resources.ApplyResources(this.btn_PreviewData, "btn_PreviewData");
            this.btn_PreviewData.Name = "btn_PreviewData";
            this.btn_PreviewData.UseVisualStyleBackColor = true;
            this.btn_PreviewData.Click += new System.EventHandler(this.btn_PreviewData_Click);
            // 
            // gb_Export
            // 
            this.gb_Export.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(150)))), ((int)(((byte)(185)))));
            this.gb_Export.Controls.Add(this.btn_PreviewData);
            this.gb_Export.Controls.Add(this.lbl_Status);
            this.gb_Export.Controls.Add(this.btn_ExportLayer);
            this.gb_Export.Controls.Add(this.btn_ExportAllLayer);
            resources.ApplyResources(this.gb_Export, "gb_Export");
            this.gb_Export.Name = "gb_Export";
            this.gb_Export.TabStop = false;
            // 
            // lbl_Status
            // 
            resources.ApplyResources(this.lbl_Status, "lbl_Status");
            this.lbl_Status.Name = "lbl_Status";
            this.lbl_Status.TextShadowColor = System.Drawing.Color.Gray;
            // 
            // btn_ExportLayer
            // 
            resources.ApplyResources(this.btn_ExportLayer, "btn_ExportLayer");
            this.btn_ExportLayer.Name = "btn_ExportLayer";
            this.btn_ExportLayer.UseVisualStyleBackColor = true;
            this.btn_ExportLayer.Click += new System.EventHandler(this.btn_ExportLayer_Click);
            // 
            // btn_ExportAllLayer
            // 
            resources.ApplyResources(this.btn_ExportAllLayer, "btn_ExportAllLayer");
            this.btn_ExportAllLayer.Name = "btn_ExportAllLayer";
            this.btn_ExportAllLayer.UseVisualStyleBackColor = true;
            this.btn_ExportAllLayer.Click += new System.EventHandler(this.btn_ExportAllLayer_Click);
            // 
            // ExtractorForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(230)))), ((int)(((byte)(255)))));
            this.Controls.Add(this.gb_Export);
            this.Controls.Add(this.gb_Data);
            this.Controls.Add(this.gb_ImportODBpp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ExtractorForm";
            this.ShowIcon = false;
            this.gb_ImportODBpp.ResumeLayout(false);
            this.gb_ImportODBpp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Data)).EndInit();
            this.gb_Data.ResumeLayout(false);
            this.gb_Data.PerformLayout();
            this.gb_Export.ResumeLayout(false);
            this.gb_Export.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private SRMControl.SRMLabel lbl_Path;
        private SRMControl.SRMInputBox txt_Path;
        private SRMControl.SRMButton btn_BrowseDir;
        private SRMControl.SRMButton btn_BrowseFile;
        private SRMControl.SRMGroupBox gb_ImportODBpp;
        private SRMControl.SRMLabel lbl_Step;
        private SRMControl.SRMLabel lbl_Layer;
        private SRMControl.SRMComboBox cbo_Step;
        private SRMControl.SRMComboBox cbo_Layer;
        private System.Windows.Forms.DataGridView dgv_Data;
        private SRMControl.SRMLabel lbl_Statistic;
        private SRMControl.SRMGroupBox gb_Data;
        private SRMControl.SRMGroupBox gb_Export;
        private SRMControl.SRMLabel lbl_Status;
        private SRMControl.SRMButton btn_ExportLayer;
        private SRMControl.SRMButton btn_ExportAllLayer;
        private SRMControl.SRMButton btn_RefreshData;
        private SRMControl.SRMComboBox cbo_Origin;
        private SRMControl.SRMLabel lbl_Origin;
        private SRMControl.SRMCheckBox chk_FlipXAxis;
        private SRMControl.SRMCheckBox chk_FlipYAxis;
        private SRMControl.SRMButton btn_PreviewData;
    }
}
