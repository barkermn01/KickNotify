const keySection = document.getElementById("keySection");
const mainSection = document.getElementById("mainSection");
const listSection = document.getElementById("listSection");
const statusEl = document.getElementById("status");
const mainStatusEl = document.getElementById("mainStatus");
const channelEl = document.getElementById("channelInfo");
const channelListEl = document.getElementById("channelList");
const btnAdd = document.getElementById("btnAdd");
const btnImport = document.getElementById("btnImport");
const btnDisconnect = document.getElementById("btnDisconnect");
const btnDisconnect2 = document.getElementById("btnDisconnect2");
const btnSaveKey = document.getElementById("btnSaveKey");
const keyInput = document.getElementById("keyInput");
const resultEl = document.getElementById("result");
const keyResultEl = document.getElementById("keyResult");

let currentSlug = null;
let onKick = false;
let activeTabId = null;

function showSection(section) {
  keySection.classList.remove("active");
  mainSection.classList.remove("active");
  listSection.classList.remove("active");
  section.classList.add("active");
}

function showResult(el, success, message) {
  el.textContent = message;
  el.className = "result " + (success ? "success" : "error");
}

function clearResult(el) {
  el.className = "result";
  el.textContent = "";
}

function tryConnect(callback) {
  chrome.runtime.sendMessage(
    { type: "list_streamers", payload: { action: "list_streamers" } },
    (res) => {
      if (chrome.runtime.lastError || !res) {
        callback(false, "not_running");
      } else if (res.error === "no_key" || res.error === "invalid_key") {
        callback(false, res.error);
      } else if (res.success) {
        callback(true);
      } else {
        callback(false, res.error || "unknown");
      }
    }
  );
}

function disconnect() {
  chrome.storage.local.remove("kicknotify_key", () => {
    showSection(keySection);
    keyInput.value = "";
    statusEl.textContent = "Please enter your KickNotify integration key";
    statusEl.className = "status disconnected";
    clearResult(keyResultEl);
  });
}

function loadChannelList() {
  chrome.runtime.sendMessage(
    { type: "list_streamers_status", payload: { action: "list_streamers_status" } },
    (res) => {
      if (chrome.runtime.lastError || !res || !res.success) {
        channelListEl.innerHTML = '<div class="no-channels">Could not load channels</div>';
        return;
      }

      const channels = res.channels;
      if (!channels || channels.length === 0) {
        channelListEl.innerHTML = '<div class="no-channels">No channels added yet</div>';
        return;
      }

      channelListEl.innerHTML = "";
      for (const ch of channels) {
        const a = document.createElement("a");
        a.className = "channel-item";
        a.href = "https://kick.com/" + ch.name;
        a.target = "_blank";
        a.innerHTML =
          '<span class="live-dot ' + (ch.is_live ? "online" : "offline") + '"></span>' +
          '<span class="name">' + ch.name + '</span>' +
          (ch.is_live ? '<span class="live-tag">LIVE</span>' : '');
        channelListEl.appendChild(a);
      }
    }
  );
}

function showConnected() {
  chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
    if (!tabs[0]) {
      showSection(listSection);
      loadChannelList();
      return;
    }
    activeTabId = tabs[0].id;

    try {
      const url = new URL(tabs[0].url);
      if (!url.hostname.includes("kick.com")) {
        onKick = false;
        showSection(listSection);
        loadChannelList();
        return;
      }
      onKick = true;
      showSection(mainSection);

      const match = url.pathname.match(/^\/([a-zA-Z0-9_-]+)\/?$/);
      if (match) {
        currentSlug = match[1];
        channelEl.textContent = "Channel: " + currentSlug;
        btnAdd.disabled = false;
      } else {
        channelEl.textContent = "No channel detected on this page";
        btnAdd.disabled = true;
      }
    } catch {
      onKick = false;
      showSection(listSection);
      loadChannelList();
    }
  });
}

// On popup open
chrome.runtime.sendMessage({ type: "get_key" }, (res) => {
  if (!res || !res.key) {
    showSection(keySection);
    return;
  }

  tryConnect((ok, reason) => {
    if (ok) {
      showConnected();
    } else if (reason === "invalid_key" || reason === "no_key") {
      showSection(keySection);
      statusEl.textContent = "Invalid key - please re-enter your integration key";
    } else {
      showSection(keySection);
      statusEl.textContent = "KickNotify not running";
    }
  });
});

