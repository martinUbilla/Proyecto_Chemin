using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace backend.Services.Extraction;

public static class PageRasterizer
{
    private const int ScaleFactor = 4;

    public static SKBitmap RenderPage(byte[] pdfBytes, int pageIndex)
    {
        using var library = DocLib.Instance;
        using var docReader = library.GetDocReader(pdfBytes, new PageDimensions(ScaleFactor));
        using var pageReader = docReader.GetPageReader(pageIndex);

        var width    = pageReader.GetPageWidth();
        var height   = pageReader.GetPageHeight();
        var rawBytes = pageReader.GetImage();

        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        var handle = GCHandle.Alloc(rawBytes, GCHandleType.Pinned);
        try
        {
            bitmap.SetPixels(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }

        return bitmap.Copy();
    }
}