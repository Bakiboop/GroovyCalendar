using Microsoft.Playwright;

namespace GroovyCalendar.SchoolScrapers
{
    public class InstagramScraper
    {
        public async Task<List<string>> ScrapeLatestPostsAsync(string username)
        {
            var postCaptions = new List<string>();
            string profileUrl = $"https://www.instagram.com/{username}/";

            Console.WriteLine($"\n[LOG] Starting scraper for @{username}");

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, SlowMo = 200 });
                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
                });

                await context.AddInitScriptAsync(@"Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");

                var page = await context.NewPageAsync();
                await page.GotoAsync("https://www.instagram.com/accounts/login/");

                Console.WriteLine("\n*******************************************************");
                Console.WriteLine(" 1. Press Enter when Logged in and Feed is visible.");
                Console.WriteLine("*******************************************************\n");

                await Task.Run(() => Console.ReadLine());

                await page.GotoAsync(profileUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                await page.WaitForTimeoutAsync(3000);

                var postUrls = await GetPostLinksFromPage(page);

                if (postUrls.Count == 0)
                {
                    await page.EvaluateAsync("window.scrollBy(0, 800)");
                    await page.WaitForTimeoutAsync(3000);
                    postUrls = await GetPostLinksFromPage(page);
                }

                Console.WriteLine($"[LOG] Found {postUrls.Count} posts. Checking for parties...");

                int maxPostsToCheck = Math.Min(10, postUrls.Count); // Ελέγχουμε 10 posts για να βρούμε τα πάρτι
                for (int i = 0; i < maxPostsToCheck; i++)
                {
                    await page.GotoAsync(postUrls[i], new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                    await page.WaitForTimeoutAsync(2000);

                    try
                    {
                        var captionElement = await page.QuerySelectorAsync("h1");
                        string caption = captionElement != null ?
                            await captionElement.InnerTextAsync() :
                            await page.EvaluateAsync<string>("() => document.querySelector('meta[property=\"og:description\"]')?.content || ''");

                        // ΕΔΩ ΕΙΝΑΙ ΤΟ ΦΙΛΤΡΟ ΜΑΣ:
                        if (IsPartyPost(caption))
                        {
                            Console.WriteLine($"[SUCCESS] Party post found! -> {postUrls[i]}");
                            postCaptions.Add(caption);
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

            return postCaptions;
        }

        // Η λογική του φίλτρου: Ψάχνει συγκεκριμένες λέξεις
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
    }
}