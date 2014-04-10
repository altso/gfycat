using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gfycat
{
    public class GifConverter : IGifConverter
    {
        private const string UploadUrlPattern = "http://upload.gfycat.com/transcode/{0}?fetchUrl={1}";
        private static readonly Regex RetryRegex = new Regex(@"Sorry, please wait another (?<timeout>\d+) seconds before your next upload.");
        private static readonly Random Random = new Random();

        public async Task<Uri> ConvertAsync(Uri gifUri, IProgress<string> progress, CancellationToken cancellationToken)
        {
            string key = CreateRandomString();
            string url = string.Format(UploadUrlPattern, key, Uri.EscapeUriString(TrimProtocol(gifUri.ToString())));

            var uploadResponse = await GetObjectAsync<UploadResponse>(new Uri(url)).ConfigureAwait(false);
            if (uploadResponse.Error != null)
            {
                var match = RetryRegex.Match(uploadResponse.Error);
                if (match.Success)
                {
                    var timeout = match.Groups["timeout"].Value;

                    throw new GfycatRetryException
                    {
                        RetryInterval = TimeSpan.FromSeconds(double.Parse(timeout))
                    };
                }
                throw new GfycatException(uploadResponse.Error);
            }

            return new Uri(uploadResponse.Mp4Url, UriKind.Absolute);
        }

        private static async Task<T> GetObjectAsync<T>(Uri uri)
        {
            var json = await new HttpClient().GetStringAsync(uri).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static string CreateRandomString()
        {
            const string s = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var c = new char[5];
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = s[(int)Math.Floor(Random.NextDouble() * s.Length)];
            }
            return new string(c);
        }

        private static string TrimProtocol(string url)
        {
            return new[] { "http://", "https://" }
                .Where(p => url.StartsWith(p, StringComparison.CurrentCultureIgnoreCase))
                .Select(p => url.Substring(p.Length))
                .Union(new[] { url })
                .First();
        }

        public class UploadResponse
        {
            public string GfyName { get; set; }
            public string Mp4Url { get; set; }
            public string Error { get; set; }
        }

    }
}