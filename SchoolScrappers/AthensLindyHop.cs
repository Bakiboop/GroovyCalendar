using AngleSharp;
using GroovyCalendar.Interfaces;
using GroovyCalendar.Models;

namespace GroovyCalendar.SchoolScrapers
{
    // Notice the ": ISchoolScraper" - This is where we sign the contract!
    public class AthensLindyHop : ISchoolScraper
    {
        public string SchoolName => "Athens Lindy Hop";
        private readonly string _url = "https://www.athenslindyhop.com/news-categories/events/";




        public async Task<List<SwingEvent>> ScrapeEventsAsync()
        {
            var eventsList = new List<SwingEvent>();
            Console.WriteLine($"Starting scrape for {SchoolName}...");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                string html = await client.GetStringAsync(_url);

                var context = BrowsingContext.New(Configuration.Default);
                var document = await context.OpenAsync(req => req.Content(html));

                // Στοχεύουμε τα αντικείμενα της λίστας του Webflow
                var eventElements = document.QuerySelectorAll(".related-news.w-dyn-item");

                foreach (var eventblock in eventElements)
                {

                    var title = eventblock.QuerySelector("h1")?.TextContent.Trim();
                    if (string.IsNullOrEmpty(title) || !title.Contains("Party", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip non-party events
                    }

                    // Παίρνουμε το link (a tag)
                    var linkElement = eventblock.QuerySelector("a");
                    var url = linkElement?.GetAttribute("href");

                    // Αν το URL είναι σχετικό (π.χ. /news/party), το κάνουμε απόλυτο
                    if (!string.IsNullOrEmpty(url) && url.StartsWith("/"))
                    {
                        url = "https://www.athenslindyhop.com" + url;
                    }


                    // Παίρνουμε την εικόνα απευθείας από το img tag
                    var imageUrl = eventblock.QuerySelector("img")?.GetAttribute("src");


                    // Deep Dive: Μπαίνουμε μέσα στο event για την περιγραφή


                    // Αρχικοποίηση μεταβλητών πριν το Deep Dive
                    string description = "";
                    string date = "Unknown Date";
                    string time = "Unknown Time";
                    string price = "Unknown Price";
                    string location = "Unknown Location";
                    if (!string.IsNullOrEmpty(url))
                    {
                        Console.WriteLine($"  -> Deep diving into: {title}");
                        try
                        {
                            var innerHtml = await client.GetStringAsync(url);
                            var innerDocument = await context.OpenAsync(req => req.Content(innerHtml));

                            // Παίρνουμε το κυρίως κείμενο. Το Webflow συνήθως χρησιμοποιεί w-richtext
                            var richText = innerDocument.QuerySelector(".w-richtext");
                            if (richText != null)
                            {

                            }
                            //description = richText.TextContent.Trim();

                            var pTags = richText?.QuerySelectorAll("p");

                            if (pTags != null)
                            {
                                foreach (var p in pTags)
                                {
                                    var text = p.TextContent.Trim();
                                    var lowerText = text.ToLower();

                                    // Προσπαθούμε να βρούμε την Τιμή / Είσοδο
                                    if (lowerText.Contains("είσοδος:") || lowerText.Contains("τιμή:") || lowerText.Contains("ευρώ") || lowerText.Contains("€"))
                                        price = text;

                                    // Προσπαθούμε να βρούμε την Ώρα
                                    else if (lowerText.StartsWith("ώρα:") || lowerText.Contains("έναρξη:"))
                                        time = text.Replace("Ώρα:", "").Trim();

                                    // Προσπαθούμε να βρούμε την τοποθεσία (πολύ βασικό keyword matching, ίσως θέλει βελτίωση)
                                    else if (location == "Unknown Location" && System.Text.RegularExpressions.Regex.IsMatch(text, @"[Α-Ωα-ωΆ-Ώά-ώ]+\s+\d{1,3}\s*,"))
                                        location = text;

                                    // Ημερομηνία: Συνήθως είναι το πρώτο p tag, ή περιέχει μέρες
                                    else if (date == "Unknown Date" && (lowerText.Contains("σάββατο") || lowerText.Contains("κυριακή") || lowerText.Contains("παρασκευή")))
                                        date = text;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"     Failed to get details for {title}: {ex.Message}");
                        }
                    }

                    eventsList.Add(new SwingEvent
                    {
                        Title = title,
                        Type = "Swing", // Προεπιλογή
                        Location = location,
                        EventUrl = url,
                        Date = date,
                        Time = time,
                        Price = price,
                        Description = description,
                        SchoolName = this.SchoolName,
                        Dj = "",
                        ImageUrl = imageUrl
                    });
                }

                Console.WriteLine($"Found {eventsList.Count} potential events/news for {SchoolName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {SchoolName}: {ex.Message}");
            }

            return eventsList;
        }
    }
}
