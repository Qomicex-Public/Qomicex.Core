using System;
using System.IO;
using SkiaSharp;

namespace Qomicex.Core.Modules.Helpers;

public enum SkinModelType
{
    Classic,
    Slim
}

public enum RenderFace
{
    Front,
    Back,
    Left,
    Right
}

public enum SkinPart
{
    HeadFront, HeadBack, HeadLeft, HeadRight, HeadTop, HeadBottom,
    HatFront, HatBack,
    BodyFront, BodyBack, BodyLeft, BodyRight,
    LeftArmFront, LeftArmBack,
    RightArmFront, RightArmBack,
    LeftLegFront, LeftLegBack,
    RightLegFront, RightLegBack
}

public class SkinHelper : IDisposable
{
    private SKBitmap _skin;
    private bool _isSlim;
    private bool _disposed;

    private static readonly SKRectI HeadFront = new(8, 8, 16, 16);
    private static readonly SKRectI HeadBack = new(24, 8, 32, 16);
    private static readonly SKRectI HeadLeft = new(0, 8, 8, 16);
    private static readonly SKRectI HeadRight = new(16, 8, 24, 16);
    private static readonly SKRectI HeadTop = new(8, 0, 16, 8);
    private static readonly SKRectI HeadBottom = new(16, 0, 24, 8);

    private static readonly SKRectI HatFront = new(40, 8, 48, 16);
    private static readonly SKRectI HatBack = new(56, 8, 64, 16);
    private static readonly SKRectI HatLeft = new(32, 8, 40, 16);
    private static readonly SKRectI HatRight = new(48, 8, 56, 16);

    private static readonly SKRectI BodyFront = new(20, 20, 28, 32);
    private static readonly SKRectI BodyBack = new(32, 20, 40, 32);
    private static readonly SKRectI BodyLeft = new(16, 20, 20, 32);
    private static readonly SKRectI BodyRight = new(28, 20, 32, 32);

    private static readonly SKRectI BodyOutFront = new(20, 36, 28, 48);
    private static readonly SKRectI BodyOutBack = new(32, 36, 40, 48);

    private static readonly SKRectI LLegFront = new(4, 20, 8, 32);
    private static readonly SKRectI LLegBack = new(12, 20, 16, 32);
    private static readonly SKRectI LLegLeft = new(0, 20, 4, 32);
    private static readonly SKRectI LLegRight = new(8, 20, 12, 32);

    private static readonly SKRectI LLegOutFront = new(4, 36, 8, 48);
    private static readonly SKRectI LLegOutBack = new(12, 36, 16, 48);

    private static readonly SKRectI RLegFront = new(20, 52, 24, 64);
    private static readonly SKRectI RLegBack = new(28, 52, 32, 64);
    private static readonly SKRectI RLegLeft = new(16, 52, 20, 64);
    private static readonly SKRectI RLegRight = new(24, 52, 28, 64);

    private static readonly SKRectI RLegOutFront = new(4, 52, 8, 64);
    private static readonly SKRectI RLegOutBack = new(12, 52, 16, 64);

    private static readonly SKRectI LArmFrontC = new(44, 20, 48, 32);
    private static readonly SKRectI LArmBackC = new(52, 20, 56, 32);
    private static readonly SKRectI LArmFrontS = new(44, 20, 47, 32);
    private static readonly SKRectI LArmBackS = new(51, 20, 54, 32);

    private static readonly SKRectI LArmOutFrontC = new(44, 36, 48, 48);
    private static readonly SKRectI LArmOutBackC = new(52, 36, 56, 48);
    private static readonly SKRectI LArmOutFrontS = new(44, 36, 47, 48);
    private static readonly SKRectI LArmOutBackS = new(51, 36, 54, 48);

    private static readonly SKRectI RArmFrontC = new(36, 52, 40, 64);
    private static readonly SKRectI RArmBackC = new(44, 52, 48, 64);
    private static readonly SKRectI RArmFrontS = new(36, 52, 39, 64);
    private static readonly SKRectI RArmBackS = new(43, 52, 46, 64);

    private static readonly SKRectI RArmOutFrontC = new(52, 52, 56, 64);
    private static readonly SKRectI RArmOutBackC = new(60, 52, 64, 64);
    private static readonly SKRectI RArmOutFrontS = new(52, 52, 55, 64);
    private static readonly SKRectI RArmOutBackS = new(59, 52, 62, 64);

