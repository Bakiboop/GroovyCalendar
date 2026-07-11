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
    public class HoppersInAthens : ISchoolScraper
    {
        public string SchoolName => "Hoppers in Athens";
        private readonly string _url = "https://hoppersinathens.com/en/events/";





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
                var eventElements = document.QuerySelectorAll(".ife_event");

                foreach (var eventblock in eventElements)
                {
                    string imageUrl = "";
                    var styleAttribute = eventblock.QuerySelector(".img_placeholder").GetAttribute("style");

                    // Βεβαιωνόμαστε ότι το style δεν είναι null και περιέχει όντως το "url('"
                    if (!string.IsNullOrEmpty(styleAttribute) && styleAttribute.Contains("url('"))
                    {

                        // Το [0]: "background: url("
                        // Το [1]: "https://hoppersinathens...jpg" 
                        // Το [2]: ") no-repeat left top;"
                        var parts = styleAttribute.Split('\'');
                        if (parts.Length >= 2)
                        {
                            imageUrl = parts[1];
                        }
                    }

                    var title = eventblock.QuerySelector(".event_title").TextContent.Trim();
                    // skip if the title does not contain "Party"
                    if (!title.Contains("Party", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string eventType = "Swing"; // default
                    if (title.Contains("Balboa", StringComparison.OrdinalIgnoreCase))
                    {
                        eventType = "Balboa";
                    }

                    var address = eventblock.QuerySelector(".event_address").TextContent.Trim();
                    var url = eventblock.QuerySelector("a[rel='bookmark']").GetAttribute("href");


                    //Phase 2: Inside each url 
                    Console.WriteLine($"  -> Deep diving into: {title}");
                    var innerHtml = await client.GetStringAsync(url);
                    var innerDocument = await context.OpenAsync(req => req.Content(innerHtml));

                    var description = innerDocument.QuerySelector(".entry-content").TextContent.Trim();

                    var strongTags = innerDocument.QuerySelectorAll("strong");
                    string eventDate = "";
                    string eventTime = "";
                    foreach (var strong in strongTags)
                    {
                        if (strong.TextContent.Contains("Date:"))
                        {
                            eventDate = strong.NextElementSibling.TextContent.Trim();
                        }
                        else if (strong.TextContent.Contains("Time:"))
                        {
                            eventTime = strong.NextElementSibling?.TextContent.Trim();
                        }
                    }

                    var priceLines = new List<string>();
                    string dj = "";
                    var lines = description.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("€") || line.Contains("Είσοδος", StringComparison.OrdinalIgnoreCase))
                        {
                            priceLines.Add(line.Trim());
                        }

                        if (string.IsNullOrEmpty(dj) && (line.Contains("DJ:") || line.Contains("DJs:") || line.Contains("Dj:") || line.Contains("Live Music:")))
                        {
                            dj = line.Trim();
                        }
                    }

                    eventsList.Add(new SwingEvent
                    {
                        Title = title,
                        Type = eventType,
                        Location = address,
                        EventUrl = url,
                        Date = eventDate,
                        Time = eventTime,
                        Price = string.Join(" | ", priceLines),
                        Description = description,
                        SchoolName = this.SchoolName,
                        Dj = dj,
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