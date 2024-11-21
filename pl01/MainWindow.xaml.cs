using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Tesseract;

namespace pl01
{
    public partial class MainWindow : Window
    {
        private System.Windows.Point startPoint; // マウスポインターでドラッグし始めた地点
        private System.Windows.Shapes.Rectangle rectangle; // キャプチャー範囲
        private bool isDrawing = false; // ドラッグしているかの判定
        private string recognizedText = string.Empty; // OCRで認識したテキスト

        public MainWindow()
        {
            InitializeComponent();
            SetDesktopBackground(); // デスクトップ背景を表示
            this.Topmost = true; // ウィンドウを最前面に表示
        }

        private void SetDesktopBackground()
        {
            using (Bitmap desktopBitmap = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight))
            {
                using (Graphics g = Graphics.FromImage(desktopBitmap))
                {
                    g.CopyFromScreen(0, 0, 0, 0, desktopBitmap.Size); // デスクトップのスクリーンショットを取得
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    desktopBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    CanvasOverlay.Background = new ImageBrush(bitmapImage); // 背景に設定
                }
            }
        }

        private void CaptureScreenButton_Click(object sender, RoutedEventArgs e)
        {
            CanvasOverlay.Children.Clear();
            MessageBox.Show("クリック成功"); // クリック確認
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) // 左ボタンが押された場合
            {
                isDrawing = true;
                startPoint = e.GetPosition(this);
                rectangle = new System.Windows.Shapes.Rectangle
                {
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 2 // ピクセル単位のこと
                };
                CanvasOverlay.Children.Add(rectangle);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                System.Windows.Point currentPoint = e.GetPosition(this);
                double x = Math.Min(startPoint.X, currentPoint.X);
                double y = Math.Min(startPoint.Y, currentPoint.Y);
                double width = Math.Abs(startPoint.X - currentPoint.X);
                double height = Math.Abs(startPoint.Y - currentPoint.Y);

                rectangle.Width = width;
                rectangle.Height = height;
                Canvas.SetLeft(rectangle, x);
                Canvas.SetTop(rectangle, y);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                CaptureScreen();
                CanvasOverlay.Children.Clear(); // キャプチャ後にオーバーレイをクリア
            }
        }

        private void CaptureScreen()
        {
            // ウィンドウ内での矩形の左上座標をスクリーン座標に変換
            var topLeft = this.PointToScreen(new System.Windows.Point(Canvas.GetLeft(rectangle), Canvas.GetTop(rectangle)));

            // キャプチャ範囲（矩形の位置とサイズ） 
            var bounds = new System.Windows.Rect(topLeft.X, topLeft.Y, rectangle.Width, rectangle.Height);

            string savePath = @"C:\KCGproject02\pl01\img\画像.png";

            using (Bitmap bitmap = new Bitmap((int)bounds.Width, (int)bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // スクリーン上の選択範囲をキャプチャ
                    g.CopyFromScreen((int)bounds.X, (int)bounds.Y, 0, 0, bitmap.Size);
                }
                bitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png); // 画像を保存
            }

            MessageBox.Show("Screenshot saved at C:\\KCGproject02\\pl01\\img\\画像.png");

            // OCR処理を実行
            PerformOCR(savePath);
        }

        private void PerformOCR(string imagePath)
        {
            string tessdataPath = @"C:\KCGproject02\pl01\tessdata"; // Tesseractデータのパス
            string language = "jpn"; // 日本語を指定

            try
            {
                using (var engine = new TesseractEngine(tessdataPath, language, EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            recognizedText = page.GetText(); // OCR結果
                            float confidence = page.GetMeanConfidence(); // 信頼度

                            MessageBox.Show($"認識結果:\n{recognizedText}\n\n認識信頼度: {confidence:P}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OCR処理中にエラーが発生しました:\n{ex.Message}");
            }
        }

        // クリップボードにテキストをコピーする処理
        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(recognizedText))
            {
                Clipboard.SetText(recognizedText); // クリップボードにコピー
                MessageBox.Show("テキストがクリップボードにコピーされました。");
            }
            else
            {
                MessageBox.Show("コピーするテキストがありません。");
            }
        }
    }
}

