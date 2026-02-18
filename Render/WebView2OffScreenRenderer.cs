using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Webview2ForHacknet.Util;

namespace Webview2ForHacknet.Render
{
    public class WebView2OffScreenRenderer : IDisposable
    {
        private CoreWebView2Environment _environment;
        private bool _isInitialized;
        private bool _isDisposed;
        private CancellationTokenSource _renderCts;

        public CoreWebView2 WebView => Controller?.CoreWebView2;
        public CoreWebView2CompositionController Controller { get; private set; }

        public bool IsInitialized => _isInitialized;

        static WebView2OffScreenRenderer()
        {
            var fieldInfo = AccessTools.Field(typeof(CoreWebView2Environment), "webView2LoaderLoaded");
            var webView2LoaderLoaded = (bool)fieldInfo.GetValue(null);
            if (!webView2LoaderLoaded)
            {
                CoreWebView2Environment.SetLoaderDllFolderPath(Path.Combine(CommonUtils.ExtensionPluginsPath(), "WebView2"));
            }
        }

        public async Task InitializeAsync(string userDataFolder = null)
        {
            if (_isInitialized)
                throw new InvalidOperationException("Already initialized");

            var options = new CoreWebView2EnvironmentOptions();
            _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            Controller = await _environment.CreateCoreWebView2CompositionControllerAsync(Process.GetCurrentProcess().MainWindowHandle);
            WebView.Settings.IsZoomControlEnabled = false;
            WebView.Settings.AreBrowserAcceleratorKeysEnabled = false;
            WebView.Settings.AreDefaultContextMenusEnabled = false;
            WebView.Settings.AreDevToolsEnabled = false;
            WebView.Settings.AreDefaultScriptDialogsEnabled = false;

            Controller.ZoomFactor = 1 / CommonUtils.GetDpiScale();

            _isInitialized = true;
            _renderCts = new CancellationTokenSource();
        }

        public Task<bool> Navigate(string uri)
        {
            if (!_isInitialized || Controller?.CoreWebView2 == null)
                throw new InvalidOperationException("Not initialized");
            var tcs = new TaskCompletionSource<bool>();

            WebView.NavigationCompleted += WebViewOnNavigationCompleted;

            WebView?.Navigate(uri);
            return tcs.Task;

            void WebViewOnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                WebView.NavigationCompleted -= WebViewOnNavigationCompleted;
                tcs.SetResult(e.IsSuccess);
            }
        }

        public Task<bool> Navigate(Uri uri) => Navigate(uri.AbsoluteUri);

        public Task<bool> NavigateToString(string htmlContent)
        {
            if (!_isInitialized || Controller?.CoreWebView2 == null)
                throw new InvalidOperationException("Not initialized");
            var tcs = new TaskCompletionSource<bool>();

            WebView.NavigationCompleted += WebViewOnNavigationCompleted;

            WebView?.NavigateToString(htmlContent);
            return tcs.Task;

            void WebViewOnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                WebView.NavigationCompleted -= WebViewOnNavigationCompleted;
                tcs.SetResult(e.IsSuccess);
            }
        }

        public void Reload()
        {
            WebView?.Reload();
        }

        public void Resize(int width, int height)
        {
            var dpiScale = CommonUtils.GetDpiScale();
            var dipWidth = (int)Math.Round(width / dpiScale);
            var dipHeight = (int)Math.Round(height / dpiScale);

            if (Controller?.CoreWebView2 != null)
            {
                Controller.Bounds = new System.Drawing.Rectangle(0, 0, dipWidth, dipHeight);
            }
        }

        public void Stop()
        {
            WebView?.Stop();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _renderCts?.Cancel();
            _renderCts?.Dispose();
            _isDisposed = true;
            _isInitialized = false;
        }
    }
}
