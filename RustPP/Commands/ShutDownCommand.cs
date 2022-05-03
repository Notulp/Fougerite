using System;
using System.Diagnostics;
using System.Timers;
using Fougerite;

namespace RustPP.Commands
{
    public class ShutDownCommand : ChatCommand
    {
        internal static System.Timers.Timer _timer;
        internal static System.Timers.Timer _timer2;
        public static int ShutdownTime = 60;
        public static int TriggerTime = 10;
        internal static int Time = 0;

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            if (ChatArguments.Length == 1)
            {
                if (ChatArguments[0] == "urgent")
                {
                    Fougerite.Hooks.IsShuttingDown = true;
                    Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Server Shutdown NOW!");
                    //UnityEngine.Application.Quit();
                    Process.GetCurrentProcess().Kill();
                }
                else if (ChatArguments[0] == "safeurgent")
                {
                    Fougerite.Hooks.IsShuttingDown = true;
                    Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Saving Server...");
                    World.GetWorld().ServerSaveHandler.ManualSave();
                    Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Saved Server Data!");
                    Fougerite.Server.GetServer().BroadcastFrom(Core.Name,
                        "Server is shutting down in " + ShutdownTime + " seconds.");
                    _timer = new Timer(TriggerTime * 1000);
                    _timer.Elapsed += Trigger;
                    _timer.Start();
                }

                return;
            }

            StartShutdown();
        }

        public static void StartShutdown()
        {
            try
            {
                ShutdownTime = int.Parse(Core.config.GetSetting("Settings", "shutdown_countdown"));
                TriggerTime = int.Parse(Core.config.GetSetting("Settings", "shutdown_trigger"));
            }
            catch
            {
                Logger.LogError("[RustPP] Failed to execute shutdown! Invalid config options!");
                return;
            }

            Fougerite.Hooks.IsShuttingDown = true;
            Fougerite.Server.GetServer()
                .BroadcastFrom(Core.Name, "Server is shutting down in " + ShutdownTime + " seconds.");
            _timer = new Timer(TriggerTime * 1000);
            _timer.Elapsed += Trigger;
            _timer.Start();
        }

        internal static void Trigger(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Time += TriggerTime;
            if (Time >= ShutdownTime)
            {
                _timer.Dispose();
                Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Saving Server...");
                World.GetWorld().ServerSaveHandler.ManualSave();
                Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Saved Server Data!");
                Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Server shutdown in 15 seconds!");
                _timer2 = new Timer(15000);
                _timer2.Elapsed += Trigger2;
                _timer2.Start();
            }
            else
            {
                Fougerite.Server.GetServer().BroadcastFrom(Core.Name,
                    "Server is shutting down in " + (ShutdownTime - Time) + " seconds.");
            }
        }

        internal static void Trigger2(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Fougerite.Server.GetServer().BroadcastFrom(Core.Name, "Server Shutdown NOW!");
            _timer2.Dispose();
            //Loom.QueueOnMainThread(UnityEngine.Application.Quit);
            //UnityEngine.Application.Quit();
            Process.GetCurrentProcess().Kill();
        }
    }
}