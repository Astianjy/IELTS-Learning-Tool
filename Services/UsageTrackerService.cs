using System;
using System.IO;
using System.Text.Json;
using IELTS_Learning_Tool.Models;

namespace IELTS_Learning_Tool.Services
{
    /// <summary>
    /// 使用记录跟踪服务，用于持久化已使用的词汇和例句
    /// </summary>
    public class UsageTrackerService
    {
        private readonly string _recordFilePath;
        private UsageRecord _record;
        
        public UsageTrackerService(string recordFilePath = "usage_record.json")
        {
            _recordFilePath = recordFilePath;
            _record = LoadRecord();
        }
        
        /// <summary>
        /// 加载使用记录
        /// </summary>
        private UsageRecord LoadRecord()
        {
            try
            {
                if (File.Exists(_recordFilePath))
                {
                    string json = File.ReadAllText(_recordFilePath);
                    var record = JsonSerializer.Deserialize<UsageRecord>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    if (record != null)
                    {
                        return record;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载使用记录失败: {ex.Message}，将创建新记录");
            }
            
            return new UsageRecord();
        }
        
        /// <summary>
        /// 保存使用记录
        /// </summary>
        public void SaveRecord()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(_record, options);
                File.WriteAllText(_recordFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存使用记录失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 记录使用的词汇
        /// </summary>
        public void RecordWord(string word)
        {
            _record.RecordWord(word);
            SaveRecord();
        }
        
        /// <summary>
        /// 批量记录使用的词汇
        /// </summary>
        public void RecordWords(IEnumerable<string> words)
        {
            foreach (var word in words)
            {
                _record.RecordWord(word);
            }
            SaveRecord();
        }
        
        /// <summary>
        /// 记录使用的例句
        /// </summary>
        public void RecordSentence(string sentence)
        {
            _record.RecordSentence(sentence);
            SaveRecord();
        }
        
        /// <summary>
        /// 批量记录使用的例句
        /// </summary>
        public void RecordSentences(IEnumerable<string> sentences)
        {
            foreach (var sentence in sentences)
            {
                _record.RecordSentence(sentence);
            }
            SaveRecord();
        }
        
        /// <summary>
        /// 检查词汇是否已使用
        /// </summary>
        public bool IsWordUsed(string word)
        {
            return _record.IsWordUsed(word);
        }
        
        /// <summary>
        /// 检查例句是否已使用
        /// </summary>
        public bool IsSentenceUsed(string sentence)
        {
            return _record.IsSentenceUsed(sentence);
        }
        
        /// <summary>
        /// 获取使用记录
        /// </summary>
        public UsageRecord GetRecord()
        {
            return _record;
        }
        
        /// <summary>
        /// 重置使用记录
        /// </summary>
        public void Reset()
        {
            _record.Reset();
            SaveRecord();
        }
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        public (int wordCount, int sentenceCount) GetStatistics()
        {
            return _record.GetStatistics();
        }
    }
}

