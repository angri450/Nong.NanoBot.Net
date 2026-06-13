const i18n = {
  zh: {
    product: "Nong.NanoBot.Net",
    workbench: "Agent 工作台",
    railChat: "对话",
    railFiles: "文件",
    railTools: "工具",
    railMemory: "记忆",
    railSettings: "设置",
    sessions: "会话",
    newSession: "新建",
    workspaceFiles: "工作区文件",
    refresh: "刷新",
    runtime: "运行时",
    light: "浅色",
    dark: "深色",
    status: "状态",
    model: "模型",
    workspace: "工作区",
    localRuntime: "本地 Agent 运行时",
    reload: "重载",
    statusJson: "状态 JSON",
    send: "发送",
    toolTimeline: "工具时间线",
    toolDetail: "工具详情",
    type: "类型",
    tool: "工具",
    run: "运行",
    session: "会话",
    filePreview: "文件预览",
    memoryPreview: "记忆预览",
    ready: "就绪",
    needsConfig: "需要配置",
    enabled: "已启用",
    disabled: "已禁用",
    noMemory: "暂无记忆。",
    noEvents: "暂无工具调用。",
    noToolSelected: "选择一个工具事件查看详情。",
    noFileSelected: "选择左侧文件查看内容。",
    messagesUnit: "条消息",
    loading: "加载中...",
    running: "运行中...",
    streamReady: "NanoBot WebUI 已就绪。",
    switched: "已切换到",
    newStarted: "已创建新会话。",
    statusRefreshed: "运行时状态已刷新。",
    statusError: "状态错误",
    requestError: "请求错误",
    root: "根目录",
    parent: "上一级",
    directory: "目录",
    file: "文件",
    binaryBlocked: "无法预览二进制文件。",
    truncated: "内容已截断。",
    prompt: "输入下一步要 NanoBot 做的事情...",
    unnamedSession: "NanoBot 会话",
    errorPrefix: "错误",
    disconnected: "事件流已断开，浏览器会自动重连。",
    directoryIcon: "目录",
    fileIcon: "文本",
    modeAgent: "Agent",
    modeWrite: "写作",
    localFiles: "本地",
    composerHint: "输入 / 可组织任务",
    plan: "计划",
    todos: "待办",
    noActivePlan: "暂无活动计划",
    planHint: "发送任务后，工具调用和会话进展会在这里沉淀。",
    todoRuntime: "确认运行时就绪",
    todoFiles: "选择工作区上下文",
    todoSend: "发送下一步任务",
    modelSettings: "模型设置",
    provider: "Provider",
    apiBase: "API 地址",
    modelId: "模型 ID",
    apiKey: "调用密钥",
    saveSettings: "保存并重载",
    clearKey: "清空 Key",
    keyConfigured: "Key 已配置",
    keyMissing: "Key 未配置",
    keySourceConfig: "当前 Key 来自本机配置。",
    keySourceEnvironment: "当前 Key 来自环境变量 DMX_API_KEY，表单不会覆盖它的运行时优先级。",
    keySourceNone: "请输入中转站调用密钥，保存后写入本机配置。",
    keepExistingKey: "留空表示保留现有 Key。",
    settingsSaved: "模型配置已保存。",
    settingsError: "设置保存失败",
    thinkingMode: "思考模式",
    thinkingAuto: "自动",
    thinkingHigh: "高",
    thinkingMax: "最大",
    thinkingOff: "关闭",
    usageCache: "用量与缓存",
    cacheHitRate: "缓存命中率",
    cachedTokens: "命中 Token",
    uncachedTokens: "未命中 Token",
    outputTokens: "输出 Token",
    reasoningTokens: "思考 Token",
    totalTokens: "总计 Token",
    cacheHit: "命中",
    cacheMiss: "未命中",
    reasoning: "思考过程",
    gitCodeAccount: "GitCode 账号",
    gitCodeNotLoggedIn: "未登录",
    gitCodeLoggedIn: "已登录",
    gitCodeLogin: "登录 GitCode",
    gitCodeLogout: "退出登录",
    gitCodeSync: "同步免费模型",
    gitCodeSyncing: "同步中...",
    gitCodeOpenBrowser: "请在新打开的浏览器窗口中完成授权",
    gitCodeTokenExpiring: "Token 即将过期，请重新登录",
    gitCodeTokenValid: "Token 有效",
    gitCodeCatalogOnly: "仅同步模型目录，暂不能直连免费网关",
    gitCodeModelsSynced: "个模型已同步",
    gitCodeSetupSuccess: "CodingPlan 设置完成",
    gitCodeSetupFailed: "设置失败",
    gitCodeClaimPlanClaimed: "计划已领取"
  },
  en: {
    product: "Nong.NanoBot.Net",
    workbench: "Agent Workbench",
    railChat: "Chat",
    railFiles: "Files",
    railTools: "Tools",
    railMemory: "Memory",
    railSettings: "Settings",
    sessions: "Sessions",
    newSession: "New",
    workspaceFiles: "Workspace Files",
    refresh: "Refresh",
    runtime: "Runtime",
    light: "Light",
    dark: "Dark",
    status: "Status",
    model: "Model",
    workspace: "Workspace",
    localRuntime: "Local Agent Runtime",
    reload: "Reload",
    statusJson: "Status JSON",
    send: "Send",
    toolTimeline: "Tool Timeline",
    toolDetail: "Tool Detail",
    type: "Type",
    tool: "Tool",
    run: "Run",
    session: "Session",
    filePreview: "File Preview",
    memoryPreview: "Memory Preview",
    ready: "Ready",
    needsConfig: "Needs configuration",
    enabled: "Enabled",
    disabled: "Disabled",
    noMemory: "No memory loaded.",
    noEvents: "No tool calls yet.",
    noToolSelected: "Select a tool event to inspect details.",
    noFileSelected: "Select a file from the left to preview it.",
    messagesUnit: "msgs",
    loading: "Loading...",
    running: "Running...",
    streamReady: "NanoBot WebUI is ready.",
    switched: "Switched to",
    newStarted: "New session started.",
    statusRefreshed: "Runtime status refreshed.",
    statusError: "Status error",
    requestError: "Request error",
    root: "Root",
    parent: "Parent",
    directory: "Directory",
    file: "File",
    binaryBlocked: "Binary file preview is blocked.",
    truncated: "Content truncated.",
    prompt: "What should NanoBot do next?",
    unnamedSession: "NanoBot Session",
    errorPrefix: "Error",
    disconnected: "Event stream disconnected. Browser will retry.",
    directoryIcon: "DIR",
    fileIcon: "TXT",
    modeAgent: "Agent",
    modeWrite: "Write",
    localFiles: "Local",
    composerHint: "Type / to organize a task",
    plan: "Plan",
    todos: "Todos",
    noActivePlan: "No active plan",
    planHint: "Send a task and runtime progress will collect here.",
    todoRuntime: "Confirm runtime readiness",
    todoFiles: "Choose workspace context",
    todoSend: "Send the next task",
    modelSettings: "Model Settings",
    provider: "Provider",
    apiBase: "API Base",
    modelId: "Model ID",
    apiKey: "API Key",
    saveSettings: "Save and reload",
    clearKey: "Clear Key",
    keyConfigured: "Key configured",
    keyMissing: "Key missing",
    keySourceConfig: "Current key is loaded from local config.",
    keySourceEnvironment: "Current key is loaded from DMX_API_KEY. Form changes do not override environment priority.",
    keySourceNone: "Enter the relay API key; it will be saved to local config.",
    keepExistingKey: "Leave blank to keep the existing key.",
    settingsSaved: "Model settings saved.",
    settingsError: "Settings save failed",
    thinkingMode: "Thinking",
    thinkingAuto: "Auto",
    thinkingHigh: "High",
    thinkingMax: "Max",
    thinkingOff: "Off",
    usageCache: "Usage & Cache",
    cacheHitRate: "Cache Hit Rate",
    cachedTokens: "Cached Tokens",
    uncachedTokens: "Uncached Tokens",
    outputTokens: "Output Tokens",
    reasoningTokens: "Reasoning Tokens",
    totalTokens: "Total Tokens",
    cacheHit: "Hit",
    cacheMiss: "Miss",
    reasoning: "Reasoning",
    gitCodeAccount: "GitCode Account",
    gitCodeNotLoggedIn: "Not logged in",
    gitCodeLoggedIn: "Logged in",
    gitCodeLogin: "Login GitCode",
    gitCodeLogout: "Logout",
    gitCodeSync: "Sync Free Models",
    gitCodeSyncing: "Syncing...",
    gitCodeOpenBrowser: "Complete authorization in the opened browser window",
    gitCodeTokenExpiring: "Token expiring, please login again",
    gitCodeTokenValid: "Token valid",
    gitCodeCatalogOnly: "Models synced only, gateway not callable yet",
    gitCodeModelsSynced: "models synced",
    gitCodeSetupSuccess: "CodingPlan setup complete",
    gitCodeSetupFailed: "Setup failed",
    gitCodeClaimPlanClaimed: "Plan claimed"
  }
};

