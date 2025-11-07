using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IELTS_Learning_Tool.Models;
using IELTS_Learning_Tool.Utils;

namespace IELTS_Learning_Tool.Services
{
    public static class ReportGenerator
    {
        public static void GenerateWordsReport(List<VocabularyWord> words)
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
                sb.AppendLine($"                    <td><div class='details'><strong>{HtmlHelper.EscapeHtml(word.Word)}</strong> ({HtmlHelper.EscapeHtml(word.Phonetics)})</div>{HtmlHelper.EscapeHtml(word.Sentence)}</td>");
                sb.AppendLine($"                    <td>{(word.IsSkipped ? "Skipped" : HtmlHelper.EscapeHtml(word.UserTranslation))}</td>");
                sb.AppendLine($"                    <td><div>{HtmlHelper.EscapeHtml(word.CorrectedTranslation)}</div><div class='details'>{HtmlHelper.EscapeHtml(word.Explanation)}</div></td>");
                
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

        public static void GenerateArticleReport(Article article)
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
            sb.AppendLine($"        <h1>{HtmlHelper.EscapeHtml(article.Title)}</h1>");
            sb.AppendLine($"        <div class=\"topic\">主题: {HtmlHelper.EscapeHtml(article.Topic)}</div>");
            
            sb.AppendLine("        <div class=\"article-section\">");
            sb.AppendLine("            <h2>英文原文</h2>");
            sb.AppendLine("            <div class=\"article-content\">");
            sb.AppendLine(HtmlHelper.EscapeHtml(article.Content).Replace("\n", "<br>"));
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"article-section\">");
            sb.AppendLine("            <h2>中文翻译</h2>");
            sb.AppendLine("            <div class=\"translation\">");
            sb.AppendLine(HtmlHelper.EscapeHtml(article.Translation).Replace("\n", "<br>"));
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
                sb.AppendLine($"                        <td class=\"word\">{HtmlHelper.EscapeHtml(word.Word)}</td>");
                sb.AppendLine($"                        <td class=\"phonetics\">{HtmlHelper.EscapeHtml(word.Phonetics)}</td>");
                sb.AppendLine($"                        <td class=\"definition\">{HtmlHelper.EscapeHtml(word.Definition)}</td>");
                sb.AppendLine($"                        <td class=\"sentence\">{HtmlHelper.EscapeHtml(word.Sentence)}</td>");
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
    }
}

