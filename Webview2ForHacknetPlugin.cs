using BepInEx;
using BepInEx.Hacknet;
using Microsoft.Web.WebView2.Core;
using Pathfinder.Daemon;
using Pathfinder.Meta.Load;
using System;
using System.IO;
using System.Reflection;
using Webview2ForHacknet.Game.Daemon;
using Webview2ForHacknet.Game.Patch;
using Webview2ForHacknet.Util;

namespace Webview2ForHacknet
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    [IgnorePlugin]
    internal class Webview2ForHacknetPlugin : HacknetPlugin
    {
        public const string ModGUID = "com.fengxu.webview2forhacknet";
        public const string ModName = "Webview2ForHacknet";
        public const string ModVer = "1.0.0";
        public static Webview2ForHacknetPlugin Instance { get; private set; }
        public override bool Load()
        {
            Instance = this;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            HarmonyInstance.PatchAll(Instance.GetType().Assembly);
            MailServerHtmlSupport.Init();
            DaemonManager.RegisterDaemon<WebServerDaemon>();
            return true;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name == "Microsoft.Web.WebView2.Core")
            {
                var dllPath = Path.Combine(CommonUtils.ExtensionPluginsPath(), "WebView2", "Microsoft.Web.WebView2.Core.dll");
                return Assembly.Load(File.ReadAllBytes(dllPath));
            }

            return null;
        }
    }
}
