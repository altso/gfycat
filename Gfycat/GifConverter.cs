using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gfycat
{
    public class GifConverter : IGifConverter
    {
        private delegate string StatusProcessor(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken);

        private static readonly string[] Servers =
        {
            "http://zippy.gfycat.com/{0}.{1}",
            "http://fat.gfycat.com/{0}.{1}",
            "http://giant.gfycat.com/{0}.{1}"
        };

        private static readonly Regex RetryRegex = new Regex(@"Sorry, please wait another (?<timeout>\d+) seconds before your next upload.");
        private readonly IDictionary<string, StatusProcessor> _processors = new Dictionary<string, StatusProcessor>(StringComparer.OrdinalIgnoreCase)
        {
            { "complete", ProcessComplete },
            { "error", ProcessError }
        };
        private static readonly Random Random = new Random();

        public async Task<Uri> ConvertAsync(Uri gifUri, IProgress<string> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string gifId = CreateRandomString() + ".gif";

            progress.Report("initiate");
            await Initiate(gifId).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report("transcode release");
            var uri = await TranscodeRelease(gifId, gifUri).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (uri != null)
            {
                return uri;
            }

            while (true)
            {
                var status = await GetStatus(gifId).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                var processor = GetProcessor(status);
                var gfyname = processor(status, progress, cancellationToken);
                if (gfyname != null)
                {
                    progress.Report("getting the url");
                    return await GetUri(gfyname).ConfigureAwait(false);
                }
            }
        }

        private async Task Initiate(string gifId)
        {
            var uriString = string.Format("http://gfycat.com/ajax/initiate/{0}", gifId);
            var json = await new HttpClient().GetStringAsync(uriString).ConfigureAwait(false);
            if (!JsonConvert.DeserializeAnonymousType(json, new { success = true }).success)
            {
                throw new GfycatException("Initiate failed.");
            }
        }

        private async Task<Uri> TranscodeRelease(string gifId, Uri gifUri)
        {
            var uriString = string.Format("http://upload.gfycat.com/transcodeRelease{0}?immediatePublish=true&fetchUrl={1}", gifId != null ? "/" + gifId : null, Uri.EscapeUriString(gifUri.ToString()));
            var json = await new HttpClient().GetStringAsync(uriString).ConfigureAwait(false);
            var result = JsonConvert.DeserializeAnonymousType(json, new
            {
                isOk = true,
                mp4Url = string.Empty,
                error = string.Empty
            });
            if (result.isOk)
            {
                // scheduled
                return null;
            }
            if (result.error != null)
            {
                throw new GfycatException(result.error);
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

        private static async Task<Uri> GetUri(string id)
        {
            foreach (var server in Servers)
            {
                string url = string.Format(server, id, "mp4");
                var uri = new Uri(url, UriKind.Absolute);
                var response = await new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
                if (response.IsSuccessStatusCode)
                {
                    return uri;
                }
            }
            throw new GfycatException("Could not find converted file.");
        }

        private static string ProcessComplete(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report(status.Task);
            return status.Gfyname;
        }

        private static string ProcessError(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report(status.Task);
            var match = RetryRegex.Match(status.Error);
            if (match.Success)
            {
                var timeout = match.Groups["timeout"].Value;

                throw new GfycatRetryException
                {
                    RetryInterval = TimeSpan.FromSeconds(double.Parse(timeout))
                };
            }
            throw new GfycatException(status.Error);
        }

        private static string ProcessUnknown(ConversionStatus status, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report(status.Task);
            return null;
        }

        private string CreateRandomString()
        {
            const string s = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var c = new char[5];
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = s[(int)Math.Floor(Random.NextDouble() * s.Length)];
            }
            return new string(c);
        }
    }
}