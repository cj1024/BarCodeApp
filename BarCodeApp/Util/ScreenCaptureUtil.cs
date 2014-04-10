using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Devices;
using Microsoft.Phone.Controls;

namespace BarCodeApp.Util
{
    public class ScreenCaptureUtil
    {
        public static bool ServiceOn { get; private set; }

        /// <summary>
        /// 默认存储文件的路径定义
        /// </summary>
        private const string DefaultPath = "Shared/ShellContent/Capture";

        /// <summary>
        /// 对指定的FrameworkElement截图并存放到独立存储空间
        /// </summary>
        /// <param name="obj">要截图的对象（需要是在屏幕上显示的空间）</param>
        /// <param name="width">截图的宽度，默认为-1，小于0会自动计算目标宽度</param>
        /// <param name="height">截图的高度，默认为-1，小于0会自动计算目标高度</param>
        /// <param name="path">存储的位置，有默认值</param>
        /// <param name="fileName">存储的文件名，默认会以时间存储</param>
        public static void Capture(FrameworkElement obj, double width = -1, double height = -1, string path = DefaultPath, string fileName = "")
        {
            try
            {
                int w = Convert.ToInt32(width < 0 ? obj.ActualWidth : width),
                    h = Convert.ToInt32(height < 0 ? obj.ActualHeight : height);
                string p = string.IsNullOrEmpty(path) ? DefaultPath : path;
                string fn = string.IsNullOrEmpty(fileName) ? DateTime.Now.ToString("yyyyMMddhhmmss") : fileName;
                if (!fn.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase) && !fn.EndsWith(".JPEG", StringComparison.OrdinalIgnoreCase))
                {
                    fn = fn + ".jpg";
                }
                var writeableBitmap = new WriteableBitmap(w, h);
                writeableBitmap.Render(obj, null);
                writeableBitmap.Invalidate();

                if (!IsolatedStorageFile.GetUserStoreForApplication().DirectoryExists(
                        string.Format("{0}/", p)))
                {
                    IsolatedStorageFile.GetUserStoreForApplication().CreateDirectory(
                        p);
                }
                var stream =
                    new IsolatedStorageFileStream(string.Format("{0}/{1}", p, fn),
                        FileMode.Create,
                        IsolatedStorageFile.GetUserStoreForApplication());
                writeableBitmap.SaveJpeg(stream, w, h, 0, 100);
                stream.Close();
                stream.Dispose();
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        public static WriteableBitmap SpecialCapture(FrameworkElement obj)
        {
            int w = Convert.ToInt32(obj.ActualWidth),
                h = Convert.ToInt32(obj.ActualHeight);
            var writeableBitmap = new WriteableBitmap(w, h);
            writeableBitmap.Render(obj, null);
            writeableBitmap.Invalidate();
            return writeableBitmap;
        }

        #region 页面导航抓取上一个页面UI使用

        private static readonly System.Security.Cryptography.SHA1 SHA1 = new System.Security.Cryptography.SHA1Managed();

        private const string DefaultPageCaptureDirectory = "PageBitmap";

        private static string GetCaptureFileName(Uri source)
        {
            const string pattern = "\\\\|\\/|:|\\?|\\*|\"|<|>|\\|";
            var regex = new Regex(pattern);
            var evaluator = new MatchEvaluator(ConvertToLegalPath);
            var bytes = Encoding.UTF8.GetBytes(source.OriginalString);
            var str = Convert.ToBase64String(SHA1.ComputeHash(bytes));
            str = regex.Replace(str, evaluator);
            return string.Format("{0}/{1}.png", DefaultPageCaptureDirectory, str);
        }

        static string ConvertToLegalPath(Match m)
        {
            return string.Format("${0}$", (int)m.Value[0]);
        }

        internal static BitmapSource TryGetPageBitmap(Uri source, FrameworkElement candidate)
        {
            var fileName = GetCaptureFileName(source);
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (storage.FileExists(fileName))
                {
                    using (var stream = storage.OpenFile(fileName, FileMode.Open))
                    {
                        var result = new BitmapImage();
                        result.SetSource(stream);
                        return result;
                    }
                }
            }
            if (candidate != null)
            {
                double rw = candidate.Width, rh = candidate.Height;
                candidate.Width = Application.Current.Host.Content.ActualWidth;
                candidate.Height = Application.Current.Host.Content.ActualHeight;
                candidate.Measure(new Size(candidate.Width, candidate.Height));
                candidate.Arrange(new Rect(0, 0, candidate.Width, candidate.Height));
                var result = TryCapturePageBitmap(candidate, source);
                candidate.Width = rw;
                candidate.Height = rh;
                return result;
            }
            return null;
        }

        internal static WriteableBitmap TryCapturePageBitmap(object content, Uri source)
        {
            var bitmap = SpecialCapture(content as FrameworkElement);
            var fileName = GetCaptureFileName(source);
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!storage.DirectoryExists(DefaultPageCaptureDirectory))
                {
                    storage.CreateDirectory(DefaultPageCaptureDirectory);
                }
                if (storage.FileExists(fileName))
                {
                    storage.DeleteFile(fileName);
                }
                using (var stream = storage.CreateFile(fileName))
                {
                    bitmap.SaveJpeg(stream, Math.Max(1, bitmap.PixelWidth), Math.Max(1, bitmap.PixelHeight), 0, 100);
                }
            }
            return bitmap;
        }

        #endregion

        /// <summary>
        /// 开始拍照键截图服务
        /// </summary>
        public static void StartService()
        {
            if (ServiceOn) return;
            //The CameraButtonsEvent won't fire without the CaptureSource.Start()
            var cs = new CaptureSource();
            cs.Start();
            ServiceOn = false;
            CameraButtons.ShutterKeyPressed += CameraButtonsShutterKeyPressed;
        }

        /// <summary>
        /// 关闭拍照键截图的服务
        /// </summary>
        public static void StopService()
        {
            if (!ServiceOn) return;
            CameraButtons.ShutterKeyPressed -= CameraButtonsShutterKeyPressed;
            ServiceOn = false;
        }

        /// <summary>
        /// 拍照键的监听事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CameraButtonsShutterKeyPressed(object sender, EventArgs e)
        {
            ServiceOn = true;
            Capture(((PhoneApplicationFrame) Application.Current.RootVisual).Content as PhoneApplicationPage);
        }

    }
}
