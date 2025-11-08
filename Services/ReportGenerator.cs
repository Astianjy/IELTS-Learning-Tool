using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IELTS_Learning_Tool.Models;
using IELTS_Learning_Tool.Utils;

namespace IELTS_Learning_Tool.Services
{
    public static class ReportGenerator
    {
        public static void GenerateWordsReport(List<VocabularyWord> words)
        {
            // 辅助方法：判断是否是Pass（包括空字符串）
            bool IsPass(VocabularyWord w) => string.IsNullOrWhiteSpace(w.UserTranslation) || w.UserTranslation == "Pass";
            
            // 计算统计数据（Pass和空字符串都不算已回答）
            var answeredWords = words.Where(w => !IsPass(w)).ToList();
            var totalScore = answeredWords.Sum(w => w.Score);
            var averageScore = answeredWords.Count > 0 ? (double)totalScore / answeredWords.Count : 0;
            var passCount = words.Count(w => IsPass(w));
            var highScoreCount = answeredWords.Count(w => w.Score >= 8);
            var mediumScoreCount = answeredWords.Count(w => w.Score >= 5 && w.Score < 8);
            var lowScoreCount = words.Count(w => IsPass(w) || (!IsPass(w) && w.Score < 5));

            var sb = new StringBuilder();
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string reportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>IELTS 答题报告 - {today}</title>");
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
            sb.AppendLine("        .details { font-size: 0.9em; color: #555; margin-top: 5px; }");
            sb.AppendLine("        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #666; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <div class=\"header\">");
            sb.AppendLine($"            <h1>📝 IELTS 答题报告</h1>");
            sb.AppendLine($"            <p>答题日期: {today} | 报告生成时间: {reportTime}</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"content\">");
            
            // 统计卡片
            sb.AppendLine("            <div class=\"stats\">");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>总单词数</h3><p class=\"value\">{words.Count}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>平均分数</h3><p class=\"value\">{averageScore:F1}/10</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>已回答</h3><p class=\"value\">{answeredWords.Count}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>Pass</h3><p class=\"value\">{passCount}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>高分 (≥8)</h3><p class=\"value\">{highScoreCount}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>中等 (5-7)</h3><p class=\"value\">{mediumScoreCount}</p></div>");
            sb.AppendLine($"                <div class=\"stat-card\"><h3>需改进 (<5)</h3><p class=\"value\">{lowScoreCount}</p></div>");
            sb.AppendLine("            </div>");

            // 答题内容
            sb.AppendLine("            <div class=\"section\">");
            sb.AppendLine("                <h2>📖 答题详情</h2>");
            sb.AppendLine("                <table>");
            sb.AppendLine("                    <thead>");
            sb.AppendLine("                        <tr>");
            sb.AppendLine("                            <th>单词与例句</th>");
            sb.AppendLine("                            <th>你的翻译</th>");
            sb.AppendLine("                            <th>修正翻译与解释</th>");
            sb.AppendLine("                            <th>得分</th>");
            sb.AppendLine("                        </tr>");
            sb.AppendLine("                    </thead>");
            sb.AppendLine("                    <tbody>");

            foreach (var word in words)
            {
                string scoreColor = word.Score >= 8 ? "score-high" : word.Score >= 5 ? "score-medium" : "score-low";
                // Pass的单词或空字符串在"你的翻译"列显示为Pass
                bool isPass = string.IsNullOrWhiteSpace(word.UserTranslation) || word.UserTranslation == "Pass";
                string userTranslationDisplay = isPass
                    ? "<em style='color:#dc3545; font-weight:bold;'>Pass</em>" 
                    : HtmlHelper.EscapeHtml(word.UserTranslation);
                
                sb.AppendLine("                        <tr>");
                sb.AppendLine($"                            <td><div class=\"word\">{HtmlHelper.EscapeHtml(word.Word)}</div><div class=\"details\">({HtmlHelper.EscapeHtml(word.Phonetics)})</div>{HtmlHelper.EscapeHtml(word.Sentence)}</td>");
                sb.AppendLine($"                            <td>{userTranslationDisplay}</td>");
                sb.AppendLine($"                            <td><div>{HtmlHelper.EscapeHtml(word.CorrectedTranslation)}</div><div class=\"details\">{HtmlHelper.EscapeHtml(word.Explanation)}</div></td>");
                sb.AppendLine($"                            <td class=\"score {scoreColor}\">{word.Score}/10</td>");
                sb.AppendLine("                        </tr>");
            }

