using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Diagnostics;
using Zx;

namespace CSharpZxScript
{
    public class ScriptRunner
    {
        private const string ProjectName = "Run";
        private const string LoadPackageStart = "//load package ";

        private string SlnPath => Path.Combine(GetProjectPath(), ProjectName + ".sln");
        private string CsProjPath => Path.Combine(GetProjectPath(), ProjectName + ".csproj");
        private readonly string _rawFilePath;
        private readonly string _filePath;

        public ScriptRunner(string filePath)
        {
            if (Path.HasExtension(filePath))
            {
                _rawFilePath = Path.GetFullPath(filePath);
            }
            else
            {
                if (File.Exists(filePath + ".cszx"))
                {
                    _rawFilePath = Path.GetFullPath(filePath + ".cszx");
                }
                else if (File.Exists(filePath + ".cs"))
                {
                    _rawFilePath = Path.GetFullPath(filePath + ".cs");
                }
                else
                {
                    throw new IOException($"File not found. {filePath}");
                }
            }

            _filePath = _rawFilePath;
        }

        public static void ResetWork()
        {
            var work = Path.Combine(Path.GetTempPath(), "work");
            Directory.Delete(work, true);
        }

        private string GetProjectPath()
        {
            var work = Path.Combine(Path.GetTempPath(), "work");
            Directory.CreateDirectory(work);
            var project = Path.Combine(work, Path.GetFileNameWithoutExtension(_filePath));
            Directory.CreateDirectory(project);
            return project;
        }

        private async Task CreateProject(string targetFrameWork, string processXVersion)
        {
            var workPath = GetProjectPath();
            Directory.CreateDirectory(workPath);

            // Load All Settings
            var slnProjectRef = new StringBuilder();
            var packageRef = new StringBuilder();
            var projectRef = new StringBuilder();
            var csRef = new StringBuilder();
            var rootSettings = Settings.GetRootSettings(Path.GetDirectoryName(_rawFilePath) ??
                                     throw new InvalidOperationException("Could not get the directory from the file path"));
            if (rootSettings != null)
            {
                var setting = rootSettings.FixSettings();

                foreach (var @ref in setting.PackageRefList)
                {
                    packageRef.Append($"    <PackageReference Include=\"{@ref.Name}\" Version=\"{@ref.Version}\" />\n");
                }

                foreach (var @ref in setting.ProjectRefList)
                {
                    slnProjectRef.Append($"Project(\"{Guid.NewGuid():B}\") = \"{Path.GetFileNameWithoutExtension(@ref.ProjectPath)}\", \"{@ref.ProjectPath}\", \"{Guid.NewGuid():B}\"\nEndProject\n");
                    projectRef.Append($"    <ProjectReference Include=\"{@ref.ProjectPath}\"/>\n");
                }

                foreach (var @ref in setting.CsRefList)
                {
                    csRef.Append($"    <Compile Include=\"{@ref.FilePath}\"/>");
                }
            }

            var sln = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{Guid.NewGuid():B}"") = ""Run"", ""Run.csproj"", ""{Guid.NewGuid():B}""
EndProject
{slnProjectRef}
";
            await File.WriteAllTextAsync(SlnPath, sln);

            var csproj = $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{targetFrameWork}</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""{_filePath}""/>
{csRef}
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""ProcessX"" Version =""{processXVersion}"" />
{packageRef}
  </ItemGroup>

  <ItemGroup>
{projectRef}
  </ItemGroup>
</Project>
";
            await File.WriteAllTextAsync(CsProjPath, csproj);


            var csFile = await File.ReadAllLinesAsync(_rawFilePath);
            var defaultCd = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = workPath;

                // load package format at [//load package "ProcessX"]
                foreach (var line in csFile)
                {
                    if (!line.StartsWith(LoadPackageStart))
                        continue;
                    try
                    {
                        var substring = line.Substring(LoadPackageStart.Length + 1);
                        await $"dotnet add package {substring.Split(' ').First()}";
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            finally
            {
                Environment.CurrentDirectory = defaultCd;
            }
        }

        private async Task CreateVsLaunchSettings(string currentDir)
        {
            var propertiesPath = Path.Combine(GetProjectPath(), "Properties");
            Directory.CreateDirectory(propertiesPath);

            var launchSettings = $@"
{{
  ""profiles"": {{
    ""CSharpScript"": {{
      ""commandName"": ""Project"",
      ""workingDirectory"": ""{currentDir.Replace("\\", "\\\\")}""
    }}
  }}
}}
";
            await File.WriteAllTextAsync(Path.Combine(propertiesPath, "launchSettings.json"), launchSettings);
        }

        public async Task CreateEnv(string targetFrameWork, string processXVersion)
        {
            await CreateProject(targetFrameWork, processXVersion);

            // Set Current Directory
            var cd = Path.GetDirectoryName(_rawFilePath) ?? throw new InvalidOperationException($"Current directory is null {_filePath}");
            Environment.CurrentDirectory = cd;
            // Visual Studio Settings
            await CreateVsLaunchSettings(cd);
            // TODO Support Other IDE
        }

        public async Task<int> Run(string[]? args)
        {
            var source = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                Console.WriteLine("Ctrl+C");
                eventArgs.Cancel = true;

                source.Cancel();
            };

            var workPath = GetProjectPath();
            var exePath = Path.Combine(workPath, "bin");
            var oldCsPath = Path.Combine(exePath, "old.cs");

            var needBuild = true;
            var newFile = await File.ReadAllTextAsync(_filePath, source.Token);
            if (File.Exists(oldCsPath))
            {
                var oldFile = await File.ReadAllTextAsync(oldCsPath, source.Token);
                if (oldFile == newFile)
                    needBuild = false;
            }

            if (needBuild)
            {
                var builder = new StringBuilder();
                try
                {
                    var buildOutput = await ProcessX.StartAsync($"dotnet build \"{CsProjPath}\" -c Release -o \"{exePath}\"").ToTask(source.Token);
                    foreach (var s in buildOutput)
                    {
                        builder.AppendLine(s);
                    }
                }
                catch (ProcessErrorException ex)
                {
                    Console.WriteLine(builder);
                    foreach (var s in ex.ErrorOutput)
                    {
                        Console.WriteLine(s);
                    }
                    return ex.ExitCode;
                }
                await File.WriteAllTextAsync(oldCsPath, newFile, source.Token);
            }

            var p = Process.Start(Path.Combine(exePath, ProjectName), args ?? ArraySegment<string>.Empty);
            await p.WaitForExitAsync(source.Token);
            return p.ExitCode;
        }

        public void Edit()
        {
            _ = Env.run($"{SlnPath}");
        }
    }
}