using System;
using System.IO;
using UnityEngine;
using Fougerite.Events;

namespace Fougerite
{
    public static class Logger
    {
        struct Writer
        {
            public StreamWriter LogWriter;
            public string DateTime;
        }

        private static string LogsFolder = Path.Combine(Config.GetPublicFolder(), "Logs");
        private static StreamWriter SpeedLogWriter;
        private static Writer RPCLogWriter;
        private static Writer LogWriter;
        private static Writer ChatWriter;
        private static bool showDebug = false;
        private static bool showErrors = false;
        private static bool showException = false;
        internal static bool showRPC = false;

        public static void Init()
        {
            try
            {
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
            
            Hooks.LoggerEvent(LoggerEventType.Log, Message);
        }
        
        public static void LogRPC(string Message)
        {
            if (!showRPC) { return;}
            try
            {
                if (RPCLogWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
                    RPCTracerInit();
                Message = $"[RPC Debug] {Message}";
                RPCLogWriter.LogWriter.WriteLine(LogFormat(Message));
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
            
            Hooks.LoggerEvent(LoggerEventType.LogWarning, Message);
        }
        
        public static void LogError(string Message, UnityEngine.Object Context = null)
        {
            if (showErrors)
                Debug.LogError(Message, Context);
            Message = $"[Error] {Message}";
            WriteLog(Message);
            
            Hooks.LoggerEvent(LoggerEventType.LogError, Message);
        }

        public static void LogErrorIgnore(string Message, UnityEngine.Object Context = null, bool IgnoreHook = false)
        {
            if (showErrors)
                Debug.LogError(Message, Context);
            Message = $"[Error] {Message}";
            WriteLog(Message);

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
            
            Hooks.LoggerEvent(LoggerEventType.LogException, Message);
        }

        public static void LogDebug(string Message, UnityEngine.Object Context = null)
        {
            if (showDebug)
                Debug.Log($"[DEBUG] {Message}", Context);
            Message = $"[Debug] {Message}";
            WriteLog(Message);
            
            Hooks.LoggerEvent(LoggerEventType.LogDebug, Message);
        }

        public static void ChatLog(string Sender, string Message)
        {
            Message = $"[CHAT] {Sender}: {Message}";
            Debug.Log(Message);
            WriteChat(Message);
            
            Hooks.LoggerEvent(LoggerEventType.ChatLog, Message);
        }
    }
}