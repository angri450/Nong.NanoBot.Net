# NanoBot WebUI 体验修复计划

日期: 2026-06-13
状态: in-progress

## P0: 致命缺失（聊天核心体验）

| # | 问题 | 根因 | 修复 |
|---|------|------|------|
| 1 | 工具调用不显示 | AgentStreamEvent 有 ToolName/ToolCallId 字段但从未 emit | 在 SendMessageStreamingAsync 中加 tool_start/tool_end 事件 |
| 2 | thinking 内容不显示 | 前端已支持 reasoning 事件，但确认流是否发到位 | 验证后端 emit 路径，加调试 |
| 3 | 无法终止任务 | streaming 未传 CancellationToken 给 Agent | 前端加停止按钮，后端 respect token |
| 4 | 状态JSON按钮没用 | 打开 raw JSON in new tab | 改成在当前页弹 panel |

## P1: 侧边栏修复（交互可用性）

| # | 问题 | 根因 | 修复 |
|---|------|------|------|
| 5 | 文件/工具/记忆侧栏点了没用 | 只写了视图切换的 CSS 显隐，未实现功能 | 简化为3个真正有用的tab：对话/状态/设置 |
| 6 | Agent/写作切换无意义 | 写作模式没实现 | 删掉写作tab，留Agent唯一模式 |
| 7 | 会话无法删除/存档 | 无 API 端点 | 加 delete session 端点，前端加叉号按钮 |
| 8 | 工作区文件打不开 | 缺少清晰的 workspace 路径展示 + 文件点击无预览 | 显示完整 workspace 路径，点击文件在 inspector 预览 |
| 9 | 运行时文字溢出 | dl 无截断样式 | 加 text-overflow:ellipsis + title 属性 |

## P2: 核心功能补齐

| # | 问题 | 修复 |
|---|------|------|
| 10 | 无nongmark渲染 | 简单方案：检测 ```nongmark 代码块，加 CSS 高亮 |
| 11 | 重载按钮旁边那一堆pill | 简化顶栏，只留 model + nong on/off |
| 12 | 工具详情看不懂 | 改成显示 tool name + args + result 摘要 |
| 13 | 记忆预览测不出来 | 加一个"获取记忆"按钮，调 memory tool |

## P3: 细节打磨

| # | 问题 | 修复 |
|---|------|------|
| 14 | Enter 发送 / Shift+Enter 换行 | 已有 textarea 默认行为，加 hint |
| 15 | 新会话后消息列表不清空 | 切 session 时清 messages |
| 16 | 模型切换联动provider | 已做 |

## 本次优先修：P0 全部 + P1 关键项

1. 加 tool_start/tool_end 流式事件 → 前端渲染工具调用卡片
2. 加停止按钮 + CancellationToken 传递
3. 简化侧边栏 → 对话/状态/设置 + 删除多余tab
4. 修复文件、运行时溢出
5. 状态JSON改成内联panel
