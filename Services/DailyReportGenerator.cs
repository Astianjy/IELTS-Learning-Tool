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
        private static volatile bool _progressComplete = false;
        
        /// <summary>
        /// 获取报告目录路径，如果不存在则创建
        /// </summary>
        private static string GetReportsDirectory()
        {
            string reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            if (!Directory.Exists(reportsDir))
            {
                Directory.CreateDirectory(reportsDir);
            }
            return reportsDir;
        }
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

            // 重置进度标志
            _progressComplete = false;
            
            // 收集所有需要生成复习例句的单词
            var wordsToReview = todayRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Word))
                .Select(r => r.Word)
                .Distinct()
                .ToList();

            if (wordsToReview.Count == 0)
            {
                Console.WriteLine("没有需要生成复习例句的单词。");
                return;
            }

            // 启动进度显示任务
            var progressTask = ShowProgressAsync(wordsToReview.Count);
            
            // 批量生成复习例句
            Dictionary<string, string> reviewSentences;
            try
            {
                reviewSentences = await geminiService.GenerateReviewSentencesBatchAsync(wordsToReview);
                _progressComplete = true;
                await progressTask;
                Console.Write("\r✓ 复习例句生成完成，正在生成报告...\n");
            }
            catch (Exception ex)
            {
                _progressComplete = true;
                await progressTask;
                Console.WriteLine($"\n批量生成复习例句失败: {ex.Message}");
                // 如果批量生成失败，使用原始例句
                reviewSentences = new Dictionary<string, string>();
                foreach (var word in wordsToReview)
                {
                    reviewSentences[word] = $"Review the usage of: {word}";
                }
            }

            // 构建ReviewWord列表
            var reviewWords = new List<ReviewWord>();
            foreach (var record in todayRecords)
            {
                if (!string.IsNullOrWhiteSpace(record.Word))
                {
                    string reviewSentence = reviewSentences.ContainsKey(record.Word)
                        ? reviewSentences[record.Word]
                        : $"Review the usage of: {record.Word}"; // 如果找不到，使用默认值

                    reviewWords.Add(new ReviewWord
                    {
                        Word = record.Word,
                        Phonetics = record.Phonetics ?? "",
                        Definition = record.Definition ?? "",
                        ReviewSentence = reviewSentence,
                        Score = record.Score,
                        UserTranslation = record.UserTranslation,
                        CorrectedTranslation = record.CorrectedTranslation,
                        Explanation = record.Explanation
                    });
                }
            }

            // 生成HTML报告
            string html = GenerateDailyReportHtml(todayRecords, reviewWords);
            
            // 保存文件
            string fileName = GetUniqueFileName("IELTS_Daily_Report");
            File.WriteAllText(fileName, html, Encoding.UTF8);
            
            Console.ForegroundColor = ConsoleColor.Green;
            // 显示相对路径（相对于当前目录）
            string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            Console.WriteLine($"\n每日报告已成功生成: {relativePath}");
            Console.ResetColor();
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

            // 辅助方法：判断是否是Pass（包括空字符串）
            bool IsPass(WordLearningRecord r) => string.IsNullOrWhiteSpace(r.UserTranslation) || r.UserTranslation == "Pass";
            
            // 计算统计数据（Pass和空字符串都不算已回答）
            int totalWords = todayRecords.Count;
            int answeredWords = todayRecords.Count(r => !IsPass(r));
            int passCount = todayRecords.Count(r => IsPass(r));
            double averageScore = todayRecords.Where(r => !IsPass(r)).Select(r => r.Score).DefaultIfEmpty(0).Average();
            int highScoreCount = todayRecords.Count(r => !IsPass(r) && r.Score >= 8);
            int mediumScoreCount = todayRecords.Count(r => !IsPass(r) && r.Score >= 5 && r.Score < 8);
            int lowScoreCount = todayRecords.Count(r => IsPass(r) || (!IsPass(r) && r.Score < 5));

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
            sb.AppendLine("        .phonetics { color: #7f8c8d; font-style: italic; margin-bottom: 5px; }");
            sb.AppendLine("        .definition { color: #555; }");
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
            sb.AppendLine("                            <th>音标与中文翻译</th>");
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
                // Pass的单词或空字符串在"你的翻译"列显示为Pass，而不是空白
                bool isPass = string.IsNullOrWhiteSpace(review.UserTranslation) || review.UserTranslation == "Pass";
                string userTranslationDisplay = isPass
                    ? "<em style='color:#dc3545; font-weight:bold;'>Pass</em>" 
                    : HtmlHelper.EscapeHtml(review.UserTranslation);
                
                // 格式化音标和中文翻译显示
                string phoneticsAndDefinition = $"<div class=\"phonetics\">{HtmlHelper.EscapeHtml(review.Phonetics)}</div>" +
                    $"<div class=\"definition\">{HtmlHelper.EscapeHtml(review.Definition)}</div>";
                
                sb.AppendLine("                        <tr>");
                sb.AppendLine($"                            <td class=\"word\">{HtmlHelper.EscapeHtml(review.Word)}</td>");
                sb.AppendLine($"                            <td>{phoneticsAndDefinition}</td>");
                sb.AppendLine($"                            <td><span class=\"review-sentence\">{HtmlHelper.EscapeHtml(review.ReviewSentence)}</span></td>");
                sb.AppendLine($"                            <td>{userTranslationDisplay}</td>");
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
            string reportsDir = GetReportsDirectory();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int counter = 0;
            string fileName;
            
            do
            {
                fileName = counter == 0 
                    ? Path.Combine(reportsDir, $"{prefix}_{timestamp}.html")
                    : Path.Combine(reportsDir, $"{prefix}_{timestamp}_{counter}.html");
                counter++;
            } while (File.Exists(fileName) && counter < 100);
            
            return fileName;
        }

        /// <summary>
        /// 显示进度动画
        /// </summary>
        private static async System.Threading.Tasks.Task ShowProgressAsync(int totalWords)
        {
            string[] spinner = { "|", "/", "-", "\\" };
            int spinnerIndex = 0;

            while (!_progressComplete)
            {
                string spinnerChar = spinner[spinnerIndex % spinner.Length];
                Console.Write($"\r[{spinnerChar}] 正在生成 {totalWords} 个单词的复习例句...");
                spinnerIndex++;
                await System.Threading.Tasks.Task.Delay(100);
            }
            
            // 清除进度行
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
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
        public string ReviewSentence { get; set; } = "";
        public int Score { get; set; }
        public string UserTranslation { get; set; } = "";
        public string CorrectedTranslation { get; set; } = "";
        public string Explanation { get; set; } = "";
    }
}