    public SkinModelType ModelType { get; private set; }
    public int SkinWidth => _skin.Width;
    public int SkinHeight => _skin.Height;

    public SkinHelper(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("皮肤文件不存在", filePath);

        using var stream = File.OpenRead(filePath);
        _skin = SKBitmap.Decode(stream)
            ?? throw new InvalidDataException("无法解码皮肤图像");

        NormalizeSkin();
        DetectModel();
    }

    public SkinHelper(byte[] data)
    {
        _skin = SKBitmap.Decode(data)
            ?? throw new InvalidDataException("无法解码皮肤图像");
        NormalizeSkin();
        DetectModel();
    }

    public SkinHelper(Stream stream)
    {
        _skin = SKBitmap.Decode(stream)
            ?? throw new InvalidDataException("无法解码皮肤图像");
        NormalizeSkin();
        DetectModel();
    }

    private void NormalizeSkin()
    {
        if (_skin.Height == 32 && _skin.Width == 64)
        {
            var newSkin = new SKBitmap(64, 64);
            using var canvas = new SKCanvas(newSkin);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(_skin, 0, 0);

            MirrorRegion(canvas, _skin, 4, 16, 12, 32, 20, 48);
            MirrorRegion(canvas, _skin, 40, 16, 48, 32, 24, 48);

            _skin.Dispose();
            _skin = newSkin;
        }
    }

    private static void MirrorRegion(SKCanvas dst, SKBitmap src,
        int sx, int sy, int sw, int sh, int dx, int dy)
    {
        int w = sw - sx, h = sh - sy;
        using var region = new SKBitmap(w, h);
        using var rc = new SKCanvas(region);
        rc.Clear(SKColors.Transparent);

        rc.Save();
        rc.Scale(-1, 1, w / 2f, 0);
        rc.DrawBitmap(src, SKRect.Create(sx, sy, w, h), SKRect.Create(0, 0, w, h));
        rc.Restore();

        dst.DrawBitmap(region, dx, dy);
    }

    private void DetectModel()
    {
        bool allTransparent = true;
        for (int y = 52; y < 64 && allTransparent; y++)
        {
            var px = _skin.GetPixel(39, y);
            if (px.Alpha != 0) allTransparent = false;
        }
        _isSlim = allTransparent;
        ModelType = _isSlim ? SkinModelType.Slim : SkinModelType.Classic;
    }

    public SKBitmap GetHeadAvatar(int size = 64, bool includeHat = true)
    {
        var bmp = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);

        DrawRegion(canvas, HeadFront, 0, 0, size, size);
        if (includeHat)
            DrawRegion(canvas, HatFront, 0, 0, size, size);