const state = {
  language: localStorage.getItem("nanobot.language") || "zh",
  theme: localStorage.getItem("nanobot.theme") || "dark",
  sessionId: localStorage.getItem("nanobot.sessionId") || "",
  sessions: [],
  runtimeReady: false,
  currentPath: "",
  toolEvents: [],
  selectedEventKey: "",
  isRunning: false,
  abortController: null,
  modelSettings: null
};

applyHashPreferences();

const elements = {
  sessions: document.getElementById("sessions"),
  messages: document.getElementById("messages"),
  events: document.getElementById("events"),
  toolDetail: document.getElementById("toolDetail"),
  fileList: document.getElementById("fileList"),
  filePath: document.getElementById("filePath"),
  filePreview: document.getElementById("filePreview"),
  filePreviewTitle: document.getElementById("filePreviewTitle"),
  memoryPreview: document.getElementById("memoryPreview"),
  runtimeStatus: document.getElementById("runtimeStatus"),
  runtimeModel: document.getElementById("runtimeModel"),
  runtimeWorkspace: document.getElementById("runtimeWorkspace"),
  runtimeNong: document.getElementById("runtimeNong"),
  runtimeReadyPill: document.getElementById("runtimeReadyPill"),
  runtimeModelPill: document.getElementById("runtimeModelPill"),
  runtimeCachePill: document.getElementById("runtimeCachePill"),
  runtimeUsagePill: document.getElementById("runtimeUsagePill"),
  runtimeNongPill: document.getElementById("runtimeNongPill"),
  runtimeNotice: document.getElementById("runtimeNotice"),
  modelSettingsForm: document.getElementById("modelSettingsForm"),
  providerId: document.getElementById("providerId"),
  apiBase: document.getElementById("apiBase"),
  modelId: document.getElementById("modelId"),
  modelSelect: document.getElementById("modelSelect"),
  apiKey: document.getElementById("apiKey"),
  apiKeyHint: document.getElementById("apiKeyHint"),
  keyStatus: document.getElementById("keyStatus"),
  clearApiKey: document.getElementById("clearApiKey"),
  settingsNotice: document.getElementById("settingsNotice"),
  thinkingMode: document.getElementById("thinkingMode"),
  sessionTitle: document.getElementById("sessionTitle"),
  sessionCount: document.getElementById("sessionCount"),
  prompt: document.getElementById("prompt"),
  composer: document.getElementById("composer"),
  composerModel: document.getElementById("composerModel"),
  sendButton: document.getElementById("sendButton"),
  stopButton: document.getElementById("stopButton"),
  newSession: document.getElementById("newSession"),
  themeToggle: document.getElementById("themeToggle"),
  languageToggle: document.getElementById("languageToggle"),
  cacheHitRate: document.getElementById("cacheHitRate"),
  cachedTokens: document.getElementById("cachedTokens"),
  uncachedTokens: document.getElementById("uncachedTokens"),
  outputTokens: document.getElementById("outputTokens"),
  reasoningTokens: document.getElementById("reasoningTokens"),
  totalTokens: document.getElementById("totalTokens"),
  gitCodeAuthStatus: document.getElementById("gitCodeAuthStatus"),
  gitCodeUserInfo: document.getElementById("gitCodeUserInfo"),
  gitCodeUserName: document.getElementById("gitCodeUserName"),
  gitCodeTokenStatus: document.getElementById("gitCodeTokenStatus"),
  gitCodeLoginBtn: document.getElementById("gitCodeLoginBtn"),
  gitCodeLogoutBtn: document.getElementById("gitCodeLogoutBtn"),
  gitCodeSyncBtn: document.getElementById("gitCodeSyncBtn"),
  gitCodeLoginStatus: document.getElementById("gitCodeLoginStatus"),
  gitCodeLoginHint: document.getElementById("gitCodeLoginHint"),
  gitCodeSyncActions: document.getElementById("gitCodeSyncActions"),
  gitCodeSetupResult: document.getElementById("gitCodeSetupResult"),
  gitCodeModelList: document.getElementById("gitCodeModelList")
};

