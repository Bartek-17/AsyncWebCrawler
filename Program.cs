// (newer dotnet versions include some using statements automatically)
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // alternative websites you can test:
        // string startUrl = "https://httpbin.org/links/10/0";
        // string startUrl = "https://p.lodz.pl";
        string startUrl = "https://www.example.com";
        int maxDepth = 2;
        int maxConcurrentRequests = 5;

        using var client = new HttpClient();
        var semaphore = new SemaphoreSlim(maxConcurrentRequests);
        var visited = new ConcurrentDictionary<string, bool>();
        var cts = new CancellationTokenSource();

        // s - sender (console) e - cancel key event (Ctrl + c)
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Cancelling crawl...");
            cts.Cancel();
        };

        Console.WriteLine($"Starting crawl at {startUrl} to depth {maxDepth}...");
        await CrawlAsync(startUrl, maxDepth, client, semaphore, visited, cts.Token);
        Console.WriteLine("Crawl complete.");
    }

    static async Task CrawlAsync(
        string url,
        int depth,
        HttpClient client,
        SemaphoreSlim semaphore,
        ConcurrentDictionary<string, bool> visited,
        CancellationToken token)
    {
        if (depth < 0 || token.IsCancellationRequested)
            return;

        if (!visited.TryAdd(url, true))
            return;

        Console.WriteLine($"[Depth {depth}] Fetching {url}");
        await semaphore.WaitAsync(token);
        try
        {
            var response = await client.GetAsync(url, token);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Depth {depth}] Skipped {url}: {response.StatusCode}");
                return;
            }

            string html = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Depth {depth}] Fetched {url} ({html.Length} chars)");

            if (depth > 0)
            {
                var links = ExtractLinks(html, url);
                var tasks = new List<Task>();
                foreach (var link in links)
                    tasks.Add(CrawlAsync(link, depth - 1, client, semaphore, visited, token));
                await Task.WhenAll(tasks);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[Depth {depth}] Cancelled {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Depth {depth}] Error {url}: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    static List<string> ExtractLinks(string html, string baseUrl)
    {
        var links = new List<string>();
        var regex = new Regex(
            "<a[^>]+href=[\"']([^\"'<>]+)[\"']",  // extract links (href attributes) from <a> (anchor) HTML tag
            RegexOptions.IgnoreCase);

        var baseUri = new Uri(baseUrl);
        foreach (Match m in regex.Matches(html))
        {
            var href = m.Groups[1].Value.Trim();
            if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                continue;

            // Resolve relative URLs
            if (!Uri.TryCreate(href, UriKind.Absolute, out var absUri))
                absUri = new Uri(baseUri, href);

            links.Add(absUri.ToString());
        }

        return links;
    }
}
