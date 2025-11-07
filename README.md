# IELTS Learning Tool - 雅思学习工具

一个基于 Google Gemini API 的智能雅思学习工具，提供单词学习和每日文章阅读功能，帮助提高雅思英语水平。

## ✨ 功能特性

### 📚 单词学习模式
- 从配置的主题列表中随机选择 IELTS 核心词汇
- 提供单词的音标、释义和例句
- 交互式翻译练习，实时评估翻译质量
- 生成详细的 HTML 学习报告，包含评分和解析

### 📖 每日文章模式
- 从配置的主题中随机生成 500-1000 词的英文文章
- 自动生成全文中文翻译
- 提取文章中的重点词汇（含音标、释义、例句）
- 生成精美的 HTML 报告，包含原文、翻译和词汇表

## 🚀 快速开始

### 系统要求

- .NET 9.0 SDK 或更高版本
- Google Gemini API 密钥

### 安装步骤

1. **克隆或下载项目**
   ```bash
   git clone <repository-url>
   cd IELTS-Learning-Tool
   ```

2. **配置 API 密钥**
   
   编辑 `config.json` 文件，将 `YOUR_API_KEY_HERE` 替换为你的 Google Gemini API 密钥：
   ```json
   {
     "GoogleApiKey": "YOUR_API_KEY_HERE",
     "WordCount": 20,
     "Topics": [...],
     "ArticleKeyWordsCount": 15
   }
   ```

   或者运行程序时，如果配置文件中没有 API 密钥，程序会提示你输入。

3. **运行程序**
   ```bash
   dotnet run
   ```

## 📖 使用方法

### 命令行参数

程序支持以下命令行参数：

```bash
# 显示帮助信息
dotnet run -- --help
dotnet run -- -h

# 单词学习模式（默认）
dotnet run -- --words
dotnet run -- -w

# 每日文章模式
dotnet run -- --article
dotnet run -- -a

# 无参数运行（默认单词学习模式）
dotnet run
```

### 单词学习模式

1. 运行程序后，程序会从配置的主题中随机选择指定数量的单词
2. 对每个单词，你需要将给出的英文例句翻译成中文
3. 输入 `Pass` 可以跳过当前单词
4. 完成后，程序会评估你的翻译并生成 HTML 报告

### 每日文章模式

1. 运行程序后，程序会随机选择一个主题
2. 自动生成英文文章、中文翻译和重点词汇
3. 生成包含完整内容的 HTML 报告

## ⚙️ 配置文件

`config.json` 文件包含以下配置项：

```json
{
  "GoogleApiKey": "YOUR_API_KEY_HERE",    // Google Gemini API 密钥
  "WordCount": 20,                        // 单词学习模式一次出题数量
  "Topics": [                             // 主题列表
    "natural geography",
    "plant research",
    "animal protection",
    ...
  ],
  "ArticleKeyWordsCount": 15              // 每日文章模式提取的重点词汇数量
}
```

### 配置说明

- **GoogleApiKey**: Google Gemini API 密钥，可以从 [Google AI Studio](https://makersuite.google.com/app/apikey) 获取
- **WordCount**: 单词学习模式每次练习的单词数量（默认 20）
- **Topics**: 可用的主题列表，你可以根据需要添加或修改主题
- **ArticleKeyWordsCount**: 文章模式中提取的重点词汇数量（默认 15）

## 📁 项目结构

```
IELTS-Learning-Tool/
├── Models/                          # 数据模型
│   ├── Article.cs                  # 文章模型
│   └── VocabularyWord.cs           # 词汇模型
│
├── Services/                        # 服务层
│   ├── GeminiService.cs            # Gemini API 服务
│   └── ReportGenerator.cs          # HTML 报告生成服务
│
├── Configuration/                   # 配置相关
│   ├── AppConfig.cs                # 配置模型
│   └── ConfigLoader.cs             # 配置加载器
│
├── Utils/                           # 工具类
│   ├── ArgumentParser.cs           # 命令行参数解析
│   ├── ArticleGenerationProgress.cs # 进度跟踪
│   ├── EnumerableHelper.cs         # 枚举辅助方法
│   ├── HelpDisplay.cs              # 帮助信息显示
│   ├── HtmlHelper.cs               # HTML 工具
│   └── ProgressDisplay.cs          # 进度显示
│
├── Program.cs                       # 主程序入口
├── config.json                      # 配置文件
└── IELTS-Learning-Tool.csproj      # 项目文件
```

## 🎯 使用示例

### 示例 1: 运行单词学习模式

```bash
dotnet run -- --words
```

输出示例：
```
--- IELTS Vocabulary Learning Mode ---

Fetching 20 new IELTS words for you... Please wait.

Successfully fetched 20 words. Let's begin!
----------------------------------------------------

Question 1/20
Word: ubiquitous
Phonetics: /juːˈbɪkwɪtəs/
Definition: adj. 普遍存在的；无所不在的

Please translate the following sentence into Chinese:
The company's logo has become ubiquitous all over the world.

Your translation: 
```

### 示例 2: 运行每日文章模式

```bash
dotnet run -- --article
```

输出示例：
```
--- IELTS Daily Article Mode ---

[|] 已选择主题: natural geography，正在生成文章内容... (0%)
[/] 文章内容生成完成，正在生成中文翻译... (40%)
[-] 翻译完成，正在提取重点词汇... (75%)
[\] 完成！ (100%)

成功生成文章！主题: natural geography
文章长度: 1245 字符
重点词汇: 15 个
正在生成 HTML 报告...

文章报告已成功生成到项目目录。
```

## 📊 报告文件

程序会在项目目录下生成以下类型的报告文件：

- **单词学习报告**: `IELTS_Report_YYYYMMDD_HHMMSS.html`
- **每日文章报告**: `IELTS_Article_YYYYMMDD_HHMMSS.html`

报告文件包含时间戳，方便区分不同时间生成的内容。

## 🛠️ 技术栈

- **.NET 9.0**: 跨平台应用程序框架
- **Google Gemini API**: AI 文本生成服务
- **Microsoft.Extensions.Configuration**: 配置管理
- **HTML/CSS**: 报告样式

## 📝 开发说明

### 构建项目

```bash
dotnet build
```

### 运行测试

确保配置文件正确后，直接运行程序进行测试。

### 扩展功能

项目采用模块化设计，易于扩展：

- 添加新的学习模式：在 `Program.cs` 中添加新的模式处理方法
- 修改报告样式：编辑 `Services/ReportGenerator.cs` 中的 HTML 模板
- 添加新主题：在 `config.json` 的 `Topics` 数组中添加

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证。

## 📧 联系方式

如有问题或建议，请提交 Issue。

---

**祝您雅思学习顺利！** 🎉

