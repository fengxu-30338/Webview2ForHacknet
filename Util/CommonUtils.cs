using BepInEx.Logging;
using Hacknet.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Hacknet;
using HarmonyLib;

namespace Webview2ForHacknet.Util
{
    [HarmonyPatch]
    public static class CommonUtils
    {
        internal static ManualLogSource GetLogger(this object _)
        {
            return Webview2ForHacknetPlugin.Instance.Log;
        }

        public static bool CurrentIsMainThread() => Thread.CurrentThread.ManagedThreadId == 1;

        public static string ExtensionPath()
        {
            return Path.GetFullPath(ExtensionLoader.ActiveExtensionInfo.FolderPath);
        }

        public static string WithExtensionSubPath(this string subPath)
        {
            return Path.Combine(ExtensionPath(), subPath);
        }

        public static string ExtensionPluginsPath()
        {
            return "Plugins".WithExtensionSubPath();
        }

        public static void Print(this Exception e, string tip = null)
        {
            if (!string.IsNullOrWhiteSpace(tip))
            {
                e.GetLogger().LogError(tip);
            }
            e.GetLogger().LogError(ExceptionPrinter.GetFullExceptionString(e));
        }

        public static float GetDpiScale()
        {
            try
            {
                return NativeCall.GetDpiForWindow(NativeCall.GetDesktopWindow()) / 96f;
            }
            catch (Exception)
            {
                var hdc = NativeCall.GetDC(IntPtr.Zero);
                var res = NativeCall.GetDeviceCaps(hdc, NativeCall.LOGPIXELSX) / 96f;
                NativeCall.ReleaseDC(IntPtr.Zero, hdc);
                return res;
            }
        }


        private static readonly ConcurrentQueue<MainTask> InvokeActionsInUpdate = new ConcurrentQueue<MainTask>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game1), nameof(Game1.Update))]
        private static void PostGameUpdate()
        {
            var taskList = new LinkedList<MainTask>();

            while (!InvokeActionsInUpdate.IsEmpty)
            {
                if (!InvokeActionsInUpdate.TryDequeue(out var task)) continue;
                try
                {
                    task.Task?.Invoke();
                }
                catch (Exception e)
                {
                    e.Print();
                }
                finally
                {
                    if (!task.RemoveCondition.Invoke())
                    {
                        taskList.AddLast(task);
                    }
                }
            }

            foreach (var mainTask in taskList)
            {
                InvokeActionsInUpdate.Enqueue(mainTask);
            }
        }

        public static void PostToMainThread(this object obj, Action invokeAction, ScheduleRule scheduleRule = ScheduleRule.ONCE)
        {
            InvokeActionsInUpdate.Enqueue(new MainTask(invokeAction, scheduleRule == ScheduleRule.ONCE ? new Func<bool>(() => true) : () => false));
        }

        public static void PostToMainThread(this object obj, Action invokeAction, Func<bool> removeCondition)
        {
            InvokeActionsInUpdate.Enqueue(new MainTask(invokeAction, removeCondition));
        }
    }

    internal class MainTask
    {
        public Action Task { get; }

        public Func<bool> RemoveCondition { get; }

        public MainTask(Action task, Func<bool> removeCondition)
        {
            Task = task;
            RemoveCondition = removeCondition;
        }
    }

    public enum ScheduleRule
    {
        ONCE,
        LOOP
    }
}
