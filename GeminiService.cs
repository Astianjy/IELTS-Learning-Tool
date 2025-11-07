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

        public GeminiService(AppConfig config)
        {
            _apiKey = config.GoogleApiKey;
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

        public async Task<List<VocabularyWord>> GetIeltsWordsAsync(int wordCount, List<string> topics)
        {
            var prompt = $@"
Please provide {wordCount} random IELTS core vocabulary words. Select them from the following topics: {string.Join(", ", topics)}.
For each word, provide its phonetics, its Chinese definition (including part of speech and comprehensive meaning), and an example sentence.
Return the response as a valid JSON array. Each object in the array should have the following keys: ""word"", ""phonetics"", ""definition"", ""sentence"".

Example format:
[
  {{
    ""word"": ""ubiquitous"",
    ""phonetics"": ""/juːˈbɪkwɪtəs/"",
    ""definition"": ""adj. 普遍存在的；无所不在的"",
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

        public async Task<Article> GetDailyArticleAsync(List<string> topics, int keyWordsCount)
        {
            // 随机选择一个主题
            var random = new Random();
            string selectedTopic = topics[random.Next(topics.Count)];

            // 生成文章的 prompt
            var articlePrompt = $@"
Please write a comprehensive IELTS-level English article about the topic: ""{selectedTopic}"".
Requirements:
1. The article should be 500-1000 words long.
2. The article should have a clear title.
3. The article should be well-structured with paragraphs.
4. Use appropriate IELTS-level vocabulary and expressions.
5. The content should be informative and engaging.

Return the response as a valid JSON object with the following keys:
- ""title"": the article title (in English)
- ""content"": the full article text (in English, preserve paragraph breaks with \n)

Example format:
{{
  ""title"": ""The Impact of Climate Change on Natural Geography"",
  ""content"": ""Climate change has become one of the most pressing issues of our time...\n\nFurthermore, rising sea levels...\n\nIn conclusion...""
}}";

            string articleResponse = await CallGeminiApiAsync(articlePrompt);

            Article article = new Article { Topic = selectedTopic };

            if (articleResponse.StartsWith("Error:"))
            {
                Console.WriteLine(articleResponse);
                return article;
            }

            try
            {
                var cleanedJson = articleResponse.Trim().Replace("```json", "").Replace("```", "");
                using (JsonDocument doc = JsonDocument.Parse(cleanedJson))
                {
                    JsonElement root = doc.RootElement;
                    article.Title = root.GetProperty("title").GetString() ?? "";
                    article.Content = root.GetProperty("content").GetString() ?? "";
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize article JSON: {ex.Message}");
                Console.WriteLine($"Raw response was: {articleResponse}");
                return article;
            }

            // 如果没有成功获取文章，直接返回
            if (string.IsNullOrWhiteSpace(article.Content))
            {
                return article;
            }

            // 生成中文翻译
            var translationPrompt = $@"
Please translate the following English article into Chinese. 
Preserve the paragraph structure and formatting.
Return only the Chinese translation, without any additional text or formatting.

Article:
{article.Content}";

            string translationResponse = await CallGeminiApiAsync(translationPrompt);
            if (!translationResponse.StartsWith("Error:"))
            {
                article.Translation = translationResponse.Trim();
            }

            // 提取重点词汇
            var keyWordsPrompt = $@"
Please extract {keyWordsCount} key vocabulary words from the following article that are important for IELTS learners.
For each word, provide its phonetics, its Chinese definition (including part of speech and comprehensive meaning), and an example sentence from the article or a similar context.
Return the response as a valid JSON array. Each object in the array should have the following keys: ""word"", ""phonetics"", ""definition"", ""sentence"".

Article:
{article.Content}

Example format:
[
  {{
    ""word"": ""ubiquitous"",
    ""phonetics"": ""/juːˈbɪkwɪtəs/"",
    ""definition"": ""adj. 普遍存在的；无所不在的"",
    ""sentence"": ""The company's logo has become ubiquitous all over the world.""
  }},
  ...
]";

            string keyWordsResponse = await CallGeminiApiAsync(keyWordsPrompt);
            if (!keyWordsResponse.StartsWith("Error:"))
            {
                try
                {
                    var cleanedJson = keyWordsResponse.Trim().Replace("```json", "").Replace("```", "");
                    var words = JsonSerializer.Deserialize<List<VocabularyWord>>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    article.KeyWords = words ?? new List<VocabularyWord>();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to deserialize key words JSON: {ex.Message}");
                    Console.WriteLine($"Raw response was: {keyWordsResponse}");
                }
            }

            return article;
        }
    }
}