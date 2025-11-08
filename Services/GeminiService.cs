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

        /// <summary>
        /// 为单词生成新的复习例句
        /// </summary>
        public async Task<string> GenerateReviewSentenceAsync(string word)
        {
            var prompt = $@"
Please generate a NEW, DIFFERENT example sentence for the IELTS vocabulary word: ""{word}"".

Requirements:
1. The sentence must clearly demonstrate the word's meaning and usage
2. The sentence should be natural and appropriate for IELTS level
3. Use American English
4. Do NOT use markdown formatting (no **, no *, no bold, no italic)
5. Return ONLY the sentence, no additional text or explanation

Word: {word}

Example sentence:";

            string response = await CallGeminiApiAsync(prompt);
            
            if (response.StartsWith("Error:"))
            {
                return $"Review the usage of: {word}";
            }
            
            // 清理 markdown 格式
            string cleaned = TextCleaner.CleanSentence(response.Trim());
            return cleaned;
        }

        /// <summary>
        /// 批量生成复习例句（一次性为多个单词生成）
        /// </summary>
        public async Task<Dictionary<string, string>> GenerateReviewSentencesBatchAsync(List<string> words)
        {
            var result = new Dictionary<string, string>();
            
            if (words == null || words.Count == 0)
            {
                return result;
            }

            var wordsList = string.Join(", ", words.Select(w => $"\"{w}\""));
            
            var prompt = $@"
Please generate NEW, DIFFERENT example sentences for the following IELTS vocabulary words.

For each word, provide a sentence that:
1. Clearly demonstrates the word's meaning and usage
2. Is natural and appropriate for IELTS level
3. Uses American English
4. Does NOT use markdown formatting (no **, no *, no bold, no italic)

Words: {wordsList}

Return the response as a valid JSON object where each key is a word and each value is the example sentence.

Example format:
{{
  ""ubiquitous"": ""The company's logo has become ubiquitous all over the world."",
  ""mitigate"": ""Governments must take action to mitigate the effects of climate change."",
  ""erosion"": ""Coastal erosion is a serious problem in many parts of the world.""
}}

Return the JSON object now:";

            string response = await CallGeminiApiAsync(prompt);
            
            if (response.StartsWith("Error:"))
            {
                // 如果批量生成失败，为每个单词返回默认值
                foreach (var word in words)
                {
                    result[word] = $"Review the usage of: {word}";
                }
                return result;
            }

            try
            {
                var cleanedJson = CleanJsonResponse(response);
                using (JsonDocument doc = JsonDocument.Parse(cleanedJson))
                {
                    JsonElement root = doc.RootElement;
                    foreach (var word in words)
                    {
                        if (root.TryGetProperty(word, out JsonElement sentenceElement))
                        {
                            string sentence = sentenceElement.GetString() ?? $"Review the usage of: {word}";
                            result[word] = TextCleaner.CleanSentence(sentence.Trim());
                        }
                        else
                        {
                            result[word] = $"Review the usage of: {word}";
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"批量生成复习例句失败: {ex.Message}");
                // 如果解析失败，为每个单词返回默认值
                foreach (var word in words)
                {
                    result[word] = $"Review the usage of: {word}";
                }
            }

            return result;
        }
        
        public async Task<List<VocabularyWord>> GetIeltsWordsAsync(int wordCount, List<string> topics, int excludeDays = 7)
        {
            // 获取指定日期范围内的已使用词汇和例句，避免重复
            var usedWords = _usageTrackerService != null
                ? _usageTrackerService.GetUsedWordsInDateRange(excludeDays).ToList()
                : new List<string>();
            
            var usedSentences = _usageTrackerService != null
                ? _usageTrackerService.GetUsedSentencesInDateRange(excludeDays).ToList()
                : new List<string>();
            
            // 使用AI从雅思词汇中生成题目，避免重复
            return await GetWordsFromAIWithAntiRepeatAsync(wordCount, topics, usedWords, usedSentences);
        }
        
        /// <summary>
        /// 使用AI从雅思词汇中生成题目，避免重复（优化版：确保数量充足，提升速度）
        /// </summary>
        private async Task<List<VocabularyWord>> GetWordsFromAIWithAntiRepeatAsync(
            int wordCount, 
            List<string> topics, 
            List<string> usedWords,
            List<string> usedSentences)
        {
            var allUniqueWords = new List<VocabularyWord>();
            var allSeenWords = new HashSet<string>(usedWords, StringComparer.OrdinalIgnoreCase);
            var allSeenSentences = new HashSet<string>(usedSentences.Select(NormalizeSentence), StringComparer.OrdinalIgnoreCase);
            
            int maxAttempts = 3; // 最多尝试3次
            int requestCount = Math.Max(wordCount, wordCount + 5); // 请求更多词汇以确保有足够候选
            
            for (int attempt = 0; attempt < maxAttempts && allUniqueWords.Count < wordCount; attempt++)
            {
                int remainingCount = wordCount - allUniqueWords.Count;
                int currentRequestCount = attempt == 0 ? requestCount : remainingCount + 3; // 第一次请求更多，后续请求刚好够的数量
                
                var words = await GenerateWordsBatchAsync(currentRequestCount, topics, allSeenWords, allSeenSentences, attempt);
                
                if (words == null || words.Count == 0)
                {
                    break;
                }
                
                // 快速去重处理（不重新生成例句，直接跳过重复的）
                foreach (var word in words)
                {
                    if (allUniqueWords.Count >= wordCount)
                    {
                        break;
                    }
                    
                    if (string.IsNullOrWhiteSpace(word.Word))
                        continue;
                    
                    var normalizedWord = word.Word.Trim().ToLower();
                    if (allSeenWords.Contains(normalizedWord))
                    {
                        continue; // 跳过重复词汇
                    }
                    
                    // 检查例句是否重复（如果重复，直接跳过，不重新生成以提升速度）
                    bool sentenceIsUnique = true;
                    if (!string.IsNullOrWhiteSpace(word.Sentence))
                    {
                        var normalizedSentence = NormalizeSentence(word.Sentence);
                        if (allSeenSentences.Contains(normalizedSentence))
                        {
                            sentenceIsUnique = false;
                        }
                        else
                        {
                            allSeenSentences.Add(normalizedSentence);
                        }
                    }
                    
                    // 如果词汇和例句都唯一，添加
                    if (sentenceIsUnique)
                    {
                        allSeenWords.Add(normalizedWord);
                        allUniqueWords.Add(word);
                        
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
                }
            }
            
            // 如果仍然不足，给出提示
            if (allUniqueWords.Count < wordCount)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"提示: 获得 {allUniqueWords.Count} 个唯一词汇（请求 {wordCount} 个）。可能已使用的词汇较多，建议重置使用记录。");
                Console.ResetColor();
            }
            
            return allUniqueWords.Take(wordCount).ToList();
        }
        
        /// <summary>
        /// 批量生成词汇（一次性生成，不单独处理例句）
        /// </summary>
        private async Task<List<VocabularyWord>> GenerateWordsBatchAsync(
            int wordCount,
            List<string> topics,
            HashSet<string> excludeWords,
            HashSet<string> excludeSentences,
            int attempt)
        {
            // 构建避免重复的提示词（只显示部分，避免prompt过长）
            string antiRepeatContext = "";
            if (excludeWords.Count > 0 && attempt == 0) // 只在第一次尝试时显示
            {
                var usedWordsSample = excludeWords.Take(30).ToList(); // 减少显示数量以加快速度
                antiRepeatContext = $@"

IMPORTANT - AVOID REPETITION:
The following words have already been used. Please DO NOT use them:
{string.Join(", ", usedWordsSample)}
{(excludeWords.Count > 30 ? $"\n(And {excludeWords.Count - 30} more words)" : "")}

Please select COMPLETELY DIFFERENT words.";
            }
            
            var prompt = $@"
You are an IELTS vocabulary expert. Please provide {wordCount} IELTS core vocabulary words from the following topics: {string.Join(", ", topics)}.

CRITICAL REQUIREMENTS:
1. Return EXACTLY {wordCount} words (or as close as possible).
2. Select words commonly tested in IELTS examinations (band 6.5-8.0 level).
3. Ensure diversity in parts of speech (nouns, verbs, adjectives, adverbs, etc.).
4. Each word must be relevant to the given topics.
5. For each word, provide:
   - Accurate phonetics in American English pronunciation (IPA format, US pronunciation)
   - Chinese definition (including part of speech and comprehensive meaning)
   - A unique, natural example sentence that clearly demonstrates the word's usage
6. All example sentences must be unique and creative.
7. Use American English phonetics (US pronunciation) for all words.
8. IMPORTANT: Do NOT use markdown formatting (no **, no *, no bold, no italic) in sentences. Use plain text only.
9. In example sentences, write the vocabulary word naturally without any special formatting or emphasis.
{antiRepeatContext}

Return the response as a valid JSON array. Each object must have: ""word"", ""phonetics"", ""definition"", ""sentence"".

Example format:
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

Return the JSON array now:";

            string response = await CallGeminiApiAsync(prompt);

            if (response.StartsWith("Error:"))
            {
                if (attempt == 0) // 只在第一次失败时显示错误
                {
                    Console.WriteLine(response);
                }
                return new List<VocabularyWord>();
            }
            
            try
            {
                var cleanedJson = CleanJsonResponse(response);
                var words = JsonSerializer.Deserialize<List<VocabularyWord>>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (words != null)
                {
                    // 清理所有句子中的 markdown 格式标记
                    foreach (var word in words)
                    {
                        if (!string.IsNullOrWhiteSpace(word.Sentence))
                        {
                            word.Sentence = TextCleaner.CleanSentence(word.Sentence);
                        }
                    }
                }
                
                return words ?? new List<VocabularyWord>();
            }
            catch (JsonException ex)
            {
                if (attempt == 0) // 只在第一次失败时显示错误
                {
                    Console.WriteLine($"Failed to deserialize word list JSON: {ex.Message}");
                }
                return new List<VocabularyWord>();
            }
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
                        
                        // 清理修正翻译和解释中的 markdown 格式
                        var correctedTranslation = result.GetProperty("correctedTranslation").GetString() ?? "";
                        words[i].CorrectedTranslation = TextCleaner.RemoveMarkdownFormatting(correctedTranslation);
                        
                        var explanation = result.GetProperty("explanation").GetString() ?? "";
                        words[i].Explanation = TextCleaner.RemoveMarkdownFormatting(explanation);
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

        /// <summary>
        /// 为Pass的单词获取正确的翻译（不评分）
        /// </summary>
        public async Task<string> GetCorrectTranslationAsync(string sentence)
        {
            var prompt = $@"
Please provide a correct and natural Chinese translation for the following English sentence.
Return only the Chinese translation, without any additional text, explanation, or formatting.

English sentence:
{sentence}

Chinese translation:";

            string response = await CallGeminiApiAsync(prompt);
            
            if (response.StartsWith("Error:"))
            {
                return "（无法获取翻译）";
            }
            
            return TextCleaner.RemoveMarkdownFormatting(response.Trim());
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
                progress.CurrentStep = 1; // 从步骤1开始（初始化已完成，显示10%）
                progress.SubStepProgress = 0;
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
6. IMPORTANT: Do NOT use markdown formatting (no **, no *, no bold, no italic) in the article content. Use plain text only.

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
                    article.Title = TextCleaner.RemoveMarkdownFormatting(root.GetProperty("title").GetString() ?? "");
                    article.Content = TextCleaner.RemoveMarkdownFormatting(root.GetProperty("content").GetString() ?? "");
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
                progress.CurrentStep = 2; // 步骤2：翻译阶段 (30-60%)
                progress.SubStepProgress = 0;
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
                article.Translation = TextCleaner.RemoveMarkdownFormatting(translationResponse.Trim());
            }

            // 更新进度：翻译完成，开始提取词汇
            if (progress != null)
            {
                progress.CurrentStatus = "翻译完成，正在提取重点词汇...";
                progress.CurrentStep = 3; // 步骤3：提取词汇阶段 (60-80%)
                progress.SubStepProgress = 0;
            }

            // 提取重点词汇
            var keyWordsPrompt = $@"
Please extract {keyWordsCount} key vocabulary words from the following article that are important for IELTS learners.

For each word, provide:
- Accurate phonetics in American English pronunciation (IPA format, US pronunciation)
- Chinese definition (including part of speech and comprehensive meaning)
- An example sentence from the article or a similar context

IMPORTANT FORMATTING RULES:
- Do NOT use markdown formatting (no **, no *, no bold, no italic) in sentences
- Write example sentences in plain text only
- Do NOT highlight or emphasize the vocabulary word in the sentence
- Use the word naturally in the sentence without any special formatting

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
                    
                    if (words != null)
                    {
                        // 清理所有句子中的 markdown 格式标记
                        foreach (var word in words)
                        {
                            if (!string.IsNullOrWhiteSpace(word.Sentence))
                            {
                                word.Sentence = TextCleaner.CleanSentence(word.Sentence);
                            }
                        }
                    }
                    
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
                progress.CurrentStep = 4; // 步骤4：完成 (100%)
                progress.SubStepProgress = 100;
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

