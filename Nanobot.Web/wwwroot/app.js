const i18n = {
  zh: {
    product: "NanoBot.net",
    workbench: "Agent 工作台",
    railChat: "聊",
    railFiles: "文",
    railTools: "工",
    railMemory: "忆",
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
    fileIcon: "文本"
  },
  en: {
    product: "NanoBot.net",
    workbench: "Agent Workbench",
    railChat: "C",
    railFiles: "F",
    railTools: "T",
    railMemory: "M",
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
    fileIcon: "TXT"
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
  isRunning: false
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
  runtimeNotice: document.getElementById("runtimeNotice"),
  sessionTitle: document.getElementById("sessionTitle"),
  prompt: document.getElementById("prompt"),
  composer: document.getElementById("composer"),
  sendButton: document.getElementById("sendButton"),
  newSession: document.getElementById("newSession"),
  reloadStatus: document.getElementById("reloadStatus"),
  refreshFiles: document.getElementById("refreshFiles"),
  themeToggle: document.getElementById("themeToggle"),
  languageToggle: document.getElementById("languageToggle")
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
  const ready = status.ready ?? status.Ready ?? false;
  const warning = status.warning || status.Warning || "";
  const error = status.error || status.Error || "";
  state.runtimeReady = ready;
  elements.runtimeStatus.textContent = ready ? t("ready") : t("needsConfig");
  elements.runtimeStatus.className = ready ? "status-ready" : "status-error";
  elements.runtimeModel.textContent = status.model || status.Model || "Unknown";
  elements.runtimeWorkspace.textContent = status.workspace || status.Workspace || "Unknown";
  elements.runtimeNong.textContent = (status.nongEnabled ?? status.NongEnabled) ? t("enabled") : t("disabled");
  elements.memoryPreview.textContent = status.memoryPreview || status.MemoryPreview || t("noMemory");

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

function updateSendState() {
  elements.sendButton.disabled = !state.runtimeReady || state.isRunning;
  elements.sendButton.textContent = state.isRunning ? t("running") : t("send");
}

async function streamMessage(message, assistantNode) {
  const response = await fetch("/api/agent/stream", {
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

  if (type === "delta") {
    appendMessage(assistantNode, event.content || event.Content || "");
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
  updateSendState();
  addMessage("user", message);
  const assistantNode = addMessage("assistant", "");

  try {
    await streamMessage(message, assistantNode);
    await loadSessions();
    await loadStatus();
  } catch (error) {
    assistantNode.classList.add("error");
    assistantNode.textContent = `${t("requestError")}: ${error.message}`;
  } finally {
    state.isRunning = false;
    updateSendState();
    elements.prompt.focus();
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

elements.refreshFiles.addEventListener("click", () => {
  loadFiles().catch(error => {
    elements.fileList.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
  });
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
    loadSessions().catch(error => addMessage("system", `${t("requestError")}: ${error.message}`)),
    loadFiles("").catch(error => {
      elements.fileList.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
    })
  ]);
  connectEvents();
}

boot();
