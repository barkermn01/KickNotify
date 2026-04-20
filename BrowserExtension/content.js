// Extracts the current channel slug from a Kick channel page URL
function getCurrentChannelSlug() {
  const match = window.location.pathname.match(/^\/([a-zA-Z0-9_-]+)\/?$/);
  return match ? match[1] : null;
}

// Scrapes followed channel names from the Kick sidebar
function scrapeFollowedChannels() {
  const channels = [];

  const sidebar = document.querySelector("#sidebar-wrapper");
  if (!sidebar) return channels;

  // Find the "Following" text in the sidebar
  let followingSection = null;
  const allElements = sidebar.querySelectorAll("*");
  for (const el of allElements) {
    if (
      el.children.length === 0 &&
      el.textContent.trim() === "Following"
    ) {
      // Walk up to the containing section
      followingSection = el.closest("section");
      break;
    }
  }

  if (!followingSection) return channels;

  // Find all <a> tags within the following section that link to channels
  const links = followingSection.querySelectorAll('a[href^="/"]');
  for (const link of links) {
    const href = link.getAttribute("href");
    if (!href) continue;

    const slug = href.replace(/^\/+/, "").split("/")[0].toLowerCase();
    if (slug && slug.length > 1) {
      channels.push(slug);
    }
  }

  return channels;
}

// Listen for messages from the popup
chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.type === "get_current_channel") {
    sendResponse({ slug: getCurrentChannelSlug() });
  } else if (msg.type === "scrape_followed") {
    sendResponse({ channels: scrapeFollowedChannels() });
  }
});
