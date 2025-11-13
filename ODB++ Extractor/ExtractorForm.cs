using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ODB___Extractor
{
    public partial class ExtractorForm : Form
    {
        public ExtractorForm()
        {
            InitializeComponent();
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
            using (var dialog = new FolderBrowserDialog())
            {
                SetFolderBrowserSelectedPath(dialog, txt_Path.Text);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txt_Path.Text = dialog.SelectedPath;
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

        private static void SetFolderBrowserSelectedPath(FolderBrowserDialog dialog, string path)
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
                    dialog.SelectedPath = candidate;
                }
            }
            catch
            {
                // ignore invalid paths
            }
        }

        private void cbo_Step_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbo_Layer_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btn_ExportAllLayer_Click(object sender, EventArgs e)
        {

        }

        private void btn_ExportLayer_Click(object sender, EventArgs e)
        {

        }

        private void txt_Path_TextChanged(object sender, EventArgs e)
        {

        }

        private void btn_RefreshData_Click(object sender, EventArgs e)
        {

        }
    }
}
