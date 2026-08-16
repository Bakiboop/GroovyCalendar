using GroovyCalendar.Models;
using GroovyCalendar.SchoolScrapers;
using System.Text.Json;

namespace GroovyCalendar
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting GroovyCalendar Instagram Test...\n");

            var instaScraper = new InstagramScraper();
            var aiParser = new GeminiParser();

            var profilesToScrape = new List<string>
            {
                "groove_inathens",
                "athenslindyhop",
                "swing_that_thing_athens",
                "jumpnjive.gr",
                "athensbalboa",
                "jazz_reactor",
                "hoppers_in_athens",
                "bluefox_athens",
                "stompingr",
                "rollinfoxes",
                "athens_boogie"
                // Πρόσθεσε όσα θες εδώ, απλά βάζοντας το username!
            };

            var allScrapedPosts = new List<(string PostUrl, string Caption, string ImageUrl, string Username)>();

            foreach (var instaHandle in profilesToScrape)
            {
                Console.WriteLine($"\n🔍 SCANNING PROFILE: @{instaHandle}");
                var scrapedPosts = await instaScraper.ScrapeLatestPostsAsync(instaHandle);

                foreach (var post in scrapedPosts)
                {
                    allScrapedPosts.Add((post.PostUrl, post.Caption, post.ImageUrl, instaHandle));
                }
            }
            Console.WriteLine($"\n=== FINISHED SCRAPING. FOUND {allScrapedPosts.Count} TOTAL PARTY POSTS. SENDING TO AI... ===");

            var allEvents = await aiParser.ExtractAllEventsAsync(allScrapedPosts);
            // Τρέχουμε τον Scraper
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

                // Βρίσκει τον φάκελο που τρέχει το πρόγραμμα και πηγαίνει "έναν φάκελο πίσω" (..) για να βρει το frontend
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Ανεβαίνουμε επίπεδα (από το bin/Debug/net8.0) για να φτάσουμε στον κεντρικό φάκελο GroovyCalendar
                string projectRoot = Directory.GetParent(baseDir).Parent.Parent.Parent.FullName;
                string reactPublicPath = Path.Combine(projectRoot, "..", "frontend", "public", "events.json");

                // Ένας μικρός έλεγχος για να δούμε πού πάει να το σώσει
                Console.WriteLine($"[LOG] Saving to dynamic path: {reactPublicPath}");

                try
                {
                    // 1. Ομαδοποιούμε τα events που είναι διπλότυπα (με βάση την Ημερομηνία και τον Τίτλο τους)
                    var uniqueEvents = allEvents
                        .GroupBy(e => new { e.Date, Title = e.Title.Trim().ToLower() })
                        .Select(group =>
                        {
                            // Παίρνουμε το πρώτο σαν βάση
                            var mergedEvent = group.First();

                            // Μαζεύουμε όλα τα διαφορετικά SchoolNames από τα collab posts
                            var schools = group.Select(e => e.SchoolName).Distinct().ToList();

                            // Τα ενώνουμε με " & " (π.χ. "Groove in Athens & Stomping Ground")
                            mergedEvent.SchoolName = string.Join(" & ", schools);

                            return mergedEvent;
                        })
                        .ToList();

                    var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                    string jsonString = JsonSerializer.Serialize(uniqueEvents, options);
                    File.WriteAllText(reactPublicPath, jsonString);

                    Console.WriteLine($"\n[SUCCESS] Saved {uniqueEvents.Count} unique events (after merging collabs) to: {reactPublicPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERROR] Could not save JSON file: {ex.Message}");
                }
            } // <--- Κλείνει το else

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("\nScraping finished!");
        }
    }
}