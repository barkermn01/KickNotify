const keySection = document.getElementById("keySection");
const mainSection = document.getElementById("mainSection");
const statusEl = document.getElementById("status");
const mainStatusEl = document.getElementById("mainStatus");
const channelEl = document.getElementById("channelInfo");
const btnAdd = document.getElementById("btnAdd");
const btnImport = document.getElementById("btnImport");
const btnDisconnect = document.getElementById("btnDisconnect");
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

function loadCurrentChannel() {
  chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
    if (!tabs[0]) return;
    activeTabId = tabs[0].id;

    try {
      const url = new URL(tabs[0].url);
      if (!url.hostname.includes("kick.com")) {
        onKick = false;
        channelEl.textContent = "Not on Kick";
        btnAdd.disabled = true;
        return;
      }
      onKick = true;

      // Extract slug from URL directly
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
      channelEl.textContent = "Not on Kick";
      btnAdd.disabled = true;
    }
  });
}

// On popup open, check if we have a key and can connect
chrome.runtime.sendMessage({ type: "get_key" }, (res) => {
  if (!res || !res.key) {
    showSection(keySection);
    return;
  }

  tryConnect((ok, reason) => {
    if (ok) {
      showSection(mainSection);
      loadCurrentChannel();
    } else if (reason === "invalid_key" || reason === "no_key") {
      showSection(keySection);
      statusEl.textContent = "Invalid key - please re-enter your integration key";
    } else {
      showSection(keySection);
      statusEl.textContent = "KickNotify not running";
    }
  });
});

// Save key and test connection
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
        showSection(mainSection);
        loadCurrentChannel();
      } else if (reason === "invalid_key") {
        showResult(keyResultEl, false, "Invalid key - check the key in KickNotify and try again");
      } else {
        showResult(keyResultEl, false, "Cannot connect - is KickNotify running?");
      }
    });
  });
});

// Disconnect
btnDisconnect.addEventListener("click", () => {
  chrome.storage.local.remove("kicknotify_key", () => {
    showSection(keySection);
    keyInput.value = "";
    statusEl.textContent = "Please enter your KickNotify integration key";
    statusEl.className = "status disconnected";
    clearResult(keyResultEl);
  });
});

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

// Import followed channels using chrome.scripting.executeScript
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
        const debug = {};
        const channels = [];

        const sidebar = document.querySelector("#sidebar-wrapper");
        debug.hasSidebar = !!sidebar;
        if (!sidebar) {
          console.log("[KickNotify Debug]", debug);
          return { channels, debug };
        }

        debug.sidebarChildCount = sidebar.children.length;
        debug.sidebarHTML = sidebar.innerHTML.substring(0, 500);

        // Find the "Following" section header (skip the nav link)
        let followingSection = null;
        const textNodes = [];
        const walker = document.createTreeWalker(
          sidebar,
          NodeFilter.SHOW_TEXT,
          null
        );
        while (walker.nextNode()) {
          const txt = walker.currentNode.textContent.trim();
          if (txt.length > 0 && txt.length < 50) {
            textNodes.push(txt);
          }
          if (txt === "Following") {
            const parent = walker.currentNode.parentElement;
            // Skip if inside a nav <a> tag (Home/Browse/Following links)
            if (parent && parent.closest("a")) continue;
            debug.followingParentTag = parent ? parent.tagName : null;
            debug.followingParentClasses = parent ? parent.className : null;
            followingSection = parent ? parent.closest("section") : null;
            if (!followingSection && parent) {
              followingSection = parent.parentElement;
            }
            break;
          }
        }
        debug.textNodesFound = textNodes;
        debug.hasFollowingSection = !!followingSection;

        if (!followingSection) {
          console.log("[KickNotify Debug]", debug);
          return { channels, debug };
        }

        debug.followingSectionTag = followingSection.tagName;
        debug.followingSectionChildCount = followingSection.children.length;

        const links = followingSection.querySelectorAll('a[href^="/"]');
        debug.linkCount = links.length;
        const linkDetails = [];
        for (const link of links) {
          const href = link.getAttribute("href");
          linkDetails.push(href);
          if (!href) continue;
          const slug = href.replace(/^\/+/, "").split("/")[0].toLowerCase();
          if (slug && slug.length > 1) {
            channels.push(slug);
          }
        }
        debug.linkHrefs = linkDetails;

        console.log("[KickNotify Debug]", debug);
        return { channels, debug };
      },
    },
    (results) => {
      console.log("[KickNotify] executeScript raw results:", results);
      console.log("[KickNotify] lastError:", chrome.runtime.lastError);

      const data = results && results[0] && results[0].result;
      const channels = data && data.channels;
      const debug = data && data.debug;

      console.log("[KickNotify] channels:", channels);
      console.log("[KickNotify] debug:", debug);

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