function t(key) {
  return i18n[state.language][key] || i18n.zh[key] || key;
}

function applyLanguage() {
  document.documentElement.lang = state.language === "zh" ? "zh-CN" : "en";
  document.querySelectorAll("[data-i18n]").forEach(node => {
    node.textContent = t(node.dataset.i18n);
  });
  elements.languageToggle.textContent = state.language === "zh" ? "EN" : "中";
  elements.themeToggle.textContent = state.theme === "dark" ? t("light") : t("dark");
  elements.prompt.placeholder = t("prompt");
  renderSessions();
  renderEvents();
  renderToolDetail();
  renderModelSettings();
  updateSendState();
}

function applyTheme() {
  document.documentElement.dataset.theme = state.theme;
  elements.themeToggle.textContent = state.theme === "dark" ? t("light") : t("dark");
}

function applyHashPreferences() {
  const hash = window.location.hash.toLowerCase();
  if (hash.includes("en")) {
    state.language = "en";
  } else if (hash.includes("zh")) {
    state.language = "zh";
  }

  if (hash.includes("light")) {
    state.theme = "light";
  } else if (hash.includes("dark")) {
    state.theme = "dark";
  }
}

async function apiJson(url, options = {}) {
  const response = await fetch(url, options);
  if (!response.ok) {
    throw new Error(await parseError(response));
  }
  return await response.json();
}

async function parseError(response) {
  try {
    const payload = await response.json();
    return payload.error || payload.Error || `HTTP ${response.status}`;
  } catch {
    return `HTTP ${response.status}`;
  }
}

function persistSessionId() {
  localStorage.setItem("nanobot.sessionId", state.sessionId);
}

async function loadSessions() {
  state.sessions = await apiJson("/api/sessions");
  elements.sessionCount.textContent = String(state.sessions.length);
  if (!state.sessionId || !state.sessions.some(session => session.id === state.sessionId)) {
    state.sessionId = state.sessions[0]?.id || "";
    persistSessionId();
  }
  renderSessions();
  if (state.sessionId) {
    await loadSession(state.sessionId);
  }
}

async function loadSession(sessionId) {
  const session = await apiJson(`/api/sessions/${encodeURIComponent(sessionId)}`);
  state.sessionId = session.id;
  persistSessionId();
  elements.sessionTitle.textContent = session.title || t("unnamedSession");
  elements.messages.innerHTML = "";
  for (const message of session.messages || []) {
    addMessage(message.role, message.content);
  }
  if ((session.messages || []).length === 0) {
    addMessage("system", t("streamReady"));
  }
  renderSessions();
}

function renderSessions() {
  elements.sessions.innerHTML = "";
  state.sessions.forEach(session => {
    const button = document.createElement("button");
    button.className = `session-item${session.id === state.sessionId ? " active" : ""}`;
    button.innerHTML = `
      <span>${escapeHtml(session.title || t("unnamedSession"))}</span>
      <small>${session.messageCount || 0} ${t("messagesUnit")}</small>
    `;
    button.addEventListener("click", async () => {
      await loadSession(session.id);
      addMessage("system", `${t("switched")} ${session.title || t("unnamedSession")}`);
    });
    elements.sessions.appendChild(button);
  });
}

function addMessage(role, text) {
  const node = document.createElement("div");
  node.className = `message ${role}`;
  node.textContent = text;
  elements.messages.appendChild(node);
  elements.messages.scrollTop = elements.messages.scrollHeight;
  return node;
}

function appendMessage(node, text) {
  node.textContent += text;
  elements.messages.scrollTop = elements.messages.scrollHeight;
}

async function loadStatus() {
  const status = await apiJson("/api/runtime/status");
  applyStatus(status);
}

