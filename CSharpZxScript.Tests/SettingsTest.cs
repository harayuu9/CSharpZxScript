using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Diagnostics;
using Xunit;

namespace CSharpZxScript.Tests
{
    public class SettingsTest : IDisposable
    {
        public SettingsTest()
        {
            if (Directory.Exists("work"))
                Directory.Delete("work", true);
            Directory.CreateDirectory("work");

            CreateTestFile().Wait();
        }

        public void Dispose()
        {
            Directory.Delete("work", true);
        }

        private static async Task CreateTestFile()
        {
            Directory.CreateDirectory("work/lib");
            Directory.CreateDirectory("work/lib/Test");
            await ProcessX.StartAsync("dotnet new classlib", "work/lib/Test", null, null).ToTask();

            Directory.CreateDirectory("work/lib/Test2");
            await ProcessX.StartAsync("dotnet new classlib", "work/lib/Test2", null, null).ToTask();
        }

        private static async Task CreateTestData(Settings settings, string dir = "./", string version = "12.0.3")
        {
            Assert.True(await settings.AddPackageRef("Newtonsoft.Json", version));
            Assert.True(settings.AddProjectRef(dir + "lib/Test/Test.csproj"));
            settings.CsRefList.Add(new Settings.CsRef
            {
                FilePath = "File"
            });
        }

        [Fact]
        public async Task CreateLoadSaveTest()
        {
            using var cd = new CurrentDirectoryHelper("work");
            var newData = Settings.CreateOrLoadCurrentSettings();
            Assert.Empty(newData.PackageRefList);
            Assert.Empty(newData.ProjectRefList);
            Assert.Empty(newData.CsRefList);

            await CreateTestData(newData);
            newData.SaveCurrentSettings();

            newData = Settings.CreateOrLoadCurrentSettings();
            Assert.True(newData.PackageRefList.Count == 1);
            Assert.True(newData.ProjectRefList.Count == 1);
            Assert.True(newData.CsRefList.Count == 1);
        }

        [Fact]
        public async Task GetRootSettingsTest()
        {
            Settings newData;
            using (new CurrentDirectoryHelper("work"))
            {
                newData = Settings.CreateOrLoadCurrentSettings();
                await CreateTestData(newData);
                newData.SaveCurrentSettings();

                Directory.CreateDirectory("dir1");

                using (new CurrentDirectoryHelper("dir1"))
                {
                    newData = Settings.CreateOrLoadCurrentSettings();
                    await CreateTestData(newData, "../");
                    newData.SaveCurrentSettings();
                }
            }

            newData = Settings.GetRootSettings("work");
            Assert.NotNull(newData);
            Assert.Null(newData.ChildSettings);

            newData = Settings.GetRootSettings("work/dir1");
            Assert.NotNull(newData);
            Assert.NotNull(newData.ChildSettings);

            Assert.Null(newData.ChildSettings.ChildSettings);
        }

        [Fact]
        public async Task FixSettingsTest()
        {
            Settings newData;
            using (new CurrentDirectoryHelper("work"))
            {
                newData = Settings.CreateOrLoadCurrentSettings();
                await CreateTestData(newData);
                newData.SaveCurrentSettings();

                Directory.CreateDirectory("dir1");

                using (new CurrentDirectoryHelper("dir1"))
                {
                    newData = Settings.CreateOrLoadCurrentSettings();
                    await CreateTestData(newData, "../", "12.0.2");
                    newData.PackageRefList.Add(new Settings.PackageRef
                    {
                        Name = "Hoge",
                        Version = "1.0.0"
                    });
                    Assert.True(newData.AddProjectRef("../lib/Test2/Test2.csproj"));
                    newData.CsRefList.Add(new Settings.CsRef
                    {
                        FilePath = "Hoge"
                    });
                    newData.SaveCurrentSettings();
                }
            }

            newData = Settings.GetRootSettings("work");
            Assert.NotNull(newData);
            var fixSettings = newData.FixSettings();
            Assert.True(fixSettings.PackageRefList.Count == 1);
            Assert.True(fixSettings.PackageRefList.First().Version == "12.0.3");

            newData = Settings.GetRootSettings("work/dir1");
            Assert.NotNull(newData);
            fixSettings = newData.FixSettings();
            Assert.True(fixSettings.PackageRefList.Count == 2);
            Assert.True(fixSettings.ProjectRefList.Count == 2);
            Assert.True(fixSettings.CsRefList.Count == 3);
            Assert.True(fixSettings.PackageRefList.First().Version == "12.0.2");
        }
    }
}