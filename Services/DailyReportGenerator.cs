using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IELTS_Learning_Tool.Models;
using IELTS_Learning_Tool.Services;
using IELTS_Learning_Tool.Utils;

namespace IELTS_Learning_Tool.Services
{
    /// <summary>
    /// 每日报告生成器，生成包含复习内容的HTML报告
    /// </summary>
    public static class DailyReportGenerator
    {
        /// <summary>
        /// 生成每日学习报告
        /// </summary>
        public static async System.Threading.Tasks.Task GenerateDailyReportAsync(
            List<WordLearningRecord> todayRecords,
            GeminiService geminiService)
        {
            if (todayRecords == null || todayRecords.Count == 0)
            {
                Console.WriteLine("没有学习记录，无法生成每日报告。");
                return;
            }

            Console.WriteLine("\n正在生成每日复习报告，请稍候...");
            Console.WriteLine("正在为今天学习的单词生成新的复习例句...");

            // 为每个单词生成新的复习例句（包括所有记录，不再过滤IsSkipped）
            var reviewWords = new List<ReviewWord>();
            int processedCount = 0;
            int totalToProcess = todayRecords.Count(r => !string.IsNullOrWhiteSpace(r.Word));
            foreach (var record in todayRecords)
            {
                if (!string.IsNullOrWhiteSpace(record.Word))
                {
                    processedCount++;
                    Console.Write($"\r正在生成复习例句 ({processedCount}/{totalToProcess})...");
                    try
                    {
                        // 生成新的复习例句
                        string newSentence = await GenerateReviewSentenceAsync(geminiService, record.Word);
                        reviewWords.Add(new ReviewWord
                        {
                            Word = record.Word,
                            Phonetics = "", // 可以从原始记录中获取，这里简化处理
                            Definition = "", // 可以从原始记录中获取
                            OriginalSentence = record.Sentence,
                            ReviewSentence = newSentence,
                            Score = record.Score,
                            UserTranslation = record.UserTranslation,
                            CorrectedTranslation = record.CorrectedTranslation,
                            Explanation = record.Explanation
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"为单词 {record.Word} 生成复习例句失败: {ex.Message}");
                        // 如果生成失败，使用原始例句
                        reviewWords.Add(new ReviewWord
                        {
                            Word = record.Word,
                            OriginalSentence = record.Sentence,
                            ReviewSentence = record.Sentence,
                            Score = record.Score,
                            UserTranslation = record.UserTranslation,
                            CorrectedTranslation = record.CorrectedTranslation,
                            Explanation = record.Explanation
                        });
                    }
                }
            }

            // 生成HTML报告
            string html = GenerateDailyReportHtml(todayRecords, reviewWords);
            
            // 保存文件
            string fileName = GetUniqueFileName("IELTS_Daily_Report");
            File.WriteAllText(fileName, html, Encoding.UTF8);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n每日报告已成功生成: {fileName}");
            Console.ResetColor();
        }

        /// <summary>
        /// 生成复习例句
        /// </summary>
        private static async System.Threading.Tasks.Task<string> GenerateReviewSentenceAsync(
            GeminiService geminiService, 
            string word)
        {
            try
            {
                return await geminiService.GenerateReviewSentenceAsync(word);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成复习例句失败: {ex.Message}");
                return $"Review the usage of: {word}";
            }
        }

        /// <summary>
        /// 生成每日报告HTML
        /// </summary>
        private static string GenerateDailyReportHtml(
            List<WordLearningRecord> todayRecords,
            List<ReviewWord> reviewWords)
        {
            var sb = new StringBuilder();
            // 从第一条记录获取日期，如果没有则使用今天
            string today = todayRecords.Count > 0 
                ? todayRecords[0].Date.ToString("yyyy-MM-dd")
                : DateTime.Now.ToString("yyyy-MM-dd");
            string reportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 计算统计数据（包括所有记录，不再区分IsSkipped）
            int totalWords = todayRecords.Count;
            int answeredWords = todayRecords.Count(r => r.UserTranslation != "Pass");
            int passCount = todayRecords.Count(r => r.UserTranslation == "Pass");
            double averageScore = todayRecords.Where(r => r.UserTranslation != "Pass").Select(r => r.Score).DefaultIfEmpty(0).Average();
            int highScoreCount = todayRecords.Count(r => r.UserTranslation != "Pass" && r.Score >= 8);
            int mediumScoreCount = todayRecords.Count(r => r.UserTranslation != "Pass" && r.Score >= 5 && r.Score < 8);
            int lowScoreCount = todayRecords.Count(r => r.UserTranslation == "Pass" || (r.UserTranslation != "Pass" && r.Score < 5));

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>IELTS 每日学习报告 - {today}</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; min-height: 100vh; }");
            sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 15px; box-shadow: 0 20px 60px rgba(0,0,0,0.3); overflow: hidden; }");
            sb.AppendLine("        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; }");
            sb.AppendLine("        .header h1 { font-size: 2.5em; margin-bottom: 10px; }");
            sb.AppendLine("        .header p { font-size: 1.1em; opacity: 0.9; }");
            sb.AppendLine("        .content { padding: 40px; }");
            sb.AppendLine("        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 40px; }");
            sb.AppendLine("        .stat-card { background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); padding: 25px; border-radius: 10px; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .stat-card h3 { color: #555; font-size: 0.9em; margin-bottom: 10px; text-transform: uppercase; }");
            sb.AppendLine("        .stat-card .value { font-size: 2.5em; font-weight: bold; color: #667eea; margin: 0; }");
            sb.AppendLine("        .section { margin-bottom: 40px; }");
            sb.AppendLine("        .section h2 { color: #667eea; font-size: 1.8em; margin-bottom: 20px; padding-bottom: 10px; border-bottom: 3px solid #667eea; }");
            sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("        th, td { padding: 15px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("        th { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; font-weight: 600; }");
            sb.AppendLine("        tr:hover { background-color: #f5f5f5; }");
            sb.AppendLine("        .word { font-weight: bold; color: #667eea; font-size: 1.1em; }");
            sb.AppendLine("        .score { text-align: center; font-weight: bold; font-size: 1.2em; }");
            sb.AppendLine("        .score-high { color: #28a745; }");
            sb.AppendLine("        .score-medium { color: #ffc107; }");
            sb.AppendLine("        .score-low { color: #dc3545; }");
            sb.AppendLine("        .review-sentence { color: #28a745; font-style: italic; margin-top: 5px; }");
            sb.AppendLine("        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #666; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <div class=\"header\">");
            sb.AppendLine($"            <h1>📚 IELTS 每日学习报告</h1>");
            sb.AppendLine($"            <p>学习日期: {today} | 报告生成时间: {reportTime}</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"content\">");
            
            // 统计卡片
            sb.AppendLine("            <div class=\"stats\">");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>总单词数</h3><p class=\"value\">{totalWords}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>平均分数</h3><p class=\"value\">{averageScore:F1}/10</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>已回答</h3><p class=\"value\">{answeredWords}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>Pass</h3><p class=\"value\">{passCount}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>高分 (≥8)</h3><p class=\"value\">{highScoreCount}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>中等 (5-7)</h3><p class=\"value\">{mediumScoreCount}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>需改进 (<5)</h3><p class=\"value\">{lowScoreCount}</p></div>");
            sb.AppendLine("            </div>");

