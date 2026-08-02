using System.Text;
using System.Text.Json;
using GroovyCalendar.Models;

namespace GroovyCalendar.SchoolScrapers
{
    public class GeminiParser
    {
        // Βάζουμε το κλειδί σου εδώ (προσωρινά)
        private readonly string _apiKey = "AQ.Ab8RN6LPF7WLdDGPazGoxjqzNyZ7y7vauqqUm7yDCGsDkp_ztg";

        public async Task<SwingEvent> ExtractEventInfoAsync(string caption)
        {
            Console.WriteLine("[AI] Sending text to Gemini for parsing...");

            using var client = new HttpClient();
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={_apiKey}";
            // Εδώ δίνουμε τις οδηγίες (Prompt) στο AI!
            string prompt = @"
You are an expert data extractor. I will give you a Greek Instagram post about a Swing Dance Party.
Extract the following information and return it EXACTLY as a JSON object, with no markdown, no ```json tags, and no extra text. 
Use these exact keys: Title, Type, Date, Time, Location, Price, Dj.
If you cannot find a piece of information, use 'N/A'.

Post text:
" + caption;

            // Φτιάχνουμε το JSON που περιμένει το Gemini
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AI ERROR] {responseString}");
                    return null;
                }

                // Διαβάζουμε την απάντηση (το JSON που μας έστειλε το AI)
                using var doc = JsonDocument.Parse(responseString);
                var aiTextResponse = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                // Καθαρίζουμε τυχόν σκουπίδια (όπως ```json)
                aiTextResponse = aiTextResponse.Replace("```json", "").Replace("```", "").Trim();

                // Μετατρέπουμε το JSON κατευθείαν στο δικό σου αντικείμενο SwingEvent!
                var swingEvent = JsonSerializer.Deserialize<SwingEvent>(aiTextResponse);
                return swingEvent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI EXCEPTION] {ex.Message}");
                return null;
            }
        }
    }
}