using System.Net;
using System.Text;
using System.Text.Json;
using GroovyCalendar.Models;

namespace GroovyCalendar.SchoolScrapers
{
    public class GeminiParser
    {
        // Retry settings for when Gemini is "too busy" (503) or rate-limited (429).
        private const int MaxAttempts = 8;                                  // 1 try + 7 retries (~10 min of waiting in total)
        private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(2);

        // Errors where waiting and trying again can help.
        // Errors like 400 (bad request) or 403 (bad API key) are NOT here: retrying those never works.
        private static bool IsTransient(HttpStatusCode code) =>
            code == HttpStatusCode.ServiceUnavailable      // 503 - "model is overloaded / too busy"
            || code == HttpStatusCode.TooManyRequests      // 429 - rate limit
            || code == HttpStatusCode.InternalServerError  // 500
            || code == HttpStatusCode.BadGateway           // 502
            || code == HttpStatusCode.GatewayTimeout;      // 504

        // Exponential backoff: 15s, 30s, 60s, then 120s for every later retry (+ a little random jitter).
        // If Google tells us how long to wait (Retry-After header), we respect that instead.
        private static TimeSpan GetRetryDelay(int attempt, HttpResponseMessage response)
        {
            TimeSpan? serverHint = response?.Headers.RetryAfter?.Delta;
            TimeSpan delay = serverHint ?? TimeSpan.FromSeconds(BaseDelay.TotalSeconds * Math.Pow(2, attempt - 1));
            if (delay > MaxDelay) delay = MaxDelay;
            return delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 3000));
        }

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
                return new List<SwingEvent>(); // Stop if the API key is missing
            }

            Console.WriteLine("[AI] Sending BATCH request to Gemini for parsing...");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";

            // Build one big prompt text, including the Username of each post for the AI.
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
5. If you cannot find a piece of information, use 'N/A'.\
6. If the post has multiple eveents, create a separate object for each event in the JSON array.
7. Beware, some posts may have then key words (party,social, dj) but they are not reffering to a social dance party, do not include those posts. but if the post is about a welcome event or event to their hub or school, include it.

Here are the posts:
{sb.ToString()}";

            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string jsonBody = JsonSerializer.Serialize(requestBody);

            // 0. Send the request with RETRY: if Gemini is "too busy", wait and try again.
            HttpResponseMessage response = null;
            string responseString = "";

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                // A new StringContent each time: the body of a sent request should not be reused.
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                try
                {
                    response = await client.PostAsync(url, content);
                    responseString = await response.Content.ReadAsStringAsync();

                    // Success, or an error that retrying cannot fix -> stop looping.
                    if (response.IsSuccessStatusCode || !IsTransient(response.StatusCode))
                        break;

                    Program.Log($"[API RETRY] Attempt {attempt}/{MaxAttempts}: Gemini returned {(int)response.StatusCode} {response.StatusCode} (busy / rate-limited).");
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    // Network problem or the 5-minute timeout. Also worth retrying.
                    response = null;
                    Program.Log($"[API RETRY] Attempt {attempt}/{MaxAttempts}: network error or timeout: {ex.Message}");
                }

                if (attempt == MaxAttempts)
                {
                    Program.Log($"[API ERROR] Gemini still unavailable after {MaxAttempts} attempts. Giving up for this run.");
                    break;
                }

                TimeSpan delay = GetRetryDelay(attempt, response);
                Program.Log($"[API RETRY] Waiting {delay.TotalSeconds:F0}s before trying again...");
                await Task.Delay(delay);
            }

            try
            {
                if (response == null)
                {
                    // Every attempt failed with a network error / timeout.
                    return new List<SwingEvent>();
                }

                // 1. Check whether the request to the AI failed (e.g. wrong API key, limit reached, etc.)
                if (!response.IsSuccessStatusCode)
                {
                    Program.Log($"[API ERROR] The API returned status code {(int)response.StatusCode} {response.StatusCode}.");
                    Console.WriteLine($"[API RAW RESPONSE]: {responseString}");
                    return new List<SwingEvent>();
                }

                using var doc = JsonDocument.Parse(responseString);

                // 2. Safely check that "candidates" exists
                if (!doc.RootElement.TryGetProperty("candidates", out var candidatesElement) || candidatesElement.GetArrayLength() == 0)
                {
                    Console.WriteLine("[AI ERROR] The 'candidates' field was not found in the response. Was it blocked by the Safety Settings?");
                    Console.WriteLine($"[API RAW RESPONSE]: {responseString}");
                    return new List<SwingEvent>();
                }

                // Safely get the response text
                var aiTextResponse = candidatesElement[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                if (string.IsNullOrWhiteSpace(aiTextResponse))
                {
                    Console.WriteLine("[AI ERROR] The response text was empty.");
                    return new List<SwingEvent>();
                }

                // 3. Strip the Markdown code fences
                aiTextResponse = aiTextResponse.Replace("```json", "").Replace("```", "").Trim();

                // 4. Case-insensitive deserialization (so if the AI writes "title" instead of "Title", the value is not lost)
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<SwingEvent>>(aiTextResponse, options) ?? new List<SwingEvent>();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI EXCEPTION] JSON parsing failed: {ex.Message}");
                return new List<SwingEvent>();
            }
        }
    }
}