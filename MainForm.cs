using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;

namespace ClippedImgToWSLPath
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon = null!;
        private System.Windows.Forms.Timer clipboardTimer;
        private string savePath = Path.Combine(Application.StartupPath, "ClipboardImages");
        private string lastClipboardHash = "";
        private string logPath = Path.Combine(Application.StartupPath, "clipboard_log.txt");
        private bool isProcessingClipboard = false;
        private DateTime lastClipboardTime = DateTime.MinValue;
        private bool enableLogging = false; // ログ出力のオン/オフ
        private bool timerEnabled = true; // Timer functionality on/off

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
            
            // Hide the form before doing anything else
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            
            SetupSystemTray();
            
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            nextClipboardViewer = SetClipboardViewer(this.Handle);
            
            // タイマーを設定して定期的にクリップボードをチェック
            clipboardTimer = new System.Windows.Forms.Timer();
            clipboardTimer.Interval = 1000; // 1秒ごと
            clipboardTimer.Tick += ClipboardTimer_Tick;
            clipboardTimer.Start();
        }

        private void SetupSystemTray()
        {
            trayIcon = new NotifyIcon();
            
            // アイコンファイルから読み込み、存在しない場合はデフォルトを使用
            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
            {
                trayIcon.Icon = new Icon(iconPath);
            }
            else
            {
                // 埋め込みリソースから読み込みを試みる
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "ClippedImgToWSLPath.icon.ico";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        trayIcon.Icon = new Icon(stream);
                    }
                    else
                    {
                        trayIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            
            trayIcon.Text = "Clipboard Image to WSL Path";
            trayIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();
            
            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => ShowSettingsDialog();
            contextMenu.Items.Add(settingsItem);
            
            var timerItem = new ToolStripMenuItem("Enable Timer");
            timerItem.Checked = timerEnabled;
            timerItem.Click += (s, e) => 
            {
                timerEnabled = !timerEnabled;
                timerItem.Checked = timerEnabled;
                if (timerEnabled)
                {
                    clipboardTimer.Start();
                    ShowBalloonTip("Timer Enabled", "Clipboard monitoring is now active", ToolTipIcon.Info);
                    WriteLog("Timer enabled");
                }
                else
                {
                    clipboardTimer.Stop();
                    ShowBalloonTip("Timer Disabled", "Clipboard monitoring is now inactive", ToolTipIcon.Info);
                    WriteLog("Timer disabled");
                }
            };
            contextMenu.Items.Add(timerItem);
            
            var loggingItem = new ToolStripMenuItem("Enable Logging");
            loggingItem.Checked = enableLogging;
            loggingItem.Click += (s, e) => 
            {
                enableLogging = !enableLogging;
                loggingItem.Checked = enableLogging;
                if (enableLogging)
                {
                    WriteLog("Logging enabled");
                }
            };
            contextMenu.Items.Add(loggingItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
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
            // 短時間に連続してイベントが発生した場合は無視
            if ((DateTime.Now - lastClipboardTime).TotalMilliseconds < 200)
            {
                WriteLog("HandleClipboardChange called - ignored (too soon)");
                return;
            }
            
            lastClipboardTime = DateTime.Now;
            WriteLog("HandleClipboardChange called - processing");
        }

        private void ClipboardTimer_Tick(object? sender, EventArgs e)
        {
            if (isProcessingClipboard || !timerEnabled) return;
            
            try
            {
                isProcessingClipboard = true;
                
                if (Clipboard.ContainsImage())
                {
                    WriteLog("Timer: Clipboard contains image");
                    Image? image = Clipboard.GetImage();
                    
                    if (image != null)
                    {
                        string hash = GetImageHash(image);
                        
                        if (hash != lastClipboardHash)
                        {
                            WriteLog($"Timer: New image found: {image.Width}x{image.Height}");
                            lastClipboardHash = hash;
                            SaveImageAndConvertPath(image);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Timer Exception: {ex.Message}");
            }
            finally
            {
                isProcessingClipboard = false;
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
                
                ShowBalloonTip("Image Saved", 
                    $"Saved to: {filePath}\nWSL Path: {wslPath}\n(WSL path copied to clipboard)", 
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ShowBalloonTip("Error", $"Failed to save image: {ex.Message}", ToolTipIcon.Error);
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
            clipboardTimer?.Stop();
            clipboardTimer?.Dispose();
            ChangeClipboardChain(this.Handle, nextClipboardViewer);
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void SetVisibleCore(bool value)
        {
            // Prevent the form from becoming visible on startup
            if (!this.IsHandleCreated)
            {
                this.CreateHandle();
                value = false;
            }
            base.SetVisibleCore(value);
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

        private void WriteLog(string message)
        {
            if (!enableLogging) return;
            
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logMessage);
            }
            catch
            {
                // ログ書き込みエラーは無視
            }
        }

        private Image GetImageFromDIB(MemoryStream dibStream)
        {
            try
            {
                WriteLog("Converting DIB to Image");
                byte[] dibBytes = dibStream.ToArray();
                
                // DIBヘッダーのサイズを取得
                int headerSize = BitConverter.ToInt32(dibBytes, 0);
                int width = BitConverter.ToInt32(dibBytes, 4);
                int height = BitConverter.ToInt32(dibBytes, 8);
                short planes = BitConverter.ToInt16(dibBytes, 12);
                short bpp = BitConverter.ToInt16(dibBytes, 14);
                
                WriteLog($"DIB info: HeaderSize={headerSize}, Width={width}, Height={height}, BPP={bpp}");
                
                // BitmapFileHeaderを作成
                byte[] bmpBytes = new byte[14 + dibBytes.Length];
                bmpBytes[0] = 0x42; // 'B'
                bmpBytes[1] = 0x4D; // 'M'
                BitConverter.GetBytes(bmpBytes.Length).CopyTo(bmpBytes, 2);
                BitConverter.GetBytes(14 + headerSize + (bpp <= 8 ? (1 << bpp) * 4 : 0)).CopyTo(bmpBytes, 10);
                
                // DIBデータをコピー
                Array.Copy(dibBytes, 0, bmpBytes, 14, dibBytes.Length);
                
                using (var ms = new MemoryStream(bmpBytes))
                {
                    return new Bitmap(ms);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"GetImageFromDIB error: {ex.Message}");
                return null!;
            }
        }
    }
}