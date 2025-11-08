using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IELTS_Learning_Tool.Configuration;
using IELTS_Learning_Tool.Models;
using IELTS_Learning_Tool.Services;
using IELTS_Learning_Tool.Utils;

namespace IELTS_Learning_Tool
{
    class Program
    {
        private static AppConfig _config = null!;
        
        // 读取密码输入（隐藏输入内容）
        private static string ReadPasswordInput()
        {
            StringBuilder password = new StringBuilder();
            ConsoleKeyInfo keyInfo;
            
            do
            {
                keyInfo = Console.ReadKey(true);
                
                if (keyInfo.Key != ConsoleKey.Backspace && keyInfo.Key != ConsoleKey.Enter)
                {
                    password.Append(keyInfo.KeyChar);
                    Console.Write("*");
                }
                else if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            while (keyInfo.Key != ConsoleKey.Enter);
            
            Console.WriteLine();
            return password.ToString();
        }
        
        // 读取用户输入（支持中文，正确处理多字节字符的删除）
        private static string ReadUserInput()
        {
            var input = new StringBuilder();
            var inputChars = new List<char>(); // 用于存储实际输入的字符
            
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (inputChars.Count > 0)
                    {
                        // 移除最后一个字符
                        char lastChar = inputChars[inputChars.Count - 1];
                        inputChars.RemoveAt(inputChars.Count - 1);
                        input.Remove(input.Length - 1, 1);
                        
                        // 根据字符类型移动光标并清除
                        if (IsWideChar(lastChar))
                        {
                            // 中文字符等宽字符，需要移动2个位置并清除
                            Console.Write("\b\b  \b\b");
                        }
                        else
                        {
                            // ASCII字符，移动1个位置并清除
                            Console.Write("\b \b");
                        }
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar) && keyInfo.KeyChar != '\0')
                {
                    // 处理输入字符（排除控制字符）
                    char ch = keyInfo.KeyChar;
                    
                    inputChars.Add(ch);
                    input.Append(ch);
                    Console.Write(ch);
                }
            }
            
            return input.ToString();
        }
        
        // 判断字符是否是宽字符（中文字符等）
        private static bool IsWideChar(char c)
        {
            // 中文字符范围
            if (c >= 0x4E00 && c <= 0x9FFF) // CJK统一汉字
                return true;
            if (c >= 0x3400 && c <= 0x4DBF) // CJK扩展A
                return true;
            if (c >= 0x20000 && c <= 0x2A6DF) // CJK扩展B
                return true;
            if (c >= 0x2A700 && c <= 0x2B73F) // CJK扩展C
                return true;
            if (c >= 0x2B740 && c <= 0x2B81F) // CJK扩展D
                return true;
            if (c >= 0x2B820 && c <= 0x2CEAF) // CJK扩展E
                return true;
            if (c >= 0xF900 && c <= 0xFAFF) // CJK兼容汉字
                return true;
            if (c >= 0x2F800 && c <= 0x2FA1F) // CJK兼容扩展
                return true;
            
            // 日文、韩文字符也是宽字符
            if (c >= 0x3040 && c <= 0x309F) // 平假名
                return true;
            if (c >= 0x30A0 && c <= 0x30FF) // 片假名
                return true;
            if (c >= 0xAC00 && c <= 0xD7AF) // 韩文音节
                return true;
            
            // 全角字符
            if (c >= 0xFF00 && c <= 0xFFEF)
                return true;
            
            return false;
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // 读取配置文件
            _config = ConfigLoader.LoadConfig();
            if (_config == null)
            {
                Console.WriteLine("Failed to load configuration file. Please check config.json.");
                return;
            }
            
            // 初始化使用记录跟踪服务（用于消除重复）
            UsageTrackerService? usageTrackerService = null;
            try
            {
                usageTrackerService = new UsageTrackerService("usage_record.json");
                var stats = usageTrackerService.GetStatistics();
                if (stats.wordCount > 0 || stats.sentenceCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"已加载使用记录: {stats.wordCount} 个词汇, {stats.sentenceCount} 个例句");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告: 加载使用记录失败: {ex.Message}，将创建新记录");
                usageTrackerService = new UsageTrackerService("usage_record.json");
            }

            // 解析命令行参数
            string mode = ArgumentParser.ParseArguments(args);
            
            // 显示帮助信息
            if (mode == "help")
            {
                HelpDisplay.ShowHelp();
                return;
            }

            // 验证 API 密钥
            if (string.IsNullOrWhiteSpace(_config.GoogleApiKey) || 
                _config.GoogleApiKey == "YOUR_API_KEY_HERE" || 
                _config.GoogleApiKey == "YOUR_GOOGLE_GEMINI_API_KEY")
            {
                Console.WriteLine("--- Welcome to the IELTS Learning Tool ---");
                Console.Write("Please enter your Google Gemini API Key: ");
                
                // 在 Windows 上可以使用 SecureString，但在跨平台环境下，我们使用简单的输入隐藏
                // 注意：这不是完全安全的，但在控制台应用中已经是合理的选择
                string apiKey = ReadPasswordInput();
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine("API Key cannot be empty. Exiting.");
                    return;
                }
                _config.GoogleApiKey = apiKey;
            }

