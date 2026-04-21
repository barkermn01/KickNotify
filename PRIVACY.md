# Privacy Policy – KickNotify Browser Extension

**Last updated:** April 2026

## Why this document exists

This privacy policy exists because the Chrome Web Store requires one for extensions that declare permissions, even when those extensions do not collect, process, store, transmit, or share personal data.

From a legal standpoint, this document is unnecessary. Under GDPR, UK GDPR, CCPA, and equivalent data protection frameworks, a privacy notice is only required when personal data is processed. This extension does not process personal data.

This document therefore exists solely to satisfy a platform requirement, not because there is anything meaningful to disclose.

## Summary

- No personal data is collected
- No personal data is transmitted off device
- No personal data is shared with third parties
- No analytics, tracking, telemetry, or advertising exists
- No cloud services are used
- All functionality is local and user initiated

## Data collection

The KickNotify browser extension does not collect any personal data, usage data, identifiers, or other information that could be used to identify a user.

There is no data collection now and none planned in the future.

## What is stored locally

The only data stored by the extension is a locally generated integration key. This key is:

- Stored using the browser’s extension scoped storage
- Not shared with websites
- Not synced externally
- Not readable by other extensions
- Not transmitted to any remote service

This key is used solely to allow the extension to communicate with a locally running desktop application on the same machine.

## Communication

The extension communicates with a companion desktop application via a WebSocket connection to 127.0.0.1 (localhost) only.

This communication:

- Never leaves the user’s device
- Does not traverse any external network
- Does not involve any third party systems
- Is limited to non personal operational data

The source code for both the browser extension and the desktop application is publicly available in the same GitHub repository as this privacy policy.

## Permissions explanation

- **storage**  
  Used only for extension scoped local storage of the integration key.

- **activeTab**  
  Used only after explicit user interaction and only on sites already authorised in the manifest.

- **scripting**  
  Used only when the user clicks an import button, to read publicly visible channel names from kick.com pages.

- **host permissions (kick.com)**  
  Required to allow the above user initiated functionality to operate on kick.com.

None of these permissions are used to collect personal data.

## Third party services

This extension does not use analytics providers, advertising networks, tracking services, or any other third party services.

## Data selling or sharing

No data is sold, shared, transferred, or disclosed because no user data is collected in the first place.

## Contact

For questions, concerns, or curiosity, please open an issue at  
https://github.com/barkermn01/KickNotify
