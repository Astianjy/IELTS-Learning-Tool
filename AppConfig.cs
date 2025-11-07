using System.Collections.Generic;

namespace IELTS_Learning_Tool
{
    public class AppConfig
    {
        public string GoogleApiKey { get; set; } = "";
        public int WordCount { get; set; } = 20;
        public List<string> Topics { get; set; } = new List<string>();
        public int ArticleKeyWordsCount { get; set; } = 15;
    }
}

