using System.IO;
using OpenCvSharp;

namespace ZaWorld.App.Services;

public sealed class WebcamSnapshotService : IDisposable
{
    private readonly object _sync = new();
    private VideoCapture? _capture;
    private DateTimeOffset _lastCapture = DateTimeOffset.MinValue;
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(750);

    public void TryCaptureJpeg(string folderPath)
    {
        try
        {
            Directory.CreateDirectory(folderPath);
        }
        catch
        {
            return;
        }

        lock (_sync)
        {
            if (DateTimeOffset.UtcNow - _lastCapture < MinInterval)
            {
                return;
            }

            _lastCapture = DateTimeOffset.UtcNow;
        }

        _ = Task.Run(() =>
        {
            try
            {
                CaptureInternal(folderPath);
            }
            catch
            {
                // Camera may be busy or denied; ignore for now.
            }
        });
    }

    private void CaptureInternal(string folderPath)
    {
        lock (_sync)
        {
            _capture ??= new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            if (!_capture.IsOpened())
            {
                _capture.Dispose();
                _capture = new VideoCapture(0);
            }

            if (_capture is null || !_capture.IsOpened())
            {
                return;
            }

            using var frame = new Mat();
            _capture.Read(frame);
            if (frame.Empty())
            {
                return;
            }

            var fileName = $"intrusion_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
            var fullPath = Path.Combine(folderPath, fileName);
            Cv2.ImWrite(fullPath, frame);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
        }
    }
}
