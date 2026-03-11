# Kick Notify
The Kick windows desktop notification system.

[![Paypal Donate Btn](https://user-images.githubusercontent.com/37368/221199123-871cff69-fdb5-4dc3-8f3c-35efff4ce670.png)](https://www.paypal.com/donate/?hosted_button_id=9YUH3GCJ83A4G)

## Not affiliated with Kick

## Installation
 Kick Notify Requires [.NET 6 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-6.0.13-windows-x64-installer).
 As of RC-1 There is now a Setup that will install into auto run aswell 

## How to use
**How to Manage Ignores:**
1. Click "Manage Ignores" from the right click notification area icon

![Image of Context Menu from Notification / Tray Icon](https://user-images.githubusercontent.com/37368/221084256-c9317bbf-ec39-48ee-8d02-325997d0d200.png)

2, the following form will open where you can manage streamers that should be ignored

![Image of Ignored Streamers UI](https://user-images.githubusercontent.com/37368/221084417-165afc63-1926-41f0-be36-e18e8c46318f.png)


_Something to note:_
Making changes inside the form is saved automatically
Streamers won't appear in the form until they have been seen 

**How to Exit:**
To exit you will have a Notification Icon / Tray icon where you can quit the application.

![Image of Context Menu from Notification / Tray Icon](https://user-images.githubusercontent.com/37368/221084256-c9317bbf-ec39-48ee-8d02-325997d0d200.png)


**What it looks like:**

![Example of notification ](https://user-images.githubusercontent.com/37368/221086733-3a379a9b-6630-4edd-a7e5-e43815e47609.png)

## Development

Want to contribute? Great!

Project is built using Visual Studios 2022, please make sure you have added support for .NET 6, You will also requrie the Windows SDK (10.0.17763.0)

If your unable to code sign you can build configuration to "Debug" or "Release" mode the signing script will only run if set to "ReleaseSign".

You need to create Application to obtain a ID and Secret on Kick Developer Console
Add a new C# Class to the project named `KickDetails.cs` add the following code with your ID and Secret
```cs
namespace KickDesktopNotifications
{
    static public class KickDetails
    {
        public static string KickClientID = "";
        public static string KickClientSecret = "";
    }
}
```

### CommunityToolkit 8.0.0 Pre-release
Project Requests `CommunityToolkit-MainLatest` NuGET Package Source

1. Tool > NuGET Package Manager > Package Manager Settings
2. Click on Package Source (just below the select General in the Left hand column
3. Click the + icon top right
5. 4. Enter the Name `CommunityToolkit-MainLatest` and Source `https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-MainLatest/nuget/v3/index.json`
6. Click Update
7. Click Ok

### Thanks
A Huge Thanks to [CaspersGG](https://kick.com/CaspersGG) for coming up with this idea.
Thanks to the following streamers for testing and promoting this application: 
1. [Uprisen](https://kick.com/uprisen)
2. [Anthogator](https://kick.com/anthogator)
3. [Strife98](https://kick.com/strife98)
4. [petfriendamy_](https://kick.com/petfriendamy_)
5. [Roxas997](https://kick.com/roxas997)
6. [Anthogator](https://kick.com/anthogator)

## License

MIT
**Free Software, Hell Yeah!**
