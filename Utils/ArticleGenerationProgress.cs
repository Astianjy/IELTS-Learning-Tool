using System;

namespace IELTS_Learning_Tool.Utils
{
    public class ArticleGenerationProgress
    {
        public bool IsComplete { get; set; } = false;
        public bool HasError { get; set; } = false;
        public string CurrentStatus { get; set; } = "正在初始化...";
        public string? ErrorMessage { get; set; }
        public int CurrentStep { get; set; } = 0;
        public int TotalSteps { get; set; } = 3;
        public int LastDisplayedStep { get; set; } = -1;
        
        // 子步骤进度（0-100，用于更精细的进度显示）
        public int SubStepProgress { get; set; } = 0;

        // 获取当前步骤的百分比
        public int GetPercentage()
        {
            if (HasError)
                return 0;
                
            // 步骤0: 初始化 (10%)
            // 步骤1: 生成文章 (10-30%)
            // 步骤2: 翻译文章 (30-60%)
            // 步骤3: 提取词汇 (60-80%)
            // 步骤4: 完成 (100%)
            int basePercentage = CurrentStep switch
            {
                0 => 10,   // 初始化
                1 => 30,   // 生成文章完成
                2 => 60,   // 翻译完成
                3 => 80,   // 提取词汇完成
                4 => 100,  // 全部完成
                _ => Math.Min(CurrentStep * 20 + 10, 100)
            };
            
            // 根据子步骤进度进行插值
            if (CurrentStep < 4)
            {
                int nextPercentage = (CurrentStep + 1) switch
                {
                    0 => 10,
                    1 => 30,
                    2 => 60,
                    3 => 80,
                    4 => 100,
                    _ => 100
                };
                
                int stepRange = nextPercentage - basePercentage;
                int subStepContribution = (int)(stepRange * (SubStepProgress / 100.0));
                return Math.Min(basePercentage + subStepContribution, 100);
            }
            
            return basePercentage;
        }
        
        public void SetError(string errorMessage)
        {
            HasError = true;
            ErrorMessage = errorMessage;
            CurrentStatus = $"错误: {errorMessage}";
        }
    }
}

