# SiliconFlow (硅基流动) API

## 平台信息

- 官网: https://siliconflow.cn
- API 文档: https://docs.siliconflow.cn
- API Key 获取: https://cloud.siliconflow.cn/account/ak
- API 基地址: `https://api.siliconflow.cn/v1`
- 协议: OpenAI-compatible

## 聊天补全

```
POST https://api.siliconflow.cn/v1/chat/completions
```

### 请求头

| Header | Value |
|--------|-------|
| Authorization | Bearer YOUR_API_KEY |
| Content-Type | application/json |

### 请求体

```json
{
  "model": "模型ID",
  "messages": [{"role": "user", "content": "..."}],
  "stream": true,
  "max_tokens": 4096,
  "temperature": 0.7,
  "top_p": 0.7,
  "frequency_penalty": 0.5,
  "tools": [{"type": "function", "function": {...}}]
}
```

### 流式响应 (SSE)

```
data: {"choices":[{"delta":{"content":"...","reasoning_content":"..."}}]}
data: [DONE]
```

### 特殊参数

| 参数 | 类型 | 说明 |
|------|------|------|
| enable_thinking | boolean | 开启思考模式（部分模型支持） |
| thinking_budget | integer | 思考 token 上限 (128-32768) |
| reasoning_effort | string | high/max (仅 DeepSeek-V4-Flash) |

### 工具调用

支持 OpenAI function-calling 格式，最多 128 个 tools。

tools 参数结构:
```json
{
  "type": "function",
  "function": {
    "name": "name",
    "description": "description",
    "parameters": { /* JSON Schema */ }
  }
}
```

响应中的 tool_calls:
```json
{
  "tool_calls": [{
    "id": "string",
    "type": "function",
    "function": {"name": "...", "arguments": "..."}
  }]
}
```

### 错误码

| 状态码 | 说明 |
|--------|------|
| 400 | 请求错误 |
| 401 | 未授权 (API Key 错误) |
| 403 | 禁止访问 |
| 429 | 频率限制 |
| 503 | 模型过载 |
| 504 | 超时 |

## 流式模式注意事项

1. Python requests 库需要同时设置 payload 中的 `stream: true` 和 `requests.post(stream=True)`
2. curl 需要 `-N` (--no-buffer) 参数
3. 流式响应以 `data: [DONE]` 结束
4. delta 中包含 `reasoning_content` 字段（模型相关）
