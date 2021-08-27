using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;

namespace CSharpZxScript
{
    internal class Program : ConsoleAppBase
    {
        private static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
        }

        #region Script

        public async Task<int> DefaultRun(
            [Option(0, "filename:script.cs")] string filename,
            [Option("fr")] string targetFrameWork = "net5.0",
            [Option("xv", "https://github.com/Cysharp/ProcessX")] string processXVersion = "1.4.5",
            [Option("sr")] bool stopError = false
        )
        {
            return await Run(filename, targetFrameWork, processXVersion, stopError);
        }
        
        [Command(new[] { "Run", "r" })]
        public async Task<int> Run(
            [Option(0, "filename:script.cs")] string filename,
            [Option("fr")] string targetFrameWork = "net5.0",
            [Option("xv", "https://github.com/Cysharp/ProcessX")] string processXVersion = "1.4.5",
            [Option("sr")] bool stopError = false
        )
        {
            var runner = new ScriptRunner(filename);
            await runner.CreateEnv(targetFrameWork, processXVersion);
            var result = await runner.Run();

            if (result != 0 && stopError)
            {
                Console.WriteLine($"\nExitCode {result}\nPlease any key...");
                Console.ReadKey();
            }
            return result;
        }

        [Command(new[] { "Edit", "e" })]
        public async Task Edit(
            [Option(0, "filename:script.cs")] string filename,
            [Option("fr")] string targetFrameWork = "net5.0",
            [Option("xv", "https://github.com/Cysharp/ProcessX")] string processXVersion = "1.4.5"
        )
        {
            if (!File.Exists(filename))
            {
                await File.WriteAllTextAsync(filename, "using Zx;\nusing static Zx.Env;\n\nawait $\"echo {\"Hello World\"}\";");
            }
            var runner = new ScriptRunner(filename);
            await runner.CreateEnv(targetFrameWork, processXVersion);
            runner.Edit();
        }

        [Command(new []{"Inline", "inl"})]
        public async Task<int> RunInline(
            [Option(0, "script:log(1234);")] string script,
            [Option("fr")] string targetFrameWork = "net5.0",
            [Option("xv", "https://github.com/Cysharp/ProcessX")] string processXVersion = "1.4.5"
        )
        {
            var builder = new StringBuilder();
            builder.AppendLine(@"
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Cysharp.Diagnostics;
using Zx;
using static Zx.Env;
");
            builder.AppendLine(script);
            await File.WriteAllTextAsync("work.cs", builder.ToString());
            try
            {
                return await Run("work.cs", targetFrameWork, processXVersion);
            }
            finally
            {
                File.Delete("work.cs");
            }
        }

        [Command(new[] { "ResetCache", "rc" })]
        public void ResetCache()
        {
            ScriptRunner.ResetWork();
        }

        #endregion

        #region Settings 
        [Command(new[] { "SettingsList", "sl" })]
        public void SettingsList(
            [Option("sd")] string settingDirectory = ".")
        {
            Settings.GetRootSettings(settingDirectory)?.WriteList(Console.Out);
        }

        [Command(new[] { "SettingsAddPackage", "sapa" })]
        public async Task SettingsAddPackage(
            [Option(0, "name:ProcessX")] string name,
            [Option("v", "Version:1.4.5")] string version = "",
            [Option("sd")] string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            await settings.AddPackageRef(name, version);
            settings.SaveCurrentSettings();
        }

        [Command(new[] { "SettingsRemovePackage", "srpa" })]
        public void SettingsRemovePackage(
            [Option(0, "name:ProcessX")] string name,
            [Option("sd")] string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            settings.RemovePackageRef(name);
            settings.SaveCurrentSettings();
        }

        [Command(new[] { "SettingsAddProject", "sapr" })]
        public void SettingsAddProject(
            [Option(0, "projectPath:../../Util/Util.csproj")] string projectPath,
            [Option("sd")] string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            settings.AddProjectRef(projectPath);
            settings.SaveCurrentSettings();
        }

        [Command(new[] { "SettingsRemoveProject", "srpr" })]
        public void SettingsRemoveProject(
            [Option(0, "projectPath:../../Util/Util.csproj")] string projectPath,
            [Option("sd")] string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            settings.RemoveProjectRef(projectPath);
            settings.SaveCurrentSettings();
        }

        [Command(new[] { "SettingsAddCs", "sac" })]
        public void SettingsAddCs(
            [Option(0, "csFilePath:../../Util/Util.cs")] string csFilePath,
            [Option("sd")] string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            if (!File.Exists(csFilePath))
            {
                Console.Error.WriteLine($"{csFilePath} is nod found");
                return;
            }
            var settings = Settings.CreateOrLoadCurrentSettings();
            var find = settings.CsRefList.FirstOrDefault(@ref => @ref.FilePath == csFilePath);
            if (find != null) return;

            settings.CsRefList.Add(new Settings.CsRef
            {
                FilePath = csFilePath
            });
            settings.SaveCurrentSettings();
        }

        [Command(new[] { "SettingsRemoveCs", "src" })]
        public void SettingsRemoveCs(
            [Option(0, "csFilePath:../../Util/Util.cs")] string csFilePath,
            [Option("sd")] string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            var find = settings.CsRefList.FirstOrDefault(@ref => @ref.FilePath == csFilePath);
            if (find == null)
            {
                Console.Error.WriteLine($"{csFilePath} is nod found");
                return;
            }
            settings.CsRefList.Remove(find);
            settings.SaveCurrentSettings();
        }
        #endregion

        #region Registry
        
        private static bool RunElevated(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = fileName,
                    Verb = "runas",
                    Arguments = arguments
                };

            try
            {
                var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }

            return true;
        }