            GeminiService? geminiService = null;
            try
            {
                geminiService = new GeminiService(_config, usageTrackerService);

                if (mode == "words")
                {
                    await RunWordsMode(geminiService);
                }
                else if (mode == "article")
                {
                    await RunArticleMode(geminiService);
                }
                else
                {
                    Console.WriteLine("错误: 无效的参数。使用 --help 或 -h 查看帮助信息。");
                }
            }
            finally
            {
                geminiService?.Dispose();
                // 确保使用记录已保存
                usageTrackerService?.SaveRecord();
            }
        }

        private static async Task RunWordsMode(GeminiService geminiService)
        {
            Console.WriteLine("--- IELTS Vocabulary Learning Mode ---");
            Console.WriteLine($"\nFetching {_config.WordCount} new IELTS words for you... Please wait.");
            List<VocabularyWord> words = await geminiService.GetIeltsWordsAsync(_config.WordCount, _config.Topics);

            if (words.Count == 0)
            {
                Console.WriteLine("\nFailed to fetch words. Please check your API key, network connection, and API quota.");
                Console.WriteLine("Exiting program.");
                return;
            }

            Console.WriteLine($"\nSuccessfully fetched {words.Count} words. Let's begin!");
            Console.WriteLine("----------------------------------------------------\n");

            foreach (var (word, index) in EnumerableHelper.WithIndex(words))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nQuestion {index + 1}/{words.Count}");
                Console.ResetColor();

                Console.Write("Word: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(word.Word);
                Console.ResetColor();

                Console.Write("Phonetics: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(word.Phonetics);
                Console.ResetColor();
                
                Console.WriteLine($"Definition: {word.Definition}");
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nPlease translate the following sentence into Chinese:");
                Console.WriteLine(word.Sentence);
                Console.ResetColor();

                Console.Write("\nYour translation: ");
                string userInput = ReadUserInput().Trim();
                if (userInput.Equals("Pass", StringComparison.OrdinalIgnoreCase))
                {
                    word.IsSkipped = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Question skipped.");
                    Console.ResetColor();
                }
                else
                {
                    word.UserTranslation = userInput;
                }
                
                Console.WriteLine("----------------------------------------------------\n");
            }

            Console.WriteLine("\nAll translations are complete. Evaluating your answers... Please wait.");
            List<VocabularyWord> evaluatedWords = await geminiService.EvaluateTranslationsAsync(words);

            foreach (var word in evaluatedWords)
            {
                if (word.IsSkipped)
                {
                    word.Score = 0;
                    word.Explanation = "(User skipped) " + word.Explanation;
                }
            }

            Console.WriteLine("Evaluation complete. Generating HTML report...");

            ReportGenerator.GenerateWordsReport(evaluatedWords);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n报告已成功生成到项目目录。");
            Console.ResetColor();
            Console.WriteLine("\nThank you for using the IELTS Learning Tool. See you next time!");
        }

        private static async Task RunArticleMode(GeminiService geminiService)
        {
            Console.WriteLine("--- IELTS Daily Article Mode ---");
            Console.WriteLine();

            // 创建进度跟踪
            var progress = new ArticleGenerationProgress();

            // 启动进度显示任务
            var progressTask = ProgressDisplay.ShowProgressAsync(progress);

            try
            {
                Article article = await geminiService.GetDailyArticleAsync(_config.Topics, _config.ArticleKeyWordsCount, progress);

                // 停止进度显示
                progress.IsComplete = true;
                await progressTask;

                if (string.IsNullOrWhiteSpace(article.Content))
                {
                    Console.WriteLine("\n\nFailed to generate article. Please check your API key, network connection, and API quota.");
                    Console.WriteLine("Exiting program.");
                    return;
                }

                Console.WriteLine($"\n成功生成文章！主题: {article.Topic}");
                Console.WriteLine($"文章长度: {article.Content.Length} 字符");
                Console.WriteLine($"重点词汇: {article.KeyWords.Count} 个");
                Console.WriteLine("\n正在生成 HTML 报告...");

                ReportGenerator.GenerateArticleReport(article);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n文章报告已成功生成到项目目录。");
                Console.ResetColor();
                Console.WriteLine("\nThank you for using the IELTS Learning Tool. See you next time!");
            }
            catch (Exception ex)
            {
                progress.IsComplete = true;
                progress.SetError(ex.Message);
                await progressTask;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n\n错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
                Console.ResetColor();
            }
        }
    }
}
