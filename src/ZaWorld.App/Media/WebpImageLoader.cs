using System.IO;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ZaWorld.App.Media;

public static class WebpImageLoader
{
    public static BitmapImage? TryLoadBitmapImage(string webpPath)
    {
        if (!File.Exists(webpPath))
        {
            return null;
        }

        using var image = Image.Load<Rgba32>(webpPath);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
