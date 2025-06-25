using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClippedImgToWSLPath
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private Timer clipboardTimer;
        private string savePath = @"C:\ClipboardImages";
        private string lastClipboardHash = "";

        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll")]
        static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        const int WM_DRAWCLIPBOARD = 0x308;
        const int WM_CHANGECBCHAIN = 0x30D;

        IntPtr nextClipboardViewer;

        public MainForm()
        {
            InitializeComponent();
            SetupSystemTray();
            
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            nextClipboardViewer = SetClipboardViewer(this.Handle);
            
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        private void SetupSystemTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "Clipboard Image to WSL Path";
            trayIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();
            
            var settingsItem = new ToolStripMenuItem("設定");
            settingsItem.Click += (s, e) => ShowSettingsDialog();
            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("終了");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.DoubleClick += (s, e) => ShowSettingsDialog();
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    HandleClipboardChange();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                        nextClipboardViewer = m.LParam;
                    else
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void HandleClipboardChange()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    Image image = Clipboard.GetImage();
                    if (image != null)
                    {
                        string hash = GetImageHash(image);
                        if (hash != lastClipboardHash)
                        {
                            lastClipboardHash = hash;
                            SaveImageAndConvertPath(image);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowBalloonTip("エラー", $"クリップボード処理中にエラーが発生しました: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private string GetImageHash(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                var bytes = ms.ToArray();
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(bytes);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void SaveImageAndConvertPath(Image image)
        {
            try
            {
                string fileName = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(savePath, fileName);
                
                image.Save(filePath, ImageFormat.Png);
                
                string wslPath = ConvertToWSLPath(filePath);
                
                Clipboard.SetText(wslPath);
                
                ShowBalloonTip("画像を保存しました", 
                    $"保存先: {filePath}\nWSLパス: {wslPath}\n(WSLパスをクリップボードにコピーしました)", 
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowBalloonTip("エラー", $"画像の保存に失敗しました: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private string ConvertToWSLPath(string windowsPath)
        {
            string path = windowsPath.Replace('\\', '/');
            
            if (path.Length >= 2 && path[1] == ':')
            {
                char driveLetter = char.ToLower(path[0]);
                path = "/mnt/" + driveLetter + path.Substring(2);
            }
            
            return path;
        }

        private void ShowBalloonTip(string title, string text, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(3000, title, text, icon);
        }

        private void ShowSettingsDialog()
        {
            using (var dialog = new SettingsDialog(savePath))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    savePath = dialog.SavePath;
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }
                }
            }
        }

        private void ExitApplication()
        {
            ChangeClipboardChain(this.Handle, nextClipboardViewer);
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
            }
            else
            {
                ChangeClipboardChain(this.Handle, nextClipboardViewer);
            }
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "MainForm";
            this.Text = "Clipboard Image to WSL Path";
            this.ResumeLayout(false);
        }
    }
}