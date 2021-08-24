using System;
using System.IO;
using System.Linq;
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

        [Command(new[] { "Run", "r" })]
        public async Task<int> Run(
            [Option(0, "filename:script.cs")] string filename,
            [Option("fr")] string targetFrameWork = "net5.0",
            [Option("xv", "https://github.com/Cysharp/ProcessX")] string processXVersion = "1.4.5",
            [Option("sr")] bool stopError = false
        )
        {
            await ScriptRunner.CreateEnv(filename, targetFrameWork, processXVersion);
            var result = await ScriptRunner.Run(filename);

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
            await ScriptRunner.CreateEnv(filename, targetFrameWork, processXVersion);
            ScriptRunner.Edit();
        }

        [Command(new[] { "SettingsList", "sl" })]
        public void SettingsList(
            [Option("sd")] string settingDirectory = ".")
        {
            Settings.GetRootSettings(settingDirectory)?.WriteList(Console.Out);
        }

        [Command(new[] { "SettingsAddPackage", "sapa" })]
        public void SettingsAddPackage(
            [Option(0, "name:ProcessX")] string name,
            [Option(1, "Version:1.4.5")] string version,
            [Option("sd")] string settingDirectory = ".")
        {
            var settings = Settings.CreateOrLoadCurrentSettings(settingDirectory);
            var find = settings.PackageRefList.FirstOrDefault(@ref => @ref.Name == name);
            if (find != null)
                find.Version = version;
            else
                settings.PackageRefList.Add(new Settings.PackageRef
                {
                    Name = name,
                    Version = version
                });
            settings.SaveCurrentSettings(settingDirectory);
        }

        [Command(new[] { "SettingsRemovePackage", "srpa" })]
        public void SettingsRemovePackage(
            [Option(0, "name:ProcessX")] string name,
            [Option("sd")] string settingDirectory = ".")
        {
            var settings = Settings.CreateOrLoadCurrentSettings(settingDirectory);
            var find = settings.PackageRefList.FirstOrDefault(@ref => @ref.Name == name);
            if (find == null)
            {
                Console.Error.WriteLine($"{name} is not found");
                return;
            }
            settings.PackageRefList.Remove(find);
            settings.SaveCurrentSettings(settingDirectory);
        }

        [Command(new[] { "SettingsAddProject", "sapr" })]
        public void SettingsAddProject(
            [Option(0, "projectPath:../../Util/Util.csproj")] string projectPath,
            [Option("sd")] string settingDirectory = ".")
        {
            var path = Path.GetFullPath(Path.Combine(settingDirectory, projectPath));
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"{path} is nod found");
                return;
            }
            var settings = Settings.CreateOrLoadCurrentSettings(settingDirectory);
            var find = settings.ProjectRefList.FirstOrDefault(@ref => @ref.ProjectPath == projectPath);
            if (find != null) return;
            
            settings.ProjectRefList.Add(new Settings.ProjectRef
            {
                ProjectPath = projectPath
            });
            settings.SaveCurrentSettings(settingDirectory);
        }

        [Command(new[] { "SettingsRemoveProject", "srpr" })]
        public void SettingsRemoveProject(
            [Option(0, "projectPath:../../Util/Util.csproj")] string projectPath,
            [Option("sd")] string settingDirectory = ".")
        {
            var settings = Settings.CreateOrLoadCurrentSettings(settingDirectory);
            var find = settings.ProjectRefList.FirstOrDefault(@ref => @ref.ProjectPath == projectPath);
            if (find == null)
            {
                Console.Error.WriteLine($"{projectPath} is nod found");
                return;
            }
            settings.ProjectRefList.Remove(find);
            settings.SaveCurrentSettings(settingDirectory);
        }

        [Command(new[] { "SettingsAddCs", "sac" })]
        public void SettingsAddCs(
            [Option(0, "csFilePath:../../Util/Util.cs")] string csFilePath,
            [Option("sd")] string settingDirectory = ".")
        {
            var path = Path.GetFullPath(Path.Combine(settingDirectory, csFilePath));
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"{path} is nod found");
                return;
            }
            var settings = Settings.CreateOrLoadCurrentSettings(settingDirectory);
            var find = settings.CsRefList.FirstOrDefault(@ref => @ref.FilePath == csFilePath);
            if (find != null) return;

            settings.CsRefList.Add(new Settings.CsRef
            {
                FilePath = csFilePath
            });
            settings.SaveCurrentSettings(settingDirectory);
        }

        [Command(new[] { "SettingsRemoveCs", "src" })]
        public void SettingsRemoveCs(
            [Option(0, "csFilePath:../../Util/Util.cs")] string csFilePath,
            [Option("sd")] string settingDirectory = ".")
        {
            var settings = Settings.CreateOrLoadCurrentSettings(settingDirectory);
            var find = settings.CsRefList.FirstOrDefault(@ref => @ref.FilePath == csFilePath);
            if (find == null)
            {
                Console.Error.WriteLine($"{csFilePath} is nod found");
                return;
            }
            settings.CsRefList.Remove(find);
            settings.SaveCurrentSettings(settingDirectory);
        }
    }
}
