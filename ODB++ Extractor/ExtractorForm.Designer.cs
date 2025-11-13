
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
            this.lbl_Path = new System.Windows.Forms.Label();
            this.txt_Path = new System.Windows.Forms.TextBox();
            this.btn_Browse = new System.Windows.Forms.Button();
            this.btn_Refresh = new System.Windows.Forms.Button();
            this.gb_ImportODBpp = new System.Windows.Forms.GroupBox();
            this.lbl_Step = new System.Windows.Forms.Label();
            this.lbl_Layer = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.dgv_Data = new System.Windows.Forms.DataGridView();
            this.lbl_Statistic = new System.Windows.Forms.Label();
            this.gb_Data = new System.Windows.Forms.GroupBox();
            this.gb_Export = new System.Windows.Forms.GroupBox();
            this.lbl_Status = new System.Windows.Forms.Label();
            this.btn_ExportLayer = new System.Windows.Forms.Button();
            this.btn_ExportAllLayer = new System.Windows.Forms.Button();
            this.gb_ImportODBpp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Data)).BeginInit();
            this.gb_Data.SuspendLayout();
            this.gb_Export.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbl_Path
            // 
            this.lbl_Path.AutoSize = true;
            this.lbl_Path.Location = new System.Drawing.Point(15, 32);
            this.lbl_Path.Name = "lbl_Path";
            this.lbl_Path.Size = new System.Drawing.Size(41, 17);
            this.lbl_Path.TabIndex = 1;
            this.lbl_Path.Text = "Path:";
            // 
            // txt_Path
            // 
            this.txt_Path.Location = new System.Drawing.Point(62, 29);
            this.txt_Path.Name = "txt_Path";
            this.txt_Path.Size = new System.Drawing.Size(491, 22);
            this.txt_Path.TabIndex = 2;
            // 
            // btn_Browse
            // 
            this.btn_Browse.Location = new System.Drawing.Point(559, 21);
            this.btn_Browse.Name = "btn_Browse";
            this.btn_Browse.Size = new System.Drawing.Size(88, 38);
            this.btn_Browse.TabIndex = 3;
            this.btn_Browse.Text = "Browse";
            this.btn_Browse.UseVisualStyleBackColor = true;
            // 
            // btn_Refresh
            // 
            this.btn_Refresh.Location = new System.Drawing.Point(653, 21);
            this.btn_Refresh.Name = "btn_Refresh";
            this.btn_Refresh.Size = new System.Drawing.Size(88, 38);
            this.btn_Refresh.TabIndex = 4;
            this.btn_Refresh.Text = "Refresh";
            this.btn_Refresh.UseVisualStyleBackColor = true;
            // 
            // gb_ImportODBpp
            // 
            this.gb_ImportODBpp.Controls.Add(this.btn_Refresh);
            this.gb_ImportODBpp.Controls.Add(this.lbl_Path);
            this.gb_ImportODBpp.Controls.Add(this.btn_Browse);
            this.gb_ImportODBpp.Controls.Add(this.txt_Path);
            this.gb_ImportODBpp.Location = new System.Drawing.Point(12, 12);
            this.gb_ImportODBpp.Name = "gb_ImportODBpp";
            this.gb_ImportODBpp.Size = new System.Drawing.Size(760, 70);
            this.gb_ImportODBpp.TabIndex = 5;
            this.gb_ImportODBpp.TabStop = false;
            this.gb_ImportODBpp.Text = "Import ODB++ Design";
            // 
            // lbl_Step
            // 
            this.lbl_Step.AutoSize = true;
            this.lbl_Step.Location = new System.Drawing.Point(16, 34);
            this.lbl_Step.Name = "lbl_Step";
            this.lbl_Step.Size = new System.Drawing.Size(41, 17);
            this.lbl_Step.TabIndex = 6;
            this.lbl_Step.Text = "Step:";
            // 
            // lbl_Layer
            // 
            this.lbl_Layer.AutoSize = true;
            this.lbl_Layer.Location = new System.Drawing.Point(300, 34);
            this.lbl_Layer.Name = "lbl_Layer";
            this.lbl_Layer.Size = new System.Drawing.Size(48, 17);
            this.lbl_Layer.TabIndex = 7;
            this.lbl_Layer.Text = "Layer:";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(63, 30);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(200, 24);
            this.comboBox1.TabIndex = 8;
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(354, 30);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(200, 24);
            this.comboBox2.TabIndex = 9;
            // 
            // dgv_Data
            // 
            this.dgv_Data.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_Data.Location = new System.Drawing.Point(19, 69);
            this.dgv_Data.Name = "dgv_Data";
            this.dgv_Data.RowHeadersWidth = 51;
            this.dgv_Data.RowTemplate.Height = 24;
            this.dgv_Data.Size = new System.Drawing.Size(723, 273);
            this.dgv_Data.TabIndex = 10;
            // 
            // lbl_Statistic
            // 
            this.lbl_Statistic.AutoSize = true;
            this.lbl_Statistic.Location = new System.Drawing.Point(16, 345);
            this.lbl_Statistic.Name = "lbl_Statistic";
            this.lbl_Statistic.Size = new System.Drawing.Size(57, 17);
            this.lbl_Statistic.TabIndex = 11;
            this.lbl_Statistic.Text = "Statistic";
            // 
            // gb_Data
            // 
            this.gb_Data.Controls.Add(this.lbl_Statistic);
            this.gb_Data.Controls.Add(this.dgv_Data);
            this.gb_Data.Controls.Add(this.lbl_Step);
            this.gb_Data.Controls.Add(this.comboBox2);
            this.gb_Data.Controls.Add(this.lbl_Layer);
            this.gb_Data.Controls.Add(this.comboBox1);
            this.gb_Data.Location = new System.Drawing.Point(11, 88);
            this.gb_Data.Name = "gb_Data";
            this.gb_Data.Size = new System.Drawing.Size(760, 377);
            this.gb_Data.TabIndex = 12;
            this.gb_Data.TabStop = false;
            this.gb_Data.Text = "Component Data";
            // 
            // gb_Export
            // 
            this.gb_Export.Controls.Add(this.lbl_Status);
            this.gb_Export.Controls.Add(this.btn_ExportLayer);
            this.gb_Export.Controls.Add(this.btn_ExportAllLayer);
            this.gb_Export.Location = new System.Drawing.Point(11, 471);
            this.gb_Export.Name = "gb_Export";
            this.gb_Export.Size = new System.Drawing.Size(760, 70);
            this.gb_Export.TabIndex = 13;
            this.gb_Export.TabStop = false;
            this.gb_Export.Text = "Export";
            // 
            // lbl_Status
            // 
            this.lbl_Status.AutoSize = true;
            this.lbl_Status.Location = new System.Drawing.Point(15, 32);
            this.lbl_Status.Name = "lbl_Status";
            this.lbl_Status.Size = new System.Drawing.Size(48, 17);
            this.lbl_Status.TabIndex = 12;
            this.lbl_Status.Text = "Status";
            // 
            // btn_ExportLayer
            // 
            this.btn_ExportLayer.Location = new System.Drawing.Point(582, 21);
            this.btn_ExportLayer.Name = "btn_ExportLayer";
            this.btn_ExportLayer.Size = new System.Drawing.Size(160, 38);
            this.btn_ExportLayer.TabIndex = 5;
            this.btn_ExportLayer.Text = "Export Current Layer";
            this.btn_ExportLayer.UseVisualStyleBackColor = true;
            // 
            // btn_ExportAllLayer
            // 
            this.btn_ExportAllLayer.Location = new System.Drawing.Point(416, 21);
            this.btn_ExportAllLayer.Name = "btn_ExportAllLayer";
            this.btn_ExportAllLayer.Size = new System.Drawing.Size(160, 38);
            this.btn_ExportAllLayer.TabIndex = 5;
            this.btn_ExportAllLayer.Text = "Export All Layer";
            this.btn_ExportAllLayer.UseVisualStyleBackColor = true;
            // 
            // ExtractorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(782, 553);
            this.Controls.Add(this.gb_Export);
            this.Controls.Add(this.gb_Data);
            this.Controls.Add(this.gb_ImportODBpp);
            this.Name = "ExtractorForm";
            this.Text = "ODB++ Extractor";
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

        private System.Windows.Forms.Label lbl_Path;
        private System.Windows.Forms.TextBox txt_Path;
        private System.Windows.Forms.Button btn_Browse;
        private System.Windows.Forms.Button btn_Refresh;
        private System.Windows.Forms.GroupBox gb_ImportODBpp;
        private System.Windows.Forms.Label lbl_Step;
        private System.Windows.Forms.Label lbl_Layer;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.DataGridView dgv_Data;
        private System.Windows.Forms.Label lbl_Statistic;
        private System.Windows.Forms.GroupBox gb_Data;
        private System.Windows.Forms.GroupBox gb_Export;
        private System.Windows.Forms.Label lbl_Status;
        private System.Windows.Forms.Button btn_ExportLayer;
        private System.Windows.Forms.Button btn_ExportAllLayer;
    }
}

