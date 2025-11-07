using System;

namespace IELTS_Learning_Tool.Utils
{
    public class ArticleGenerationProgress
    {
        public bool IsComplete { get; set; } = false;
        public string CurrentStatus { get; set; } = "正在初始化...";
        public int CurrentStep { get; set; } = 0;
        public int TotalSteps { get; set; } = 3;
        public int LastDisplayedStep { get; set; } = -1;

        // 获取当前步骤的百分比
        public int GetPercentage()
        {
            // 步骤0: 生成文章 (0-40%)
            // 步骤1: 翻译文章 (40-75%)
            // 步骤2: 提取词汇 (75-95%)
            // 步骤3: 完成 (100%)
            return CurrentStep switch
            {
                0 => 0,
                1 => 40,
                2 => 75,
                3 => 100,
                _ => Math.Min(CurrentStep * 33, 100)
            };
        }
    }
}

