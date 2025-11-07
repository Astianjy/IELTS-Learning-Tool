using System;
using System.Collections.Generic;
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

            // 解析命令行参数
            string mode = ArgumentParser.ParseArguments(args);
            
            // 显示帮助信息
            if (mode == "help")
            {
                HelpDisplay.ShowHelp();
                return;
            }

            // 验证 API 密钥
            if (string.IsNullOrWhiteSpace(_config.GoogleApiKey) || _config.GoogleApiKey == "YOUR_API_KEY_HERE")
            {
                Console.WriteLine("--- Welcome to the IELTS Learning Tool ---");
                Console.Write("Please enter your Google Gemini API Key: ");
                string apiKey = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine("API Key cannot be empty. Exiting.");
                    return;
                }
                _config.GoogleApiKey = apiKey;
            }

            var geminiService = new GeminiService(_config);

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
                string userInput = Console.ReadLine()?.Trim() ?? "";
                if (userInput.Equals("Pass", StringComparison.OrdinalIgnoreCase))
                {
                    word.IsSkipped = true;
                    Console.WriteLine("Question skipped.");
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
            Console.WriteLine($"\nReport 'IELTS_Report.html' has been successfully generated in the project directory.");
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
                await progressTask;
                Console.WriteLine($"\n\n错误: {ex.Message}");
            }
        }
    }
}
