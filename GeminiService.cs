using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IELTS_Learning_Tool
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return ParseResponse(responseBody);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Error: {response.StatusCode} - {errorBody}";
            }
        }

        private string ParseResponse(string responseBody)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
                    {
                        if (candidates[0].TryGetProperty("content", out JsonElement content))
                        {
                            if (content.TryGetProperty("parts", out JsonElement parts) && parts.GetArrayLength() > 0)
                            {
                                if (parts[0].TryGetProperty("text", out JsonElement text))
                                {
                                    return text.GetString() ?? "No text found in response.";
                                }
                            }
                        }
                    }
                    return "Could not parse the response.";
                }
            }
            catch (JsonException ex)
            {
                return $"Error parsing JSON response: {ex.Message}. Raw response: {responseBody}";
            }
        }

        public async Task<List<VocabularyWord>> GetIeltsWordsAsync()
        {
            var topics = new[] { "natural geography", "plant research", "animal protection", "space exploration", "school education", "technological inventions", "cultural history", "language evolution", "entertainment and sports", "materials and substances", "fashion trends", "diet and health", "architecture and places", "transport and travel", "international government", "social economy", "laws and regulations", "battlefield conflicts", "social roles", "behaviors and actions", "body and health", "time and dates" };
            
            var prompt = $@"
Please provide 20 random IELTS core vocabulary words. Select them from the following topics: {string.Join(", ", topics)}.
For each word, provide its phonetics, its English definition, and an example sentence.
Return the response as a valid JSON array. Each object in the array should have the following keys: ""word"", ""phonetics"", ""definition"", ""sentence"".

Example format:
[
  {{
    ""word"": ""ubiquitous"",
    ""phonetics"": ""/juːˈbɪkwɪtəs/"",
    ""definition"": ""Present, appearing, or found everywhere."",
    ""sentence"": ""The company's logo has become ubiquitous all over the world.""
  }},
  ...
]";

            string response = await CallGeminiApiAsync(prompt);

            if (response.StartsWith("Error:"))
            {
                Console.WriteLine(response);
                return new List<VocabularyWord>();
            }
            
            try
            {
                // Clean the response to ensure it's valid JSON
                var cleanedJson = response.Trim().Replace("```json", "").Replace("```", "");
                var words = JsonSerializer.Deserialize<List<VocabularyWord>>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return words ?? new List<VocabularyWord>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize word list JSON: {ex.Message}");
                Console.WriteLine($"Raw response was: {response}");
                return new List<VocabularyWord>();
            }
        }

        public async Task<List<VocabularyWord>> EvaluateTranslationsAsync(List<VocabularyWord> words)
        {
            var evaluationRequest = new List<object>();
            foreach (var word in words)
            {
                evaluationRequest.Add(new { originalSentence = word.Sentence, userTranslation = word.UserTranslation });
            }

            var prompt = $@"
Please evaluate the following list of Chinese translations for the given English sentences.
For each sentence, provide a score from 1 to 10, a corrected Chinese translation, and a brief explanation for the score.
The input is a JSON array of objects, each containing the original sentence and the user's translation.
Return a valid JSON array of objects as your response. Each object in the array must have the following keys: ""score"", ""correctedTranslation"", ""explanation"".

Input:
{JsonSerializer.Serialize(evaluationRequest)}

Example output format:
[
  {{
    ""score"": 8,
    ""correctedTranslation"": ""这家公司的标志在世界各地已经无处不在。"",
    ""explanation"": ""翻译准确，但'变得'可以省略，使句子更简洁。""
  }},
  ...
]";

            string response = await CallGeminiApiAsync(prompt);

            if (response.StartsWith("Error:"))
            {
                Console.WriteLine(response);
                return words;
            }

            try
            {
                var cleanedJson = response.Trim().Replace("```json", "").Replace("```", "");
                var results = JsonSerializer.Deserialize<List<JsonElement>>(cleanedJson);

                if (results != null && results.Count == words.Count)
                {
                    for (int i = 0; i < words.Count; i++)
                    {
                        var result = results[i];
                        words[i].Score = result.GetProperty("score").GetInt32();
                        words[i].CorrectedTranslation = result.GetProperty("correctedTranslation").GetString() ?? "";
                        words[i].Explanation = result.GetProperty("explanation").GetString() ?? "";
                    }
                }
                return words;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize evaluation result JSON: {ex.Message}");
                Console.WriteLine($"Raw response was: {response}");
                return words;
            }
        }
    }
}