function applyStatus(status) {
  const ready = status.ready ?? status.Ready ?? false;
  const warning = status.warning || status.Warning || "";
  const error = status.error || status.Error || "";
  state.runtimeReady = ready;
  elements.runtimeStatus.textContent = ready ? t("ready") : t("needsConfig");
  elements.runtimeStatus.className = ready ? "status-ready" : "status-error";
  elements.runtimeModel.textContent = status.model || status.Model || "Unknown";
  elements.runtimeWorkspace.textContent = status.workspace || status.Workspace || "Unknown";
  elements.runtimeNong.textContent = (status.nongEnabled ?? status.NongEnabled) ? t("enabled") : t("disabled");
  elements.runtimeReadyPill.textContent = ready ? t("ready") : t("needsConfig");
  elements.runtimeReadyPill.className = `status-pill ${ready ? "ready" : "error"}`;
  elements.runtimeModelPill.textContent = status.model || status.Model || "Unknown";
  elements.runtimeNongPill.textContent = (status.nongEnabled ?? status.NongEnabled) ? "Nong on" : "Nong off";
  elements.composerModel.textContent = status.model || status.Model || "NanoBot";
  elements.memoryPreview.textContent = status.memoryPreview || status.MemoryPreview || t("noMemory");

  const cacheRate = status.cacheHitRate ?? status.CacheHitRate;
  if (cacheRate !== null && cacheRate !== undefined) {
    elements.runtimeCachePill.hidden = false;
    elements.runtimeCachePill.textContent = `${t("cacheHit")} ${(cacheRate * 100).toFixed(1)}%`;
    elements.runtimeCachePill.className = `status-pill ${cacheRate > 0.5 ? "ready" : "warn"}`;
  }

  const ctxTokens = status.contextTokens ?? status.ContextTokens;
  if (ctxTokens !== null && ctxTokens !== undefined) {
    elements.runtimeUsagePill.hidden = false;
    elements.runtimeUsagePill.textContent = `Token ${formatNumber(ctxTokens)}`;
  }

  const thinkingMode = status.thinkingMode ?? status.ThinkingMode;
  if (thinkingMode && elements.thinkingMode) {
    elements.thinkingMode.value = thinkingMode;
  }

  if (error || warning) {
    elements.runtimeNotice.hidden = false;
    elements.runtimeNotice.textContent = error || warning;
    elements.runtimeNotice.className = `runtime-notice ${error ? "error" : "warning"}`;
  } else {
    elements.runtimeNotice.hidden = true;
    elements.runtimeNotice.textContent = "";
    elements.runtimeNotice.className = "runtime-notice";
  }

  updateSendState();
}

async function loadModelSettings() {
  state.modelSettings = await apiJson("/api/settings/model");
  renderModelSettings();
}

function renderModelSettings() {
  if (!elements.modelSettingsForm || !state.modelSettings) {
    return;
  }

  const settings = state.modelSettings;
  elements.providerId.value = settings.providerId || settings.ProviderId || "siliconflow";
  elements.apiBase.value = settings.apiBase || settings.ApiBase || "https://api.siliconflow.cn/v1/";
  elements.modelId.value = settings.model || settings.Model || "nex-agi/Nex-N2-Pro";
  if (elements.modelSelect) {
    // Sync dropdown with model value
    var v = settings.model || settings.Model || "";
    var option = elements.modelSelect.querySelector('option[value="' + v + '"]');
    if (option) {
      elements.modelSelect.value = v;
    } else {
      // Custom model: add it as an option
      var opt = document.createElement('option');
      opt.value = v;
      opt.textContent = v;
      elements.modelSelect.appendChild(opt);
      elements.modelSelect.value = v;
    }
  }
  elements.apiKey.value = "";

  const thinkingMode = settings.thinkingMode || settings.ThinkingMode || "auto";
  if (elements.thinkingMode) {
    elements.thinkingMode.value = thinkingMode;
  }

  const hasKey = settings.hasApiKey ?? settings.HasApiKey ?? false;
  const keySource = settings.keySource || settings.KeySource || "none";
  const preview = settings.apiKeyPreview || settings.ApiKeyPreview || "";
  elements.keyStatus.textContent = hasKey ? t("keyConfigured") : t("keyMissing");
  elements.keyStatus.className = `section-count ${hasKey ? "status-ready" : "status-error"}`;

  let sourceText = t("keySourceNone");
  if (keySource === "config") {
    sourceText = `${t("keySourceConfig")} ${preview ? `(${preview})` : ""} ${t("keepExistingKey")}`;
  } else if (keySource === "environment") {
    sourceText = `${t("keySourceEnvironment")} ${preview ? `(${preview})` : ""}`;
  }
  elements.apiKeyHint.textContent = sourceText;
}

function showSettingsNotice(message, kind = "info") {
  elements.settingsNotice.hidden = false;
  elements.settingsNotice.textContent = message;
  elements.settingsNotice.className = `settings-notice ${kind}`;
}

function updateSendState() {
  elements.sendButton.disabled = !state.runtimeReady || state.isRunning;
  elements.sendButton.textContent = state.isRunning ? t("running") : t("send");
}

async function streamMessage(message, assistantNode, abortSignal) {
  const response = await fetch("/api/agent/stream", {
    signal: abortSignal,
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      sessionId: state.sessionId,
      message
    })
  });

  if (!response.ok || !response.body) {
    throw new Error(await parseError(response));
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { value, done } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split("\n");
    buffer = lines.pop() || "";
    for (const line of lines) {
      if (line.trim()) {
        handleStreamEvent(JSON.parse(line), assistantNode);
      }
    }
  }

  if (buffer.trim()) {
    handleStreamEvent(JSON.parse(buffer), assistantNode);
  }
}

function handleStreamEvent(event, assistantNode) {
  const type = event.type || event.Type;
  if (type === "session") {
    state.sessionId = event.sessionId || event.SessionId || state.sessionId;
    persistSessionId();
    return;
  }

  if (type === "reasoning") {
    const reasoning = event.reasoning || event.Reasoning || event.content || event.Content || "";
    let reasoningBlock = assistantNode.querySelector(".reasoning-block");
    if (!reasoningBlock) {
      reasoningBlock = document.createElement("details");
      reasoningBlock.className = "reasoning-block";
      reasoningBlock.innerHTML = `<summary>${t("reasoning")}</summary><div class="reasoning-content"></div>`;
      assistantNode.insertBefore(reasoningBlock, assistantNode.firstChild);
    }
    reasoningBlock.querySelector(".reasoning-content").textContent += reasoning;
    reasoningBlock.open = true;
    elements.messages.scrollTop = elements.messages.scrollHeight;
    return;
  }

  if (type === "delta") {
    appendMessage(assistantNode, event.content || event.Content || "");
    return;
  }

  if (type === "usage") {
    updateUsageDisplay({
      cacheHitRate: event.cacheHitRate ?? event.CacheHitRate,
      inputTokens: event.inputTokens ?? event.InputTokens,
      outputTokens: event.outputTokens ?? event.OutputTokens,
      cachedTokens: event.cachedTokens ?? event.CachedTokens
    });
    return;
  }

  if (type === "complete") {
    const answer = event.answer || event.Answer || assistantNode.textContent;
    assistantNode.textContent = answer || assistantNode.textContent;
    return;
  }

  if (type === "error") {
    assistantNode.classList.add("error");
    assistantNode.textContent = `${t("errorPrefix")}: ${event.error || event.Error || "Unknown"}`;
  }
}

