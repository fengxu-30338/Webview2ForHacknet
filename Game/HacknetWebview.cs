using Hacknet;
using Microsoft.Web.WebView2.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Pathfinder.Replacements;
using Pathfinder.Util.XML;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hacknet.Effects;
using Webview2ForHacknet.Render;
using Webview2ForHacknet.Util;
using Point = System.Drawing.Point;

namespace Webview2ForHacknet.Game
{
    public class HacknetWebview: IDisposable
    {
        public WebView2OffScreenRenderer Render { get; } = new WebView2OffScreenRenderer();
        private Task initTask = null;
        private Task<bool> navigateTask = null;
        private Task<Texture2D> captureTask = null;
        private Texture2D _lastTexture2D = null;
        private bool _disposed = false;
        private Rectangle _lastRect = Rectangle.Empty;
        public bool IsInit { get; private set; }
        public bool NavigateCompleted => navigateTask == null || navigateTask.IsCompleted;

        public event Predicate<string> OnReceiveWebMessageHandler;

        public HacknetWebview()
        {
            this.PostToMainThread(this.WaitInit, () => _disposed);
        }

        private void WaitInit()
        {
            if (IsInit) return;
            if (initTask != null)
            {
                if (!initTask.IsCompleted)
                {
                    return;
                }

                InitEnv();

                IsInit = true;
                initTask = null;
                return;
            }

            initTask = Render.InitializeAsync();
        }

        private void InitEnv()
        {
            SetVirtualHostNameToFolderMapping("hacknet.asset", CommonUtils.ExtensionPath());
            Render.WebView.WebMessageReceived += WebView_WebMessageReceived;
            RegisterMethodsToWeb();
        }

        private void RegisterMethodsToWeb()
        {
            Render.WebView.AddScriptToExecuteOnDocumentCreatedAsync(@"
                window.PostMessage = window.ExecuteAction = function(content) {
                    window.chrome.webview.postMessage(content);
                };

                window.AddCSharpMessageListener = function(func) {
                    window.chrome.webview.addEventListener('message', func);
                };

                window.RemoveCSharpMessageListener = function(func) {
                    window.chrome.webview.removeEventListener('message', func);
                };
            ");
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message;
            try
            {
                message = e.TryGetWebMessageAsString();
            }
            catch (Exception ex)
            {
                ex.Print("WebView message receive error");
                return;
            }
            if (string.IsNullOrWhiteSpace(message)) return;

            var handled = OnReceiveWebMessageHandler?.Invoke(message);
            if (handled.HasValue && handled.Value)
            {
                return;
            }

            // 当action处理
            try
            {
                var eventExecutor = new EventExecutor(message, false);
                eventExecutor.RegisterExecutor("*", (exec, info) =>
                {
                    var readAction = ActionsLoader.ReadAction(info);
                    readAction.Trigger(OS.currentInstance);
                }, ParseOption.ParseInterior);
                eventExecutor.Parse();
            }
            catch (Exception ex)
            {
                ex.Print("Execute Action In Js Failed");
            }
        }

        private void HandleMouseEvent()
        {
            if (!IsInit || _disposed || !NavigateCompleted) return;

            if (!_lastRect.Contains(GuiData.getMousePoint()))
            {
                Render.Controller.SendMouseInput(CoreWebView2MouseEventKind.Leave, CoreWebView2MouseEventVirtualKeys.None, 0, new Point());
                NativeCall.SetFocus(Process.GetCurrentProcess().MainWindowHandle);
                return;
            }

            var mouseState = Mouse.GetState();
            var dpiScale = CommonUtils.GetDpiScale();
            var relativeX = (int)((mouseState.X - _lastRect.X) / dpiScale);
            var relativeY = (int)((mouseState.Y - _lastRect.Y) / dpiScale);
            var eventKind = GetMouseEventKind(mouseState);
            Render.Controller.SendMouseInput(eventKind, 
                CoreWebView2MouseEventVirtualKeys.None,
                eventKind == CoreWebView2MouseEventKind.Wheel ? (uint)mouseState.ScrollWheelValue : 0, 
                new Point(relativeX, relativeY));
        }

        private CoreWebView2MouseEventKind GetMouseEventKind(MouseState mouseState)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                return CoreWebView2MouseEventKind.LeftButtonDown;
            }

