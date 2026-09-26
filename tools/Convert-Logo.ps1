param(
    [string]$SourcePath = (Join-Path $PSScriptRoot '..\logo-round.png'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$SourcePath = (Resolve-Path -LiteralPath $SourcePath).Path
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
[void][System.IO.Directory]::CreateDirectory($OutputDirectory)

Add-Type -AssemblyName System.Drawing
if (-not ('TicketLogAssets.LogoConverter' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TicketLogAssets
{
    public static class LogoConverter
    {
        private static readonly int[] IconSizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };

        public static void Convert(string sourcePath, string outputDirectory)
        {
            using (var source = new Bitmap(sourcePath))
            {
                if (source.Width != source.Height || source.Width < 256)
                    throw new InvalidDataException("A logo original deve ser quadrada e ter pelo menos 256 pixels.");

                using (var logo = Resize(source, Math.Min(512, source.Width)))
                    logo.Save(Path.Combine(outputDirectory, "logo-ui.png"), ImageFormat.Png);

                var frames = new List<byte[]>();
                foreach (int size in IconSizes)
                {
                    using (var bitmap = Resize(source, size))
                        frames.Add(size == 256 ? EncodePng(bitmap) : EncodeDib(bitmap));
                }

                using (var stream = File.Create(Path.Combine(outputDirectory, "logo.ico")))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((ushort)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)IconSizes.Length);
                    uint offset = (uint)(6 + 16 * IconSizes.Length);
                    for (int i = 0; i < IconSizes.Length; i++)
                    {
                        byte dimension = (byte)(IconSizes[i] == 256 ? 0 : IconSizes[i]);
                        writer.Write(dimension);
                        writer.Write(dimension);
                        writer.Write((byte)0);
                        writer.Write((byte)0);
                        writer.Write((ushort)1);
                        writer.Write((ushort)32);
                        writer.Write((uint)frames[i].Length);
                        writer.Write(offset);
                        offset += (uint)frames[i].Length;
                    }
                    foreach (byte[] frame in frames)
                        writer.Write(frame);
                }
            }
        }

        private static Bitmap Resize(Bitmap source, int size)
        {
            var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(96, 96);
            using (var graphics = Graphics.FromImage(bitmap))
            using (var attributes = new ImageAttributes())
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(source, new Rectangle(0, 0, size, size),
                    0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return bitmap;
        }

        private static byte[] EncodePng(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private static byte[] EncodeDib(Bitmap bitmap)
        {
            // Quadros pequenos usam BGRA de 32 bits e mascara AND; o de 256 px usa PNG.
            int size = bitmap.Width;
            int maskStride = ((size + 31) / 32) * 4;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(40);
                writer.Write(size);
                writer.Write(size * 2);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(0);
                writer.Write(size * size * 4 + maskStride * size);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);

                for (int y = size - 1; y >= 0; y--)
                    for (int x = 0; x < size; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        writer.Write(pixel.B);
                        writer.Write(pixel.G);
                        writer.Write(pixel.R);
                        writer.Write(pixel.A);
                    }

                for (int y = size - 1; y >= 0; y--)
                {
                    var mask = new byte[maskStride];
                    for (int x = 0; x < size; x++)
                        if (bitmap.GetPixel(x, y).A == 0)
                            mask[x / 8] |= (byte)(0x80 >> (x % 8));
                    writer.Write(mask);
                }
                return stream.ToArray();
            }
        }
    }
}
'@
}

[TicketLogAssets.LogoConverter]::Convert($SourcePath, $OutputDirectory)
Write-Output 'Logo convertida: logo-ui.png com transparencia e logo.ico com 10 tamanhos (16 a 256 px).'
