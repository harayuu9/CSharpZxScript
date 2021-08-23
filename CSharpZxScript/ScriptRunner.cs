using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Diagnostics;
using Zx;

#nullable enable

namespace CSharpZxScript
{
    public static class ScriptRunner
    {
        private const string ProjectName = "Run";
        private static string SlnPath => Path.Combine(GetWorkPath(), ProjectName + ".sln");
        private static string CsProjPath => Path.Combine(GetWorkPath(), ProjectName + ".csproj");

        private static string GetWorkPath()
        {
            Assembly myAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Assembly is not found");
            return Path.Combine(
                Path.GetDirectoryName(myAssembly.Location) ?? throw new InvalidOperationException($"Assembly directory is not found ({myAssembly.Location})"),
                "work");
        }

        private static async Task CreateProject(string filePath, string targetFrameWork, string processXVersion)
        {
            var workPath = GetWorkPath();
            Directory.CreateDirectory(workPath);

            // Load All Settings
            var slnProjectRef = new StringBuilder();
            var packageRef = new StringBuilder();
            var projectRef = new StringBuilder();
            var csRef = new StringBuilder();
            var rootSettings = Settings.GetRootSettings(Path.GetDirectoryName(filePath) ??
                                     throw new InvalidOperationException("Could not get the directory from the file path"));
            if (rootSettings != null)
            {
                var setting = rootSettings.FixSettings();

                foreach (var @ref in setting.PackageRefList)
                {
                    packageRef.AppendFormat("    <PackageReference Include=\"{0}\" Version=\"{1}\" />\n", @ref.Name, @ref.Version);
                }

                foreach (var @ref in setting.ProjectRefList)
                {
                    slnProjectRef.AppendFormat("Project(\"{0:B}\") = \"{1}\", \"{2}\", \"{3:B}\"\nEndProject\n",
                        Guid.NewGuid(), Path.GetFileNameWithoutExtension(@ref.ProjectPath),
                        @ref.ProjectPath, Guid.NewGuid());
                    projectRef.AppendFormat("    <ProjectReference Include=\"{0}\"/>\n", @ref.ProjectPath);
                }

                foreach (var @ref in setting.CsRefList)
                {
                    csRef.AppendFormat("    <Compile Include=\"{0}\"/>", @ref.FilePath);
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
    <Compile Include=""{Path.GetFullPath(filePath)}""/>
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
        }

        private static async Task CreateVsLaunchSettings(string currentDir)
        {
            var propertiesPath = Path.Combine(GetWorkPath(), "Properties");
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

        public static async Task CreateEnv(string filePath, string targetFrameWork, string processXVersion)
        {
            var fileFullPath = Path.GetFullPath(filePath);

            if (!File.Exists(fileFullPath))
            {
                await File.WriteAllTextAsync(fileFullPath, "using Zx;\nusing static Zx.Env;\n\nawait $\"echo {\"Hello World\"}\";");
            }
            await CreateProject(fileFullPath, targetFrameWork, processXVersion);

            // Set Current Directory
            var cd = Path.GetDirectoryName(fileFullPath) ?? throw new InvalidOperationException($"Current directory is null {fileFullPath}");
            Environment.CurrentDirectory = cd;
            // Visual Studio Settings
            await CreateVsLaunchSettings(cd);
            // TODO Support Other IDE
        }

        public static async Task<int> Run(string filePath)
        {
            var workPath = GetWorkPath();
            var exePath = Path.Combine(workPath, "bin");
            var oldCsPath = Path.Combine(exePath, "old.cs");

            var needBuild = true;
            var newFile = await File.ReadAllTextAsync(filePath);
            if (File.Exists(oldCsPath))
            {
                var oldFile = await File.ReadAllTextAsync(oldCsPath);
                if (oldFile == newFile)
                    needBuild = false;
            }

            if (needBuild)
            {
                await ProcessX.StartAsync($"dotnet build \"{CsProjPath}\" -c Release -o \"{exePath}\"").ToTask();
                await File.WriteAllTextAsync(oldCsPath, newFile);
            }

            try
            {
                await foreach (var item in ProcessX.StartAsync($"\"{Path.Combine(exePath, ProjectName)}\""))
                    Console.WriteLine(item);
            }
            catch (ProcessErrorException ex)
            {
                return ex.ExitCode;
            }

            return 0;
        }

        public static void Edit()
        {
            Env.run($"{SlnPath}");
        }
    }
}