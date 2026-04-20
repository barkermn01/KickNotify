const WS_URL = "ws://127.0.0.1:32585/";

function getStoredKey() {
  return new Promise((resolve) => {
    chrome.storage.local.get(["kicknotify_key"], (result) => {
      resolve(result.kicknotify_key || "");
    });
  });
}

function connectAndSend(payload) {
  return new Promise(async (resolve, reject) => {
    const key = await getStoredKey();
    if (!key) {
      reject(new Error("no_key"));
      return;
    }

    const ws = new WebSocket(WS_URL + "?key=" + encodeURIComponent(key));
    const timeout = setTimeout(() => {
      ws.close();
      reject(new Error("Connection timeout - is KickNotify running?"));
    }, 5000);

    ws.onopen = () => {
      ws.send(JSON.stringify(payload));
    };

    ws.onmessage = (event) => {
      clearTimeout(timeout);
      try {
        resolve(JSON.parse(event.data));
      } catch {
        resolve({ success: false, error: "Invalid response" });
      }
      ws.close();
    };

    ws.onerror = () => {
      clearTimeout(timeout);
      reject(new Error("Cannot connect to KickNotify - is it running?"));
    };

    ws.onclose = (event) => {
      clearTimeout(timeout);
      // HTTP 401 means the server rejected the upgrade due to invalid key
      if (event.code === 1006 && !event.wasClean) {
        reject(new Error("invalid_key"));
      }
    };
  });
}

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.type === "save_key") {
    chrome.storage.local.set({ kicknotify_key: msg.key }, () => {
      sendResponse({ success: true });
    });
    return true;
  }

  if (msg.type === "get_key") {
    getStoredKey().then((key) => sendResponse({ key }));
    return true;
  }

  if (msg.type === "add_streamer" || msg.type === "import_streamers" || msg.type === "list_streamers" || msg.type === "list_streamers_status") {
    connectAndSend(msg.payload)
      .then((res) => sendResponse(res))
      .catch((err) => sendResponse({ success: false, error: err.message }));
    return true;
  }
});