        return bmp;
    }

    public SKBitmap GetFullBody(RenderFace face = RenderFace.Front,
                                 int scale = 4, bool includeOuter = true)
    {
        bool isFront = face == RenderFace.Front;
        int W = 16 * scale, H = 32 * scale;

        var bmp = new SKBitmap(W, H, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);

        int armW = _isSlim ? 3 : 4;

        if (isFront)
        {
            DrawRegion(canvas, LArmFrontC, 0, 8 * scale, armW * scale, 12 * scale, flip: !isFront);
            DrawRegion(canvas, HeadFront, 4 * scale, 0, 8 * scale, 8 * scale);
            DrawRegion(canvas, BodyFront, 4 * scale, 8 * scale, 8 * scale, 12 * scale);
            DrawRegion(canvas, RLegFront, 4 * scale, 20 * scale, 4 * scale, 12 * scale);
            DrawRegion(canvas, LLegFront, 8 * scale, 20 * scale, 4 * scale, 12 * scale);
            DrawRegion(canvas, RArmFrontC, (4 + 8) * scale, 8 * scale, armW * scale, 12 * scale);

            if (includeOuter)
            {
                DrawRegion(canvas, HatFront, 4 * scale, 0, 8 * scale, 8 * scale);
                DrawRegion(canvas, BodyOutFront, 4 * scale, 8 * scale, 8 * scale, 12 * scale);
                DrawRegion(canvas, LArmOutFrontC, 0, 8 * scale, armW * scale, 12 * scale);
                DrawRegion(canvas, RArmOutFrontC, (4 + 8) * scale, 8 * scale, armW * scale, 12 * scale);
                DrawRegion(canvas, LLegOutFront, 8 * scale, 20 * scale, 4 * scale, 12 * scale);
                DrawRegion(canvas, RLegOutFront, 4 * scale, 20 * scale, 4 * scale, 12 * scale);
            }
        }
        else
        {
            DrawRegion(canvas, RArmBackC, 0, 8 * scale, armW * scale, 12 * scale);
            DrawRegion(canvas, HeadBack, 4 * scale, 0, 8 * scale, 8 * scale);
            DrawRegion(canvas, BodyBack, 4 * scale, 8 * scale, 8 * scale, 12 * scale);
            DrawRegion(canvas, LLegBack, 4 * scale, 20 * scale, 4 * scale, 12 * scale);
            DrawRegion(canvas, RLegBack, 8 * scale, 20 * scale, 4 * scale, 12 * scale);
            DrawRegion(canvas, LArmBackC, (4 + 8) * scale, 8 * scale, armW * scale, 12 * scale);

            if (includeOuter)
            {
                DrawRegion(canvas, HatBack, 4 * scale, 0, 8 * scale, 8 * scale);
                DrawRegion(canvas, BodyOutBack, 4 * scale, 8 * scale, 8 * scale, 12 * scale);
                DrawRegion(canvas, RArmOutBackC, 0, 8 * scale, armW * scale, 12 * scale);
                DrawRegion(canvas, LArmOutBackC, (4 + 8) * scale, 8 * scale, armW * scale, 12 * scale);
                DrawRegion(canvas, LLegOutBack, 4 * scale, 20 * scale, 4 * scale, 12 * scale);
                DrawRegion(canvas, RLegOutBack, 8 * scale, 20 * scale, 4 * scale, 12 * scale);
            }
        }

        return bmp;
    }

    public SKBitmap GetPartRaw(SkinPart part)
    {
        var rect = GetPartRect(part);
        return ExtractRegion(rect);
    }

    public SKBitmap GetFrontBackComparison(int scale = 4, bool includeOuter = true)
    {
        using var front = GetFullBody(RenderFace.Front, scale, includeOuter);
        using var back = GetFullBody(RenderFace.Back, scale, includeOuter);

        int gap = 8 * scale;
        var bmp = new SKBitmap(front.Width * 2 + gap, front.Height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(front, 0, 0);
        canvas.DrawBitmap(back, front.Width + gap, 0);
        return bmp;
    }

    public static void SavePng(SKBitmap bitmap, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    public static byte[] ToPngBytes(SKBitmap bitmap)
    {
        using var img = SKImage.FromBitmap(bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void DrawRegion(SKCanvas canvas, SKRectI srcRect,
        int dx, int dy, int dw, int dh, bool flip = false)
    {
        var src = new SKRect(srcRect.Left, srcRect.Top, srcRect.Right, srcRect.Bottom);
        var dst = new SKRect(dx, dy, dx + dw, dy + dh);

        if (flip)
        {
            canvas.Save();
            canvas.Scale(-1, 1, dx + dw / 2f, 0);
        }

        canvas.DrawBitmap(_skin, src, dst, new SKPaint
        {
            FilterQuality = SKFilterQuality.None,
            IsAntialias = false
        });

        if (flip) canvas.Restore();
    }

    private SKBitmap ExtractRegion(SKRectI rect)
    {
        int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
        var bmp = new SKBitmap(w, h);
        using var canvas = new SKCanvas(bmp);
        canvas.DrawBitmap(_skin, new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom),
                                 new SKRect(0, 0, w, h));
        return bmp;
    }

    private SKRectI GetPartRect(SkinPart part) => part switch
    {
        SkinPart.HeadFront => HeadFront,
        SkinPart.HeadBack => HeadBack,
        SkinPart.HeadLeft => HeadLeft,
        SkinPart.HeadRight => HeadRight,
        SkinPart.HeadTop => HeadTop,
        SkinPart.HeadBottom => HeadBottom,
        SkinPart.HatFront => HatFront,
        SkinPart.HatBack => HatBack,
        SkinPart.BodyFront => BodyFront,
        SkinPart.BodyBack => BodyBack,
        SkinPart.BodyLeft => BodyLeft,
        SkinPart.BodyRight => BodyRight,
        SkinPart.LeftArmFront => _isSlim ? LArmFrontS : LArmFrontC,
        SkinPart.LeftArmBack => _isSlim ? LArmBackS : LArmBackC,
        SkinPart.RightArmFront => _isSlim ? RArmFrontS : RArmFrontC,
        SkinPart.RightArmBack => _isSlim ? RArmBackS : RArmBackC,
        SkinPart.LeftLegFront => LLegFront,
        SkinPart.LeftLegBack => LLegBack,
        SkinPart.RightLegFront => RLegFront,
        SkinPart.RightLegBack => RLegBack,
        _ => throw new ArgumentOutOfRangeException(nameof(part))
    };

    public void Dispose()
    {
        if (!_disposed)
        {
            _skin?.Dispose();
            _disposed = true;
        }
    }
}

public static class SkinRenderer
{
    public static SKBitmap GetRoundAvatar(SkinHelper helper, int size = 128, bool includeHat = true)
    {
        using var square = helper.GetHeadAvatar(size, includeHat);

        var result = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        using var path = new SKPath();
        path.AddOval(new SKRect(0, 0, size, size));
        canvas.ClipPath(path, antialias: true);
        canvas.DrawBitmap(square, 0, 0);

        return result;
    }

    public static SKBitmap GetAvatarWithBackground(SkinHelper helper,
        SKColor background, int size = 128, bool includeHat = true, int padding = 8)
    {
        var result = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(result);
        canvas.Clear(background);

        using var avatar = helper.GetHeadAvatar(size - padding * 2, includeHat);
        canvas.DrawBitmap(avatar, padding, padding);

        return result;
    }

    public static SKBitmap GetRoundedAvatar(SkinHelper helper,
        int size = 128, float cornerRadius = 16f, bool includeHat = true)
    {
        using var square = helper.GetHeadAvatar(size, includeHat);

        var result = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        using var path = new SKPath();
        path.AddRoundRect(new SKRect(0, 0, size, size), cornerRadius, cornerRadius);
        canvas.ClipPath(path, antialias: true);
        canvas.DrawBitmap(square, 0, 0);

        return result;
    }

    public static SKBitmap GetSkinAtlas(SkinHelper helper, int scale = 4)
    {
        int w = helper.SkinWidth * scale;
        int h = helper.SkinHeight * scale;

        var parts = new (SkinPart part, int col, int row)[]
        {
            (SkinPart.HeadFront,    0, 0), (SkinPart.HeadBack,     1, 0),
            (SkinPart.HeadLeft,     2, 0), (SkinPart.HeadRight,    3, 0),
            (SkinPart.HatFront,     4, 0), (SkinPart.HatBack,      5, 0),
            (SkinPart.BodyFront,    0, 1), (SkinPart.BodyBack,     1, 1),
            (SkinPart.BodyLeft,      2, 1), (SkinPart.BodyRight,   3, 1),
            (SkinPart.LeftArmFront, 0, 2), (SkinPart.LeftArmBack,  1, 2),
            (SkinPart.RightArmFront, 2, 2), (SkinPart.RightArmBack, 3, 2),
            (SkinPart.LeftLegFront, 0, 3), (SkinPart.LeftLegBack,  1, 3),
            (SkinPart.RightLegFront, 2, 3), (SkinPart.RightLegBack, 3, 3),
        };

        int cellSize = 16 * scale;
        int cols = 6, rows = 4;
        var atlas = new SKBitmap(cols * cellSize, rows * cellSize);
        using var canvas = new SKCanvas(atlas);
        canvas.Clear(new SKColor(32, 32, 32));

        using var gridPaint = new SKPaint
        {
            Color = new SKColor(64, 64, 64),
            StrokeWidth = 1,
            IsStroke = true
        };

        foreach (var (part, col, row) in parts)
        {
            using var partBmp = helper.GetPartRaw(part);
            var dst = new SKRect(col * cellSize, row * cellSize,
                                 (col + 1) * cellSize, (row + 1) * cellSize);
            canvas.DrawBitmap(partBmp, dst, new SKPaint { FilterQuality = SKFilterQuality.None });

            using var labelPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = scale * 2,
                IsAntialias = true
            };
            canvas.DrawText(part.ToString(), dst.Left + 2, dst.Bottom - 2, labelPaint);
            canvas.DrawRect(dst, gridPaint);
        }

        return atlas;
    }

    public static SKBitmap PixelScale(SKBitmap source, int scale)
    {
        int w = source.Width * scale;
        int h = source.Height * scale;
        var result = new SKBitmap(w, h, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(result);
        canvas.DrawBitmap(source, new SKRect(0, 0, source.Width, source.Height),
                                  new SKRect(0, 0, w, h),
            new SKPaint { FilterQuality = SKFilterQuality.None });
        return result;
    }
}
