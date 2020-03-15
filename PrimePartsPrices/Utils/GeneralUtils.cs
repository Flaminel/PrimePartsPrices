using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PrimePartsPrices.Utils
{
    public static class GeneralUtils
    {
        private static readonly string _currentAssemblyPath;

        static GeneralUtils()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            _currentAssemblyPath = Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Gets the path to the current assembly
        /// </summary>
        /// <returns>The path to the current assembly</returns>
        public static string GetAssemblyPath()
        {
            return _currentAssemblyPath;
        }

        /// <summary>
        /// Deletes the given files
        /// </summary>
        /// <param name="files">The file paths of the files to be deleted</param>
        public static void DeleteFiles(List<string> files)
        {
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// Deletes the given file
        /// </summary>
        /// <param name="file">The file path to the file to be deleted</param>
        public static void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
