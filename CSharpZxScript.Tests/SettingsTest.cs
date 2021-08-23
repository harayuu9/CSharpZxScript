using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CSharpZxScript.Tests
{
    public class SettingsTest : IDisposable
    {
        public SettingsTest()
        {
            if(Directory.Exists("work"))
                Directory.Delete("work", true);
            Directory.CreateDirectory("work");
        }

        public void Dispose()
        {
            Directory.Delete("work", true);
        }

        private static void CreateTestData(Settings settings, string version = "1.0.0")
        {
            settings.PackageRefList.Add(new Settings.PackageRef
            {
                Name    = "Package",
                Version = version
            });
            settings.ProjectRefList.Add(new Settings.ProjectRef
            {
                ProjectPath = "Path"
            });
            settings.CsRefList.Add(new Settings.CsRef
            {
                FilePath = "File"
            });
        }

        [Fact]
        public void CreateLoadSaveTest()
        {
            var newData = Settings.CreateOrLoadCurrentSettings("work");
            Assert.Empty(newData.PackageRefList);
            Assert.Empty(newData.ProjectRefList);
            Assert.Empty(newData.CsRefList);

            CreateTestData(newData);
            newData.SaveCurrentSettings("work");

            newData = Settings.CreateOrLoadCurrentSettings("work");
            Assert.True(newData.PackageRefList.Count == 1);
            Assert.True(newData.ProjectRefList.Count == 1);
            Assert.True(newData.CsRefList.Count      == 1);
        }

        [Fact]
        public void GetRootSettingsTest()
        {
            var newData = Settings.CreateOrLoadCurrentSettings("work");
            CreateTestData(newData);
            newData.SaveCurrentSettings("work");

            Directory.CreateDirectory("work/dir1");
            newData = Settings.CreateOrLoadCurrentSettings("work/dir1");
            CreateTestData(newData);
            newData.SaveCurrentSettings("work/dir1");

            newData = Settings.GetRootSettings("work");
            Assert.NotNull(newData);
            Assert.Null(newData.ChildSettings);

            newData = Settings.GetRootSettings("work/dir1");
            Assert.NotNull(newData);
            Assert.NotNull(newData.ChildSettings);

            Assert.Null(newData.ChildSettings.ChildSettings);
        }

        [Fact]
        public void FixSettingsTest()
        {
            var newData = Settings.CreateOrLoadCurrentSettings("work");
            CreateTestData(newData, "1.0.2");
            newData.SaveCurrentSettings("work");

            Directory.CreateDirectory("work/dir1");
            newData = Settings.CreateOrLoadCurrentSettings("work/dir1");
            CreateTestData(newData, "10.0.5");
            newData.PackageRefList.Add(new Settings.PackageRef
            {
                Name    = "Hoge",
                Version = "1.0.0"
            });
            newData.ProjectRefList.Add(new Settings.ProjectRef
            {
                ProjectPath = "Hoge"
            });
            newData.CsRefList.Add(new Settings.CsRef
            {
                FilePath = "Hoge"
            });
            newData.SaveCurrentSettings("work/dir1");

            newData = Settings.GetRootSettings("work");
            Assert.NotNull(newData);
            var fixSettings = newData.FixSettings();
            Assert.True(fixSettings.PackageRefList.Count           == 1);
            Assert.True(fixSettings.PackageRefList.First().Version == "1.0.2");

            newData = Settings.GetRootSettings("work/dir1");
            Assert.NotNull(newData);
            fixSettings = newData.FixSettings();
            Assert.True(fixSettings.PackageRefList.Count           == 2);
            Assert.True(fixSettings.ProjectRefList.Count           == 3);
            Assert.True(fixSettings.CsRefList.Count                == 3);
            Assert.True(fixSettings.PackageRefList.First().Version == "10.0.5");
        }
    }
}