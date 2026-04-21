# Privacy Policy — KickNotify Browser Extension

**Last updated:** April 2026

> **Note:** This privacy policy exists solely because Google requires one for Chrome Web Store listings that use any permissions. Since this extension collects no personal data whatsoever, this document is legally unnecessary under GDPR, CCPA, and any other applicable data protection legislation. No processing of personal data occurs, therefore no privacy notice is required by law. You are reading this because Google mandates it, not because there is anything to disclose.

## Data Collection

KickNotify Browser Extension does not collect, store, or transmit any personal data.

## What is stored

The only data stored locally by the extension is an integration key used to authenticate with the KickNotify desktop application running on your machine. This key is stored in the browser's local storage and never leaves your device.

## Communication

All communication occurs over a local WebSocket connection to `localhost` (127.0.0.1). No data is sent to any external servers, third parties, or cloud services.

## Permissions

- **storage** — Used solely to store the local integration key.
- **activeTab** — Used to detect if the current tab is on kick.com and read the channel name from the URL.
- **scripting** — Used to read followed channel names from the kick.com sidebar when the user explicitly clicks the import button.
- **host_permissions (kick.com)** — Required for the above functionality to operate on kick.com pages.

## Third-party services

This extension does not use any analytics, tracking, advertising, or third-party services.

## Contact

For questions about this privacy policy, open an issue at https://github.com/barkermn01/KickNotify
