# 模型/密钥配置分拆 (2026-06-13)

## 问题

之前的 config.json 把所有东西混在一起：API key、模型参数、网关配置全在一个文件里。而且 API key 明文写在环境变量里，管理麻烦。

## 方案

拆成三个文件，像正经的 LLM 工具那样：

```
~/.nanobot/
  models.json    — 模型定义（名称/上下文窗口/能力标记/输出上限）
  secrets.json   — 密钥（每个 provider 的 apiKey）
  config.json    — 运行时设置（Agent/网关/工具白名单）
```

**models.json**：纯声明，不包含密钥。一个 provider 可以挂多个模型，每个模型有完整的参数配置（contextWindow、maxOutputTokens、reasoning 等）。

**secrets.json**：只存密钥。gitignore 保护，永不提交。

**config.json**：只存运行时设置。通过 `provider::model` 引用模型，密钥不在里面。

## 改动

ConfigLoader.cs:
- 新增三文件合并逻辑：models.json → ProviderModelDef → ModelSettings, secrets.json → ApiKey
- Provider 通过 models.json 的 providers 键定义，config.json 不再需要 providers 段
- 向后兼容：如果用户还保留旧的 config.json 格式，模型信息从 config.json 读

CLI Program.cs (onboard):
- 生成三个文件代替以前的一个大 JSON
- secrets.json 初始为空 API key，提示用户手动填

Web Program.cs:
- SaveModelSettings 写 models.json + secrets.json + config.json 三个文件
- LoadOrCreateJson(path) 替代 LoadConfigJson()

.gitignore:
- 新增 secrets.json

## 验证

- dotnet build: 0 errors
- dotnet test: 102 passed