async function loadFiles(path = state.currentPath) {
  const query = path ? `?path=${encodeURIComponent(path)}` : "";
  const result = await apiJson(`/api/workspace/files${query}`);
  state.currentPath = result.path || result.Path || "";
  renderFiles(result.entries || result.Entries || []);
}

function renderFiles(entries) {
  elements.filePath.textContent = state.currentPath || t("root");
  elements.fileList.innerHTML = "";

  if (state.currentPath) {
    const parentButton = document.createElement("button");
    parentButton.className = "file-item";
    parentButton.innerHTML = `<span class="file-icon">..</span><span>${t("parent")}</span>`;
    parentButton.addEventListener("click", () => loadFiles(parentPath(state.currentPath)));
    elements.fileList.appendChild(parentButton);
  }

  entries.forEach(entry => {
    const kind = entry.kind || entry.Kind;
    const path = entry.path || entry.Path || "";
    const name = entry.name || entry.Name || path;
    const button = document.createElement("button");
    button.className = "file-item";
    button.innerHTML = `
      <span class="file-icon">${kind === "directory" ? t("directoryIcon") : t("fileIcon")}</span>
      <span>${escapeHtml(name)}</span>
      <small>${kind === "directory" ? t("directory") : formatBytes(entry.size ?? entry.Size)}</small>
    `;
    button.addEventListener("click", async () => {
      if (kind === "directory") {
        await loadFiles(path);
      } else {
        await previewFile(path);
      }
    });
    elements.fileList.appendChild(button);
  });
}

async function previewFile(path) {
  try {
    const file = await apiJson(`/api/workspace/file?path=${encodeURIComponent(path)}`);
    elements.filePreviewTitle.textContent = file.path || file.Path || path;
    elements.filePreview.textContent = (file.content || file.Content || "") + ((file.truncated || file.Truncated) ? `\n\n... ${t("truncated")}` : "");
  } catch (error) {
    elements.filePreviewTitle.textContent = path;
    elements.filePreview.textContent = `${t("errorPrefix")}: ${error.message}`;
  }
}

function parentPath(path) {
  const parts = path.split("/").filter(Boolean);
  parts.pop();
  return parts.join("/");
}

function addEventItem(event) {
  const normalized = normalizeRuntimeEvent(event);
  state.toolEvents.unshift(normalized);
  if (state.toolEvents.length > 100) {
    state.toolEvents.pop();
  }
  state.selectedEventKey ||= normalized.key;
  renderEvents();
  renderToolDetail();

  // Also render tool events inline in chat
  if (normalized.type === "ToolStarted" || normalized.type === "ToolCompleted" || normalized.type === "ToolFailed") {
    const toolName = normalized.toolName || "Unknown";
    if (normalized.type === "ToolStarted") {
      addMessage("system", `[TOOL] ${toolName} ...`);
    } else if (normalized.type === "ToolFailed") {
      addMessage("system", `[TOOL] ${toolName} FAILED: ${normalized.error || ""}`);
    }
  }
}

function normalizeRuntimeEvent(event) {
  return {
    key: `${event.ToolCallId || event.toolCallId || ""}-${event.Type || event.type || "Runtime"}-${event.Timestamp || event.timestamp || Date.now()}`,
    type: event.Type || event.type || "Runtime",
    runId: event.RunId || event.runId || "",
    sessionId: event.SessionId || event.sessionId || "",
    timestamp: event.Timestamp || event.timestamp || new Date().toISOString(),
    toolName: event.ToolName || event.toolName || "",
    toolCallId: event.ToolCallId || event.toolCallId || "",
    content: event.Content || event.content || "",
    error: event.ErrorMessage || event.errorMessage || ""
  };
}

function renderEvents() {
  elements.events.innerHTML = "";
  if (state.toolEvents.length === 0) {
    elements.events.innerHTML = `<div class="empty-state">${t("noEvents")}</div>`;
    return;
  }

  state.toolEvents.forEach(event => {
    const button = document.createElement("button");
    button.className = `event-item${event.key === state.selectedEventKey ? " active" : ""}`;
    button.innerHTML = `
      <strong>${escapeHtml(event.type)}</strong>
      <span>${escapeHtml(event.toolName || "Runtime")}</span>
      ${event.error ? `<small class="danger">${escapeHtml(event.error)}</small>` : ""}
    `;
    button.addEventListener("click", () => {
      state.selectedEventKey = event.key;
      renderEvents();
      renderToolDetail();
    });
    elements.events.appendChild(button);
  });
}

function renderToolDetail() {
  const selected = state.toolEvents.find(event => event.key === state.selectedEventKey) || state.toolEvents[0];
  if (!selected) {
    elements.toolDetail.innerHTML = `<div class="empty-state">${t("noToolSelected")}</div>`;
    return;
  }

  elements.toolDetail.innerHTML = `
    <dl class="detail-list">
      <div><dt>${t("type")}</dt><dd>${escapeHtml(selected.type)}</dd></div>
      <div><dt>${t("tool")}</dt><dd>${escapeHtml(selected.toolName || "Runtime")}</dd></div>
      <div><dt>${t("run")}</dt><dd>${escapeHtml(selected.runId)}</dd></div>
      <div><dt>${t("session")}</dt><dd>${escapeHtml(selected.sessionId)}</dd></div>
    </dl>
    ${selected.error ? `<pre class="detail-pre error">${escapeHtml(selected.error)}</pre>` : ""}
    ${selected.content ? `<pre class="detail-pre">${escapeHtml(selected.content)}</pre>` : ""}
  `;
}

