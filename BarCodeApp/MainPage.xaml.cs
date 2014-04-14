using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using BarCodeApp.Util;
using com.google.zxing;
using com.google.zxing.qrcode;
using com.google.zxing.qrcode.decoder;
using CtripWP7.PNGExtention;
using Microsoft.Devices;
using Microsoft.Phone.Tasks;

namespace BarCodeApp
{
    public partial class MainPage
    {

        private readonly BarcodeDecodeManager _decodeManager = new BarcodeDecodeManager();

        // 构造函数
        public MainPage()
        {
            InitializeComponent();

            // 用于本地化 ApplicationBar 的示例代码
            //BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HandleMainPivotSelectionChanged();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            StopCammera();
        }

        private void GenerateQRCode_OnClick(object sender, RoutedEventArgs e)
        {
            var content = TextToGenerate.Text;
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            var encoder = new QRCodeWriter();
            var tempResult = encoder.encode(content, BarcodeFormat.QR_CODE, 140, 140, new Dictionary<object, object> {{EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H}});
            var bitmap = tempResult.GenerateWriteableBitmap(Colors.Transparent, Colors.White, Colors.Orange);
            using (var stream = new IsolatedStorageFileStream("Shared/ShellContent/Test.png", FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication()))
            {
                bitmap.SavePng(stream);
            }
            GeneratedQRCodeImage.Source = bitmap;
        }

        private void Pivot_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleMainPivotSelectionChanged();
        }

        private void HandleMainPivotSelectionChanged()
        {
            if (MainPivot.SelectedIndex == 1)
            {
                StartCammera();
            }
            else
            {
                StopCammera();
            }
        }

        private BarCodeCamera _camera;

        private void StartCammera()
        {
            if (Camera.IsCameraTypeSupported(CameraType.Primary))
            {
                var camera = new PhotoCamera(CameraType.Primary);
                if (_camera != null)
                {
                    _camera.GetBuffImage -= Camera_GetBuffImage;
                    _camera.Dispose();
                }
                _camera = new BarCodeCamera(camera);
                _camera.GetBuffImage += Camera_GetBuffImage;
                camera.Initialized += (sender, e) => Dispatcher.BeginInvoke(StartCapture);
                CameraPreviewRotation.Angle = camera.Orientation;
                CameraPreview.SetSource(camera);
            }
        }

        private void Camera_GetBuffImage(object sender, BarCodeCameraContentReadyEventArgs e)
        {
            var task = new BarcodeDecodeTask(e.ImageStream);
            task.Decoded += Task_Decoded;
            _decodeManager.AddTask(task);
        }

        private void StopCammera()
        {
            if (_camera != null)
            {
                _camera.Dispose();
            }
            StopCapture();
            _camera = null;
        }

        private void StartCapture()
        {
            if (_camera != null)
            {
                _camera.Start(TimeSpan.FromMilliseconds(100));
            }
        }

        private void StopCapture()
        {
            if (_camera != null)
            {
                _camera.Stop();
            }
            _decodeManager.ClearQueuedTask();
        }

        private void Task_Decoded(object sender, BarcodeDecodeResult e)
        {
            Dispatcher.BeginInvoke(() =>
                                   {
                                       ScanResultOuntput.Text = e.Message;
                                   });
            if (e.Result == BarcodeDecodeResultType.Success)
            {
                ((BarcodeDecodeTask) sender).ImageStream.Dispose();
                Dispatcher.BeginInvoke(StopCapture);
            }
        }

        private void ScanResultClicked(object sender, RoutedEventArgs e)
        {
            Uri uri;
            if (Uri.TryCreate(ScanResultOuntput.Text, UriKind.RelativeOrAbsolute, out uri))
            {
                if (uri.IsAbsoluteUri)
                {
                    new WebBrowserTask
                    {
                        Uri = uri
                    }.Show();
                }
                else
                {
                    NavigationService.Navigate(uri);
                }
            }
        }
    }

}
