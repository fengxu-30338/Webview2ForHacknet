using Hacknet;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Replacements;
using Pathfinder.Util;
using Pathfinder.Util.XML;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Webview2ForHacknet.Util;

namespace Webview2ForHacknet.Game.Patch
{
    [HarmonyPatch]
    internal class MailServerHtmlSupport
    {
        private static readonly Regex HtmlRegex = new Regex(@"<html(?:>|\s+)[\s\S]*</html>", RegexOptions.Compiled);
        private static readonly Dictionary<int, HacknetWebview> HacknetWebviews = new Dictionary<int, HacknetWebview>();
        public static void Init()
        {
            MissionLoader.RegisterExecutor<MissionEmailHtmlBodyExecutor>("mission.email.html", ParseOption.ParseInterior);
            HacknetWebviews.PostToMainThread(ClearWebview, ScheduleRule.LOOP);
        }

        private class MissionEmailHtmlBodyExecutor : MissionLoader.MissionExecutor
        {
            public override void Execute(EventExecutor exec, ElementInfo info)
            {
                if (!info.Attributes.ContainsKey("Url"))
                {
                    throw new InvalidDataException("Missing Url attribute");
                }
                var htmlFilepath = LocalizedFileLoader.GetLocalizedFilepath(info.Attributes.GetString("Url").WithExtensionSubPath());
                if (!File.Exists(htmlFilepath))
                {
                    throw new FileNotFoundException($"Load mission html file not found: {htmlFilepath}");
                }
                Mission.email.body = File.ReadAllText(htmlFilepath);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MailServer), nameof(MailServer.DrawMailMessageText))]
        public static bool PrefixDrawMailMessageText(MailServer __instance, Rectangle textBounds, SpriteBatch sb)
        {
            var html = __instance.emailData[3];
            if (HtmlRegex.IsMatch(html))
            {
                var rect = textBounds;
                rect.X -= 20;
                rect.Width += 17;
                if (!HacknetWebviews.TryGetValue(html.GetHashCode(), out var webview))
                {
                    webview = new HacknetWebview();
                    webview.NavigateWithHtmlContent(html);
                    HacknetWebviews[html.GetHashCode()] = webview;
                }
                webview.Draw(sb, rect);
                return false;
            }

            return true;
        }

        private static void ClearWebview()
        {
            if (OS.currentInstance == null)
            {
                ClearWebviewResources();
                return;
            }

            var comp = OS.currentInstance.connectedComp;
            if (comp == null)
            {
                ClearWebviewResources();
                return;
            }

            var mailServer = (MailServer)comp.getDaemon(typeof(MailServer));
            if (mailServer == null)
            {
                ClearWebviewResources();
                return;
            }

            if (mailServer.state != 4)
            {
                ClearWebviewResources();
                return;
            }
        }

        private static void ClearWebviewResources()
        {
            foreach (var keyValuePair in HacknetWebviews)
            {
                keyValuePair.Value.Dispose();
            }
            HacknetWebviews.Clear();
        }
    }
}
