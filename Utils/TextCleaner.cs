using System.Text.RegularExpressions;

namespace IELTS_Learning_Tool.Utils
{
    /// <summary>
    /// 文本清理工具，用于移除不需要的格式标记
    /// </summary>
    public static class TextCleaner
    {
        /// <summary>
        /// 移除 Markdown 格式标记（**、*、__、_等）
        /// </summary>
        public static string RemoveMarkdownFormatting(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            
            var cleaned = text;
            
            // 移除 **word** 格式（粗体）
            cleaned = Regex.Replace(cleaned, @"\*\*([^*]+)\*\*", "$1");
            
            // 移除 *word* 格式（斜体，但保留单独的*号）
            // 只匹配单词周围的*号，避免误删
            cleaned = Regex.Replace(cleaned, @"(?<!\*)\*([^*\s]+)\*(?!\*)", "$1");
            
            // 移除 __word__ 格式（粗体）
            cleaned = Regex.Replace(cleaned, @"__([^_]+)__", "$1");
            
            // 移除 _word_ 格式（斜体）
            cleaned = Regex.Replace(cleaned, @"(?<!_)_([^_\s]+)_(?!_)", "$1");
            
            // 移除多余的空白字符
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            return cleaned;
        }
        
        /// <summary>
        /// 清理句子中的格式标记，特别处理单词周围的标记
        /// </summary>
        public static string CleanSentence(string? sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return "";
            
            return RemoveMarkdownFormatting(sentence);
        }
    }
}

