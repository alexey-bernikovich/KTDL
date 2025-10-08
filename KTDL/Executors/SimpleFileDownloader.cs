using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace KTDL.Executors
{
    // TODO: cancellationToken is not used - fix this
    // TODO: add error response
    // TODO: require refactoring
    internal class SimpleFileDownloader : IFileDownloader
    {
        private readonly ILogger<SimpleFileDownloader> _logger;
        private int temp_max = 8;
        private readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        public SimpleFileDownloader(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimpleFileDownloader>();
        }

        public async Task<List<string>> DownloadAlbumFilesAsync(
            string url,
            string outputDir,
            Func<int, int, string, Task> onProgess,
            CancellationToken cancellationToken)
        {
            var songPageLinks = await GetFilesFromPage(url);
            var donwloadedFiles = new List<string>();

            int donwloaded = 0;

            await Parallel.ForEachAsync(songPageLinks,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = temp_max,
                    CancellationToken = cancellationToken
                },
                async (link, ct) =>
                {
                    try
                    {
                        var audioUrl = await GetAudioSrcFromSongPage(link);
                        if (string.IsNullOrEmpty(audioUrl))
                            return;

                        var fileName = MakeSafeFileName(GetFileNameFromUrl(audioUrl));
                        var destPath = Path.Combine(outputDir, fileName);

                        await DownloadFileAsync(audioUrl, destPath);
                        donwloadedFiles.Add(destPath);

                        _logger.LogInformation("Downloaded {File} of {Total} mp3 file(s) ({Percent}%)",
                            ++donwloaded, songPageLinks.Count, 
                            (songPageLinks.Count > 0 ? (donwloaded * 100 / songPageLinks.Count) : 0));

                        await onProgess?.Invoke(donwloaded, songPageLinks.Count, "mp3");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error downloading from {Link}: {Message}", link, ex.Message);
                    }
                }
            );
            return donwloadedFiles;
        }

        public async Task<string> GetAlbumCoverAsync(
            string url, 
            string outputDir, 
            CancellationToken cancellationToken)
        {
            string? imageDestPath = null;
            string albumImageUrl = await GetAlbumImageUrl(url);

            if (!string.IsNullOrEmpty(albumImageUrl))
            {
                try
                {
                    var extension = Path.GetExtension(GetFileNameFromUrl(albumImageUrl));
                    var imageFileName = MakeSafeFileName($"cover{extension}");
                    imageDestPath = Path.Combine(outputDir, imageFileName);

                    await DownloadFileAsync(albumImageUrl, imageDestPath);
                    _logger.LogInformation($"Downloaded album cover");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error downloading album image from {Url}: {Message}", albumImageUrl, ex.Message);
                }
            }
            return imageDestPath;
        }

        public async Task<Dictionary<string, string>> GetAlbumInfoAsync(
            string url,
            Func<int, int, string, Task> onProgress,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string slug = ExtractSlugFromUrl(url);

            if (string.IsNullOrEmpty(slug))
            {
                _logger.LogError("Can't get any slugs from url");
                return result;
            }

            string infoUrl = $"https://vgmtreasurechest.com/soundtracks/{slug}/khinsider.info.txt";
            _logger.LogInformation("Created info URL: {Url}", infoUrl);

            string txt = await _http.GetStringAsync(infoUrl);
            int getCount = 0;

            string? title;
            if (TryGetValue(txt, @"(?mi)^Name:\s*(.+)$", out title))
            {
                result["AlbumTitle"] = title;
                _logger.LogInformation("Downloaded {File} of {Total} info file(s)",
                    ++getCount, 2);
                await onProgress?.Invoke(getCount, 2, "info");
            }

            string? year;
            if (TryGetValue(txt, @"(?mi)^Year:\s*(.+)$", out year))
            {
                result["AlbumYear"] = year;
                _logger.LogInformation("Downloaded {File} of {Total} info file(s)",
                    ++getCount, 2);
                await onProgress?.Invoke(getCount, 2, "info");
            }

            return result;
        }

        private string ExtractSlugFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.TrimEnd('/');
                var slug = path[(path.LastIndexOf('/') + 1)..];
                return slug;
            }
            catch
            {
                return null;
            }
        }

        private bool TryGetValue(string input, string pattern, out string value)
        {
            value = null;
            var m = Regex.Match(input, pattern);

            if(m.Success)
            {
                value = m.Groups[1].Value.Trim();
                return true;
            }
            return false;
        }

        //public static async Task<bool> CheckIfImageIsSquareAsync(string imageUrl)
        //{
        //    using var http = new HttpClient();
        //    using var stream = await http.GetStreamAsync(imageUrl);
        //    using var image = await Image.LoadAsync(stream);

        //    Console.WriteLine($"Image: {image.Width}x{image.Height}");
        //    return image.Width == image.Height;
        //}

        private async Task<List<string>> GetFilesFromPage(string pageUrl)
        {
            var html = await _http.GetStringAsync(pageUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = new ConcurrentBag<string>();

            var table = doc.DocumentNode.SelectSingleNode("//table[@id='songlist']");
            if (table == null)
            {
                _logger.LogError("Table \"songlist\" not found on page.");
                return links.ToList();
            }

            var anchors = table.SelectNodes(".//a[@href]");
            if (anchors == null)
                return links.ToList();

            await Parallel.ForEachAsync(anchors,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = temp_max,
                    CancellationToken = CancellationToken.None
                },
                async (a, ct) =>
                {
                    var href = a.GetAttributeValue("href", null);
                    if (string.IsNullOrEmpty(href))
                        return;

                    links.Add(MakeAbsoluteUrl(pageUrl, href));
                });
            return links.Distinct().ToList();
        }

        private async Task<string?> GetAlbumImageUrl(string pageUrl)
        {
            var html = await _http.GetStringAsync(pageUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var div = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'albumImage')]");
            if (div == null)
            {
                _logger.LogWarning("Album image not found on page.");
                return null;
            }

            var imageLink = div.SelectSingleNode(".//a[@href]");
            if (imageLink == null)
            {
                _logger.LogError("Link to album image not found.");
                return null;
            }

            var href = imageLink.GetAttributeValue("href", null);
            if (string.IsNullOrEmpty(href))
            {
                _logger.LogError("Link to album image not parsed.");
                return null;
            }

            if (Uri.TryCreate(href, UriKind.Absolute, out var abs))
            {
                return abs.ToString();
            }

            if (Uri.TryCreate(new Uri(pageUrl), href, out var rel))
            {
                return rel.ToString();
            }

            //if(!await CheckIfImageIsSquareAsync(result))
            //{
            //    Console.WriteLine("Album image is not square, skipping.");
            //    return null;
            //}

            return href;
        }

        private async Task DownloadFileAsync(string fileUrl, string destPath)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, fileUrl);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Downloader/1.0)");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            using var contentStream = await resp.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 8192, useAsync: true);
            await contentStream.CopyToAsync(fileStream);
        }

        private string GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fname = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(fname))
                {
                    var q = uri.Query;
                    if (!string.IsNullOrEmpty(q))
                        return MakeSafeFileName(WebUtility.UrlDecode(q).Trim('?'));
                    return $"download_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp3";
                }
                return WebUtility.UrlDecode(fname);
            }
            catch
            {
                // TODO: Replace with default name
                return "downloaded.mp3";
            }
        }

        private string MakeSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            if (name.Length > 200) name = name.Substring(name.Length - 200);
            return name;
        }

        private async Task<string?> GetAudioSrcFromSongPage(string songPageUrl)
        {
            var html = await _http.GetStringAsync(songPageUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var audioNode = doc.DocumentNode.SelectSingleNode("//audio[@id='audio' and @src]");
            if (audioNode == null)
            {
                _logger.LogError("Audio element with id='audio' not found on page: {Url}", songPageUrl);
                return null;
            }

            var src = audioNode.GetAttributeValue("src", null);
            if(string.IsNullOrEmpty(src))
            {
                _logger.LogError("Audio element does not have a valid 'src' attribute on page: {Url}", songPageUrl);
                return null;
            }
            return MakeAbsoluteUrl(songPageUrl, src);
        }        

        static string MakeAbsoluteUrl(string baseUrl, string href)
        {
            if (string.IsNullOrWhiteSpace(href))
                return href ?? "";

            if (Uri.TryCreate(href, UriKind.Absolute, out var ab))
                return ab.ToString();

            if (Uri.TryCreate(new Uri(baseUrl), href, out var rel))
                return rel.ToString();

            return href;
        }
    }
}
