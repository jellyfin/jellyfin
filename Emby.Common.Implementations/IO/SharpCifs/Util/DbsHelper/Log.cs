using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SharpCifs.Util.DbsHelper
{
    public class Log
    {
        /// <summary>
        /// コンソールへのログ出力を行うか否か
        /// </summary>
        public static bool IsActive { get; set; } = false;

        public static void Out(string message)
        {
            if (!Log.IsActive
                || string.IsNullOrEmpty(message))
                return;

            var msg = DateTime.Now.ToString("HH:mm:ss.fff") 
                      + ": [ThID: "
                      + System.Environment.CurrentManagedThreadId.ToString().PadLeft(3)
                      + " "
                      + message;

            Debug.WriteLine(msg);
            Console.WriteLine(msg);
        }

        /// <summary>
        /// 例外のログ出力を行う。
        /// </summary>
        /// <param name="ex"></param>
        public static void Out(Exception ex)
        {
            if (!Log.IsActive
                || ex == null)
                return;

            Log.Out($"{ex}");
            var message = Log.GetHighlighted(Log.GetErrorString(ex));
            Log.Out(message);
        }

        /// <summary>
        /// Cast string-arrary to Highlighted message
        /// 文字列配列を強調メッセージ形式文字列に変換する。
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static string GetHighlighted(params System.String[] messages)
        {
            var time = DateTime.Now;
            var list = new List<string>();

            list.Add("");
            list.Add("");
            list.Add(time.ToString("HH:mm:ss.fff") + ":");
            list.Add("##################################################");
            list.Add("#");

            foreach (string message in messages)
            {
                var lines = message.Replace("\r\n", "\n").Replace("\r", "\n").Trim('\n').Split('\n');
                foreach (var line in lines)
                {
                    list.Add($"# {line}");
                }
            }

            list.Add("#");
            list.Add("##################################################");
            list.Add("");
            list.Add("");

            return string.Join("\r\n", list);
        }

        /// <summary>
        /// Get Formatted Exception-Info string-array
        /// 例外情報を整形した文字列配列を返す。
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static string[] GetErrorString(Exception ex)
        {
            var list = new List<string>();

            if (ex.Message != null)
            {
                list.Add(ex.Message ?? "");
                list.Add("");
            }

            if (ex.StackTrace != null)
            {
                list.AddRange(ex.StackTrace.Split(new string[] { "場所", "at " },
                                                  StringSplitOptions.None)
                                           .AsEnumerable()
                                           .Select(row => "\r\nat " + row));
            }

            if (ex.InnerException != null)
            {
                //InnerExceptionを再帰取得する。
                list.Add("");
                list.Add("Inner Exception");
                list.AddRange(Log.GetErrorString(ex.InnerException));
            }

            return list.ToArray();
        }
    }
}
