using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpZxScript
{
    public class CurrentDirectoryHelper : IDisposable
    {
        private readonly string _beforeCurrentDirectory;
        public CurrentDirectoryHelper(string currentDirectory)
        {
            _beforeCurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.Combine(_beforeCurrentDirectory, currentDirectory);
        }

        public void Dispose()
        {
            Environment.CurrentDirectory = _beforeCurrentDirectory;
        }
    }
}
