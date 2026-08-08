using System.Text;
using System.Text.Json;
using GroovyCalendar.Models;

namespace GroovyCalendar.SchoolScrapers
{
    public class GeminiParser
    {
        private readonly string _apiKey = "AQ.Ab8RN6LPF7WLdDGPazGoxjqzNyZ7y7vauqqUm7yDCGsDkp_ztg";

        public async Task<SwingEvent> ExtractEventInfoAsync(string caption)
        {
            Console.WriteLine("[AI] Sending text to Gemini for parsing...");

            using var client = new HttpClient();

            // Πάμε με το 3.5 Flash που είδαμε ότι υπάρχει σίγουρα στη λίστα σου
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={_apiKey}";

            string prompt = @"You are an expert data extractor. I will give you a Greek Instagram post about a Swing Dance Party.Extract the following information and return it EXACTLY as a JSON object, with no markdown, no ```json tags, and no extra text. 
                            Use these exact keys: Title, Date, Time, Location, Price, Dj, SchoolName.
                            For the 'SchoolName' key, always fill it, and use the value from the username's post, but clean it up, for example (hoppers_in_athens -> Hoppers in Athens,groove_inathens -> Groove in Athens, jumpnjive.gr -> Jump n Jive etc).
                            If you cannot find a piece of information, use 'N/A'.
                            Post text:" + caption;

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
                    // Προσθέσαμε τον κωδικό κατάστασης (π.χ. 400, 403, 500) για να ξέρουμε τι φταίει
                    Console.WriteLine($"[AI ERROR] Status Code: {(int)response.StatusCode} ({response.StatusCode})");
                    Console.WriteLine($"[AI ERROR] Response: {responseString}");
                    return null;
                }

                using var doc = JsonDocument.Parse(responseString);
                var aiTextResponse = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                aiTextResponse = aiTextResponse.Replace("```json", "").Replace("```", "").Trim();

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