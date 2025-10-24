//---------------------------------------------------------------------------
// Sword3 Engine (c) 1999-2000 by Kingsoft
//
// File:	KFilePath.cs
// Date:	2000.08.08
// Code:	WangWei(Daphnis)
// Desc:	File Path Utility - Ported to C#
//---------------------------------------------------------------------------

namespace KUnpack.EngineSharp
{
    /// <summary>
    /// File Path Utility Class - Ported from C++ KFilePath
    /// </summary>
    public static class KFilePath
    {
        // Constants
        private const int MAXPATH = 260;

        // Static variables for path management
        private static string s_rootPath = "C:";        // Root path

        /// <summary>
        /// Set the root path of the application
        /// </summary>
        /// <param name="pathName">Path name, null to use current directory</param>
        public static void SetRootPath(string? pathName = null)
        {
            if (!string.IsNullOrEmpty(pathName))
            {
                s_rootPath = pathName;
            }
            else
            {
                s_rootPath = Directory.GetCurrentDirectory();
            }

            // Remove trailing '\' or '/'
            if (s_rootPath.Length > 0 && (s_rootPath.EndsWith("\\") || s_rootPath.EndsWith("/")))
            {
                s_rootPath = s_rootPath.Substring(0, s_rootPath.Length - 1);
            }
        }

        /// <summary>
        /// Get the root path of the application
        /// </summary>
        /// <returns>Root path</returns>
        public static string GetRootPath()
        {
            return s_rootPath;
        }

        /// <summary>
        /// Get the full path name of a file
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Full path</returns>
        public static string GetFullPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // File has full path (e.g., "C:\path\file")
            if (fileName.Length > 1 && fileName[1] == ':')
            {
                return fileName;
            }

            // File has partial path (e.g., "\path\file")
            if (fileName.StartsWith("\\") || fileName.StartsWith("/"))
            {
                return Path.Combine(s_rootPath, fileName);
            }

            // Relative path - combine with root path
            return Path.Combine(s_rootPath, fileName);
        }

        /// <summary>
        /// Check if file exists in pack or on hard disk
        /// </summary>
        /// <param name="fileName">Path name + file name</param>
        /// <returns>True if exists, false otherwise</returns>
        public static bool FileExists(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            try
            {
                string fullName = GetFullPath(fileName);
                return File.Exists(fullName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert file name to 32-bit hash ID
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>File name hash 32-bit ID</returns>
        public static uint FileName2Id(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return 0;

            uint id = 0;
            for (int i = 0; i < fileName.Length; i++)
            {
                char c = fileName[i];

                // For Linux path lookup
                if (c == '/')
                    c = '\\';

                id = (id + (uint)((i + 1) * c)) % 0x8000000b * 0xffffffef;
            }
            return (id ^ 0x12345678);
        }
    }
}