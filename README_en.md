# Webview2ForHacknet
Supports embedded browser in Hacknet, allowing HTML content to be rendered anywhere. Also supports interaction with HTML pages!

[简体中文](README.md)



## Prerequisites

You need to install [Pathfinder](https://github.com/Arkhist/Hacknet-Pathfinder) before using this mod

Since it uses Webview2, Linux or older versions of Windows are not supported! If your device cannot display web pages, please update your Edge browser.

[Microsoft Edge WebView2 | Microsoft Edge Developer](https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH#download)



## Usage

Extract the Release package and copy all files (except the `Doc` folder) to: `Extension root directory/Plugins`.



## Daemon

Added `WebServerDaemon` to replace the original `addWebServer`

Usage:

```xml
<WebServerDaemon Name="Test Page" Url="Web/input.html" />
```

Url: Relative path of the HTML file from the extension root directory



## Email Content Supports HTML

```xml
<mission id="introMission1" activeCheck="true" shouldIgnoreSenderVerification="false">

    <email>
        <sender>Sender222</sender>
        <subject>Subject22222</subject>
        <body></body>
        <!--If html tag exists, email content will use html first, otherwise use body
			Url: Relative path of the HTML file from the extension root directory
		-->
        <html Url="Web/Img.html" />
    </email>
</mission>
```



## Hacknet and HTML Interaction

### 1. File Path Lookup Rules

```html
<html>
<head>
    <meta charset="UTF-8">
    <base href='http://hacknet.asset/Web/'>
</head>
<body>
    <img src="Img/test.png" alt="" />
</body> 
</html>
```

In the example above, `<base href='http://hacknet.asset/Web/'>` indicates that the base path for static resource lookup for this webpage is the **Web directory under the extension root directory**, where `hacknet.asset` is actually mapped to the current extension's root directory. Note that the **`/`** at the end of the href path cannot be omitted!!!

In the img tag above, the actual image path is: `Extension root directory/Web/Img/test.png`



### 2. Execute Action in JavaScript

```html
<script>
    document.addEventListener('DOMContentLoaded', function() {
        ExecuteAction(`
            <ChangeActiveFontGroup Name="desc" />
        `);
    });
</script>
```

You can directly call the ExecuteAction method in JavaScript with the action's XML content as a parameter to execute Hacknet actions in JavaScript.



## Using in Other Mods (Code Reference)

You can reference this mod's `0Webview2ForHacknet.dll` file in your mod to call internal methods and render pages anywhere.

**Here is a simple example**

```c#
var webview = new HacknetWebview();
webview.NavigateWithHtmlContent(File.ReadAllText("xxx.html"));

// In the draw method
webview.Draw(spriteBatch, rect);
```

By default, a loading animation will play before the webpage finishes loading. You can pass a third parameter when calling webview.Draw to prevent the loading animation from playing, and you can determine for yourself to perform other drawing operations before the webview is initialized.



### C# and JavaScript Interoperability



```c#
// C#
var webview = new HacknetWebview();
webview.NavigateWithHtmlContent(File.ReadAllText("xxx.html"));

webview.PostMessageToWeb("Message content sent to JS");

// Listen for messages sent from JS
hacknetWebview.OnReceiveWebMessageHandler += msg =>
{
    Console.WriteLine("Received message from JS: " + msg);

    // Return true to indicate you have consumed this message, otherwise the message will be executed as an action internally.
    return true;
};
```



```js
// JS

function OnReceiveMsg(event) {
    // Send message to C#
    PostMessage(`Received message:[${event.data}], processing result returned to you`);
}

// Listen for messages from C#
AddCSharpMessageListener(OnReceiveMsg);
// Remove listener
// RemoveCSharpMessageListener(OnReceiveMsg);
```



## Editor Tips

For your user experience, I recommend using Visual Studio Code editor as it supports syntax highlighting and IntelliSense for XML files.

You can install the following plugins in Visual Studio Code to get a better editing experience:

- XML Tools: Provides syntax highlighting and IntelliSense for XML files
- [HacknetExtensionHelper](https://marketplace.visualstudio.com/items?itemName=fengxu30338.hacknetextensionhelper): Provides IntelliSense related to Hacknet extensions

If your installed HacknetExtensionHelper plugin version is greater than or equal to `0.3.3`, you can reference this mod's [hint file](.EditorHints/Webview2ForHacknet.xml) through the `Include` tag in the `Hacknet-EditorHint.xml` file in the extension root directory

```xml
<!-- Hacknet-EditorHint.xml in extension root directory -->
<HacknetEditorHint>
    <Include path=".EditorHints/Webview2ForHacknet.xml" />
</HacknetEditorHint>
```



## About

If you use this mod, please indicate the source in your mod description.
