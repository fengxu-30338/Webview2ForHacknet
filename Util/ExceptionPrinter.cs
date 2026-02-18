using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Webview2ForHacknet.Util
{
    public static class ExceptionPrinter
    {
        /// <summary>
        /// 打印异常的完整信息，包括所有内部异常和聚合异常
        /// </summary>
        /// <param name="ex">要打印的异常</param>
        /// <param name="indentLevel">缩进级别（用于递归调用）</param>
        public static void PrintFullException(Exception ex, int indentLevel = 0)
        {
            if (ex == null) return;

            string indent = new string(' ', indentLevel * 2);
            StringBuilder sb = new StringBuilder();

            // 打印异常基本信息
            sb.AppendLine($"{indent}异常类型: {ex.GetType().FullName}");
            sb.AppendLine($"{indent}异常消息: {ex.Message}");
            if (ex is FileNotFoundException fileNotFoundException)
            {
                sb.AppendLine($"{indent}异常文件路径: {fileNotFoundException.FileName}");
            }

            if (!string.IsNullOrEmpty(ex.HelpLink))
                sb.AppendLine($"{indent}帮助链接: {ex.HelpLink}");

            if (ex.Source != null)
                sb.AppendLine($"{indent}异常源: {ex.Source}");

            // 堆栈跟踪
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine($"{indent}堆栈跟踪:");
                string[] stackLines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string line in stackLines)
                {
                    sb.AppendLine($"{indent}  {line}");
                }
            }

            // 数据集合
            if (ex.Data != null && ex.Data.Count > 0)
            {
                sb.AppendLine($"{indent}异常数据:");
                foreach (System.Collections.DictionaryEntry entry in ex.Data)
                {
                    sb.AppendLine($"{indent}  {entry.Key} = {entry.Value}");
                }
            }

            Console.WriteLine(sb.ToString());

            // 处理聚合异常
            if (ex is AggregateException aggregateEx)
            {
                Console.WriteLine($"{indent}=== 聚合异常包含 {aggregateEx.InnerExceptions.Count} 个内部异常 ===");

                int counter = 1;
                foreach (var innerEx in aggregateEx.InnerExceptions)
                {
                    Console.WriteLine($"{indent}[内部异常 #{counter++}]");
                    PrintFullException(innerEx, indentLevel + 2);
                }
            }
            // 处理普通内部异常
            else if (ex.InnerException != null)
            {
                Console.WriteLine($"{indent}=== 内部异常 ===");
                PrintFullException(ex.InnerException, indentLevel + 1);
            }
        }

        /// <summary>
        /// 获取异常的完整信息字符串
        /// </summary>
        public static string GetFullExceptionString(Exception ex, int indentLevel = 0)
        {
            var originalOutput = Console.Out;
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                PrintFullException(ex, indentLevel);
                Console.SetOut(originalOutput);
                return writer.ToString();
            }
        }

        /// <summary>
        /// 递归获取异常树的所有叶节点（最深层的内部异常）
        /// </summary>
        public static List<Exception> GetLeafExceptions(Exception ex)
        {
            var leafExceptions = new List<Exception>();
            CollectLeafExceptions(ex, leafExceptions);
            return leafExceptions;
        }

        private static void CollectLeafExceptions(Exception ex, List<Exception> leaves)
        {
            if (ex == null) return;

            if (ex is AggregateException aggregateEx)
            {
                foreach (var inner in aggregateEx.InnerExceptions)
                {
                    CollectLeafExceptions(inner, leaves);
                }
            }
            else if (ex.InnerException != null)
            {
                CollectLeafExceptions(ex.InnerException, leaves);
            }
            else
            {
                leaves.Add(ex);
            }
        }
    }
}
