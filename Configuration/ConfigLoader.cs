using System.IO;
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
            return config;
        }
    }
}

