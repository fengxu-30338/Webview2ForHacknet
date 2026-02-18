using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Daemon;
using Pathfinder.Util;
using Pathfinder.Util.XML;
using System.IO;
using System.Linq;
using Webview2ForHacknet.Properties;
using Webview2ForHacknet.Util;

namespace Webview2ForHacknet.Game.Daemon
{
    public class WebServerDaemon :BaseDaemon
    {
        public override string Identifier => "WebServer";

        [XMLStorage]
        public string Url { get; set; }

        [XMLStorage]
        public string Name { get; set; }


        private readonly HacknetWebview _webview;

        public WebServerDaemon(Computer computer, string serviceName, OS opSystem) : base(computer, serviceName, opSystem)
        {
            _webview = new HacknetWebview();
        }

        public override void initFiles()
        {
            base.initFiles();
            InitBaseFolder();
            var filesRoot = this.comp.files.root;

            var webFolder = filesRoot.searchForFolder("Web");
            if (webFolder != null)
            {
                return;
            }

            webFolder = new Folder("Web");
            filesRoot.folders.Add(webFolder);
            webFolder.files.Add(new FileEntry(File.ReadAllText(Url), "index.html"));
        }

        public override void LoadFromXml(ElementInfo info)
        {
            base.LoadFromXml(info);
            InitBaseFolder();
            if (!string.IsNullOrWhiteSpace(Name))
            {
                this.name = Name;
            }
        }

        private void InitBaseFolder()
        {
            Url = LocalizedFileLoader.GetLocalizedFilepath(Url.WithExtensionSubPath());
        }

        public override void navigatedTo()
        {
            base.navigatedTo();

            var filesRoot = this.comp.files.root;
            var webFolder = filesRoot.folders.FirstOrDefault(folder => folder.name == "Web");
            var fileEntry = webFolder?.files.FirstOrDefault(fn => fn.name == "index.html");
            if (fileEntry == null)
            {
                _webview.NavigateWithHtmlContent(Resources.Page404);
                return;
            }
            _webview.NavigateWithHtmlContent(fileEntry.data);
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);
            var rect = Utils.InsetRectangle(bounds, 1);
            _webview.Draw(sb, rect);

            if (!Button.doButton(195371000, rect.X + 20, rect.Bottom - 60, 120, 40, LocaleTerms.Loc("Exit"), new Color?(this.os.highlightColor)))
                return;
            this.os.display.command = "connect";
        }
    }
}
