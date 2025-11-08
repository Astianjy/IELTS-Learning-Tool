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
                
            // 步骤0: 生成文章 (0-40%)
            // 步骤1: 翻译文章 (40-75%)
            // 步骤2: 提取词汇 (75-95%)
            // 步骤3: 完成 (100%)
            int basePercentage = CurrentStep switch
            {
                0 => 0,
                1 => 40,
                2 => 75,
                3 => 100,
                _ => Math.Min(CurrentStep * 33, 100)
            };
            
            // 根据子步骤进度进行插值
            if (CurrentStep < 3)
            {
                int nextPercentage = (CurrentStep + 1) switch
                {
                    0 => 0,
                    1 => 40,
                    2 => 75,
                    3 => 100,
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

