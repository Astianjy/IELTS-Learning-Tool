using System.Text;

namespace IELTS_Learning_Tool.Utils
{
    public static class HtmlHelper
    {
        // Helper to escape HTML special characters
        // 使用 StringBuilder 提高性能，特别是对于长文本
        public static string EscapeHtml(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            // 先替换 &，避免双重转义
            var sb = new StringBuilder(text.Length * 2);
            
            foreach (char c in text)
            {
                switch (c)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '\'':
                        sb.Append("&#39;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            
            return sb.ToString();
        }
    }
}

