const state = {
  sessionId: localStorage.getItem("nanobot.sessionId") || `web-${Date.now()}`,
  sessions: JSON.parse(localStorage.getItem("nanobot.sessions") || "[]"),
  runtimeReady: false
};

const elements = {
  sessions: document.getElementById("sessions"),
  messages: document.getElementById("messages"),
  events: document.getElementById("events"),
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
  reloadStatus: document.getElementById("reloadStatus")
};

function ensureSession() {
  if (!state.sessions.some(session => session.id === state.sessionId)) {
    state.sessions.unshift({
      id: state.sessionId,
      title: "GroundPA Session",
      createdAt: new Date().toISOString()
    });
  }
  persistSessions();
}

function persistSessions() {
  localStorage.setItem("nanobot.sessionId", state.sessionId);
  localStorage.setItem("nanobot.sessions", JSON.stringify(state.sessions.slice(0, 12)));
}

function renderSessions() {
  elements.sessions.innerHTML = "";
  state.sessions.forEach(session => {
    const button = document.createElement("button");
    button.className = `session-item${session.id === state.sessionId ? " active" : ""}`;
    button.textContent = session.title;
    button.addEventListener("click", () => {
      state.sessionId = session.id;
      elements.sessionTitle.textContent = session.title;
      persistSessions();
      renderSessions();
      addMessage("system", `Switched to ${session.title}`);
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
}

function addEventItem(event) {
  const node = document.createElement("div");
  node.className = "event-item";
  const type = event.Type || event.type || "Runtime";
  const tool = event.ToolName || event.toolName || "";
  const error = event.ErrorMessage || event.errorMessage || "";
  node.innerHTML = `<strong>${escapeHtml(type)}</strong>${tool ? ` · ${escapeHtml(tool)}` : ""}${error ? `<br>${escapeHtml(error)}` : ""}`;
  elements.events.prepend(node);
  while (elements.events.children.length > 80) {
    elements.events.removeChild(elements.events.lastChild);
  }
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;");
}

async function loadStatus() {
  const response = await fetch("/api/runtime/status");
  if (!response.ok) {
    throw new Error(`Status failed: ${response.status}`);
  }
  const status = await response.json();
  const ready = status.ready ?? status.Ready ?? false;
  const warning = status.warning || status.Warning || "";
  const error = status.error || status.Error || "";
  state.runtimeReady = ready;
  elements.runtimeStatus.textContent = ready ? "Ready" : "Needs configuration";
  elements.runtimeStatus.className = ready ? "status-ready" : "status-error";
  elements.runtimeModel.textContent = status.model || status.Model || "Unknown";
  elements.runtimeWorkspace.textContent = status.workspace || status.Workspace || "Unknown";
  elements.runtimeNong.textContent = (status.nongEnabled ?? status.NongEnabled) ? "Enabled" : "Disabled";
  elements.memoryPreview.textContent = status.memoryPreview || status.MemoryPreview || "No memory loaded.";
  elements.sendButton.disabled = !state.runtimeReady;

  if (error || warning) {
    elements.runtimeNotice.hidden = false;
    elements.runtimeNotice.textContent = error || warning;
    elements.runtimeNotice.className = `runtime-notice ${error ? "error" : "warning"}`;
  } else {
    elements.runtimeNotice.hidden = true;
    elements.runtimeNotice.textContent = "";
    elements.runtimeNotice.className = "runtime-notice";
  }
}

async function parseError(response) {
  try {
    const payload = await response.json();
    return payload.error || payload.Error || `HTTP ${response.status}`;
  } catch {
    return `HTTP ${response.status}`;
  }
}

async function sendMessage(message) {
  const response = await fetch("/api/agent/message", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      sessionId: state.sessionId,
      message
    })
  });
  if (!response.ok) {
    throw new Error(await parseError(response));
  }
  return await response.json();
}

elements.composer.addEventListener("submit", async event => {
  event.preventDefault();
  const message = elements.prompt.value.trim();
  if (!message) return;

  elements.prompt.value = "";
  elements.sendButton.disabled = true;
  addMessage("user", message);
  addMessage("system", "Running...");

  try {
    const response = await sendMessage(message);
    const answer = response.answer || response.Answer || "";
    addMessage("agent", answer || "No response.");
    await loadStatus();
  } catch (error) {
    addMessage("agent", `Error: ${error.message}`);
  } finally {
    elements.sendButton.disabled = !state.runtimeReady;
    elements.prompt.focus();
  }
});

elements.newSession.addEventListener("click", () => {
  state.sessionId = `web-${Date.now()}`;
  const session = {
    id: state.sessionId,
    title: `Session ${new Date().toLocaleTimeString()}`,
    createdAt: new Date().toISOString()
  };
  state.sessions.unshift(session);
  elements.sessionTitle.textContent = session.title;
  persistSessions();
  renderSessions();
  addMessage("system", "New session started.");
});

elements.reloadStatus.addEventListener("click", async () => {
  try {
    await loadStatus();
    addMessage("system", "Runtime status refreshed.");
  } catch (error) {
    addMessage("system", `Status error: ${error.message}`);
  }
});

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
    addEventItem({ type: "EventStream", errorMessage: "Disconnected. Browser will retry." });
  };
}

ensureSession();
renderSessions();
addMessage("system", "NanoBot WebUI is ready.");
loadStatus().catch(error => {
  state.runtimeReady = false;
  elements.sendButton.disabled = true;
  addMessage("system", `Status error: ${error.message}`);
});
connectEvents();
