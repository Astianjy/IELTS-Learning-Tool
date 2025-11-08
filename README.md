# 你想在终端上学习英语吗？ 🚀

厌倦了复杂的应用界面和广告干扰？想要一个专注、高效的英语学习体验？

**IELTS Learning Tool** 是一个专为终端设计的智能雅思学习工具，让你在简洁的命令行界面中，享受 AI 驱动的个性化英语学习。

## ✨ 为什么选择终端学习？

- 🎯 **专注无干扰** - 纯文本界面，让你专注于学习本身
- ⚡ **快速高效** - 无需启动浏览器或应用，秒开即用
- 💻 **极客风格** - 在熟悉的终端环境中学习，更有仪式感
- 📊 **精美报告** - 自动生成 HTML 学习报告，随时回顾
- 🔄 **智能去重** - 自动记录已学词汇，确保每次都是新内容

## 🌟 核心功能特色

### 📚 词汇学习模式 - 智能出题，永不重复

**特色亮点：**
- ✨ **AI 智能出题** - 基于 Google Gemini AI，从雅思官方词汇范围中智能生成题目
- 🎲 **主题丰富** - 22 个主题领域，涵盖自然地理、科技发明、文化历史等
- 🚫 **智能去重** - 自动记录已使用的词汇和例句，确保每次练习都是新内容
- 📈 **词性平衡** - 自动平衡名词、动词、形容词等不同词性
- 🇺🇸 **美式发音** - 所有音标使用美式英语发音（US pronunciation）
- ⏱️ **快速生成** - 优化后的批量生成算法，3 个单词仅需 10-15 秒
- 📝 **翻译练习** - 交互式翻译练习，AI 实时评估并给出详细解析
- 🎯 **Pass 功能** - 输入 `Pass` 标记不会的单词，系统会显示正确答案供复习
- 📊 **学习报告** - 生成包含平均分、高分统计等详细数据的精美 HTML 报告
- 💾 **学习记录** - 自动保存学习记录，支持生成每日复习报告

**使用示例：**
```bash
./IELTS-Learning-Tool --words
```

你会看到：
```
--- IELTS Vocabulary Learning Mode ---

Fetching 3 new IELTS words for you... Please wait.

Successfully fetched 3 words. Let's begin!

Question 1/3
Word: ubiquitous
Phonetics: /juˈbɪkwɪtəs/
Definition: adj. 普遍存在的；无所不在的

Please translate the following sentence into Chinese:
The company's logo has become ubiquitous all over the world.

Your translation: 
```

### 📖 每日文章模式 - 个性化阅读，全面提升

**特色亮点：**
- 📰 **AI 生成文章** - 根据主题自动生成 500-1000 词的雅思水平文章
- 🌍 **主题随机** - 从 22 个主题中随机选择，每次都是新体验
- 🔄 **双语对照** - 自动生成完整的中文翻译，方便理解
- 📚 **重点词汇** - 智能提取文章中的重点词汇，含音标、释义、例句
- 📊 **进度显示** - 实时显示生成进度（10% → 30% → 60% → 80% → 100%）
- 🎨 **精美报告** - 生成包含原文、翻译、词汇表的现代化 HTML 报告

### 📅 每日复习报告模式 - 回顾学习，巩固记忆

**特色亮点：**
- 📚 **复习报告** - 从学习记录中读取指定日期的单词，生成复习报告
- 🔄 **新例句生成** - 为每个单词生成新的复习例句，避免重复
- 📊 **学习统计** - 显示总单词数、平均分数、高分/中等/需改进分布
- 📝 **完整记录** - 包含原始例句、复习例句、你的翻译、修正翻译和得分
- 🎨 **统一样式** - 与答题报告和文章报告使用相同的现代化设计风格

**使用示例：**
```bash
# 生成今天的复习报告
./IELTS-Learning-Tool --daily-report

# 生成指定日期的复习报告
./IELTS-Learning-Tool --daily-report 2024-11-08
```

你会看到：
```
正在生成 2024-11-08 的每日复习报告...
正在生成每日复习报告，请稍候...
正在为今天学习的单词生成新的复习例句...
正在生成复习例句 (1/4)...
正在生成复习例句 (2/4)...
正在生成复习例句 (3/4)...
正在生成复习例句 (4/4)...
每日报告已成功生成: IELTS_Daily_Report_20241108_160000.html
```

## 🚀 快速开始

### 系统要求

