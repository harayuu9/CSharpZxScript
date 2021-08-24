using System;
using System.Diagnostics;
using System.IO;

namespace CSharpZxScript
{
    public static class ExePathUtil
    {
        public static string ExePath => Process.GetCurrentProcess().MainModule.FileName;
        public static string AssemblyDir => Path.GetDirectoryName(ExePath) ?? throw new InvalidOperationException($"Assembly directory is not found ({ExePath})");
    }
}