            // 复习内容
            sb.AppendLine("            <div class=\"section\">");
            sb.AppendLine("                <h2>📖 今日复习内容</h2>");
            sb.AppendLine("                <table>");
            sb.AppendLine("                    <thead>");
            sb.AppendLine("                        <tr>");
            sb.AppendLine("                            <th>单词</th>");
            sb.AppendLine("                            <th>原始例句</th>");
            sb.AppendLine("                            <th>复习例句</th>");
            sb.AppendLine("                            <th>你的翻译</th>");
            sb.AppendLine("                            <th>修正翻译</th>");
            sb.AppendLine("                            <th>得分</th>");
            sb.AppendLine("                        </tr>");
            sb.AppendLine("                    </thead>");
            sb.AppendLine("                    <tbody>");

            foreach (var review in reviewWords)
            {
                string scoreColor = review.Score >= 8 ? "score-high" : review.Score >= 5 ? "score-medium" : "score-low";
                sb.AppendLine("                        <tr>");
                sb.AppendLine($"                            <td class=\"word\">{HtmlHelper.EscapeHtml(review.Word)}</td>");
                sb.AppendLine($"                            <td>{HtmlHelper.EscapeHtml(review.OriginalSentence)}</td>");
                sb.AppendLine($"                            <td><span class=\"review-sentence\">{HtmlHelper.EscapeHtml(review.ReviewSentence)}</span></td>");
                sb.AppendLine($"                            <td>{HtmlHelper.EscapeHtml(review.UserTranslation)}</td>");
                sb.AppendLine($"                            <td>{HtmlHelper.EscapeHtml(review.CorrectedTranslation)}</td>");
                sb.AppendLine($"                            <td class=\"score {scoreColor}\">{review.Score}/10</td>");
                sb.AppendLine("                        </tr>");
            }

            sb.AppendLine("                    </tbody>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");

            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"footer\">");
            sb.AppendLine("            <p>IELTS Learning Tool - 每日学习报告</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// 获取唯一的文件名
        /// </summary>
        private static string GetUniqueFileName(string prefix)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int counter = 0;
            string fileName;
            
            do
            {
                fileName = counter == 0 
                    ? $"{prefix}_{timestamp}.html"
                    : $"{prefix}_{timestamp}_{counter}.html";
                counter++;
            } while (File.Exists(fileName) && counter < 100);
            
            return fileName;
        }
    }

    /// <summary>
    /// 复习单词信息
    /// </summary>
    internal class ReviewWord
    {
        public string Word { get; set; } = "";
        public string Phonetics { get; set; } = "";
        public string Definition { get; set; } = "";
        public string OriginalSentence { get; set; } = "";
        public string ReviewSentence { get; set; } = "";
        public int Score { get; set; }
        public string UserTranslation { get; set; } = "";
        public string CorrectedTranslation { get; set; } = "";
        public string Explanation { get; set; } = "";
    }
}

