namespace IELTS_Learning_Tool.Models
{
    public class VocabularyWord
    {
        public string Word { get; set; } = "";
        public string Phonetics { get; set; } = "";
        public string Definition { get; set; } = "";
        public string Sentence { get; set; } = "";
        public string UserTranslation { get; set; } = "";
        public int Score { get; set; }
        public string CorrectedTranslation { get; set; } = "";
        public string Explanation { get; set; } = "";
        public string OtherIncorrectWords { get; set; } = ""; // 句子中其他翻译不准确的单词
    }
}