            sb.AppendLine("                    </tbody>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");

            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"footer\">");
            sb.AppendLine("            <p>IELTS Learning Tool - 答题报告</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"IELTS_Report_{timestamp}.html";
            
            // 确保文件名唯一（如果存在则添加序号）
            int counter = 1;
            while (File.Exists(fileName))
            {
                fileName = $"IELTS_Report_{timestamp}_{counter}.html";
                counter++;
            }
            
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        public static void GenerateArticleReport(Article article)
        {
            var sb = new StringBuilder();
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string reportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>IELTS 每日文章 - {today}</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; min-height: 100vh; }");
            sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 15px; box-shadow: 0 20px 60px rgba(0,0,0,0.3); overflow: hidden; }");
            sb.AppendLine("        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; }");
            sb.AppendLine("        .header h1 { font-size: 2.5em; margin-bottom: 10px; }");
            sb.AppendLine("        .header p { font-size: 1.1em; opacity: 0.9; }");
            sb.AppendLine("        .content { padding: 40px; }");
            sb.AppendLine("        .section { margin-bottom: 40px; }");
            sb.AppendLine("        .section h2 { color: #667eea; font-size: 1.8em; margin-bottom: 20px; padding-bottom: 10px; border-bottom: 3px solid #667eea; }");
            sb.AppendLine("        .article-content { background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); padding: 25px; border-radius: 10px; white-space: pre-wrap; font-size: 1.1em; line-height: 1.8; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .translation { background: linear-gradient(135deg, #e8f4f8 0%, #b8d4e3 100%); padding: 25px; border-radius: 10px; white-space: pre-wrap; font-size: 1.1em; line-height: 1.8; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }");
            sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("        th, td { padding: 15px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("        th { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; font-weight: 600; }");
            sb.AppendLine("        tr:hover { background-color: #f5f5f5; }");
            sb.AppendLine("        .word { font-weight: bold; color: #667eea; font-size: 1.1em; }");
            sb.AppendLine("        .phonetics { color: #7f8c8d; font-style: italic; }");
            sb.AppendLine("        .definition { color: #555; }");
            sb.AppendLine("        .sentence { color: #2c3e50; font-style: italic; margin-top: 5px; }");
            sb.AppendLine("        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #666; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <div class=\"header\">");
            sb.AppendLine($"            <h1>📰 {HtmlHelper.EscapeHtml(article.Title)}</h1>");
            sb.AppendLine($"            <p>主题: {HtmlHelper.EscapeHtml(article.Topic)} | 生成时间: {reportTime}</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"content\">");
            
            sb.AppendLine("            <div class=\"section\">");
            sb.AppendLine("                <h2>📄 英文原文</h2>");
            sb.AppendLine("                <div class=\"article-content\">");
            sb.AppendLine(HtmlHelper.EscapeHtml(article.Content).Replace("\n", "<br>"));
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");

            sb.AppendLine("            <div class=\"section\">");
            sb.AppendLine("                <h2>🌐 中文翻译</h2>");
            sb.AppendLine("                <div class=\"translation\">");
            sb.AppendLine(HtmlHelper.EscapeHtml(article.Translation).Replace("\n", "<br>"));
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");

            sb.AppendLine("            <div class=\"section\">");
            sb.AppendLine("                <h2>📚 重点词汇</h2>");
            sb.AppendLine("                <table>");
            sb.AppendLine("                    <thead>");
            sb.AppendLine("                        <tr>");
            sb.AppendLine("                            <th>单词</th>");
            sb.AppendLine("                            <th>音标</th>");
            sb.AppendLine("                            <th>释义</th>");
            sb.AppendLine("                            <th>例句</th>");
            sb.AppendLine("                        </tr>");
            sb.AppendLine("                    </thead>");
            sb.AppendLine("                    <tbody>");

            foreach (var word in article.KeyWords)
            {
                sb.AppendLine("                        <tr>");
                sb.AppendLine($"                            <td class=\"word\">{HtmlHelper.EscapeHtml(word.Word)}</td>");
                sb.AppendLine($"                            <td class=\"phonetics\">{HtmlHelper.EscapeHtml(word.Phonetics)}</td>");
                sb.AppendLine($"                            <td class=\"definition\">{HtmlHelper.EscapeHtml(word.Definition)}</td>");
                sb.AppendLine($"                            <td class=\"sentence\">{HtmlHelper.EscapeHtml(word.Sentence)}</td>");
                sb.AppendLine("                        </tr>");
            }

            sb.AppendLine("                    </tbody>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");

            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"footer\">");
            sb.AppendLine("            <p>IELTS Learning Tool - 每日文章</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"IELTS_Article_{timestamp}.html";
            
            // 确保文件名唯一（如果存在则添加序号）
            int counter = 1;
            while (File.Exists(fileName))
            {
                fileName = $"IELTS_Article_{timestamp}_{counter}.html";
                counter++;
            }
            
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }
    }
}

