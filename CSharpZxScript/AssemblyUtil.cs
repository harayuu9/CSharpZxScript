using System;
using System.IO;
using System.Reflection;

namespace CSharpZxScript
{
    public static class AssemblyUtil
    {
        public static string AssemblyPath
        {
            get
            {
                Assembly myAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Assembly is not found");
                var path = myAssembly.Location;
                if (Path.GetExtension(path) == ".dll")
                {
                    path = Path.ChangeExtension(path, ".exe");
                }
                return path;
            }
        } 

        public static string AssemblyDir => Path.GetDirectoryName(AssemblyPath) ?? throw new InvalidOperationException($"Assembly directory is not found ({AssemblyPath})");
    }
}