function connectEvents() {
  const source = new EventSource("/api/events");
  source.addEventListener("runtime", event => {
    try {
      addEventItem(JSON.parse(event.data));
    } catch {
      addEventItem({ type: "Runtime", errorMessage: event.data });
    }
  });
  source.onerror = () => {
    addEventItem({ type: "EventStream", errorMessage: t("disconnected") });
  };
}

function updateUsageDisplay(data) {
  if (data.cacheHitRate !== null && data.cacheHitRate !== undefined) {
    elements.cacheHitRate.textContent = (data.cacheHitRate * 100).toFixed(1) + "%";
  }
  if (data.cachedTokens !== null && data.cachedTokens !== undefined) {
    elements.cachedTokens.textContent = formatNumber(data.cachedTokens);
  }
  if (data.inputTokens !== null && data.inputTokens !== undefined) {
    const uncached = data.inputTokens - (data.cachedTokens || 0);
    elements.uncachedTokens.textContent = formatNumber(uncached);
  }
  if (data.outputTokens !== null && data.outputTokens !== undefined) {
    elements.outputTokens.textContent = formatNumber(data.outputTokens);
  }
  if (data.reasoningTokens !== null && data.reasoningTokens !== undefined) {
    elements.reasoningTokens.textContent = formatNumber(data.reasoningTokens);
  }
  if (data.totalTokens !== null && data.totalTokens !== undefined) {
    elements.totalTokens.textContent = formatNumber(data.totalTokens);
  }
}

function formatNumber(value) {
  if (value === null || value === undefined) return "--";
  if (value >= 1_000_000) return (value / 1_000_000).toFixed(1) + "M";
  if (value >= 1_000) return (value / 1_000).toFixed(1) + "K";
  return String(value);
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;");
}

