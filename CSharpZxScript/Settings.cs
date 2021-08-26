using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cysharp.Diagnostics;

namespace CSharpZxScript
{
    public class Settings
    {
        public const string SettingsName = "ZxScriptSettings.json";
        public Settings? ChildSettings;
        public string FilePath = "";

        public class PackageRef
        {
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
        }

        public class ProjectRef
        {
            public string ProjectPath { get; set; } = "";
        }

        public class CsRef
        {
            public string FilePath { get; set; } = "";
        }

        public List<PackageRef> PackageRefList { get; set; } = new List<PackageRef>();
        public List<ProjectRef> ProjectRefList { get; set; } = new List<ProjectRef>();
        public List<CsRef> CsRefList { get; set; } = new List<CsRef>();
        
        public async Task<bool> AddPackageRef(string name, string version = "")
        {
            var work = Path.Combine(Path.GetTempPath(), "cszxwork");
            Directory.CreateDirectory(work);

            await ProcessX.StartAsync("dotnet new console --force --no-restore true", work, null, null).ToTask();

            var builder = new StringBuilder();
            try
            {
                await foreach (var item in ProcessX.StartAsync($"dotnet add package \"{name}\"" + (version != "" ? $" -v {version}" : ""), work, null, null))
                    builder.AppendLine(item);
            }
            catch (ProcessErrorException)
            {
                Console.Write(builder);
                Directory.Delete(work, true);
                return false;
            }

            var csproj = XDocument.Load(Path.Combine(work, "cszxwork.csproj"));
            var packageNode = csproj.Descendants("PackageReference").FirstOrDefault(pr => pr.Attribute("Include")?.Value == name);
            if (packageNode == null)
            {
                Console.Write(builder);
                Directory.Delete(work, true);
                return false;
            }

            Directory.Delete(work, true);
            var find = PackageRefList.FirstOrDefault(@ref => @ref.Name == name);
            if (find != null)
            {
                find.Version = packageNode.Attribute("Version")?.Value ?? throw new InvalidOperationException();
                Console.WriteLine($"Success Update Package [name : {PackageRefList.Last().Name} version : {find.Version}]");
            }
            else
            {
                PackageRefList.Add(new PackageRef
                {
                    Name = name,
                    Version = packageNode.Attribute("Version")?.Value ?? throw new InvalidOperationException()
                });
                Console.WriteLine($"Success Add Package [name : {PackageRefList.Last().Name} version : {PackageRefList.Last().Version}]");
            }

            return true;
        }

        public void RemovePackageRef(string name)
        {
            var find = PackageRefList.FirstOrDefault(@ref => @ref.Name == name);
            if (find == null)
            {
                Console.Error.WriteLine($"{name} is not found");
                return;
            }
            PackageRefList.Remove(find);
        }

        public bool AddProjectRef(string projectPath)
        {
            projectPath = Path.GetFullPath(projectPath);
            projectPath = Path.GetRelativePath(Environment.CurrentDirectory, projectPath);
            
            if (!File.Exists(projectPath))
            {
                Console.WriteLine($"{projectPath} is not found");
                return false;
            }

            var find = ProjectRefList.FirstOrDefault(@ref => Path.GetFullPath(@ref.ProjectPath) == Path.GetFullPath(projectPath));
            if (find != null) return true;
            ProjectRefList.Add(new ProjectRef
            {
                ProjectPath = projectPath
            });
            return true;
        }

        public void RemoveProjectRef(string projectPath)
        {
            var find = ProjectRefList.FirstOrDefault(@ref => Path.GetFullPath(@ref.ProjectPath) == Path.GetFullPath(projectPath));
            if (find == null)
            {
                Console.Error.WriteLineAsync($"{projectPath} is nod found");
                return;
            }
            ProjectRefList.Remove(find);
        }

