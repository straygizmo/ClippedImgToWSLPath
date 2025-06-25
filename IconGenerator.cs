using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ClippedImgToWSLPath
{
    public static class IconGenerator
    {
        public static void GenerateIcon()
        {
            int size = 256;
            using (Bitmap bitmap = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // 背景の円
                using (Brush bgBrush = new LinearGradientBrush(
                    new Point(0, 0), new Point(size, size),
                    Color.FromArgb(52, 152, 219), Color.FromArgb(41, 128, 185)))
                {
                    g.FillEllipse(bgBrush, 10, 10, size - 20, size - 20);
                }

                // クリップボードアイコン
                int clipWidth = size / 3;
                int clipHeight = (int)(size / 2.5);
                int clipX = (size - clipWidth) / 2;
                int clipY = size / 4;

                using (Pen pen = new Pen(Color.White, 8))
                {
                    // クリップボード本体
                    Rectangle clipRect = new Rectangle(clipX, clipY, clipWidth, clipHeight);
                    g.FillRectangle(Brushes.White, clipRect);
                    
                    // クリップ部分
                    int clipTopWidth = clipWidth / 3;
                    int clipTopHeight = size / 8;
                    Rectangle clipTop = new Rectangle(
                        clipX + (clipWidth - clipTopWidth) / 2, 
                        clipY - clipTopHeight / 2, 
                        clipTopWidth, 
                        clipTopHeight);
                    g.FillRectangle(Brushes.White, clipTop);
                }

                // WSLテキスト
                using (Font font = new Font("Arial", size / 8, FontStyle.Bold))
                {
                    string text = "WSL";
                    SizeF textSize = g.MeasureString(text, font);
                    float textX = (size - textSize.Width) / 2;
                    float textY = size * 0.65f;
                    
                    g.DrawString(text, font, Brushes.White, textX, textY);
                }

                // 複数サイズのアイコンを作成
                CreateIconFile(bitmap, "icon.ico");
            }
        }

        private static void CreateIconFile(Bitmap source, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                // ICONDIRヘッダー
                fs.WriteByte(0); fs.WriteByte(0); // Reserved
                fs.WriteByte(1); fs.WriteByte(0); // Type (1 = Icon)
                fs.WriteByte(3); fs.WriteByte(0); // Count (3 images)

                int offset = 6 + (16 * 3); // Header + 3 ICONDIRENTRY structures

                // 各サイズのオフセットを計算
                int[] sizes = { 16, 32, 48 };
                int[] offsets = new int[sizes.Length];
                offsets[0] = offset;

                // 各サイズのバイト数を計算
                for (int i = 1; i < sizes.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (Bitmap resized = new Bitmap(source, sizes[i - 1], sizes[i - 1]))
                        {
                            resized.Save(ms, ImageFormat.Png);
                            offsets[i] = offsets[i - 1] + (int)ms.Length;
                        }
                    }
                }

                // ICONDIRENTRY structures
                for (int i = 0; i < sizes.Length; i++)
                {
                    fs.WriteByte((byte)sizes[i]); // Width
                    fs.WriteByte((byte)sizes[i]); // Height
                    fs.WriteByte(0); // Color palette
                    fs.WriteByte(0); // Reserved
                    fs.WriteByte(1); fs.WriteByte(0); // Color planes
                    fs.WriteByte(32); fs.WriteByte(0); // Bits per pixel

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (Bitmap resized = new Bitmap(source, sizes[i], sizes[i]))
                        {
                            resized.Save(ms, ImageFormat.Png);
                            byte[] sizeBytes = BitConverter.GetBytes((int)ms.Length);
                            fs.Write(sizeBytes, 0, 4); // Size
                            byte[] offsetBytes = BitConverter.GetBytes(offsets[i]);
                            fs.Write(offsetBytes, 0, 4); // Offset
                        }
                    }
                }

                // 実際の画像データ
                for (int i = 0; i < sizes.Length; i++)
                {
                    using (Bitmap resized = new Bitmap(source, sizes[i], sizes[i]))
                    {
                        resized.Save(fs, ImageFormat.Png);
                    }
                }
            }
        }
    }
}