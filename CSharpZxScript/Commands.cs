using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleAppFramework;

namespace CSharpZxScript
{
    public class Commands
    {
        private const string DefaultDotnetVersion = "net8.0";
        private const string DefaultProcessXVersion = "1.5.5";

        #region Script

        /// <summary>
        /// Default Run
        /// </summary>
        /// <param name="filename">script.cszx</param>
        /// <param name="args">args</param>
        /// <param name="targetFrameWork">-fr</param>
        /// <param name="processXVersion">-xv, https://github.com/Cysharp/ProcessX</param>
        /// <param name="stopError">-sr</param>
        /// <returns></returns>
        [Command("")]
        public async Task<int> DefaultRun(
            [Argument] string filename,
            [Argument] string? args = null,
            string targetFrameWork = DefaultDotnetVersion,
            string processXVersion = DefaultProcessXVersion,
            bool stopError = false
        )
        {
            var runner = new ScriptRunner(filename);
            await runner.CreateEnv(targetFrameWork, processXVersion);
            var result = await runner.Run(args);

            if (result != 0 && stopError)
            {
                Console.WriteLine($"\nExitCode {result}\nPlease any key...");
                Console.ReadKey();
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="targetFrameWork">-fr</param>
        /// <param name="processXVersion">-xv</param>
        /// <returns></returns>
        [Command("Edit,e")]
        public async Task Edit(
            [Argument] string filename,
            string targetFrameWork = DefaultDotnetVersion,
            string processXVersion = DefaultProcessXVersion
        )
        {
            var runner = new ScriptRunner(filename);
            await runner.CreateEnv(targetFrameWork, processXVersion);
            runner.Edit();
        }

        [Command("ResetCache,rc")]
        public void ResetCache()
        {
            ScriptRunner.ResetWork();
        }

        #endregion

        #region Settings
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingDirectory">-sd</param>
        [Command("SettingList,sl")]
        public void SettingsList(
            string settingDirectory = ".")
        {
            Settings.GetRootSettings(settingDirectory)?.WriteList(Console.Out);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">name:ProcessX</param>
        /// <param name="version">-v</param>
        /// <param name="settingDirectory">-sd</param>
        /// <returns></returns>
        [Command("SettingsAddPackage,sapa")]
        public async Task SettingsAddPackage(
            [Argument] string name,
            string version = "",
            string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            await settings.AddPackageRef(name, version);
            settings.SaveCurrentSettings();
        }

        /// <summary>
        /// Removes a package reference from the current settings.
        /// </summary>
        /// <param name="name">-name, The name of the package to remove.</param>
        /// <param name="settingDirectory">-sd, The directory where the settings file is located. Defaults to the current directory.</param>
        [Command("SettingsRemovePackage,srpa")]
        public Task SettingsRemovePackage(
            [Argument] string name,
            string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            settings.RemovePackageRef(name);
            settings.SaveCurrentSettings();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a project reference to the current settings.
        /// </summary>
        /// <param name="projectPath">-projectPath, The path to the project file (.csproj) to add.</param>
        /// <param name="settingDirectory">-sd, The directory where the settings file is located. Defaults to the current directory.</param>
        [Command("SettingsAddProject,sapr")]
        public Task SettingsAddProject(
            [Argument] string projectPath,
            string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            settings.AddProjectRef(projectPath);
            settings.SaveCurrentSettings();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a project reference from the current settings.
        /// </summary>
        /// <param name="projectPath">-projectPath, The path to the project file (.csproj) to remove.</param>
        /// <param name="settingDirectory">-sd, The directory where the settings file is located. Defaults to the current directory.</param>
        [Command("SettingsRemoveProject,srpr")]
        public Task SettingsRemoveProject(
            [Argument] string projectPath,
            string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            settings.RemoveProjectRef(projectPath);
            settings.SaveCurrentSettings();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a C# file reference to the current settings.
        /// </summary>
        /// <param name="csFilePath">-csFilePath, The path to the C# file (.cs) to add.</param>
        /// <param name="settingDirectory">-sd, The directory where the settings file is located. Defaults to the current directory.</param>
        [Command("SettingsAddCs,sac")]
        public Task SettingsAddCs(
            [Argument] string csFilePath,
            string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            if (!File.Exists(csFilePath))
            {
                Console.Error.WriteLine($"{csFilePath} is nod found");
                return Task.CompletedTask;
            }

            var settings = Settings.CreateOrLoadCurrentSettings();
            var find = settings.CsRefList.FirstOrDefault(@ref => @ref.FilePath == csFilePath);
            if (find != null) return Task.CompletedTask;

            settings.CsRefList.Add(new Settings.CsRef
            {
                FilePath = csFilePath
            });
            settings.SaveCurrentSettings();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a C# file reference from the current settings.
        /// </summary>
        /// <param name="csFilePath">-csFilePath, The path to the C# file (.cs) to remove.</param>
        /// <param name="settingDirectory">-sd, The directory where the settings file is located. Defaults to the current directory.</param>
        [Command("SettingsRemoveCs,src")]
        public Task SettingsRemoveCs(
            [Argument] string csFilePath,
            string settingDirectory = ".")
        {
            using var cd = new CurrentDirectoryHelper(settingDirectory);
            var settings = Settings.CreateOrLoadCurrentSettings();
            var find = settings.CsRefList.FirstOrDefault(@ref => @ref.FilePath == csFilePath);
            if (find == null)
            {
                Console.Error.WriteLine($"{csFilePath} is nod found");
                return Task.CompletedTask;
            }

            settings.CsRefList.Remove(find);
            settings.SaveCurrentSettings();
            return Task.CompletedTask;
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
            catch (Win32Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add Run ZxScript and Edit ZxScript to the right-click menu of .cs
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Install()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                void InstallExt(string ext)
                {
                    using var shellRegKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\SystemFileAssociations\\" + ext + "\\shell");

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
                }

                InstallExt(".cs");
                InstallExt(".cszx");

                RunElevated("cmd.exe", "/c assoc .cszx=zxscript");
                RunElevated("cmd.exe", "/c ftype zxscript=" + ExePathUtil.ExePath + " %1");

                Console.WriteLine("Finish Install");
            }
            else
            {
                throw new ArgumentException("install is for Windows only");
            }
        }

        /// <summary>
        /// Remove right-click menu
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void UnInstall()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                void UnInstallExt(string ext)
                {
                    var shellRegKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\SystemFileAssociations\\" + ext + "\\shell", true);
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
                }

                UnInstallExt(".cs");
                UnInstallExt(".cszx");

                RunElevated("cmd.exe", "/c assoc .cszx=");
                RunElevated("cmd.exe", "/c ftype zxscript=");

                Console.WriteLine("Finish UnInstall");
            }
            else
            {
                throw new ArgumentException("uninstall is for Windows only");
            }
        }

        #endregion
    }
}