        public void WriteList(TextWriter stream)
        {
            void WritePackageImpl(Settings settings)
            {
                stream.WriteLine("Package List");
                while (true)
                {
                    if (settings.PackageRefList.Any())
                    {
                        stream.WriteLine("  " + settings.FilePath);
                        foreach (var @ref in settings.PackageRefList)
                        {
                            stream.WriteLine($"    {@ref.Name} {@ref.Version}");
                        }
                    }

                    if (settings.ChildSettings != null)
                    {
                        settings = settings.ChildSettings;
                        continue;
                    }

                    break;
                }
            }

            void WriteProjectImpl(Settings settings)
            {
                stream.WriteLine("Project List");
                while (true)
                {
                    if (settings.ProjectRefList.Any())
                    {
                        stream.WriteLine("  " + settings.FilePath);
                        foreach (var @ref in settings.ProjectRefList)
                        {
                            stream.WriteLine($"    {@ref.ProjectPath}");
                        }
                    }

                    if (settings.ChildSettings != null)
                    {
                        settings = settings.ChildSettings;
                        continue;
                    }

                    break;
                }
            }

            void WriteCsImpl(Settings settings)
            {
                stream.WriteLine("Cs List");
                while (true)
                {
                    if (settings.CsRefList.Any())
                    {
                        stream.WriteLine("  " + settings.FilePath);
                        foreach (var @ref in settings.CsRefList)
                        {
                            stream.WriteLine($"    {@ref.FilePath}");
                        }
                    }

                    if (settings.ChildSettings != null)
                    {
                        settings = settings.ChildSettings;
                        continue;
                    }

                    break;
                }
            }

            WritePackageImpl(this);
            stream.WriteLine();
            WriteProjectImpl(this);
            stream.WriteLine();
            WriteCsImpl(this);
        }

        public string Serialize()
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true
            };
            return JsonSerializer.Serialize(this, options);
        }

        public static Settings Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Settings>(json) ?? 
                   throw new InvalidOperationException($"Failed Deserialized Settings\n{json}");
        }

        public void SaveCurrentSettings()
        {
            if (!PackageRefList.Any() && !ProjectRefList.Any() && !CsRefList.Any())
            {
                if(File.Exists(SettingsName))
                    File.Delete(SettingsName);
            }
            else
            {
                File.WriteAllText(SettingsName, Serialize());
            }
        }
        public static Settings CreateOrLoadCurrentSettings()
        {
            return File.Exists(SettingsName) ?
                Deserialize(File.ReadAllText(SettingsName)) : new Settings();
        }

        public static Settings? GetRootSettings(string currentDirectory)
        {
            Settings? result = null;
            Settings? currentSettings = null;

            currentDirectory = Path.GetFullPath(currentDirectory) ??
                               throw new InvalidOperationException("currentDirectory is not found");
            while (true)
            {
                var path = Path.Combine(currentDirectory, SettingsName);
                if (File.Exists(path))
                {
                    var work = Deserialize(File.ReadAllText(path));
                    work.FilePath = path;

                    if (currentSettings != null) 
                        currentSettings.ChildSettings = work;
                    currentSettings = work;
                    result ??= work;
                }

                var parentDir = Directory.GetParent(currentDirectory);
                if (parentDir != null)
                {
                    currentDirectory = parentDir.FullName;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        public Settings FixSettings()
        {
            var result = new Settings();

            var currentSettings = this;
            while (currentSettings != null)
            {
                foreach (var packageRef in currentSettings.PackageRefList)
                {
                    if (result.PackageRefList.All(@ref => @ref.Name != packageRef.Name))
                    {
                        result.PackageRefList.Add(packageRef);
                    }
                }

                foreach (var projectRef in currentSettings.ProjectRefList)
                {
                    var fullPath = Path.Combine(Path.GetDirectoryName(currentSettings.FilePath) ??
                                                throw new InvalidOperationException(), projectRef.ProjectPath);

                    if (result.ProjectRefList.All(@ref => @ref.ProjectPath != fullPath))
                    {
                        result.ProjectRefList.Add(new ProjectRef {ProjectPath = fullPath});
                    }
                }

                foreach (var csRef in currentSettings.CsRefList)
                {
                    var fullPath = Path.Combine(Path.GetDirectoryName(currentSettings.FilePath) ??
                                                throw new InvalidOperationException(), csRef.FilePath);

                    if (result.CsRefList.All(@ref => @ref.FilePath != fullPath))
                    {
                        result.CsRefList.Add(new CsRef { FilePath = fullPath });
                    }
                }

                currentSettings = currentSettings.ChildSettings;
            }

            return result;
        }
    }
}
