using System.Collections.Generic;

namespace IELTS_Learning_Tool.Configuration
{
    public class AppConfig
    {
        public string GoogleApiKey { get; set; } = "";
        public int WordCount { get; set; } = 20;
        public List<string> Topics { get; set; } = new List<string>();
        public int ArticleKeyWordsCount { get; set; } = 15;
        public int ExcludeDays { get; set; } = 7; // 排除最近几天的单词，默认7天
    }
}

