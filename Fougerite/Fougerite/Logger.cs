using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Fougerite.Events;
using UnityEngine;

namespace Fougerite
{
    public static class Logger
    {
        struct Writer
        {
            public StreamWriter LogWriter;
            public string DateTime;
        }

        private static string LogsFolder;
        private static Writer RPCLogWriter;
        private static Writer LogWriter;
        private static Writer ChatWriter;
        private static bool showDebug = false;
        private static bool showErrors = false;
        private static bool showException = false;
        internal static bool showRPC = false;
        private static int _mainThreadId;

        /// <summary>
        /// Native export from Fougerite LibRust x64 (Notulp/Fougerite_LibRust_x64).
        /// Writes a log line directly to the dedicated server's console window.
        /// Used here so logs from background threads also show up in the console,
        /// since Unity's log callback only reaches the console on the main thread.
        /// </summary>
        [DllImport("librust.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void ConsoleLog(string log, string trace, int type);

        /// <summary>
        /// Forwards a log message to the native LibRust x64 console.
        /// Skips on x86 builds (I don't trust facepunch code) and
        /// skips on the main thread, since the existing Unity log hook already
        /// covers that case and would otherwise print every line twice.
        /// </summary>
        /// <param name="message">Already-formatted log line to print.</param>
        /// <param name="unityLogType">Maps to UnityEngine.LogType (Error=0, Warning=2, Log=3, Exception=4).</param>
        private static void ForwardToNativeConsole(string message, int unityLogType)
        {
            // Not our x64 bit custom build
            if (IntPtr.Size != 8) 
                return;
            
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId) 
                return;

            try
            {
                ConsoleLog(message, string.Empty, unityLogType);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void Init()
        {
            _mainThreadId = Util.GetUtil().MainThreadID;

            try
            {
                LogsFolder = Path.Combine(Config.GetPublicFolder(), "Logs");
                showDebug = Config.GetBoolValue("Logging", "debug");
                showErrors = Config.GetBoolValue("Logging", "error");
                showException = Config.GetBoolValue("Logging", "exception");
                showRPC = Config.GetBoolValue("Logging", "rpctracer");
                Debug.Log(showRPC.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse logging values: {ex}");
            }

            try
            {
                Directory.CreateDirectory(LogsFolder);
                if (!File.Exists(Path.Combine(LogsFolder, "HookSpeed.log"))) { File.Create(Path.Combine(LogsFolder, "HookSpeed.log")).Dispose(); }
                LogWriterInit();
                ChatWriterInit();
                RPCTracerInit();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void RPCTracerInit()
        {
            try
            {
                if (RPCLogWriter.LogWriter != null)
                    RPCLogWriter.LogWriter.Close();

                RPCLogWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
                RPCLogWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder,
                    $"RPCTracer_{RPCLogWriter.DateTime}.log"), true);
                RPCLogWriter.LogWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void LogWriterInit()
        {
            try
            {
                if (LogWriter.LogWriter != null)
                    LogWriter.LogWriter.Close();

                LogWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
                LogWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, $"Log_{LogWriter.DateTime}.log"), true);
                LogWriter.LogWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void ChatWriterInit()
        {
            try
            {
                if (ChatWriter.LogWriter != null)
                    ChatWriter.LogWriter.Close();

                ChatWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
                ChatWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, $"Chat_{ChatWriter.DateTime}.log"), true);
                ChatWriter.LogWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        private static string LogFormat(string Text)
        {
            Text = $"[{DateTime.Now}] {Text}";
            return Text;
        }

        private static void WriteLog(string Message)
        {
            try
            {
                if (LogWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
                    LogWriterInit();
                LogWriter.LogWriter.WriteLine(LogFormat(Message));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void WriteChat(string Message)
        {
            try
            {
                if (ChatWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
                    ChatWriterInit();
                ChatWriter.LogWriter.WriteLine(LogFormat(Message));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void Log(string Message, UnityEngine.Object Context = null)
        {
            Debug.Log(Message, Context);
            Message = $"[Console] {Message}";
            WriteLog(Message);
            ForwardToNativeConsole(Message, 3);

            Hooks.LoggerEvent(LoggerEventType.Log, Message);
        }
        
        /// <summary>
        /// This is called and used to trace all RPC calls (may produce a lot of logs)
        /// coming from any client, useful to debug client-server communication
        /// hacks, floods, etc. The call is manually patched in ULink.dll Class4
        /// and is enabled with the "rpctracer" option in the config file.
        /// </summary>
        /// <param name="Message"></param>
        public static void LogRPC(string Message)
        {
            if (!showRPC)
            {
                return;
            }
            
            try
            {
                if (RPCLogWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
                    RPCTracerInit();
                Message = $"[RPC Debug] {Message}";
                RPCLogWriter.LogWriter.WriteLine(LogFormat(Message));
                ForwardToNativeConsole(Message, 3);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            
            Hooks.LoggerEvent(LoggerEventType.LogRPC, Message);
        }

        public static void LogWarning(string Message, UnityEngine.Object Context = null)
        {
            Debug.LogWarning(Message, Context);
            Message = $"[Warning] {Message}";
            WriteLog(Message);
            ForwardToNativeConsole(Message, 2);
            
            Hooks.LoggerEvent(LoggerEventType.LogWarning, Message);
        }
        
        public static void LogError(string Message, UnityEngine.Object Context = null)
        {
            if (showErrors)
                Debug.LogError(Message, Context);
            Message = $"[Error] {Message}";
            WriteLog(Message);
            ForwardToNativeConsole(Message, 0);
            
            Hooks.LoggerEvent(LoggerEventType.LogError, Message);
        }

        public static void LogErrorIgnore(string Message, UnityEngine.Object Context = null, bool IgnoreHook = false)
        {
            if (showErrors)
                Debug.LogError(Message, Context);
            Message = $"[Error] {Message}";
            WriteLog(Message);
            ForwardToNativeConsole(Message, 0);

            if (!IgnoreHook)
            {
                Hooks.LoggerEvent(LoggerEventType.LogError, Message);
            }
        }

        public static void LogException(Exception Ex, UnityEngine.Object Context = null)
        {
            if (showException)
                Debug.LogException(Ex, Context);

            string Trace = "";
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            for (int i = 1; i < stackTrace.FrameCount; i++)
            {
                var declaringType = stackTrace.GetFrame(i).GetMethod().DeclaringType;
                if (declaringType != null)
                    Trace += $"{declaringType.Name}->{stackTrace.GetFrame(i).GetMethod().Name} | ";
            }

            string Message = $"[Exception] [ {Trace}]\r\n{Ex}";
            WriteLog(Message);
            ForwardToNativeConsole(Message, 4);
            
            Hooks.LoggerEvent(LoggerEventType.LogException, Message);
        }

        public static void LogDebug(string Message, UnityEngine.Object Context = null)
        {
            if (showDebug)
                Debug.Log($"[DEBUG] {Message}", Context);
            Message = $"[Debug] {Message}";
            WriteLog(Message);
            ForwardToNativeConsole(Message, 3);
            
            Hooks.LoggerEvent(LoggerEventType.LogDebug, Message);
        }

        public static void ChatLog(string Sender, string Message)
        {
            Message = $"[CHAT] {Sender}: {Message}";
            Debug.Log(Message);
            WriteChat(Message);
            ForwardToNativeConsole(Message, 3);
            
            Hooks.LoggerEvent(LoggerEventType.ChatLog, Message);
        }
    }
}