using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gfycat
{
    public class GifConverter : IGifConverter
    {
        private delegate bool StatusProcessor(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken);

        private readonly IDictionary<string, StatusProcessor> _processors = new Dictionary<string, StatusProcessor>(StringComparer.OrdinalIgnoreCase)
        {
            { "complete", ProcessComplete }
        };
        private readonly Random _random = new Random();

        public async Task<Uri> ConvertAsync(Uri gifUri, IProgress<string> progress, CancellationToken cancellationToken)
        {
            string gifId = CreateRandomString() + ".gif";

            progress.Report("initiate");
            await Initiate(gifId).ConfigureAwait(false);

            progress.Report("transcode release");
            var uri = await TranscodeRelease(gifId, gifUri).ConfigureAwait(false);
            if (uri != null)
            {
                return uri;
            }

            while (true)
            {
                var status = await GetStatus(gifId).ConfigureAwait(false);
                var processor = GetProcessor(status);
                if (processor(status, progress, cancellationToken))
                {
                    progress.Report("getting the url");
                    return await TranscodeRelease(gifId, gifUri).ConfigureAwait(false);
                }
            }
        }

        private async Task Initiate(string gifId)
        {
            var uriString = string.Format("http://gfycat.com/ajax/initiate/{0}", gifId);
            var json = await new HttpClient().GetStringAsync(uriString).ConfigureAwait(false);
            if (!JsonConvert.DeserializeAnonymousType(json, new { success = true }).success)
            {
                throw new Exception("Initiate failed.");
            }
        }

        private async Task<Uri> TranscodeRelease(string gifId, Uri gifUri)
        {
            var uriString = string.Format("http://upload.gfycat.com/transcodeRelease/{0}?immediatePublish=true&fetchUrl={1}", gifId, Uri.EscapeUriString(gifUri.ToString()));
            var json = await new HttpClient().GetStringAsync(uriString).ConfigureAwait(false);
            var result = JsonConvert.DeserializeAnonymousType(json, new
            {
                isOk = true,
                mp4Url = string.Empty
            });
            if (result.isOk)
            {
                // scheduled
                return null;
            }
            return new Uri(result.mp4Url, UriKind.Absolute);
        }

        private async Task<ConversionStatus> GetStatus(string gifId)
        {
            var uriString = string.Format("http://tracking.gfycat.com/status/{0}", gifId);
            var client = new HttpClient();
            var json = await client.GetStringAsync(new Uri(uriString)).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ConversionStatus>(json);
        }

        private StatusProcessor GetProcessor(ConversionStatus status)
        {
            StatusProcessor processor;
            return _processors.TryGetValue(status.Task, out processor) ? processor : ProcessUnknown;
        }

        private static bool ProcessComplete(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report(status.Task);
            return true;
        }

        private static bool ProcessUnknown(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report(status.Task);
            return false;
        }

        private string CreateRandomString()
        {
            const string s = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var c = new char[5];
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = s[(int)Math.Floor(_random.NextDouble() * s.Length)];
            }
            return new string(c);
        }
    }
}