using System.Collections.Generic;

namespace IELTS_Learning_Tool.Models
{
    public class Article
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Translation { get; set; } = "";
        public string Topic { get; set; } = "";
        public List<VocabularyWord> KeyWords { get; set; } = new List<VocabularyWord>();
    }
}

