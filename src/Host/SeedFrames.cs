using System.Collections.Concurrent;
using SkiaSharp;

namespace CarGallery.Host;

/// <summary>
/// `0016b` — geliştirme tohumunun galeri kareleri. Yalnız <c>Development:SeedShowcase</c>
/// açıkken eşlenen uçtan çağrılır; üretimde erişilebilir bir yüzeyi yoktur.
/// </summary>
internal static class SeedFrames
{
    private static readonly ConcurrentDictionary<(int, int, int), byte[]> Cache = new();

    /// <summary>Kare numarası ve boyutu verilen JPEG; aynı istek bir kez üretilir.</summary>
    internal static byte[] Jpeg(int index, int width, int height) =>
        Cache.GetOrAdd((index, width, height), key => Uret(key.Item1, key.Item2, key.Item3));

    private static byte[] Uret(int index, int width, int height)
    {
        using SKBitmap bitmap = new(width, height);

        using (SKCanvas tuval = new(bitmap))
        {
            // Koordinatlar ve ton TAM SAYI: kare bir piksel ızgarası ve Yasak Listesi 26
            // `src` altında kayan noktalı bildirime istisnasız kapalı.
            int ton = index * 37 % 360;

            using SKPaint zemin = new()
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(width, height),
                    [SKColor.FromHsl(ton, 35, 30), SKColor.FromHsl((ton + 60) % 360, 45, 55)],
                    SKShaderTileMode.Clamp),
            };
            tuval.DrawRect(0, 0, width, height, zemin);

            // Izgara ve çizgiler: JPEG'e gerçek fotoğrafa yakın bir bilgi yoğunluğu verir.
            using SKPaint cizgi = new() { Color = new SKColor(255, 255, 255, 40), StrokeWidth = Math.Max(1, width / 400f), IsAntialias = true };
            int adim = Math.Max(8, width / 24);
            for (int x = 0; x < width; x += adim)
            {
                tuval.DrawLine(x, 0, x + (height / 3f), height, cizgi);
            }

            for (int y = 0; y < height; y += adim)
            {
                tuval.DrawLine(0, y, width, y + (adim / 2), cizgi);
            }

            using SKFont yazi = new() { Size = Math.Max(12, Math.Min(width, height) / 4f) };
            using SKPaint boya = new() { Color = SKColors.White, IsAntialias = true };
            tuval.DrawText((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), width / 2f, (height / 2f) + (yazi.Size / 3), SKTextAlign.Center, yazi, boya);

            using SKFont kucuk = new() { Size = Math.Max(10, width / 30f) };
            tuval.DrawText($"{width}×{height}", width / 2f, height * 0.88f, SKTextAlign.Center, kucuk, boya);
        }

        using SKData veri = bitmap.Encode(SKEncodedImageFormat.Jpeg, 75);
        return veri.ToArray();
    }
}
