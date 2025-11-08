namespace IELTS_Learning_Tool.Utils
{
    public class ParseResult
    {
        public string mode { get; set; } = "";
        public string? date { get; set; }
    }

    public static class ArgumentParser
    {
        public static ParseResult ParseArguments(string[] args)
        {
            var result = new ParseResult();
            
            // 如果没有参数，显示帮助信息
            if (args.Length == 0)
            {
                result.mode = "help";
                return result;
            }

            // 解析参数
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower().Trim();

                // 处理帮助参数
                if (arg == "--help" || arg == "-h")
                {
                    result.mode = "help";
                    return result;
                }

                // 处理单词学习模式
                if (arg == "--words" || arg == "-w")
                {
                    result.mode = "words";
                    return result;
                }

                // 处理每日文章模式
                if (arg == "--article" || arg == "-a")
                {
                    result.mode = "article";
                    return result;
                }

                // 处理每日报告模式
                if (arg == "--daily-report" || arg == "-d")
                {
                    result.mode = "daily-report";
                    // 检查是否有日期参数
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        result.date = args[i + 1];
                    }
                    return result;
                }
            }

            // 如果参数不匹配，返回空字符串（会在主函数中显示错误）
            result.mode = "";
            return result;
        }
    }
}

