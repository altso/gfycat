using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gfycat
{
    public interface IGifConverter
    {
        Task<Uri> ConvertAsync(Uri gifUri, IProgress<string> progress, CancellationToken cancellationToken);
    }
}