using ZaWorld.Core.Configuration;

namespace ZaWorld.Core.Tests;

public class CapturePathResolverTests
{
    [Fact]
    public void GetEffectiveCaptureFolder_ReturnsCustomPath_WhenCustomPathExistsAndWritable()
    {
        var sut = new CapturePathResolver();
        var picturesPath = @"C:\Users\me\Pictures";
        var customPath = @"D:\ZaWorld\Captures";

        var result = sut.GetEffectiveCaptureFolder(
            customPath,
            picturesPath,
            _ => true,
            _ => true);

        Assert.Equal(customPath, result);
    }

    [Fact]
    public void GetEffectiveCaptureFolder_FallsBackToDefault_WhenCustomPathMissing()
    {
        var sut = new CapturePathResolver();
        var picturesPath = @"C:\Users\me\Pictures";
        var customPath = @"D:\Missing\Captures";

        var result = sut.GetEffectiveCaptureFolder(
            customPath,
            picturesPath,
            path => path == picturesPath + @"\TheWorldLock\Captures",
            path => path == picturesPath + @"\TheWorldLock\Captures");

        Assert.Equal(picturesPath + @"\TheWorldLock\Captures", result);
    }

    [Fact]
    public void GetEffectiveCaptureFolder_FallsBackToDefault_WhenCustomPathNotWritable()
    {
        var sut = new CapturePathResolver();
        var picturesPath = @"C:\Users\me\Pictures";
        var customPath = @"D:\ReadOnly\Captures";

        var result = sut.GetEffectiveCaptureFolder(
            customPath,
            picturesPath,
            _ => true,
            path => path != customPath);

        Assert.Equal(picturesPath + @"\TheWorldLock\Captures", result);
    }
}

public class AppSettingsFactoryTests
{
    [Fact]
    public void CreateDefault_UsesPicturesFolderForCapturePath()
    {
        var sut = new AppSettingsFactory();
        var picturesPath = @"C:\Users\me\Pictures";

        var result = sut.CreateDefault(picturesPath);

        Assert.Equal(@"C:\Users\me\Pictures\TheWorldLock\Captures", result.CaptureFolderPath);
    }

    [Fact]
    public void CreateDefault_SetsDefaultUnlockChord()
    {
        var sut = new AppSettingsFactory();

        var result = sut.CreateDefault(@"C:\Users\me\Pictures");

        Assert.Equal("Ctrl+Alt+Shift+U", result.UnlockHotkey);
    }
}