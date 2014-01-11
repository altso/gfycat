using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;

namespace Gfycat
{
    public static class GifConvert
    {
        public static Task<Uri> ConvertAsync(Uri gifUri)
        {
            return ConvertAsync(gifUri, new Progress<string>(), CancellationToken.None);
        }

        public static Task<Uri> ConvertAsync(Uri gifUri, IProgress<string> progress, CancellationToken cancellationToken)
        {
            return new GifConverter().ConvertAsync(gifUri, progress, cancellationToken);
        }
    }
}