- .NET 9.0 SDK 或更高版本
- Google Gemini API 密钥（[免费获取](https://makersuite.google.com/app/apikey)）

### 安装步骤

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd IELTS-Learning-Tool
   ```

2. **配置 API 密钥**
   
   编辑 `config.json` 文件：
   ```json
   {
     "GoogleApiKey": "YOUR_GOOGLE_GEMINI_API_KEY",
     "WordCount": 20,
     "Topics": [...],
     "ArticleKeyWordsCount": 15
   }
   ```
   
   或者运行程序时，程序会安全地提示你输入 API 密钥（输入时显示 `*` 号）。

3. **运行程序**
   ```bash
   # 开发模式
   dotnet run -- --words
   
   # 或发布后运行
   ./IELTS-Learning-Tool --words
   ```

## 📖 使用方法

### 命令行参数

```bash
# 显示帮助信息
./IELTS-Learning-Tool --help
./IELTS-Learning-Tool -h

# 词汇学习模式
./IELTS-Learning-Tool --words
./IELTS-Learning-Tool -w

# 每日文章模式
./IELTS-Learning-Tool --article
./IELTS-Learning-Tool -a

# 每日复习报告模式（生成今天的复习报告）
./IELTS-Learning-Tool --daily-report
./IELTS-Learning-Tool -d

# 每日复习报告模式（生成指定日期的复习报告）
./IELTS-Learning-Tool --daily-report 2024-01-15
./IELTS-Learning-Tool -d 2024-01-15
```

### 词汇学习流程

1. **启动程序** - 运行 `--words` 模式
2. **智能出题** - AI 从雅思词汇范围中生成题目，自动避免重复
3. **翻译练习** - 将英文例句翻译成中文（支持中文输入，删除时自动处理宽字符）
4. **Pass 选项** - 输入 `Pass` 标记不会的单词（视为不会，得分 0，但仍会显示正确答案供复习）
5. **AI 评估** - 程序自动评估你的翻译，给出分数和详细解析
6. **生成报告** - 自动生成包含统计数据的精美 HTML 学习报告
7. **保存记录** - 学习记录自动保存到 `usage_record.json`，可用于生成复习报告

### 文章学习流程

1. **启动程序** - 运行 `--article` 模式
2. **选择主题** - 从配置的主题中随机选择
3. **生成内容** - AI 自动生成文章、翻译和重点词汇
4. **查看报告** - 打开生成的 HTML 报告，包含完整的学习内容

### 每日复习报告流程

1. **启动程序** - 运行 `--daily-report` 模式（可选日期参数）
2. **读取记录** - 从 `usage_record.json` 中读取指定日期的学习记录
3. **生成例句** - 为每个单词生成新的复习例句
4. **生成报告** - 自动生成包含复习内容的精美 HTML 报告

## ⚙️ 配置文件

`config.json` 文件包含以下配置项：

```json
{
  "GoogleApiKey": "YOUR_GOOGLE_GEMINI_API_KEY",  // Google Gemini API 密钥
  "WordCount": 20,                               // 单词学习模式一次出题数量
  "Topics": [                                     // 主题列表（22个主题）
    "natural geography",
    "plant research",
    "animal protection",
    "space exploration",
    "school education",
    "technological inventions",
    "cultural history",
    "language evolution",
    "entertainment and sports",
    "materials and substances",
    "fashion trends",
    "diet and health",
    "architecture and places",
    "transport and travel",
    "international government",
    "social economy",
    "laws and regulations",
    "battlefield conflicts",
    "social roles",
    "behaviors and actions",
    "body and health",
    "time and dates"
  ],
  "ArticleKeyWordsCount": 15                      // 文章模式提取的重点词汇数量
}
```

### 配置说明

- **GoogleApiKey**: Google Gemini API 密钥，可以从 [Google AI Studio](https://makersuite.google.com/app/apikey) 免费获取
- **WordCount**: 单词学习模式每次练习的单词数量（建议 3-50）
- **Topics**: 可用的主题列表，你可以根据需要添加或修改主题
- **ArticleKeyWordsCount**: 文章模式中提取的重点词汇数量（建议 10-30）

## 🎯 核心优势

### 1. 智能去重系统
- 自动记录已使用的词汇和例句
- 确保每次练习都是新内容
- 使用记录持久化保存（`usage_record.json`）

### 2. 优化的性能
- 批量生成算法，大幅提升速度
- 3 个单词仅需 10-15 秒（优化前需要 60 秒）
- 智能补充机制，确保数量充足

### 3. 中文输入优化
- 完美支持中文输入
- 删除时自动处理宽字符（中文字符占 2 个位置）
- 流畅的输入体验

### 4. 精美的学习报告
所有报告采用统一的现代化设计风格，包括渐变背景、卡片式布局和响应式设计。

- **词汇学习报告**包含：
  - 📊 平均分数统计
  - 📈 高分/中等/低分/Pass 分布
  - 📝 详细翻译解析（包括 Pass 单词的正确答案）
  - 🎨 现代化的卡片式设计
  
- **文章学习报告**包含：
  - 📰 完整英文原文
  - 🇨🇳 完整中文翻译
  - 📚 重点词汇表（含音标、释义、例句）
  - 🎨 统一的现代化样式

- **每日复习报告**包含：
  - 📚 今日学习统计
  - 🔄 原始例句和新的复习例句
  - 📝 你的翻译、修正翻译和得分
  - 🎨 与答题报告相同的现代化样式

## 📊 报告文件

程序会在项目目录下生成以下类型的报告文件：

- **词汇学习报告**: `IELTS_Report_YYYYMMDD_HHMMSS.html`
  - 包含答题详情、统计数据和翻译解析
  - Pass 的单词会显示正确答案，便于复习
  
- **文章学习报告**: `IELTS_Article_YYYYMMDD_HHMMSS.html`
  - 包含完整文章、翻译和重点词汇表
  
- **每日复习报告**: `IELTS_Daily_Report_YYYYMMDD_HHMMSS.html`
  - 从学习记录中读取指定日期的单词
  - 包含新的复习例句和学习统计

所有报告文件包含时间戳，方便区分不同时间生成的内容。可以直接在浏览器中打开查看，支持打印和分享。

### 学习记录文件

- **使用记录**: `usage_record.json`
  - 自动保存已使用的词汇和例句
  - 按日期分类保存学习记录（包含单词、得分、翻译等）
  - 用于生成每日复习报告和避免重复出题

## 🛠️ 技术栈

- **.NET 9.0** - 跨平台应用程序框架
- **Google Gemini API** - AI 文本生成服务
- **Microsoft.Extensions.Configuration** - 配置管理
- **HTML/CSS** - 精美的报告样式

## 📝 开发说明

### 构建项目

```bash
dotnet build
```

### 发布项目

```bash
# 发布为单文件可执行程序
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

# 发布后，可执行文件位于：
# bin/Release/net9.0/osx-arm64/publish/IELTS-Learning-Tool
```

### 项目结构

```
IELTS-Learning-Tool/
├── Models/                          # 数据模型
│   ├── Article.cs                   # 文章模型
│   ├── VocabularyWord.cs            # 词汇模型
│   └── UsageRecord.cs               # 使用记录模型
│
├── Services/                         # 服务层
│   ├── GeminiService.cs             # Gemini API 服务
│   ├── ReportGenerator.cs           # HTML 报告生成服务（答题报告、文章报告）
│   ├── DailyReportGenerator.cs      # 每日复习报告生成服务
│   └── UsageTrackerService.cs       # 使用记录跟踪服务
│
├── Configuration/                    # 配置相关
│   ├── AppConfig.cs                 # 配置模型
│   └── ConfigLoader.cs              # 配置加载器
│
├── Utils/                            # 工具类
│   ├── ArgumentParser.cs            # 命令行参数解析
│   ├── ArticleGenerationProgress.cs # 进度跟踪
│   ├── EnumerableHelper.cs          # 枚举辅助方法
│   ├── HelpDisplay.cs              # 帮助信息显示
│   ├── HtmlHelper.cs                # HTML 工具
│   └── ProgressDisplay.cs           # 进度显示
│
├── Program.cs                        # 主程序入口
├── config.json                       # 配置文件
└── IELTS-Learning-Tool.csproj       # 项目文件
```

## 💡 使用技巧

1. **Pass 功能使用** - 遇到不会的单词时输入 `Pass`，系统会标记为不会（得分 0），但仍会显示正确答案供复习
2. **生成复习报告** - 使用 `--daily-report` 参数可以随时生成指定日期的复习报告，帮助巩固记忆
3. **重置使用记录** - 如果想重新学习已学过的词汇，删除 `usage_record.json` 文件即可
4. **调整出题数量** - 在 `config.json` 中修改 `WordCount`，建议 3-50 个
5. **自定义主题** - 在 `config.json` 的 `Topics` 数组中添加或修改主题
6. **查看报告** - 生成的 HTML 报告可以直接在浏览器中打开，支持打印和分享
7. **定期复习** - 建议每天学习后，第二天使用 `--daily-report` 生成复习报告，巩固记忆

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证。

---

**在终端中学习英语，专注、高效、无干扰。开始你的雅思学习之旅吧！** 🎉
