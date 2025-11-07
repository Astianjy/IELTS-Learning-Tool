namespace IELTS_Learning_Tool.Utils
{
    public static class HtmlHelper
    {
        // Helper to escape HTML special characters
        public static string EscapeHtml(string text)
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

