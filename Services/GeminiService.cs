using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IELTS_Learning_Tool.Configuration;
using IELTS_Learning_Tool.Models;
using IELTS_Learning_Tool.Utils;

namespace IELTS_Learning_Tool.Services
{
    public class GeminiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly UsageTrackerService? _usageTrackerService;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;

        public GeminiService(
            AppConfig config, 
            UsageTrackerService? usageTrackerService = null)
        {
            _apiKey = config.GoogleApiKey;
            _usageTrackerService = usageTrackerService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120); // 设置超时时间为120秒
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private async Task<string> CallGeminiApiAsync(string prompt, int retryCount = 0)
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

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return ParseResponse(responseBody);
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    
                    // 对于特定错误码，进行重试
                    if ((response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                         response.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                         response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) && 
                        retryCount < MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs * (retryCount + 1)); // 指数退避
                        return await CallGeminiApiAsync(prompt, retryCount + 1);
                    }
                    
                    return $"Error: {response.StatusCode} - {errorBody}";
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                if (retryCount < MaxRetries)
                {
                    await Task.Delay(RetryDelayMs * (retryCount + 1));
                    return await CallGeminiApiAsync(prompt, retryCount + 1);
                }
                return $"Error: Request timeout after {retryCount + 1} attempts";
            }
            catch (HttpRequestException ex)
            {
                if (retryCount < MaxRetries)
                {
                    await Task.Delay(RetryDelayMs * (retryCount + 1));
                    return await CallGeminiApiAsync(prompt, retryCount + 1);
                }
                return $"Error: Network error - {ex.Message}";
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
            // 获取已使用的词汇和例句，避免重复
            var usedWords = _usageTrackerService != null
                ? _usageTrackerService.GetRecord().UsedWords.ToList()
                : new List<string>();
            
            var usedSentences = _usageTrackerService != null
                ? _usageTrackerService.GetRecord().UsedSentences.ToList()
                : new List<string>();
            
            // 使用AI从雅思词汇中生成题目，避免重复
            return await GetWordsFromAIWithAntiRepeatAsync(wordCount, topics, usedWords, usedSentences);
        }
        
        /// <summary>
        /// 使用AI从雅思词汇中生成题目，避免重复
        /// </summary>
        private async Task<List<VocabularyWord>> GetWordsFromAIWithAntiRepeatAsync(
            int wordCount, 
            List<string> topics, 
            List<string> usedWords,
            List<string> usedSentences)
        {
            // 构建避免重复的提示词
            string antiRepeatContext = "";
            if (usedWords.Count > 0)
            {
                var usedWordsSample = usedWords.Take(50).ToList(); // 最多显示50个已使用的词汇
                antiRepeatContext = $@"

IMPORTANT - AVOID REPETITION:
The following words have already been used in previous sessions. Please DO NOT use these words:
{string.Join(", ", usedWordsSample)}
{(usedWords.Count > 50 ? $"\n(And {usedWords.Count - 50} more words that have been used previously)" : "")}

Please select COMPLETELY DIFFERENT words that are appropriate for IELTS examination.";
            }
            
            // 构建例句去重提示
            string sentenceContext = "";
            if (usedSentences.Count > 0)
            {
                var usedSentencesSample = usedSentences.Take(20).ToList(); // 最多显示20个已使用的例句
                sentenceContext = $@"

IMPORTANT - UNIQUE SENTENCES:
Please ensure that the example sentences are COMPLETELY DIFFERENT from these previously used sentences:
{string.Join("\n", usedSentencesSample.Select((s, i) => $"{i + 1}. {s}"))}
{(usedSentences.Count > 20 ? $"\n(And {usedSentences.Count - 20} more sentences that have been used previously)" : "")}

Each sentence must be unique, creative, and demonstrate the word's meaning in a different context.";
            }
            
            var prompt = $@"
You are an IELTS vocabulary expert. Please provide EXACTLY {wordCount} IELTS core vocabulary words from the following topics: {string.Join(", ", topics)}.

CRITICAL REQUIREMENTS:
1. You MUST return EXACTLY {wordCount} words, no more, no less.
2. Select words that are commonly tested in IELTS examinations (band 6.5-8.0 level).
3. Ensure diversity in parts of speech (include nouns, verbs, adjectives, adverbs, etc.).
4. Each word must be relevant to the given topics.
5. For each word, provide:
   - Accurate phonetics in American English pronunciation (IPA format, US pronunciation)
   - Chinese definition (including part of speech and comprehensive meaning)
   - A unique, natural example sentence that clearly demonstrates the word's usage
6. The example sentences should be varied, creative, and appropriate for academic English.
7. Use American English phonetics (US pronunciation) for all words.
{antiRepeatContext}
{sentenceContext}

Return the response as a valid JSON array. Each object in the array must have the following keys: ""word"", ""phonetics"", ""definition"", ""sentence"".

Example format (use American English phonetics):
[
  {{
    ""word"": ""ubiquitous"",
    ""phonetics"": ""/juˈbɪkwɪtəs/"",
    ""definition"": ""adj. 普遍存在的；无所不在的"",
    ""sentence"": ""The company's logo has become ubiquitous all over the world.""
  }},
  {{
    ""word"": ""mitigate"",
    ""phonetics"": ""/ˈmɪtɪˌɡeɪt/"",
    ""definition"": ""v. 减轻；缓解"",
    ""sentence"": ""Governments must take action to mitigate the effects of climate change.""
  }}
]

REMEMBER: Return EXACTLY {wordCount} words, and use American English (US) pronunciation for all phonetics.";

            string response = await CallGeminiApiAsync(prompt);

            if (response.StartsWith("Error:"))
            {
                Console.WriteLine(response);
                return new List<VocabularyWord>();
            }
            
            try
            {
                var cleanedJson = CleanJsonResponse(response);
                var words = JsonSerializer.Deserialize<List<VocabularyWord>>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (words == null || words.Count == 0)
                {
                    return new List<VocabularyWord>();
                }
                
                // 严格限制返回数量为配置的数量
                // 如果AI返回的数量超过请求的数量，只取前N个
                if (words.Count > wordCount)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"警告: AI返回了 {words.Count} 个词汇，但配置要求 {wordCount} 个。将只使用前 {wordCount} 个。");
                    Console.ResetColor();
                    words = words.Take(wordCount).ToList();
                }
                
                // 验证并去重（检查AI返回的词汇是否重复）
                var uniqueWords = new List<VocabularyWord>();
                var seenWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenSentences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var word in words)
                {
                    // 如果已经达到目标数量，停止处理
                    if (uniqueWords.Count >= wordCount)
                    {
                        break;
                    }
                    
                    // 检查词汇是否重复
                    if (string.IsNullOrWhiteSpace(word.Word))
                        continue;
                    
                    var normalizedWord = word.Word.Trim().ToLower();
                    if (seenWords.Contains(normalizedWord) || 
                        (usedWords.Count > 0 && usedWords.Contains(normalizedWord, StringComparer.OrdinalIgnoreCase)))
                    {
                        continue; // 跳过重复的词汇
                    }
                    
                    // 检查例句是否重复
                    if (!string.IsNullOrWhiteSpace(word.Sentence))
                    {
                        var normalizedSentence = NormalizeSentence(word.Sentence);
                        if (seenSentences.Contains(normalizedSentence) ||
                            (usedSentences.Count > 0 && usedSentences.Any(s => NormalizeSentence(s).Equals(normalizedSentence, StringComparison.OrdinalIgnoreCase))))
                        {
                            // 如果例句重复，尝试生成新的例句
                            word.Sentence = await GenerateUniqueSentenceForWordAsync(word.Word, word.Definition, seenSentences, usedSentences);
                            normalizedSentence = NormalizeSentence(word.Sentence);
                            if (seenSentences.Contains(normalizedSentence))
                            {
                                continue; // 如果生成后仍然重复，跳过
                            }
                        }
                        seenSentences.Add(normalizedSentence);
                    }
                    
                    seenWords.Add(normalizedWord);
                    uniqueWords.Add(word);
                    
                    // 记录使用的词汇和例句
                    if (_usageTrackerService != null)
                    {
                        _usageTrackerService.RecordWord(word.Word);
                        if (!string.IsNullOrWhiteSpace(word.Sentence))
                        {
                            _usageTrackerService.RecordSentence(word.Sentence);
                        }
                    }
                }
                
                // 如果去重后数量不足，给出警告
                if (uniqueWords.Count < wordCount)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"提示: 去重后获得 {uniqueWords.Count} 个词汇（请求 {wordCount} 个）。");
                    Console.ResetColor();
                }
                
                return uniqueWords;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize word list JSON: {ex.Message}");
                Console.WriteLine($"Raw response was: {response}");
                return new List<VocabularyWord>();
            }
        }
        
        /// <summary>
        /// 为词汇生成唯一的例句
        /// </summary>
        private async Task<string> GenerateUniqueSentenceForWordAsync(
            string word, 
            string definition,
            HashSet<string> currentSentences,
            List<string> usedSentences)
        {
            var prompt = $@"
Generate a NEW and UNIQUE example sentence for the IELTS vocabulary word: ""{word}"".

Word: {word}
Definition: {definition}

Requirements:
1. The sentence must be at IELTS academic level.
2. The sentence should clearly demonstrate the word's meaning and usage.
3. The sentence must be grammatically correct and natural.
4. The sentence should be CREATIVE and DIFFERENT from common examples.
5. Do NOT use the word in an obvious or repetitive way.
6. Return ONLY the sentence, without quotes, numbering, or additional text.
7. Use American English spelling and expressions.

Generate a unique sentence now:";

            // 最多尝试3次生成不重复的例句
            for (int attempt = 0; attempt < 3; attempt++)
            {
                string response = await CallGeminiApiAsync(prompt);
                
                if (response.StartsWith("Error:"))
                {
                    continue;
                }
                
                var sentence = CleanSentenceResponse(response);
                if (string.IsNullOrWhiteSpace(sentence))
                {
                    continue;
                }
                
                var normalized = NormalizeSentence(sentence);
                
                // 检查是否与当前批次重复
                if (currentSentences.Contains(normalized))
                {
                    continue;
                }
                
                // 检查是否与历史记录重复
                if (usedSentences.Any(s => NormalizeSentence(s).Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                
                // 找到唯一例句
                return sentence;
            }
            
            // 如果所有尝试都失败，返回一个基本例句
            return $"The word '{word}' is commonly used in academic contexts.";
        }
        
        /// <summary>
        /// 清理AI返回的句子
        /// </summary>
        private string CleanSentenceResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "";
            
            var cleaned = response.Trim();
            
            // 移除可能的引号
            if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2);
            }
            if (cleaned.StartsWith("'") && cleaned.EndsWith("'"))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2);
            }
            
            // 移除可能的编号或标记
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^\d+[\.\)]\s*", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[-\*]\s*", "");
            
            return cleaned.Trim();
        }
        
        /// <summary>
        /// 标准化句子用于比较
        /// </summary>
        private string NormalizeSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return "";
            
            var normalized = sentence.ToLower().Trim();
            var charsToRemove = new[] { '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' };
            foreach (var c in charsToRemove)
            {
                normalized = normalized.Replace(c.ToString(), "");
            }
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
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
                var cleanedJson = CleanJsonResponse(response);
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

        public async Task<Article> GetDailyArticleAsync(List<string> topics, int keyWordsCount, ArticleGenerationProgress? progress = null)
        {
            // 随机选择一个主题
            var random = new Random();
            string selectedTopic = topics[random.Next(topics.Count)];

            // 更新进度：选择主题
            if (progress != null)
            {
                progress.CurrentStatus = $"已选择主题: {selectedTopic}，正在生成文章内容...";
                progress.CurrentStep = 0;
            }

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
                var cleanedJson = CleanJsonResponse(articleResponse);
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

            // 更新进度：文章生成完成，开始翻译
            if (progress != null)
            {
                progress.CurrentStatus = "文章内容生成完成，正在生成中文翻译...";
                progress.CurrentStep = 1;
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

            // 更新进度：翻译完成，开始提取词汇
            if (progress != null)
            {
                progress.CurrentStatus = "翻译完成，正在提取重点词汇...";
                progress.CurrentStep = 2;
            }

            // 提取重点词汇
            var keyWordsPrompt = $@"
Please extract {keyWordsCount} key vocabulary words from the following article that are important for IELTS learners.

For each word, provide:
- Accurate phonetics in American English pronunciation (IPA format, US pronunciation)
- Chinese definition (including part of speech and comprehensive meaning)
- An example sentence from the article or a similar context

Return the response as a valid JSON array. Each object in the array must have the following keys: ""word"", ""phonetics"", ""definition"", ""sentence"".

Article:
{article.Content}

Example format (use American English phonetics):
[
  {{
    ""word"": ""ubiquitous"",
    ""phonetics"": ""/juˈbɪkwɪtəs/"",
    ""definition"": ""adj. 普遍存在的；无所不在的"",
    ""sentence"": ""The company's logo has become ubiquitous all over the world.""
  }},
  ...
]

REMEMBER: Use American English (US) pronunciation for all phonetics.";

            string keyWordsResponse = await CallGeminiApiAsync(keyWordsPrompt);
            if (!keyWordsResponse.StartsWith("Error:"))
            {
                try
                {
                    var cleanedJson = CleanJsonResponse(keyWordsResponse);
                    var words = JsonSerializer.Deserialize<List<VocabularyWord>>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    article.KeyWords = words ?? new List<VocabularyWord>();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to deserialize key words JSON: {ex.Message}");
                    Console.WriteLine($"Raw response was: {keyWordsResponse}");
                }
            }

            // 更新进度：完成
            if (progress != null)
            {
                progress.CurrentStatus = "完成！";
                progress.CurrentStep = 3;
            }

            return article;
        }
        
        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "{}";
                
            var cleaned = response.Trim();
            
            // 移除可能存在的代码块标记
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7).TrimStart();
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3).TrimStart();
            }
            
            if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();
            }
            
            // 移除前后空白字符
            return cleaned.Trim();
        }
    }
}

