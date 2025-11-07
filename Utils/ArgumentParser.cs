namespace IELTS_Learning_Tool.Utils
{
    public static class ArgumentParser
    {
        public static string ParseArguments(string[] args)
        {
            // 如果没有参数，默认返回 words 模式
            if (args.Length == 0)
            {
                return "words";
            }

            // 解析参数
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower().Trim();

                // 处理帮助参数
                if (arg == "--help" || arg == "-h")
                {
                    return "help";
                }

                // 处理单词学习模式
                if (arg == "--words" || arg == "-w")
                {
                    return "words";
                }

                // 处理每日文章模式
                if (arg == "--article" || arg == "-a")
                {
                    return "article";
                }
            }

            // 如果参数不匹配，返回空字符串（会在主函数中显示错误）
            return "";
        }
    }
}

