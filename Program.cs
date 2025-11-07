using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
            _config = LoadConfig();
            if (_config == null)
            {
                Console.WriteLine("Failed to load configuration file. Please check config.json.");
                return;
            }

            // 解析命令行参数
            string mode = ParseArguments(args);
            
            // 显示帮助信息
            if (mode == "help")
            {
                ShowHelp();
                return;
            }

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

        private static AppConfig LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();
            var config = new AppConfig();
            configuration.Bind(config);
            return config;
        }

        private static string ParseArguments(string[] args)
        {
            // 如果没有参数，默认返回 words 模式
            if (args.Length == 0)
            {
                return "words";
            }

            // 解析参数
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower().Trim();

                // 处理帮助参数
                if (arg == "--help" || arg == "-h")
                {
                    return "help";
                }

                // 处理单词学习模式
                if (arg == "--words" || arg == "-w")
                {
                    return "words";
                }

                // 处理每日文章模式
                if (arg == "--article" || arg == "-a")
                {
                    return "article";
                }
            }

            // 如果参数不匹配，返回空字符串（会在主函数中显示错误）
            return "";
        }

        private static void ShowHelp()
        {
            Console.WriteLine("IELTS Learning Tool - 雅思学习工具");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  IELTS-Learning-Tool [选项]");
            Console.WriteLine();
            Console.WriteLine("选项:");
            Console.WriteLine("  --words, -w           单词学习模式（默认模式）");
            Console.WriteLine("                         从配置的主题中随机选择单词，提供翻译练习");
            Console.WriteLine();
            Console.WriteLine("  --article, -a         每日文章模式");
            Console.WriteLine("                         从配置的主题中随机选择一篇，生成500-1000词的英文文章");
            Console.WriteLine("                         包含全文翻译和重点词汇解释");
            Console.WriteLine();
            Console.WriteLine("  --help, -h            显示此帮助信息");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  IELTS-Learning-Tool --words      # 运行单词学习模式");
            Console.WriteLine("  IELTS-Learning-Tool --article    # 运行每日文章模式");
            Console.WriteLine("  IELTS-Learning-Tool --help       # 显示帮助信息");
            Console.WriteLine();
            Console.WriteLine("配置文件:");
            Console.WriteLine("  程序会读取项目目录下的 config.json 配置文件");
            Console.WriteLine("  包含 API 密钥、单词数量、主题列表等配置项");
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

            foreach (var (word, index) in WithIndex(words))
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

            GenerateHtmlReport(evaluatedWords);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nReport 'IELTS_Report.html' has been successfully generated in the project directory.");
            Console.ResetColor();
            Console.WriteLine("\nThank you for using the IELTS Learning Tool. See you next time!");
        }

        private static async Task RunArticleMode(GeminiService geminiService)
        {
            Console.WriteLine("--- IELTS Daily Article Mode ---");
            Console.WriteLine("\nGenerating daily article... Please wait.");

            Article article = await geminiService.GetDailyArticleAsync(_config.Topics, _config.ArticleKeyWordsCount);

            if (string.IsNullOrWhiteSpace(article.Content))
            {
                Console.WriteLine("\nFailed to generate article. Please check your API key, network connection, and API quota.");
                Console.WriteLine("Exiting program.");
                return;
            }

            Console.WriteLine($"\nSuccessfully generated article on topic: {article.Topic}");
            Console.WriteLine($"Article length: {article.Content.Length} characters");
            Console.WriteLine($"Key words extracted: {article.KeyWords.Count}");
            Console.WriteLine("\nGenerating HTML report...");

            GenerateArticleHtmlReport(article);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nArticle report has been successfully generated in the project directory.");
            Console.ResetColor();
            Console.WriteLine("\nThank you for using the IELTS Learning Tool. See you next time!");
        }

        private static void GenerateHtmlReport(List<VocabularyWord> words)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>IELTS Learning Report</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; background-color: #f0f2f5; }");
            sb.AppendLine("        .container { max-width: 1000px; margin: 20px auto; padding: 20px; background-color: #fff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("        h1 { color: #1c2a38; text-align: center; }");
            sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("        th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("        th { background-color: #4a6fa5; color: white; }");
            sb.AppendLine("        tr:nth-child(even) { background-color: #f8f9fa; }");
            sb.AppendLine("        .score { font-weight: bold; text-align: center; }");
            sb.AppendLine("        .score-high { color: #28a745; }");
            sb.AppendLine("        .score-medium { color: #ffc107; }");
            sb.AppendLine("        .score-low { color: #dc3545; }");
            sb.AppendLine("        .details { font-size: 0.9em; color: #555; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <h1>IELTS Vocabulary Translation Report</h1>");
            sb.AppendLine("        <table>");
            sb.AppendLine("            <thead>");
            sb.AppendLine("                <tr>");
            sb.AppendLine("                    <th>Original Sentence & Word</th>");
            sb.AppendLine("                    <th>Your Translation</th>");
            sb.AppendLine("                    <th>Corrected Translation & Explanation</th>");
            sb.AppendLine("                    <th style=\"text-align: center;\">Score</th>");
            sb.AppendLine("                </tr>");
            sb.AppendLine("            </thead>");
            sb.AppendLine("            <tbody>");

            foreach (var word in words)
            {
                sb.AppendLine("                <tr>");
                sb.AppendLine($"                    <td><div class='details'><strong>{EscapeHtml(word.Word)}</strong> ({EscapeHtml(word.Phonetics)})</div>{EscapeHtml(word.Sentence)}</td>");
                sb.AppendLine($"                    <td>{(word.IsSkipped ? "Skipped" : EscapeHtml(word.UserTranslation))}</td>");
                sb.AppendLine($"                    <td><div>{EscapeHtml(word.CorrectedTranslation)}</div><div class='details'>{EscapeHtml(word.Explanation)}</div></td>");
                
                string scoreColor = word.Score >= 8 ? "score-high" : word.Score >= 5 ? "score-medium" : "score-low";
                sb.AppendLine($"                    <td class='score {scoreColor}'>{word.Score}/10</td>");
                sb.AppendLine("                </tr>");
            }

            sb.AppendLine("            </tbody>");
            sb.AppendLine("        </table>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.WriteAllText($"IELTS_Report_{timestamp}.html", sb.ToString());
        }

        private static void GenerateArticleHtmlReport(Article article)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>IELTS Daily Article</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; background-color: #f0f2f5; line-height: 1.6; }");
            sb.AppendLine("        .container { max-width: 1200px; margin: 20px auto; padding: 20px; background-color: #fff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("        h1 { color: #1c2a38; text-align: center; border-bottom: 3px solid #4a6fa5; padding-bottom: 10px; }");
            sb.AppendLine("        h2 { color: #2c3e50; margin-top: 30px; border-left: 4px solid #4a6fa5; padding-left: 15px; }");
            sb.AppendLine("        .topic { text-align: center; color: #7f8c8d; font-style: italic; margin-bottom: 20px; }");
            sb.AppendLine("        .article-section { margin: 30px 0; }");
            sb.AppendLine("        .article-content { background-color: #f8f9fa; padding: 20px; border-radius: 5px; white-space: pre-wrap; font-size: 1.1em; }");
            sb.AppendLine("        .translation { background-color: #e8f4f8; padding: 20px; border-radius: 5px; white-space: pre-wrap; font-size: 1.1em; }");
            sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("        th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("        th { background-color: #4a6fa5; color: white; }");
            sb.AppendLine("        tr:nth-child(even) { background-color: #f8f9fa; }");
            sb.AppendLine("        .word { font-weight: bold; color: #2c3e50; }");
            sb.AppendLine("        .phonetics { color: #7f8c8d; font-style: italic; }");
            sb.AppendLine("        .definition { color: #555; }");
            sb.AppendLine("        .sentence { color: #2c3e50; font-style: italic; margin-top: 5px; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine($"        <h1>{EscapeHtml(article.Title)}</h1>");
            sb.AppendLine($"        <div class=\"topic\">主题: {EscapeHtml(article.Topic)}</div>");
            
            sb.AppendLine("        <div class=\"article-section\">");
            sb.AppendLine("            <h2>英文原文</h2>");
            sb.AppendLine("            <div class=\"article-content\">");
            sb.AppendLine(EscapeHtml(article.Content).Replace("\n", "<br>"));
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"article-section\">");
            sb.AppendLine("            <h2>中文翻译</h2>");
            sb.AppendLine("            <div class=\"translation\">");
            sb.AppendLine(EscapeHtml(article.Translation).Replace("\n", "<br>"));
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"article-section\">");
            sb.AppendLine("            <h2>重点词汇</h2>");
            sb.AppendLine("            <table>");
            sb.AppendLine("                <thead>");
            sb.AppendLine("                    <tr>");
            sb.AppendLine("                        <th>单词</th>");
            sb.AppendLine("                        <th>音标</th>");
            sb.AppendLine("                        <th>释义</th>");
            sb.AppendLine("                        <th>例句</th>");
            sb.AppendLine("                    </tr>");
            sb.AppendLine("                </thead>");
            sb.AppendLine("                <tbody>");

            foreach (var word in article.KeyWords)
            {
                sb.AppendLine("                    <tr>");
                sb.AppendLine($"                        <td class=\"word\">{EscapeHtml(word.Word)}</td>");
                sb.AppendLine($"                        <td class=\"phonetics\">{EscapeHtml(word.Phonetics)}</td>");
                sb.AppendLine($"                        <td class=\"definition\">{EscapeHtml(word.Definition)}</td>");
                sb.AppendLine($"                        <td class=\"sentence\">{EscapeHtml(word.Sentence)}</td>");
                sb.AppendLine("                    </tr>");
            }

            sb.AppendLine("                </tbody>");
            sb.AppendLine("            </table>");
            sb.AppendLine("        </div>");

            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.WriteAllText($"IELTS_Article_{timestamp}.html", sb.ToString());
        }
        
        // Helper to get index in a foreach loop
        private static IEnumerable<(T item, int index)> WithIndex<T>(IEnumerable<T> source)
        {
            int i = 0;
            foreach (var item in source)
            {
                yield return (item, i++);
            }
        }

        // Helper to escape HTML special characters
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}