using System;
using System.Threading.Tasks;

namespace IELTS_Learning_Tool.Utils
{
    public static class ProgressDisplay
    {
        public static async Task ShowProgressAsync(ArticleGenerationProgress progress)
        {
            string[] spinner = { "|", "/", "-", "\\" };
            int spinnerIndex = 0;
            string lastStatus = "";
            int lastStep = -1;
            bool isFirstUpdate = true;

            while (!progress.IsComplete)
            {
                string currentStatus = progress.CurrentStatus;
                int currentStep = progress.CurrentStep;

                // 只在状态改变时更新显示
                if (currentStatus != lastStatus || currentStep != lastStep)
                {
                    // 如果不是第一次更新，保留之前的输出并换行
                    if (!isFirstUpdate)
                    {
                        Console.WriteLine();
                    }
                    else
                    {
                        isFirstUpdate = false;
                    }

                    int percentage = progress.GetPercentage();
                    string spinnerChar = spinner[spinnerIndex % spinner.Length];
                    
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{spinnerChar}] {currentStatus} ({percentage}%)");
                    Console.ResetColor();

                    lastStatus = currentStatus;
                    lastStep = currentStep;
                    progress.LastDisplayedStep = currentStep;
                    spinnerIndex++;
                }
                else
                {
                    // 状态未改变时，更新旋转动画
                    Console.Write("\r");
                    int percentage = progress.GetPercentage();
                    string spinnerChar = spinner[spinnerIndex % spinner.Length];
                    
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{spinnerChar}] {currentStatus} ({percentage}%)");
                    Console.ResetColor();
                    
                    spinnerIndex++;
                }

                await Task.Delay(100);
            }

            // 完成时，换行并保留最后的进度信息
            Console.WriteLine();
        }
    }
}

