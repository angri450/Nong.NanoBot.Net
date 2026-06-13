# NanoBot 配置指南

## 文件结构

```
~/.nanobot/
  models.json    — 模型定义（名称/上下文窗口/能力/输出上限）
  secrets.json   — API 密钥（不要提交 Git）
  config.json    — 运行时设置
```

## 1. 配置 SiliconFlow

### 获取 API Key

1. 注册 https://cloud.siliconflow.cn
2. 获取 API Key: https://cloud.siliconflow.cn/account/ak

### 编辑 secrets.json

```json
{
  "siliconflow": {
    "apiKey": "sk-你的密钥"
  }
}
```

### 编辑 models.json 选择模型

```json
{
  "providers": {
    "siliconflow": {
      "name": "硅基流动 SiliconFlow",
      "apiBase": "https://api.siliconflow.cn/v1/",
      "defaultModel": "deepseek-ai/DeepSeek-V3.2",
      "models": [
        {
          "id": "deepseek-ai/DeepSeek-V3.2",
          "displayName": "DeepSeek V3.2",
          "supportsStreaming": true,
          "supportsTools": true,
          "supportsReasoning": true
        }
      ]
    }
  }
}
```

### 编辑 config.json 指定使用哪个模型

需要填 config.json`

```json
{
  "agents": {
    "defaults": {
      "model": "siliconflow::deepseek-ai/DeepSeek-V3.2"
    }
  }
}
```

然后启动 NanoBot。

## 硅基流动可用模型速览

| 用途 | 推荐模型 |
|------|---------|
| 日常推理 | `deepseek-ai/DeepSeek-V3.2` |
| 高级推理 | `Pro/deepseek-ai/DeepSeek-V3.2` |
| 代码/长文 | `Qwen/Qwen3.5-397B-A17B` |
| 轻量快速 | `Qwen/Qwen3.5-35B-A3B` |
| 视觉理解 | `zai-org/GLM-4.6V` |
| OCR | `deepseek-ai/DeepSeek-OCR` |
