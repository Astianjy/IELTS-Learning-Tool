using System;
using System.Collections.Generic;

namespace IELTS_Learning_Tool.Models
{
    /// <summary>
    /// 使用记录，用于跟踪已使用的词汇和例句
    /// </summary>
    public class UsageRecord
    {
        public HashSet<string> UsedWords { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> UsedSentences { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
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
    }
}

