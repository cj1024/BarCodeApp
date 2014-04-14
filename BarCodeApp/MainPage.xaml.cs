﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private PhotoCamera _camera;

        private void StartCammera()
        {
            if (Camera.IsCameraTypeSupported(CameraType.Primary))
            {
                _camera = new PhotoCamera(CameraType.Primary);
                CameraPreviewRotation.Angle = _camera.Orientation;
                _camera.Initialized += (sender, e) =>
                                       {
                                           if (_camera.PreviewResolution.Width < BarcodeDecodeManager.MinPictureWidth || _camera.PreviewResolution.Height < BarcodeDecodeManager.MinPictureHeight)
                                           {
                                               _camera.CaptureThumbnailAvailable += Camera_CaptureThumbnailAvailableAndNeedAddTask;
                                           }
                                           else
                                           {
                                               _camera.CaptureImageAvailable += Camera_CaptureThumbnailAvailableAndNeedAddTask;
                                           }
                                           _camera.CaptureCompleted += Camera_CaptureCompletedAndNeedRestartCapture;
                                           _camera.FlashMode = FlashMode.Off;
                                           StartCapture();
                                       };
                CameraPreview.SetSource(_camera);
            }
        }

        private void StopCammera()
        {
            if (_camera != null)
            {
                _camera.CaptureThumbnailAvailable -= Camera_CaptureThumbnailAvailableAndNeedAddTask;
                _camera.Dispose();
            }
            StopCapture();
            _camera = null;
        }

        private void StartCapture()
        {
            ThreadPool.QueueUserWorkItem(p =>
                                         {
                                             Thread.Sleep(500);
                                             if (_camera != null)
                                             {
                                                 _camera.CaptureImage();
                                             }
                                         });
        }

        private void StopCapture()
        {
            if (_camera != null)
            {
                _camera.CaptureCompleted -= Camera_CaptureCompletedAndNeedRestartCapture;
            }
            _decodeManager.ClearQueuedTask();
        }

        private void Camera_CaptureCompletedAndNeedRestartCapture(object sender, CameraOperationCompletedEventArgs e)
        {
            StartCapture();
        }

        private void Camera_CaptureThumbnailAvailableAndNeedAddTask(object sender, ContentReadyEventArgs e)
        {
            if (_decodeManager.IsFree)
            {
                var task = new BarcodeDecodeTask(e.ImageStream);
                task.Decoded += Task_Decoded;
                _decodeManager.AddTask(task);
            }
        }

        private void Task_Decoded(object sender, BarcodeDecodeResult e)
        {
            Dispatcher.BeginInvoke(() =>
                                   {
                                       ScanResultOuntput.Text = e.Message;
                                   });
            if (e.Result == BarcodeDecodeResultType.Success)
            {
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