// Save key
btnSaveKey.addEventListener("click", () => {
  const key = keyInput.value.trim();
  if (!key) {
    showResult(keyResultEl, false, "Please enter a key");
    return;
  }

  btnSaveKey.disabled = true;
  btnSaveKey.textContent = "Connecting...";
  clearResult(keyResultEl);

  chrome.runtime.sendMessage({ type: "save_key", key }, () => {
    tryConnect((ok, reason) => {
      btnSaveKey.disabled = false;
      btnSaveKey.textContent = "Connect";

      if (ok) {
        showConnected();
      } else if (reason === "invalid_key") {
        showResult(keyResultEl, false, "Invalid key - check the key in KickNotify and try again");
      } else {
        showResult(keyResultEl, false, "Cannot connect - is KickNotify running?");
      }
    });
  });
});

btnDisconnect.addEventListener("click", disconnect);
btnDisconnect2.addEventListener("click", disconnect);

// Add current channel
btnAdd.addEventListener("click", () => {
  if (!onKick) {
    showResult(resultEl, false, "Can't be used - not on Kick");
    return;
  }
  if (!currentSlug) {
    showResult(resultEl, false, "No channel detected on this page");
    return;
  }

  btnAdd.disabled = true;
  btnAdd.textContent = "Adding...";
  clearResult(resultEl);

  chrome.runtime.sendMessage(
    { type: "add_streamer", payload: { action: "add_streamer", streamer: currentSlug } },
    (res) => {
      btnAdd.disabled = false;
      btnAdd.textContent = "Add Current Channel";
      if (chrome.runtime.lastError) {
        showResult(resultEl, false, "Failed to connect");
        return;
      }
      if (res.error === "invalid_key" || res.error === "no_key") {
        showSection(keySection);
        statusEl.textContent = "Key rejected - please re-enter your integration key";
        return;
      }
      showResult(resultEl, res.success, res.success ? res.message : res.error);
    }
  );
});

// Import followed channels
btnImport.addEventListener("click", () => {
  if (!onKick) {
    showResult(resultEl, false, "Can't be used - not on Kick");
    return;
  }
  if (!activeTabId) {
    showResult(resultEl, false, "No active tab");
    return;
  }

  btnImport.disabled = true;
  btnImport.textContent = "Importing...";
  clearResult(resultEl);

  chrome.scripting.executeScript(
    {
      target: { tabId: activeTabId },
      func: () => {
        const channels = [];
        const sidebar = document.querySelector("#sidebar-wrapper");
        if (!sidebar) return channels;

        let followingSection = null;
        const walker = document.createTreeWalker(sidebar, NodeFilter.SHOW_TEXT, null);
        while (walker.nextNode()) {
          if (walker.currentNode.textContent.trim() === "Following") {
            const parent = walker.currentNode.parentElement;
            if (parent && parent.closest("a")) continue;
            followingSection = parent ? parent.closest("section") : null;
            if (!followingSection && parent) followingSection = parent.parentElement;
            break;
          }
        }

        if (!followingSection) return channels;

        const links = followingSection.querySelectorAll('a[href^="/"]');
        for (const link of links) {
          const href = link.getAttribute("href");
          if (!href) continue;
          const slug = href.replace(/^\/+/, "").split("/")[0].toLowerCase();
          if (slug && slug.length > 1) channels.push(slug);
        }
        return channels;
      },
    },
    (results) => {
      const channels = results && results[0] && results[0].result;

      if (!channels || channels.length === 0) {
        btnImport.disabled = false;
        btnImport.textContent = "Import Followed Channels";
        showResult(resultEl, false, "No followed channels found in sidebar. Make sure the Following section is visible.");
        return;
      }

      chrome.runtime.sendMessage(
        { type: "import_streamers", payload: { action: "import_streamers", streamers: channels } },
        (wsRes) => {
          btnImport.disabled = false;
          btnImport.textContent = "Import Followed Channels";
          if (chrome.runtime.lastError) {
            showResult(resultEl, false, "Failed to connect to KickNotify");
            return;
          }
          if (wsRes.error === "invalid_key" || wsRes.error === "no_key") {
            showSection(keySection);
            statusEl.textContent = "Key rejected - please re-enter your integration key";
            return;
          }
          showResult(resultEl, wsRes.success, wsRes.success ? wsRes.message : wsRes.error);
        }
      );
    }
  );
});