        [Command(new[] { "AddRightClickMenu", "arc" }, "Add Run ZxScript and Edit ZxScript to the right-click menu of .cs")]
        public void AddRightClickMenu()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var shellRegKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\Classes\\SystemFileAssociations\\.cs\\shell");

                using var zxScriptRunRegKey = shellRegKey.CreateSubKey("ZxScriptRun");
                zxScriptRunRegKey.SetValue("", "Run ZxScript");
                zxScriptRunRegKey.SetValue("icon", ExePathUtil.ExePath);
                using var zxScriptRunCommandRegKey = zxScriptRunRegKey.CreateSubKey("command");
                zxScriptRunCommandRegKey.SetValue("", $"\"{ExePathUtil.ExePath}\" r %1 -sr true");

                using var zxScriptEditRegKey = shellRegKey.CreateSubKey("ZxScriptEdit");
                zxScriptEditRegKey.SetValue("", "Edit ZxScript");
                zxScriptEditRegKey.SetValue("icon", ExePathUtil.ExePath);
                using var zxScriptEditCommandRegKey = zxScriptEditRegKey.CreateSubKey("command");
                zxScriptEditCommandRegKey.SetValue("", $"\"{ExePathUtil.ExePath}\" e %1");

                RunElevated("cmd.exe", "/c assoc .cszx=zxscript");
                RunElevated("cmd.exe", "/c ftype zxscript=" + ExePathUtil.ExePath + " %1");

                Console.WriteLine("Finish Add Menu");
            }
            else
            {
                throw new ArgumentException("AddRightClickMenu is for Windows only");
            }
        }

        [Command(new[] { "RemoveRightClickMenu", "rrc" }, "Remove right-click menu")]
        public void RemoveRightClickMenu()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var shellRegKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Classes\\SystemFileAssociations\\.cs\\shell", true);
                if (shellRegKey == null)
                {
                    Console.WriteLine("Shell Key is not found");
                    return;
                }

                using (shellRegKey)
                {
                    shellRegKey.DeleteSubKeyTree("ZxScriptRun");
                    shellRegKey.DeleteSubKeyTree("ZxScriptEdit");
                }
                Console.WriteLine("Finish Remove Menu");
            }
            else
            {
                throw new ArgumentException("RemoveRightClickMenu is for Windows only");
            }
        }

        #endregion
    }
}
