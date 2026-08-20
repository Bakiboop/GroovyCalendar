using System.Text;
using System.Text.Json;
using GroovyCalendar.Models;

namespace GroovyCalendar.SchoolScrapers
{
    public class GeminiParser
    {

        public async Task<List<SwingEvent>> ExtractAllEventsAsync(List<(string PostUrl, string Caption, string ImageUrl, string Username)> allPosts)
        {
            string apiKey = "";
            try
            {
                string settingsJson = File.ReadAllText("appsettings.json");
                using JsonDocument doc = JsonDocument.Parse(settingsJson);
                apiKey = doc.RootElement.GetProperty("GeminiApiKey").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CRITICAL ERROR] Could not read API Key from appsettings.json!");
                Console.WriteLine($"Make sure the file exists and is copied to the output directory. Error: {ex.Message}");
                return new List<SwingEvent>(); // Σταματάει αν δεν βρει το κλειδί
            }

            Console.WriteLine("[AI] Sending BATCH request to Gemini for parsing...");

            using var client = new HttpClient();
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";

            // Χτίζουμε το τεράστιο κείμενο, δίνοντας στο AI και το Username του κάθε post!
            var sb = new StringBuilder();
            for (int i = 0; i < allPosts.Count; i++)
            {
                sb.AppendLine($"--- POST #{i + 1} ---");
                sb.AppendLine($"USERNAME: {allPosts[i].Username}");
                sb.AppendLine($"URL: {allPosts[i].PostUrl}");
                sb.AppendLine($"IMAGE: {allPosts[i].ImageUrl}");
                sb.AppendLine($"CAPTION: {allPosts[i].Caption}");
                sb.AppendLine("------------------\n");
            }

            string prompt = $@"
You are an expert data extractor. I am giving you multiple Greek Instagram posts about Swing Dance Parties, separated by lines.
For EACH post, extract the information and return an EXACT JSON array of objects `[ {{...}}, {{...}} ]`, with no markdown, no ```json tags, and no extra text. 
Use these exact keys: Title, Date, Time, Location, Price, Dj, SchoolName, ImageUrl, EventUrl.

CRITICAL RULES:
1. For the 'SchoolName' key, use the USERNAME provided for that specific post and clean it up (e.g. hoppers_in_athens -> Hoppers in Athens, groove_inathens -> Groove in Athens, jumpnjive.gr -> Jump n Jive, etc).
2. The 'Date' key MUST be in the strict format YYYY-MM-DD. Assume the current year is 2026.
3. 'ImageUrl' MUST be the exact URL provided in the IMAGE field of that post.
4. 'EventUrl' MUST be the exact URL provided in the URL field of that post.
5. If you cannot find a piece of information, use 'N/A'.

Here are the posts:
{sb.ToString()}";

            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                // 1. Έλεγχος αν το Request στο AI απέτυχε (π.χ. λάθος API Key, Limit Reached κλπ)
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[API ERROR] Το API επέστρεψε κωδικό {response.StatusCode}.");
                    Console.WriteLine($"[API RAW RESPONSE]: {responseString}");
                    return new List<SwingEvent>();
                }

                using var doc = JsonDocument.Parse(responseString);

                // 2. Ασφαλής έλεγχος αν υπάρχει το "candidates"
                if (!doc.RootElement.TryGetProperty("candidates", out var candidatesElement) || candidatesElement.GetArrayLength() == 0)
                {
                    Console.WriteLine("[AI ERROR] Δεν βρέθηκε το πεδίο 'candidates' στην απάντηση. Μήπως μπλοκαρίστηκε από τα Safety Settings;");
                    Console.WriteLine($"[API RAW RESPONSE]: {responseString}");
                    return new List<SwingEvent>();
                }

                // Παίρνουμε το κείμενο με ασφάλεια
                var aiTextResponse = candidatesElement[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                if (string.IsNullOrWhiteSpace(aiTextResponse))
                {
                    Console.WriteLine("[AI ERROR] Το κείμενο (text) της απάντησης ήταν άδειο.");
                    return new List<SwingEvent>();
                }

                // 3. Καθαρισμός του Markdown
                aiTextResponse = aiTextResponse.Replace("```json", "").Replace("```", "").Trim();

                // 4. Case-insensitive Deserialization (ώστε αν το AI γράψει "title" αντί για "Title" να μην χαθεί)
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<SwingEvent>>(aiTextResponse, options) ?? new List<SwingEvent>();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI EXCEPTION] Απέτυχε το Parsing του JSON: {ex.Message}");
                return new List<SwingEvent>();
            }
        }
    }
}