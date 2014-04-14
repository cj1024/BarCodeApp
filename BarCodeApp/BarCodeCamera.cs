using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Devices;

namespace BarCodeApp
{

    public class BarCodeCameraContentReadyEventArgs : EventArgs
    {
        
        public Stream ImageStream { get; private set; }

        internal BarCodeCameraContentReadyEventArgs(Stream imageStream)
        {
            ImageStream = imageStream;
        }

    }

    public class BarCodeCamera : DependencyObject, IDisposable
    {

        private readonly PhotoCamera _camera;

        private readonly DispatcherTimer _timer;

        private const string StartedState = "Started";
        private const string StoppedState = "Stopped";

        private string _state = StoppedState;

        public BarCodeCamera(PhotoCamera camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException("camera");
            }
            _camera = camera;
            _timer = new DispatcherTimer();
            _timer.Tick += TryCapture;
        }

        private void TryCapture(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                lock (_state)
                {
                    if (_camera != null && StartedState.Equals(_state))
                    {
                        var bitmap = new WriteableBitmap((int)_camera.PreviewResolution.Width, (int)_camera.PreviewResolution.Height);
                        _camera.GetPreviewBufferArgb32(bitmap.Pixels);
                        var stream = new MemoryStream();
                        bitmap.SaveJpeg(stream, bitmap.PixelWidth, bitmap.PixelHeight, 0, 100);
                        if (GetBuffImage != null)
                        {
                            GetBuffImage(_camera, new BarCodeCameraContentReadyEventArgs(stream));
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            _timer.Stop();
            if (_camera != null)
            {
                _camera.Dispose();
            }
        }

        public void Start(TimeSpan interval)
        {
            lock (_state)
            {
                _timer.Stop();
                _timer.Interval = interval;
                _timer.Start();
                _state = StartedState;
            }
        }

        public void Stop()
        {
            lock (_state)
            {
                _timer.Stop();
                _state = StoppedState;
            }
        }

        public event EventHandler<BarCodeCameraContentReadyEventArgs> GetBuffImage;

    }

}
