using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FileCopier
{
    public class MainForm : Form
    {
        private TextBox txtSelectedFile;
        private Button btnBrowse;
        private TextBox txtRemotePath;
        private Button btnChangeRemote;
        private CheckBox chkDateSubfolder;
        private Button btnCopy;
        private ProgressBar progressBar;
        private Label lblStatus;
        private BackgroundWorker copyWorker;
        private AppSettings settings;

        public MainForm()
        {
            settings = AppSettings.Load();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "File Copier";
            Size = new Size(600, 320);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            var lblFile = new Label
            {
                Text = "Selected File:",
                Location = new Point(15, 20),
                AutoSize = true
            };

            txtSelectedFile = new TextBox
            {
                Location = new Point(15, 40),
                Size = new Size(450, 25),
                ReadOnly = true
            };

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(475, 38),
                Size = new Size(90, 27)
            };
            btnBrowse.Click += BtnBrowse_Click;

            var lblRemote = new Label
            {
                Text = "Remote Destination (UNC):",
                Location = new Point(15, 80),
                AutoSize = true
            };

            txtRemotePath = new TextBox
            {
                Location = new Point(15, 100),
                Size = new Size(450, 25),
                Text = settings.RemoteFolder
            };

            btnChangeRemote = new Button
            {
                Text = "Browse...",
                Location = new Point(475, 98),
                Size = new Size(90, 27)
            };
            btnChangeRemote.Click += BtnChangeRemote_Click;

            chkDateSubfolder = new CheckBox
            {
                Text = "Create date subfolder (yyyy-MM-dd)",
                Location = new Point(15, 135),
                AutoSize = true,
                Checked = settings.CreateSubfolderByDate
            };

            btnCopy = new Button
            {
                Text = "Copy File",
                Location = new Point(15, 175),
                Size = new Size(550, 35),
                Enabled = false
            };
            btnCopy.Click += BtnCopy_Click;

            progressBar = new ProgressBar
            {
                Location = new Point(15, 220),
                Size = new Size(550, 25),
                Minimum = 0,
                Maximum = 100
            };

            lblStatus = new Label
            {
                Text = "Ready. Select a file to copy.",
                Location = new Point(15, 252),
                Size = new Size(550, 20)
            };

            Controls.AddRange(new Control[]
            {
                lblFile, txtSelectedFile, btnBrowse,
                lblRemote, txtRemotePath, btnChangeRemote,
                chkDateSubfolder,
                btnCopy, progressBar, lblStatus
            });

            copyWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            copyWorker.DoWork += CopyWorker_DoWork;
            copyWorker.ProgressChanged += CopyWorker_ProgressChanged;
            copyWorker.RunWorkerCompleted += CopyWorker_RunWorkerCompleted;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select a file to copy",
                Filter = "All Files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtSelectedFile.Text = dialog.FileName;
                btnCopy.Enabled = true;
            }
        }

        private void BtnChangeRemote_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select remote destination folder",
                SelectedPath = txtRemotePath.Text,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtRemotePath.Text = dialog.SelectedPath;
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSelectedFile.Text))
            {
                MessageBox.Show("Please select a file first.", "No File Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRemotePath.Text))
            {
                MessageBox.Show("Please enter a remote destination path.", "No Destination",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save settings
            settings.RemoteFolder = txtRemotePath.Text;
            settings.CreateSubfolderByDate = chkDateSubfolder.Checked;
            settings.Save();

            // Build destination path
            var destFolder = txtRemotePath.Text.TrimEnd('\\');
            if (chkDateSubfolder.Checked)
                destFolder = Path.Combine(destFolder, DateTime.Now.ToString("yyyy-MM-dd"));

            var sourceFile = txtSelectedFile.Text;
            var destFile = Path.Combine(destFolder, Path.GetFileName(sourceFile));

            SetControlsEnabled(false);
            progressBar.Value = 0;
            lblStatus.Text = "Copying...";

            copyWorker.RunWorkerAsync(new CopyArgs(sourceFile, destFile, destFolder));
        }

        private void CopyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = (CopyArgs)e.Argument;

            // Create destination directory if needed
            Directory.CreateDirectory(args.DestFolder);

            var sourceInfo = new FileInfo(args.SourceFile);
            long totalBytes = sourceInfo.Length;
            long copiedBytes = 0;

            using var sourceStream = new FileStream(args.SourceFile, FileMode.Open, FileAccess.Read);
            using var destStream = new FileStream(args.DestFile, FileMode.Create, FileAccess.Write);

            var buffer = new byte[81920]; // 80KB buffer
            int bytesRead;

            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (copyWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                destStream.Write(buffer, 0, bytesRead);
                copiedBytes += bytesRead;

                int progress = totalBytes > 0
                    ? (int)(copiedBytes * 100 / totalBytes)
                    : 100;
                copyWorker.ReportProgress(progress);
            }
        }

        private void CopyWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            lblStatus.Text = $"Copying... {e.ProgressPercentage}%";
        }

        private void CopyWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetControlsEnabled(true);

            if (e.Error != null)
            {
                progressBar.Value = 0;
                lblStatus.Text = "Copy failed.";
                MessageBox.Show($"Error copying file:\n\n{e.Error.Message}",
                    "Copy Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Cancelled)
            {
                progressBar.Value = 0;
                lblStatus.Text = "Copy cancelled.";
            }
            else
            {
                progressBar.Value = 100;
                lblStatus.Text = "Copy completed successfully!";
                MessageBox.Show("File copied successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnBrowse.Enabled = enabled;
            btnCopy.Enabled = enabled;
            btnChangeRemote.Enabled = enabled;
            txtRemotePath.Enabled = enabled;
            chkDateSubfolder.Enabled = enabled;
        }

        private record CopyArgs(string SourceFile, string DestFile, string DestFolder);
    }
}
