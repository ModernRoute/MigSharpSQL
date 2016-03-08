using System;
using System.IO;
using System.Reflection;

namespace MigSharpSQL.Test.Helpers
{
    static class AssemblyExtensions
    {
        public static string GetDirectory(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            return Path.GetDirectoryName(path);
        }
    }
}
