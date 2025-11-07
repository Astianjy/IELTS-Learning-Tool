namespace IELTS_Learning_Tool
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
    }
}