function formatBytes(value) {
  if (value === null || value === undefined) return "";
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / 1024 / 1024).toFixed(1)} MB`;
}

elements.composer.addEventListener("submit", async event => {
  event.preventDefault();
  const message = elements.prompt.value.trim();
  if (!message || state.isRunning) return;

  elements.prompt.value = "";
  state.isRunning = true;
  state.abortController = new AbortController();
  elements.stopButton.hidden = false;
  updateSendState();
  addMessage("user", message);
  const assistantNode = addMessage("assistant", "");

  try {
    await streamMessage(message, assistantNode, state.abortController.signal);
    await loadSessions();
    await loadStatus();
  } catch (error) {
    if (error.name !== 'AbortError') {
      assistantNode.classList.add('error');
      assistantNode.textContent = `${t("requestError")}: ${error.message}`;
    } else {
      assistantNode.textContent += '\n[已停止]';
    }
  } finally {
    state.isRunning = false;
    state.abortController = null;
    elements.stopButton.hidden = true;
    updateSendState();
    elements.prompt.focus();
  }
});

elements.stopButton.addEventListener('click', () => {
  if (state.abortController) {
    state.abortController.abort();
  }
});
elements.newSession.addEventListener("click", async () => {
  const session = await apiJson("/api/sessions", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title: t("unnamedSession") })
  });
  state.sessionId = session.id;
  persistSessionId();
  await loadSessions();
  addMessage("system", t("newStarted"));
});

elements.reloadStatus.addEventListener("click", async () => {
  try {
    await loadStatus();
    addMessage("system", t("statusRefreshed"));
  } catch (error) {
    addMessage("system", `${t("statusError")}: ${error.message}`);
  }
});

// Model select dropdown → sync to hidden modelId input
if (elements.modelSelect) {
  elements.modelSelect.addEventListener("change", () => {
    elements.modelId.value = elements.modelSelect.value;
  });
}

// Provider switch → change API base
if (elements.providerId) {
  elements.providerId.addEventListener("change", () => {
    var v = elements.providerId.value;
    if (v === "siliconflow") {
      elements.apiBase.value = "https://api.siliconflow.cn/v1/";
      elements.modelId.value = "nex-agi/Nex-N2-Pro";
      if (elements.modelSelect) elements.modelSelect.value = "nex-agi/Nex-N2-Pro";
    } else if (v === "dmx") {
      elements.apiBase.value = "https://www.dmxapi.cn/v1/";
      elements.modelId.value = "deepseek-v4-pro-guan";
    }
  });
}

elements.refreshFiles.addEventListener("click", () => {
  loadFiles().catch(error => {
    elements.fileList.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
  });
});

elements.modelSettingsForm.addEventListener("submit", async event => {
  event.preventDefault();
  showSettingsNotice(t("running"), "info");
  try {
    const result = await apiJson("/api/settings/model", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        providerId: elements.providerId.value,
        apiKey: elements.apiKey.value,
        apiBase: elements.apiBase.value,
        model: elements.modelId.value,
        clearApiKey: false
      })
    });
    state.modelSettings = result.settings || result.Settings;
    const status = result.status || result.Status;
    if (status) {
      applyStatus(status);
    } else {
      await loadStatus();
    }
    renderModelSettings();
    showSettingsNotice(result.message || result.Message || t("settingsSaved"), "success");
    addMessage("system", result.message || result.Message || t("settingsSaved"));
  } catch (error) {
    showSettingsNotice(`${t("settingsError")}: ${error.message}`, "error");
  }
});

elements.clearApiKey.addEventListener("click", async () => {
  showSettingsNotice(t("running"), "info");
  try {
    const result = await apiJson("/api/settings/model", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        providerId: elements.providerId.value,
        apiKey: "",
        apiBase: elements.apiBase.value,
        model: elements.modelId.value,
        clearApiKey: true
      })
    });
    state.modelSettings = result.settings || result.Settings;
    const status = result.status || result.Status;
    if (status) {
      applyStatus(status);
    } else {
      await loadStatus();
    }
    renderModelSettings();
    showSettingsNotice(result.message || result.Message || t("settingsSaved"), "success");
  } catch (error) {
    showSettingsNotice(`${t("settingsError")}: ${error.message}`, "error");
  }
});

elements.languageToggle.addEventListener("click", () => {
  state.language = state.language === "zh" ? "en" : "zh";
  localStorage.setItem("nanobot.language", state.language);
  applyLanguage();
  loadStatus().catch(() => {});
});

elements.themeToggle.addEventListener("click", () => {
  state.theme = state.theme === "dark" ? "light" : "dark";
  localStorage.setItem("nanobot.theme", state.theme);
  applyTheme();
});

async function boot() {
  applyTheme();
  applyLanguage();
  elements.filePreview.textContent = t("noFileSelected");
  renderEvents();
  renderToolDetail();
  await Promise.all([
    loadStatus().catch(error => addMessage("system", `${t("statusError")}: ${error.message}`)),
    loadModelSettings().catch(error => showSettingsNotice(`${t("settingsError")}: ${error.message}`, "error")),
    loadSessions().catch(error => addMessage("system", `${t("requestError")}: ${error.message}`)),
    loadFiles("").catch(error => {
      elements.fileList.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
    }),
    loadGitCodeAuthStatus().catch(() => {})
  ]);
  connectEvents();
}

// GitCode auth functions
async function loadGitCodeAuthStatus() {
  const status = await apiJson("/api/gitcode/auth/status");
  renderGitCodeAuthStatus(status);
}

function renderGitCodeAuthStatus(status) {
  const loggedIn = status.loggedIn ?? false;
  elements.gitCodeAuthStatus.textContent = loggedIn ? t("gitCodeLoggedIn") : t("gitCodeNotLoggedIn");
  elements.gitCodeAuthStatus.className = `section-count ${loggedIn ? "status-ready" : ""}`;

  if (loggedIn) {
    elements.gitCodeLoginBtn.hidden = true;
    elements.gitCodeLogoutBtn.hidden = false;
    elements.gitCodeSyncActions.hidden = false;
    elements.gitCodeUserInfo.hidden = false;
    elements.gitCodeUserName.textContent = status.login || status.name || "User";

    const tokenValid = status.tokenValid ?? false;
    elements.gitCodeTokenStatus.textContent = tokenValid ? t("gitCodeTokenValid") : t("gitCodeTokenExpiring");
    elements.gitCodeTokenStatus.className = tokenValid ? "" : "danger";
  } else {
    elements.gitCodeLoginBtn.hidden = false;
    elements.gitCodeLogoutBtn.hidden = true;
    elements.gitCodeSyncActions.hidden = true;
    elements.gitCodeUserInfo.hidden = true;
  }
}

elements.gitCodeLoginBtn.addEventListener("click", async () => {
  try {
    elements.gitCodeLoginBtn.disabled = true;
    elements.gitCodeLoginBtn.textContent = t("running");
    const login = await apiJson("/api/gitcode/auth/login/start", { method: "POST" });
    elements.gitCodeLoginStatus.hidden = false;
    elements.gitCodeLoginHint.textContent = login.loginUrl || "";
    window.open(login.loginUrl, "_blank");
    state.gitCodeLoginId = login.loginId;
    await pollGitCodeLogin();
  } catch (error) {
    elements.gitCodeLoginHint.textContent = `${t("requestError")}: ${error.message}`;
  } finally {
    elements.gitCodeLoginBtn.disabled = false;
    elements.gitCodeLoginBtn.textContent = t("gitCodeLogin");
  }
});

async function pollGitCodeLogin() {
  const loginId = state.gitCodeLoginId;
  if (!loginId) return;

  let attempts = 0;
  const maxAttempts = 60;
  const poll = async () => {
    if (attempts >= maxAttempts) {
      elements.gitCodeLoginHint.textContent = "Login timed out.";
      elements.gitCodeLoginStatus.hidden = true;
      return;
    }
    attempts++;

    try {
      const result = await apiJson(`/api/gitcode/auth/login/${encodeURIComponent(loginId)}/poll`, { method: "POST" });
      if (result.status === "authorized") {
        elements.gitCodeLoginStatus.hidden = true;
        await loadGitCodeAuthStatus();
        addMessage("system", t("gitCodeLoggedIn"));
      } else if (result.status === "expired" || result.status === "failed") {
        elements.gitCodeLoginStatus.hidden = true;
        elements.gitCodeLoginHint.textContent = `Login ${result.status}`;
      } else {
        setTimeout(poll, 2000);
      }
    } catch (error) {
      elements.gitCodeLoginHint.textContent = `Poll error: ${error.message}`;
      setTimeout(poll, 2000);
    }
  };
  poll();
}

elements.gitCodeLogoutBtn.addEventListener("click", async () => {
  try {
    await apiJson("/api/gitcode/auth/logout", { method: "POST" });
    await loadGitCodeAuthStatus();
    elements.gitCodeModelList.innerHTML = "";
    elements.gitCodeSetupResult.hidden = true;
  } catch (error) {
    addMessage("system", `${t("requestError")}: ${error.message}`);
  }
});

elements.gitCodeSyncBtn.addEventListener("click", async () => {
  try {
    elements.gitCodeSyncBtn.disabled = true;
    elements.gitCodeSyncBtn.textContent = t("gitCodeSyncing");
    const result = await apiJson("/api/gitcode/codingplan/setup", { method: "POST" });
    elements.gitCodeSetupResult.hidden = false;
    elements.gitCodeSetupResult.innerHTML = result.success
      ? `<div class="settings-notice success">${t("gitCodeSetupSuccess")}: ${result.steps?.claim?.message || ""}, ${result.steps?.models?.message || ""}</div>`
      : `<div class="settings-notice error">${t("gitCodeSetupFailed")}: ${result.steps?.claim?.message || ""}</div>`;

    // Render synced models
    const models = result.models || [];
    renderGitCodeModels(models);
    await loadStatus();
    await loadGitCodeAuthStatus();
  } catch (error) {
    elements.gitCodeSetupResult.hidden = false;
    elements.gitCodeSetupResult.innerHTML = `<div class="settings-notice error">${t("gitCodeSetupFailed")}: ${error.message}</div>`;
  } finally {
    elements.gitCodeSyncBtn.disabled = false;
    elements.gitCodeSyncBtn.textContent = t("gitCodeSync");
  }
});

function renderGitCodeModels(models) {
  elements.gitCodeModelList.innerHTML = "";
  if (models.length === 0) return;

  const heading = document.createElement("div");
  heading.className = "section-title";
  heading.textContent = `${models.length} ${t("gitCodeModelsSynced")}`;
  elements.gitCodeModelList.appendChild(heading);

  models.forEach(model => {
    const row = document.createElement("div");
    row.className = `gitcode-model-item${!model.planAvailable ? " locked" : ""}`;
    row.innerHTML = `
      <span>${escapeHtml(model.displayModelName || model.DisplayModelName || "")}</span>
      <small>${(model.contextWindow || model.ContextWindow || 0) >= 1000000 ? "1M" : formatNumber(model.contextWindow || model.ContextWindow || 0)} ctx</small>
    `;
    if (!model.planAvailable) {
      row.innerHTML += `<small class="danger">locked</small>`;
    }
    elements.gitCodeModelList.appendChild(row);
  });

  const note = document.createElement("div");
  note.className = "settings-hint";
  note.textContent = t("gitCodeCatalogOnly");
  elements.gitCodeModelList.appendChild(note);
}

// ===== Status Panel =====

let statusViewActive = false;

async function refreshStatusPanel() {
  if (!statusViewActive) return;
  try {
    const status = await apiJson("/api/system/status");
    const nong = status.nong || status.Nong;
    const toolkit = status.toolkit || status.Toolkit;
    const rt = status.runtime || status.Runtime;

    document.getElementById("statusNanoState").textContent = (rt && (rt.ready || rt.Ready)) ? "就绪" : "未配置";
    document.getElementById("statusNanoModel").textContent = rt ? (rt.model || rt.Model || "-") : "-";
    document.getElementById("statusNanoWorkspace").textContent = rt ? (rt.workspace || rt.Workspace || "-") : "-";

    if (nong) {
      document.getElementById("statusNongState").textContent = "已安装";
      document.getElementById("statusNongState").className = "status-ready";
      document.getElementById("statusNongVersion").textContent = nong.version || nong.Version || "-";
      document.getElementById("statusNongCommands").textContent = nong.commandCount || nong.CommandCount || "-";

      // External tools
      var extTools = nong.externalTools || nong.ExternalTools;
      var extDiv = document.getElementById("statusExternalTools");
      if (extTools && extTools.length > 0) {
        var html = '<div class="ext-tools">';
        for (var i = 0; i < extTools.length; i++) {
          var et = extTools[i];
          var cls = (et.installed || et.Installed) ? "ext-tool-ok" : "ext-tool-missing";
          var label = (et.installed || et.Installed) ? "OK" : "MISS";
          html += '<span class="ext-tool ' + cls + '" title="' + (et.packageId || et.PackageId || "") + '">'
            + escapeHtml(et.name || et.Name || "") + ': ' + label + '</span> ';
        }
        html += '</div>';
        extDiv.innerHTML = html;
      } else {
        extDiv.innerHTML = "<em>—</em>";
      }

      // OCR models
      var ocr = nong.ocrModels || nong.OcrModels;
      var ocrDiv = document.getElementById("statusOcrModels");
      if (ocr) {
        var v6ok = ocr.v6Available || ocr.V6Available;
        var v6sz = ocr.v6Size || ocr.V6Size || "?";
        var v5ok = ocr.v5Available || ocr.V5Available;
        ocrDiv.innerHTML = "v6: <span class='" + (v6ok ? "status-ready" : "status-warn") + "'>"
          + (v6ok ? v6sz + " OK" : "NONE") + "</span> | v5: "
          + (v5ok ? "OK" : "NONE");
      } else {
        ocrDiv.innerHTML = "<em>—</em>";
      }
    } else {
      document.getElementById("statusNongState").textContent = "未安装";
      document.getElementById("statusNongState").className = "status-error";
      document.getElementById("statusNongVersion").textContent = "-";
      document.getElementById("statusNongCommands").textContent = "-";
    }

    if (toolkit) {
      document.getElementById("statusToolkitState").textContent = toolkit.installed || toolkit.Installed ? "已加载" : "未加载";
      document.getElementById("statusToolkitState").className = (toolkit.installed || toolkit.Installed) ? "status-ready" : "status-error";
      document.getElementById("statusToolkitSkills").textContent = toolkit.skillCount || toolkit.SkillCount || "-";
      const detail = document.getElementById("statusDetail");
      if (toolkit.skillNames || toolkit.SkillNames) {
        const names = toolkit.skillNames || toolkit.SkillNames;
        detail.innerHTML = "<h4>已加载的技能</h4><div class='skill-tags'>" +
          names.map(n => "<span class='skill-tag'>" + escapeHtml(n) + "</span>").join("") +
          "</div>";
      }
    } else {
      document.getElementById("statusToolkitState").textContent = "未加载";
      document.getElementById("statusToolkitState").className = "status-error";
      document.getElementById("statusToolkitSkills").textContent = "-";
      document.getElementById("statusDetail").innerHTML = "";
    }
  } catch (e) {
    document.getElementById("statusNongState").textContent = "检测失败";
    document.getElementById("statusNongState").className = "status-error";
  }
}

document.querySelectorAll(".rail-button[data-view='status']").forEach(btn => {
  btn.addEventListener("click", () => {
    document.querySelectorAll(".rail-button").forEach(b => b.classList.remove("active"));
    btn.classList.add("active");
    document.getElementById("messages").style.display = "none";
    document.getElementById("composer").style.display = "none";
    document.getElementById("statusPanel").style.display = "block";
    statusViewActive = true;
    refreshStatusPanel();
  });
});

// Switch back to chat view when other rail buttons are clicked
document.querySelectorAll(".rail-button[data-view='chat']").forEach(btn => {
  btn.addEventListener("click", () => {
    document.getElementById("messages").style.display = "";
    document.getElementById("composer").style.display = "";
    document.getElementById("statusPanel").style.display = "none";
    statusViewActive = false;
  });
});

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

boot();
