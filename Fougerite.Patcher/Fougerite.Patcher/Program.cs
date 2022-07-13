using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Fougerite.Patcher
{
    internal class Program
    {
        public const string Version = "1.9.0";

        private static void Main(string[] args)
        {
            bool firstPass = CommandLine.HasSwitch("0", "a", "update-all", "1", "f", "update-fields");
            bool secondPass = CommandLine.HasSwitch("0", "a", "update-all", "2", "m", "update-methods");

            Environment.CurrentDirectory = Path.GetFullPath(CommandLine.GetSwitch(new[] { "dir", "d", "cd", "current-directory" },
                Environment.CurrentDirectory));

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            Logger.Clear();

            if (!firstPass && !secondPass) 
            {
                Logger.Log("Fougerite Patcher V" + Version);
                Logger.Log("No command specified.");
                Logger.Log($"Launch patcher with args:");
                Logger.Log("Patch fields: -1, -f, -update-fields");
                Logger.Log("Patch methods: -2, -m, -update-methods");
                Logger.Log("Patch methods & fields: -0, -a, -update-all");
                Logger.Log("Patch directory: -d, -dir, -cd, -current-directory [path-to-directory]");


                if (!CommandLine.HasCommandLine)
                {
                    Logger.Log("Choose patch method: \"0\" - patch all | \"1\" - patch fields | \"2\" - patch methods");

                    var response = Console.ReadKey(true);

                    switch (response.Key)
                    {
                        case ConsoleKey.D0:
                        case ConsoleKey.NumPad0:
                            firstPass = true;
                            secondPass = true;
                            break;

                        case ConsoleKey.D1:
                        case ConsoleKey.NumPad1:
                            firstPass = true;
                            break;

                        case ConsoleKey.D2:
                        case ConsoleKey.NumPad2:
                            secondPass = true;
                            break;

                        default:
                            Logger.Log($"Invalid key pressed: \"{response.KeyChar}\"");
                            return;
                    }
                }
                else
                {
                    return;
                }
                
            }

            if (!File.Exists("Assembly-CSharp.dll"))
            {
                Logger.Log($"Unable to find Assembly-CSharp.dll in \"{Environment.CurrentDirectory}\"");
                Logger.Log("Either move the patcher to folder rust's managed directory or override the with the following args:");
                Logger.Log("Patch directory: -d, -dir, -cd, -current-directory [path-to-directory]");

                if (CommandLine.HasCommandLine) return;

                Logger.Log("Press any key to close this window. . .");
                Console.ReadKey(true);

                return;
            }

            ILPatcher patcher = new ILPatcher();

            bool result = true;
            if (firstPass) 
            {
                result = result && patcher.FirstPass();
            }

            if (secondPass) 
            {
                result = result && patcher.SecondPass();
            }

            if (result) 
            {
                Logger.Log("The patch was applied successfully!");
            }

            if (!CommandLine.HasCommandLine)
            {
                Logger.Log("Press any key to close this window. . .");
                Console.ReadKey(true);
            }
        }

        private static readonly string[] WhitelistedAssemblies = new[] {"Mono.Cecil"};

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs e)
        {
            var an = new AssemblyName(e.Name);

            if (!WhitelistedAssemblies.Contains(an.Name))
            {
                return null;
            }

            var fp = Path.Combine(Environment.CurrentDirectory, $"{an.Name}.dll");


            return File.Exists(fp) ? Assembly.LoadFile(fp) : null;
        }
    }
}
