using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClippedImgToWSLPath
{
    public class SettingsDialog : Form
    {
        private TextBox pathTextBox = null!;
        private Button browseButton = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;
        private Label pathLabel = null!;

        public string SavePath { get; private set; }

        public SettingsDialog(string currentPath)
        {
            InitializeComponent();
            SavePath = currentPath;
            pathTextBox.Text = currentPath;
        }

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(500, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            pathLabel = new Label
            {
                Text = "Save Location:",
                Location = new Point(20, 20),
                Size = new Size(100, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pathTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(350, 25),
                ReadOnly = true
            };

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(380, 48),
                Size = new Size(80, 25)
            };
            browseButton.Click += BrowseButton_Click;

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(280, 100),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(380, 100),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(pathLabel);
            this.Controls.Add(pathTextBox);
            this.Controls.Add(browseButton);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void BrowseButton_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder to save images";
                dialog.SelectedPath = pathTextBox.Text;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    pathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            SavePath = pathTextBox.Text;
        }
    }
}