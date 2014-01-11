using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Gfycat
{
    class Program
    {
        static void Main(string[] args)
        {
            string url;
            if (args.Length < 1)
            {
                do
                {
                    Console.Write("Enter gif url: ");
                    url = Console.ReadLine();
                } while (string.IsNullOrWhiteSpace(url));
            }
            else
            {
                url = args[0];
            }

            Run(new Uri(url, UriKind.Absolute)).Wait();
        }

        private static async Task Run(Uri gifUri)
        {
            var uri = await GifConvert.ConvertAsync(gifUri, new Progress<string>(Console.WriteLine), CancellationToken.None);
            Console.WriteLine(uri);
            Process.Start(uri.ToString());
        }
    }
}
