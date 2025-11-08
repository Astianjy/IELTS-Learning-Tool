using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace IELTS_Learning_Tool.Configuration
{
    public static class ConfigLoader
    {
        public static AppConfig LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();
            var config = new AppConfig();
            configuration.Bind(config);
            
            // 验证配置
            ValidateConfig(config);
            
            return config;
        }
        
        private static void ValidateConfig(AppConfig config)
        {
            var errors = new List<string>();
            
            if (config.WordCount <= 0)
            {
                errors.Add("WordCount 必须大于 0");
            }
            
            if (config.WordCount > 100)
            {
                errors.Add("WordCount 不应超过 100（建议值：10-50）");
            }
            
            if (config.ArticleKeyWordsCount <= 0)
            {
                errors.Add("ArticleKeyWordsCount 必须大于 0");
            }
            
            if (config.ArticleKeyWordsCount > 50)
            {
                errors.Add("ArticleKeyWordsCount 不应超过 50（建议值：10-30）");
            }
            
            if (config.Topics == null || config.Topics.Count == 0)
            {
                errors.Add("Topics 列表不能为空");
            }
            
            if (errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("配置验证警告：");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }
}

