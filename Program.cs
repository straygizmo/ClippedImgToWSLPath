using System;
using System.IO;
using System.Windows.Forms;

namespace ClippedImgToWSLPath
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // アイコンファイルが存在しない場合は生成
            if (!File.Exists("icon.ico"))
            {
                IconGenerator.GenerateIcon();
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}