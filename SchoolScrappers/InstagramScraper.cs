using Microsoft.Playwright;
using System.IO;

namespace GroovyCalendar.SchoolScrapers
{
    public class InstagramScraper
    {
        public async Task<List<(string PostUrl, string Caption, string ImageUrl)>> ScrapeLatestPostsAsync(string username)
        {
            {
                var extractedPosts = new List<(string PostUrl, string Caption, string ImageUrl)>();
                string authFile = "auth.json";
                string profileUrl = $"https://www.instagram.com/{username}/";

                Console.WriteLine($"\n[LOG] Starting scraper for @{username}");

                try
                {
                    //Playright setup
                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, SlowMo = 200 });
                    var contextOptions = new BrowserNewContextOptions
                    {
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
                        ViewportSize = new ViewportSize { Width = 1800, Height = 1000 }
                    };

                    if (File.Exists(authFile))
                    {
                        Console.WriteLine("[LOG] Found auth.json! Using saved session...");
                        contextOptions.StorageStatePath = authFile;
                    }

                    var context = await browser.NewContextAsync(contextOptions);
                    await context.AddInitScriptAsync(@"Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");


                    //Login manually to instagram
                    var page = await context.NewPageAsync();
                    if (!File.Exists(authFile))
                    {
                        Console.WriteLine("[LOG] No saved session found. Navigating to login...");
                        await page.GotoAsync("https://www.instagram.com/accounts/login/");
                        Console.WriteLine("\n*******************************************************");
                        Console.WriteLine(" 1. Press Enter when Logged in and Feed is visible.");
                        Console.WriteLine("*******************************************************\n");
                        await Task.Run(() => Console.ReadLine());

                        Console.WriteLine("[LOG] Saving session to auth.json...");
                        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = authFile });
                    }

                    // Navigate to the profile page
                    await page.GotoAsync(profileUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load });

                    try
                    {
                        // Περιμένει δυναμικά μέχρι να σκάσει μύτη έστω και 1 post, το πολύ για 15 δευτερόλεπτα
                        await page.WaitForSelectorAsync("a[href*='/p/']", new PageWaitForSelectorOptions { Timeout = 15000 });
                        Console.WriteLine("[LOG] Posts loaded successfully!");
                    }
                    catch
                    {
                        Console.WriteLine("[WARNING] Timeout waiting for posts, proceeding anyway...");
                    }

                    var postUrls = await GetPostLinksFromPage(page);

                    if (postUrls.Count == 0)
                    {
                        await page.EvaluateAsync("window.scrollBy(0, 800)");
                        await page.WaitForTimeoutAsync(800);
                        postUrls = await GetPostLinksFromPage(page);
                    }
                    Console.WriteLine($"[LOG] Found {postUrls.Count} posts. Checking for parties...");

                    //
                    int maxPostsToCheck = Math.Min(6, postUrls.Count); // Ελέγχουμε 10 posts για να βρούμε τα πάρτι
                    for (int i = 0; i < maxPostsToCheck; i++)
                    {
                        await page.GotoAsync(postUrls[i], new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                        await page.WaitForTimeoutAsync(800);

                        try
                        {
                            //take caption from post
                            var captionElement = await page.QuerySelectorAsync("h1");
                            string caption = "";

                            if (captionElement != null)
                                caption = await captionElement.InnerTextAsync();
                            else
                                caption = await page.EvaluateAsync<string>("() => document.querySelector('meta[property=\"og:description\"]')?.content || ''"); //secret fallback to meta description

                            string imageUrl = await page.EvaluateAsync<string>(@"() => {
    // 1. Δοκιμή: Ψάχνουμε πρώτα στο επίσημο container (_aagv)
    const container = document.querySelector('div._aagv');
    if (container) {
        const img = container.querySelector('img');
        if (img) {
            if (img.srcset) {
                const regex = /(https?:\/\/[^\s]+)\s+(\d+)w/g;
                let match;
                let bestUrl = '';
                let maxW = 0;
                while ((match = regex.exec(img.srcset)) !== null) {
                    const width = parseInt(match[2]);
                    if (width > maxW) {
                        maxW = width;
                        bestUrl = match[1];
                    }
                }
                if (bestUrl) return bestUrl;
            }
            if (img.src) return img.src;
        }
    }

    // 2. Εναλλακτική (Plan B): Ψάχνουμε στα <link rel=""preload"" as=""image""> του <head>
    const preloadLinks = document.querySelectorAll('link[rel=""preload""][as=""image""]');
    for (let link of preloadLinks) {
        const href = link.getAttribute('href');
        
        if (href) {
            // Χρησιμοποιούμε const αντί για bool επειδή είμαστε σε JavaScript!
            const isCdn = href.includes('fbcdn.net') || href.includes('cdninstagram.com');
            const isSystemAsset = href.includes('rsrc.php') || href.includes('.webp');
            const isSmall = href.includes('p150x150') || href.includes('s150x150');

            if (isCdn && !isSystemAsset && !isSmall) {
                return href; // Βρήκαμε την κανονική αφίσα!
            }
        }
    }

    // 3. Έσχατη λύση: og:image
    const metaImg = document.querySelector('meta[property=""og:image""]');
    return metaImg ? metaImg.content : '';
}");



                            // Filter Party posts based on keywords
                            if (IsPartyPost(caption))
                            {
                                Console.WriteLine($"[SUCCESS] Party post found! -> {postUrls[i]}");
                                extractedPosts.Add((postUrls[i], caption, imageUrl));
                            }
                            else
                            {
                                Console.WriteLine($"[SKIP] Not a party post. -> {postUrls[i]}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CRITICAL ERROR] {ex.Message}");
                }

                return extractedPosts;
            }
        }

        #region Helper Methods
        private bool IsPartyPost(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            string lowerText = text.ToLower();
            return lowerText.Contains("party") ||
                   lowerText.Contains("πάρτι") ||
                   lowerText.Contains("dj");
        }

        private async Task<List<string>> GetPostLinksFromPage(IPage page)
        {
            var validUrls = new List<string>();
            var allLinks = await page.Locator("a").AllAsync();
            foreach (var link in allLinks)
            {
                var href = await link.GetAttributeAsync("href");
                if (!string.IsNullOrEmpty(href) && href.Contains("/p/"))
                {
                    string fullUrl = href.StartsWith("http") ? href : $"https://www.instagram.com{href}";
                    if (!validUrls.Contains(fullUrl)) validUrls.Add(fullUrl);
                }
            }
            return validUrls;
        }
        #endregion
    }
}