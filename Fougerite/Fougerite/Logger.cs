﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        private static string LogsFolder = Path.Combine(Config.GetPublicFolder(), "Logs");
        private static Writer LogWriter;
        private static Writer ChatWriter;
        private static bool showDebug = false;
        private static bool showErrors = false;
        private static bool showException = false;

        public static void Init()
        {
            try
            {
                showDebug = Config.GetBoolValue("Logging", "debug");
                showErrors = Config.GetBoolValue("Logging", "error");
                showException = Config.GetBoolValue("Logging", "exception");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                Directory.CreateDirectory(LogsFolder);

                LogWriterInit();
                ChatWriterInit();
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
                LogWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, string.Format("Log_{0}.log", LogWriter.DateTime)), true);
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
                ChatWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, string.Format("Chat_{0}.log", ChatWriter.DateTime)), true);
                ChatWriter.LogWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        private static string LogFormat(string Text)
        {
            Text = "[" + DateTime.Now + "] " + Text;
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
            Message = string.Format("[Console] {0}", Message);
            Debug.Log(Message, Context);
            WriteLog(Message);
        }

        public static void LogWarning(string Message, UnityEngine.Object Context = null)
        {
            Message = string.Format("[Warning] {0}", Message);
            Debug.LogWarning(Message, Context);
            WriteLog(Message);
        }

        public static void LogError(string Message, UnityEngine.Object Context = null)
        {
            if (showErrors)
            {
                Message = string.Format("[Error] {0}", Message);
                Debug.LogError(Message, Context);
                WriteLog(Message);
            }
        }

        public static void LogException(Exception Ex, UnityEngine.Object Context = null)
        {
            if (showException)
            {
                Debug.LogException(Ex, Context);

                string Trace = string.Empty;
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                for (int i = 1; i < stackTrace.FrameCount; i++)
                    Trace += string.Format("{0}->{1} | ", stackTrace.GetFrame(i).GetMethod().DeclaringType.Name, stackTrace.GetFrame(i).GetMethod().Name);

                string Message = string.Format("[Exception] [ {0}]\r\n{1}", Trace, Ex.ToString());
                WriteLog(Message);
            }
        }

        public static void LogDebug(string Message, UnityEngine.Object Context = null)
        {
            if (showDebug)
            {
                Message = string.Format("[Debug] {0}", Message);
                Debug.Log(Message, Context);
                WriteLog(Message);
            }
        }

        public static void ChatLog(string Sender, string Msg)
        {
            Msg = string.Format("[CHAT] {0}: {1}", Sender, Msg);
            Debug.Log(Msg);
            WriteChat(Msg);
        }
    }
}