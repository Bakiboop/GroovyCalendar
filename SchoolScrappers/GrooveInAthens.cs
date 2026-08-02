using AngleSharp;
using GroovyCalendar.Interfaces;
using GroovyCalendar.Models;
using Microsoft.Playwright;

namespace GroovyCalendar.SchoolScrapers
{
    // Notice the ": ISchoolScraper" - This is where we sign the contract!
    public class GrooveInAthens : ISchoolScraper
    {
        public string SchoolName => "Groove in Athens";
        private readonly string _url = "https://grooveinathens.gr/en/events/category/party/list/?eventDisplay=past";

        public async Task<List<SwingEvent>> ScrapeEventsAsync()
        {
            var eventsList = new List<SwingEvent>();
            Console.WriteLine($"Starting scrape for {SchoolName}...");

            try
            {
                using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

                await using var browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                // 3. Φτιάχνουμε ένα νέο tab και πάμε στο URL
                var page = await browser.NewPageAsync();
                await page.GotoAsync(_url);

                // 4. Περιμένουμε λίγο για σιγουριά ώστε να τρέξουν τα JavaScripts του Groove in Athens
                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

                // 5. Παίρνουμε το τελικό, καθαρό HTML!
                string html = await page.ContentAsync();

                // AngleSharp
                var context = BrowsingContext.New(Configuration.Default);
                var document = await context.OpenAsync(req => req.Content(html));

                // All event blocks
                var eventElements = document.QuerySelectorAll(".tribe-events-calendar-list__event");

                foreach (var eventblock in eventElements)
                {
                    // Πάντα βάζουμε ? πριν από TextContent ή GetAttribute για να γλυτώσουμε τα NullReference Exceptions!
                    var titleElement = eventblock.QuerySelector(".tribe-events-calendar-list__event-title a");
                    var title = titleElement?.TextContent.Trim();
                    var url = titleElement?.GetAttribute("href");

                    var imageUrl = eventblock.QuerySelector(".tribe-events-calendar-list__event-featured-image")?.GetAttribute("src");

                    // Example: "July 5, 2025 @ 10:00 pm - July 6, 2025 @ 2:00 am"
                    var dateTimeFull = eventblock.QuerySelector(".tribe-events-calendar-list__event-datetime")?.TextContent.Trim();

                    var address = eventblock.QuerySelector(".tribe-events-calendar-list__event-venue")?.TextContent.Trim();

                    string eventType = "Swing"; // default

                    var price = eventblock.QuerySelector(".tribe-events-c-small-cta__price")?.TextContent.Trim();
                    if (string.IsNullOrWhiteSpace(price) || !price.Contains('€', StringComparison.OrdinalIgnoreCase))
                        price = "";

                    eventsList.Add(new SwingEvent
                    {
                        Title = title,
                        Type = eventType,
                        Location = address,
                        EventUrl = url,
                        Date = dateTimeFull,
                        Time = "", // Η ώρα είναι ήδη μέσα στο dateTimeFull σύμφωνα με το site τους
                        Price = price,
                        Description = "",
                        SchoolName = this.SchoolName,
                        Dj = "",
                        ImageUrl = imageUrl
                    });

                }

                Console.WriteLine($"Found {eventsList.Count} parties for {SchoolName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {SchoolName}: {ex.Message}");
            }

            return eventsList;
        }
    }
}
