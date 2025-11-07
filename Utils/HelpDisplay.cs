using System;

namespace IELTS_Learning_Tool.Utils
{
    public static class HelpDisplay
    {
        public static void ShowHelp()
        {
            Console.WriteLine("IELTS Learning Tool - 雅思学习工具");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  IELTS-Learning-Tool [选项]");
            Console.WriteLine();
            Console.WriteLine("选项:");
            Console.WriteLine("  --words, -w           单词学习模式");
            Console.WriteLine("                         从配置的主题中随机选择单词，提供翻译练习");
            Console.WriteLine();
            Console.WriteLine("  --article, -a         每日文章模式");
            Console.WriteLine("                         从配置的主题中随机选择一篇，生成500-1000词的英文文章");
            Console.WriteLine("                         包含全文翻译和重点词汇解释");
            Console.WriteLine();
            Console.WriteLine("  --help, -h            显示此帮助信息");
            Console.WriteLine();
            Console.WriteLine("注意: 必须指定一个模式参数才能运行程序。");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  IELTS-Learning-Tool --words      # 运行单词学习模式");
            Console.WriteLine("  IELTS-Learning-Tool --article    # 运行每日文章模式");
            Console.WriteLine("  IELTS-Learning-Tool --help       # 显示帮助信息");
            Console.WriteLine();
            Console.WriteLine("配置文件:");
            Console.WriteLine("  程序会读取项目目录下的 config.json 配置文件");
            Console.WriteLine("  包含 API 密钥、单词数量、主题列表等配置项");
        }
    }
}

