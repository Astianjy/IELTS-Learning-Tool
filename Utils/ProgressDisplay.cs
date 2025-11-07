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

            while (!progress.IsComplete)
            {
                string currentStatus = progress.CurrentStatus;
                int currentStep = progress.CurrentStep;

                // 只在状态改变时更新显示
                if (currentStatus != lastStatus || currentStep != progress.LastDisplayedStep)
                {
                    Console.Write("\r");
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.Write("\r");

                    int percentage = progress.GetPercentage();
                    string spinnerChar = spinner[spinnerIndex % spinner.Length];
                    
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{spinnerChar}] {currentStatus} ({percentage}%)");
                    Console.ResetColor();

                    lastStatus = currentStatus;
                    progress.LastDisplayedStep = currentStep;
                    spinnerIndex++;
                }

                await Task.Delay(100);
            }

            // 清除进度行
            Console.Write("\r");
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.Write("\r");
        }
    }
}

