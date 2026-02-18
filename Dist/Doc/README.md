# Webview2ForHacknet
在Hacknet中支持内嵌浏览器，可以将html画面渲染到任何地方。并且支持与html页面交互！

[English](README_en.md)



## 前置要求

您需要安装[Pathfinder](https://github.com/Arkhist/Hacknet-Pathfinder)后才可使用本模组

由于使用的是Webview2，所以不支持linux或较低版本的windows!，如果您的设备无法播放网页，那么请您自行更新edge浏览器。

[Microsoft Edge WebView2 | Microsoft Edge Developer](https://developer.microsoft.com/zh-CN/microsoft-edge/webview2/?form=MA13LH#download)



## 用法

解压Release包，将其中所有文件(除了`Doc`文件夹)复制到: `扩展根目录/Plugins`下即可。



## Daemon

新增了`WebServerDaemon`替代原有的`addWebServer`

用法如下：

```xml
<WebServerDaemon Name="测试网页" Url="Web/input.html" />
```

Url： html文件位于扩展根目录的相对路径



## 邮件内容支持Html

```xml
<mission id="introMission1" activeCheck="true" shouldIgnoreSenderVerification="false">

    <email>
        <sender>发送者222</sender>
        <subject>主题22222</subject>
        <body></body>
        <!--只要存在html标签则邮件内容优先使用html否则使用body
			Url: html文件位于扩展根目录的相对路径
		-->
        <html Url="Web/Img.html" />
    </email>
</mission>
```



## Hacknet与Html交互

### 1.文件路径查找规则

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

在上述例子中，`<base href='http://hacknet.asset/Web/'>`表示该网页的静态资源查找的基础路径为**扩展根目录下的Web目录**，其中`hacknet.asset`实际会被映射到当前扩展的根目录，注意href路径末尾的**`/`**不可少！！！

在以上img标签中，实际的图片路径为：`扩展根目录/Web/Img/test.png`



### 2.js中执行Action

```html
<script>
    document.addEventListener('DOMContentLoaded', function() {
        ExecuteAction(`
            <ChangeActiveFontGroup Name="desc" />
        `);
    });
</script>
```

你可以直接在js中调用ExecuteAction方法，参数为action的xml内容，即可在js中执行hacknet的action。



## 在其他Mod中使用(代码引用)

您可以在您的mod中引用本mod的`0Webview2ForHacknet.dll`文件，即可调用内部方法将页面渲染到任何地方。

**以下是一个简单的例子**

```c#
var webview = new HacknetWebview();
webview.NavigateWithHtmlContent(File.ReadAllText("xxx.html"));

// 在draw方法中
webview.Draw(spriteBatch, rect);
```

默认在网页加载完成前会播放加载中的动画，您可以在调用webview.Draw时传入第三个参数阻止加载动画的播放，并且您可以自行判断在webview初始化完成前，执行一些其他的绘制操作。



### C#与JS的互操作



```c#
// C#
var webview = new HacknetWebview();
webview.NavigateWithHtmlContent(File.ReadAllText("xxx.html"));

webview.PostMessageToWeb("发送给js的消息内容");

// 监听js发送的消息
hacknetWebview.OnReceiveWebMessageHandler += msg =>
{
    Console.WriteLine("收到从js发送的消息: " + msg);

    // 返回true则表示您已经消化了该条信息，否则消息会在内部被当做action执行。
    return true;
};
```



```js
// JS

function OnReceiveMsg(event) {
    // 发送消息给c#
    PostMessage(`收到了消息:[${event.data}]，处理结果返回给你`);
}

// 监听来自c#的消息
AddCSharpMessageListener(OnReceiveMsg);
// 取消监听
// RemoveCSharpMessageListener(OnReceiveMsg);
```



## 编辑器提示

为了您的使用体验，我建议您使用Visual Studio Code编辑器，因为它支持XML文件的语法高亮和智能提示。

您可以在Visual Studio Code中安装以下插件来获得更好的翻译体验：

- XML Tools: 提供XML文件的语法高亮和智能提示
- [HacknetExtensionHelper](https://marketplace.visualstudio.com/items?itemName=fengxu30338.hacknetextensionhelper): 提供Hacknet扩展相关的智能提示

如果您安装的HacknetExtensionHelper插件版本大于等于`0.3.3`，您可以在扩展根目录的`Hacknet-EditorHint.xml`文件中通过`Include`标签引用本Mod的[提示文件](.EditorHints/Webview2ForHacknet.xml)

```xml
<!-- 扩展根目录下的Hacknet-EditorHint.xml-->
<HacknetEditorHint>
    <Include path=".EditorHints/Webview2ForHacknet.xml" />
</HacknetEditorHint>
```



## 关于

若您使用了本模组请在模组说明处注明来源。
