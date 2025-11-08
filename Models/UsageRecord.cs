using System;
using System.Collections.Generic;
using System.Linq;

namespace IELTS_Learning_Tool.Models
{
    /// <summary>
    /// 单词学习记录，包含日期和得分信息
    /// </summary>
    public class WordLearningRecord
    {
        public string Word { get; set; } = "";
        public string Sentence { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public int Score { get; set; } = 0;
        public string UserTranslation { get; set; } = "";
        public string CorrectedTranslation { get; set; } = "";
        public string Explanation { get; set; } = "";
        public bool IsSkipped { get; set; } = false;
    }
    
    /// <summary>
    /// 使用记录，用于跟踪已使用的词汇和例句
    /// </summary>
    public class UsageRecord
    {
        public HashSet<string> UsedWords { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> UsedSentences { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        // 按日期分类的单词学习记录
        public Dictionary<string, List<WordLearningRecord>> DailyRecords { get; set; } = new Dictionary<string, List<WordLearningRecord>>();
        
        /// <summary>
        /// 记录使用的词汇
        /// </summary>
        public void RecordWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                UsedWords.Add(word.Trim().ToLower());
                LastUpdated = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 记录使用的例句
        /// </summary>
        public void RecordSentence(string sentence)
        {
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                // 标准化句子（去除标点，转换为小写）用于比较
                var normalized = NormalizeSentence(sentence);
                UsedSentences.Add(normalized);
                LastUpdated = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 检查词汇是否已使用
        /// </summary>
        public bool IsWordUsed(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;
            return UsedWords.Contains(word.Trim().ToLower());
        }
        
        /// <summary>
        /// 检查例句是否已使用（基于标准化比较）
        /// </summary>
        public bool IsSentenceUsed(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return false;
            var normalized = NormalizeSentence(sentence);
            return UsedSentences.Contains(normalized);
        }
        
        /// <summary>
        /// 标准化句子用于比较（去除标点、空格，转换为小写）
        /// </summary>
        private string NormalizeSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return "";
            
            // 转换为小写，移除标点符号和多余空格
            var normalized = sentence.ToLower().Trim();
            // 移除常见的标点符号
            var charsToRemove = new[] { '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' };
            foreach (var c in charsToRemove)
            {
                normalized = normalized.Replace(c.ToString(), "");
            }
            // 标准化空格
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }
        
        /// <summary>
        /// 重置使用记录（可选：按时间或手动重置）
        /// </summary>
        public void Reset()
        {
            UsedWords.Clear();
            UsedSentences.Clear();
            LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        public (int wordCount, int sentenceCount) GetStatistics()
        {
            return (UsedWords.Count, UsedSentences.Count);
        }
        
        /// <summary>
        /// 记录单词学习信息（包含日期和得分）
        /// </summary>
        public void RecordWordLearning(WordLearningRecord record)
        {
            string dateKey = record.Date.ToString("yyyy-MM-dd");
            if (!DailyRecords.ContainsKey(dateKey))
            {
                DailyRecords[dateKey] = new List<WordLearningRecord>();
            }
            DailyRecords[dateKey].Add(record);
            
            // 同时记录到 UsedWords 和 UsedSentences（用于去重）
            RecordWord(record.Word);
            RecordSentence(record.Sentence);
        }
        
        /// <summary>
        /// 获取指定日期范围内的已使用单词
        /// </summary>
        public HashSet<string> GetUsedWordsInDateRange(int days)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DateTime cutoffDate = DateTime.Now.AddDays(-days);
            
            foreach (var kvp in DailyRecords)
            {
                if (DateTime.TryParse(kvp.Key, out DateTime recordDate) && recordDate >= cutoffDate)
                {
                    foreach (var record in kvp.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(record.Word))
                        {
                            result.Add(record.Word.Trim().ToLower());
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定日期范围内的已使用例句
        /// </summary>
        public HashSet<string> GetUsedSentencesInDateRange(int days)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DateTime cutoffDate = DateTime.Now.AddDays(-days);
            
            foreach (var kvp in DailyRecords)
            {
                if (DateTime.TryParse(kvp.Key, out DateTime recordDate) && recordDate >= cutoffDate)
                {
                    foreach (var record in kvp.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(record.Sentence))
                        {
                            var normalized = NormalizeSentence(record.Sentence);
                            result.Add(normalized);
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定日期的学习记录
        /// </summary>
        public List<WordLearningRecord> GetDailyRecords(string date)
        {
            if (DailyRecords.ContainsKey(date))
            {
                return DailyRecords[date];
            }
            return new List<WordLearningRecord>();
        }
        
        /// <summary>
        /// 获取今天的学习记录
        /// </summary>
        public List<WordLearningRecord> GetTodayRecords()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            return GetDailyRecords(today);
        }
    }
}

