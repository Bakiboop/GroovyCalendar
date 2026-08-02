using GroovyCalendar.Models;
using GroovyCalendar.SchoolScrapers;

namespace GroovyCalendar
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting GroovyCalendar Instagram Test...\n");

            var instaScraper = new InstagramScraper();
            var aiParser = new GeminiParser();
            var allEvents = new List<SwingEvent>();

            // Τρέχουμε τον Scraper
            var partyCaptions = await instaScraper.ScrapeLatestPostsAsync("groove_inathens");

            Console.WriteLine($"\n=== FOUND {partyCaptions.Count} PARTY POSTS. SENDING TO AI... ===");

            foreach (var caption in partyCaptions)
            {
                // Στέλνουμε το καθαρισμένο κείμενο στο Gemini
                var parsedEvent = await aiParser.ExtractEventInfoAsync(caption);

                if (parsedEvent != null)
                {
                    allEvents.Add(parsedEvent);
                }
            }

            Console.WriteLine("\n=== FINAL CALENDAR ===");

            if (allEvents.Count == 0)
            {
                Console.WriteLine("No parties parsed successfully.");
            }
            else
            {
                foreach (var ev in allEvents)
                {
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine($"TITLE:    {ev.Title}");
                    Console.WriteLine($"TYPE:     {ev.Type}");
                    Console.WriteLine($"DATE:     {ev.Date} at {ev.Time}");
                    Console.WriteLine($"LOCATION: {ev.Location}");
                    Console.WriteLine($"PRICE:    {ev.Price}");
                    Console.WriteLine($"DJ:       {ev.Dj}");
                }
            }

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("\nScraping finished!");
        }
    }
}