using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CSharpZxScript.Tests
{
    public class BasicScriptTest
    {
        [Fact]
        public async Task Basic()
        {
            const string scriptData = """
                                      using System;
                                      using static Zx.Env;

                                      log("Hello World", ConsoleColor.Blue);
                                      log(args[0], ConsoleColor.Red);
                                      """;
            const string filePath = "test.cszx";
            await File.WriteAllTextAsync(filePath, scriptData);

            try
            {
                var script = new ScriptRunner(filePath);
                await script.CreateEnv("net8.0", "1.5.3");
                var result = await script.Run(new[] { "Args0" });
                Assert.Equal(0, result);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task PortResult()
        {
            const string scriptData = """
                                      using System;
                                      using static Zx.Env;

                                      log("Hello World", ConsoleColor.Blue);
                                      log(args[0], ConsoleColor.Red);
                                      Environment.Exit(5);
                                      """;
            const string filePath = "test.cszx";
            await File.WriteAllTextAsync(filePath, scriptData);

            try
            {
                var script = new ScriptRunner(filePath);
                await script.CreateEnv("net8.0", "1.5.3");
                var result = await script.Run(new[] { "Args0" });
                Assert.NotEqual(0, result);
                Assert.Equal(5, result);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}