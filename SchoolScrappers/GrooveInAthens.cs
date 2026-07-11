using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using GroovyCalendar.Interfaces;
using GroovyCalendar.Models;

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
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                string html = await client.GetStringAsync(_url);

                // AngleSharp
                var context = BrowsingContext.New(Configuration.Default);
                var document = await context.OpenAsync(req => req.Content(html));

                // All event blocks
                var eventElements = document.QuerySelectorAll(".tribe-events-calendar-list__event");

                foreach (var eventblock in eventElements)
                {
                    var titleElement = eventblock.QuerySelector(".tribe-events-calendar-list__event-title a");
                    var title = titleElement.TextContent.Trim();
                    var url = titleElement.GetAttribute("href");

                    var imageUrl = eventblock.QuerySelector(".tribe-events-calendar-list__event-featured-image").GetAttribute("src");

                    // Example: "July 5, 2025 @ 10:00 pm - July 6, 2025 @ 2:00 am"
                    var dateTimeFull = eventblock.QuerySelector(".tribe-events-calendar-list__event-datetime").TextContent.Trim();

                    var address = eventblock.QuerySelector(".tribe-events-calendar-list__event-venue").TextContent.Trim();
                    //address = System.Text.RegularExpressions.Regex.Replace(address, @"\s+", " ");

                    string eventType = "Swing"; // default

                    //var address = eventblock.QuerySelector(".event_address").TextContent.Trim();
                    //var url = eventblock.QuerySelector("a[rel='bookmark']").GetAttribute("href");

                    var price = eventblock.QuerySelector(".tribe-events-c-small-cta__price").TextContent.Trim();



                    eventsList.Add(new SwingEvent
                    {
                        Title = title,
                        Type = eventType,
                        Location = address,
                        EventUrl = url,
                        Date = dateTimeFull,
                        Time = "",
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