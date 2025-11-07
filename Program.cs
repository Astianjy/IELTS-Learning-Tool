using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IELTS_Learning_Tool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("--- Welcome to the IELTS Learning Tool ---");
            Console.Write("Please enter your Google Gemini API Key: ");
            string apiKey = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("API Key cannot be empty. Exiting.");
                return;
            }

            var geminiService = new GeminiService(apiKey);

            Console.WriteLine("\nFetching 20 new IELTS words for you... Please wait.");
            List<VocabularyWord> words = await geminiService.GetIeltsWordsAsync();

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
                sb.AppendLine($"                    <td><div class='details'><strong>{word.Word}</strong> ({word.Phonetics})</div>{word.Sentence}</td>");
                sb.AppendLine($"                    <td>{(word.IsSkipped ? "Skipped" : word.UserTranslation)}</td>");
                sb.AppendLine($"                    <td><div>{word.CorrectedTranslation}</div><div class='details'>{word.Explanation}</div></td>");
                
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
        
        // Helper to get index in a foreach loop
        private static IEnumerable<(T item, int index)> WithIndex<T>(IEnumerable<T> source)
        {
            int i = 0;
            foreach (var item in source)
            {
                yield return (item, i++);
            }
        }
    }
}