            if (mouseState.LeftButton == ButtonState.Released && GuiData.lastMouse.LeftButton == ButtonState.Pressed)
            {
                return CoreWebView2MouseEventKind.LeftButtonUp;
            }

            if (mouseState.MiddleButton == ButtonState.Pressed)
            {
                return CoreWebView2MouseEventKind.MiddleButtonDown;
            }

            if (mouseState.MiddleButton == ButtonState.Released && GuiData.lastMouse.MiddleButton == ButtonState.Pressed)
            {
                return CoreWebView2MouseEventKind.MiddleButtonUp;
            }

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                return CoreWebView2MouseEventKind.RightButtonDown; 
            }

            if (mouseState.RightButton == ButtonState.Released && GuiData.lastMouse.RightButton == ButtonState.Pressed)
            {
                return CoreWebView2MouseEventKind.RightButtonUp;
            }

            if (mouseState.ScrollWheelValue != 0)
            {
                return CoreWebView2MouseEventKind.Wheel;
            }

            return CoreWebView2MouseEventKind.Move;
        }

        public void PostMessageToWeb(string message)
        {
            this.PostToMainThread(() =>
            {
                if (IsInit && !_disposed)
                {
                    Render.Controller.CoreWebView2.PostWebMessageAsString(message);
                }
            });
        }

        public void Navigate(string url)
        {
            if (IsInit && !_disposed && CommonUtils.CurrentIsMainThread())
            {
                navigateTask = Render.Navigate(url);
                ClearDrawCache();
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            navigateTask = tcs.Task;
            ClearDrawCache();
            this.PostToMainThread(() =>
            {
                if (IsInit && !_disposed)
                {
                    navigateTask = Render.Navigate(url);
                    tcs.TrySetCanceled();
                }
            }, () => IsInit);
        }


        public void NavigateWithHtmlContent(string html)
        {
            if (IsInit && !_disposed && CommonUtils.CurrentIsMainThread())
            {
                navigateTask = Render.NavigateToString(html);
                ClearDrawCache();
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            navigateTask = tcs.Task;
            ClearDrawCache();
            this.PostToMainThread(() =>
            {
                if (IsInit && !_disposed)
                {
                    navigateTask = Render.NavigateToString(html);
                    tcs.TrySetCanceled();
                }
            }, () => IsInit);
        }

        public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"SetVirtualHostNameToFolderMapping: Folder not found at {folderPath}");
            }

            this.PostToMainThread(() =>
            {
                if (IsInit && !_disposed)
                {
                    Render.WebView.SetVirtualHostNameToFolderMapping(hostName, folderPath, CoreWebView2HostResourceAccessKind.Allow);
                }
            }, () => IsInit);
        }

        public void Draw(SpriteBatch sb, Rectangle rect, bool showLoading = true)
        {
            if (_disposed) return;
            if (!IsInit || !NavigateCompleted)
            {
                if (showLoading)
                {
                    WebpageLoadingEffect.DrawLoadingEffect(rect, sb, OS.currentInstance);
                }
                return;
            }
            _lastRect = rect;
            Render.Resize(rect.Width, rect.Height);

            if (captureTask == null)
            {
                captureTask = CaptureFrameAsync(sb);
            }

            if (captureTask.IsCompleted)
            {
                _lastTexture2D?.Dispose();
                _lastTexture2D = captureTask.Result;
                captureTask = CaptureFrameAsync(sb);
            }

            if (_lastTexture2D != null)
            {
                sb.Draw(_lastTexture2D, new Vector2(rect.X, rect.Y), Color.White);
            }

            HandleMouseEvent();
        }

        public void ClearDrawCache()
        {
            _lastTexture2D?.Dispose();
            _lastTexture2D = null;
        }

        private async Task<Texture2D> CaptureFrameAsync(SpriteBatch sb)
        {
            if (!Render.IsInitialized || Render.Controller == null)
                throw new InvalidOperationException("Not initialized");

            try
            {
                using (var stream = new MemoryStream())
                {
                    await Render.Controller.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,
                        stream);

                    stream.Seek(0, SeekOrigin.Begin);

                    var texture = Texture2D.FromStream(sb.GraphicsDevice, stream);

                    return texture;
                }
            }
            catch (Exception e)
            {
                e.GetLogger().LogWarning($"CaptureFrame failed: {e.Message}");
            }

            return null;
        }

        public void Dispose()
        {
            _lastTexture2D?.Dispose();
            Render?.Dispose();
            _disposed = true;
        }
    }
}
