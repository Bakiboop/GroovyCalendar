using GroovyCalendar.SchoolScrapers;
using System.Text.Json;
using GroovyCalendar.Models;

namespace GroovyCalendar
{
    class Program
    {
        //Log
        public static List<string> AppLogs = new List<string>();

        public static void Log(string message)
        {
            Console.WriteLine(message);
            AppLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting GroovyCalendar Instagram Test...\n");

            var instaScraper = new InstagramScraper();
            var aiParser = new GeminiParser();

            // 1. Ορίζουμε το path για το events.json
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Directory.GetParent(baseDir).Parent.Parent.Parent.FullName;
            string reactPublicPath = Path.Combine(projectRoot, "frontend", "public", "events.json");
            // 2. Διαβάζουμε τα ΥΠΑΡΧΟΝΤΑ events για να βρούμε τα URLs (ώστε να μην τα ξανακατεβάσουμε)
            List<string> existingUrls = new List<string>();
            string existingJson = "[]";

            if (File.Exists(reactPublicPath))
            {
                existingJson = File.ReadAllText(reactPublicPath);
                using (JsonDocument doc = JsonDocument.Parse(existingJson))
                {
                    foreach (JsonElement element in doc.RootElement.EnumerateArray())
                    {
                        // Αν το event έχει ήδη URL (EventUrl ή PostUrl ανάλογα πώς το έχεις ονομάσει), το κρατάμε
                        if (element.TryGetProperty("EventUrl", out JsonElement urlElement) ||
                            element.TryGetProperty("PostUrl", out urlElement))
                        {
                            existingUrls.Add(urlElement.GetString());
                        }
                    }
                }
                Console.WriteLine($"[LOG] Found {existingUrls.Count} existing events in database. Scraper will skip them.");
            }

            var profilesToScrape = new List<string>
            {
                "groove_inathens", "athenslindyhop", "swing_that_thing_athens",
                "jumpnjive.gr", "athensbalboa", "jazz_reactor", "hoppers_in_athens",
                "bluefox_athens", "stompingr", "rollinfoxes", "athens_boogie"
            };

            var allScrapedPosts = new List<(string PostUrl, string Caption, string ImageUrl, string Username)>();

            foreach (var instaHandle in profilesToScrape)
            {
                Console.WriteLine($"\n🔍 SCANNING PROFILE: @{instaHandle}");

                // 3. Περνάμε τη λίστα με τα υπάρχοντα URLs στον Scraper!
                var scrapedPosts = await instaScraper.ScrapeLatestPostsAsync(instaHandle, existingUrls);

                foreach (var post in scrapedPosts)
                {
                    allScrapedPosts.Add((post.PostUrl, post.Caption, post.ImageUrl, instaHandle));
                }
            }

            Console.WriteLine($"\n=== FINISHED SCRAPING. FOUND {allScrapedPosts.Count} NEW PARTY POSTS. SENDING TO AI... ===");

            // 4. Στέλνουμε στο AI ΜΟΝΟ τα καινούργια! Το AI γλιτώνει χρόνο και εσύ tokens.
            var newEvents = await aiParser.ExtractAllEventsAsync(allScrapedPosts);
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            Console.WriteLine("\n=== MERGING WITH EXISTING CALENDAR ===");

            var existingEvents = JsonSerializer.Deserialize<List<SwingEvent>>(existingJson, options) ?? new List<SwingEvent>();

            var allEvents = new List<SwingEvent>();
            allEvents.AddRange(existingEvents); // Βάζουμε τα παλιά
            allEvents.AddRange(newEvents);      // Προσθέτουμε τα νέα

            if (allEvents.Count == 0)
            {
                Console.WriteLine("No parties in calendar.");
            }
            else
            {
                // 6. Ξεκαθαρίζουμε διπλότυπα & Merge collabs από όλη τη βάση πλέον
                var uniqueEvents = allEvents
                    .GroupBy(e => new { e.Date, Title = e.Title.Trim().ToLower() })
                    .Select(group =>
                    {
                        var mergedEvent = group.First();
                        var schools = group.Select(e => e.SchoolName).Distinct().ToList();
                        mergedEvent.SchoolName = string.Join(" & ", schools);
                        return mergedEvent;
                    })
                    .ToList();

                foreach (var ev in uniqueEvents)
                {
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine($"TITLE:    {ev.Title}");
                    Console.WriteLine($"DATE:     {ev.Date} at {ev.Time}");
                    Console.WriteLine($"SCHOOL:   {ev.SchoolName}");
                }

                Console.WriteLine($"[LOG] Saving to dynamic path: {reactPublicPath}");

                try
                {
                    string jsonString = JsonSerializer.Serialize(uniqueEvents, options);
                    File.WriteAllText(reactPublicPath, jsonString);
                    Console.WriteLine($"\n[SUCCESS] Saved {uniqueEvents.Count} total events to JSON!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERROR] Could not save JSON file: {ex.Message}");
                }
            }

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("\nScraping finished!");


            //Logging to JSON file

            Log("--------------------------------------------------");
            Log("\nScraping finished!");

            // --- ΝΕΟΣ ΚΩΔΙΚΑΣ: ΑΠΟΘΗΚΕΥΣΗ LOGS ΣΕ JSON ---
            try
            {
                string logFilePath = Path.Combine(projectRoot, "frontend", "public", "scraper_logs.json");
                // Δημιουργούμε το αντικείμενο του σημερινού τρεξίματος
                var currentRunLog = new
                {
                    RunDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalMessages = AppLogs.Count,
                    Messages = AppLogs
                };


                var allHistoryLogs = new List<object>();
                if (File.Exists(logFilePath))
                {
                    string existingLogs = File.ReadAllText(logFilePath);
                    allHistoryLogs = JsonSerializer.Deserialize<List<object>>(existingLogs) ?? new List<object>();
                }

                allHistoryLogs.Add(currentRunLog);

                // Τα κάνουμε save!
                string logJsonString = JsonSerializer.Serialize(allHistoryLogs, options);
                File.WriteAllText(logFilePath, logJsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save logs: {ex.Message}");
            }
        }
    }
}
