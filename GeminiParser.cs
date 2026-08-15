using System.Text;
using System.Text.Json;
using GroovyCalendar.Models;

namespace GroovyCalendar.SchoolScrapers
{
    public class GeminiParser
    {
        private readonly string _apiKey = "AQ.Ab8RN6LPF7WLdDGPazGoxjqzNyZ7y7vauqqUm7yDCGsDkp_ztg";

        public async Task<List<SwingEvent>> ExtractAllEventsAsync(List<(string PostUrl, string Caption, string ImageUrl, string Username)> allPosts)
        {
            Console.WriteLine("[AI] Sending BATCH request to Gemini for parsing...");

            using var client = new HttpClient();
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={_apiKey}";

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

                using var doc = JsonDocument.Parse(responseString);
                var aiTextResponse = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                aiTextResponse = aiTextResponse.Replace("```json", "").Replace("```", "").Trim();

                return JsonSerializer.Deserialize<List<SwingEvent>>(aiTextResponse);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI EXCEPTION] {ex.Message}");
                return new List<SwingEvent>();
            }
        }
    }
}