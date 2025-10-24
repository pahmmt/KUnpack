using KUnpack.EngineSharp;

namespace KUnpack.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ cho các thao tác trích xuất file
    /// </summary>
    public static class ExtractionHelper
    {
        public static string GetFileNameForExtraction(XPackIndexInfo indexInfo, byte[] fileData, Dictionary<uint, string> idToPathMap)
        {
            if (idToPathMap.ContainsKey(indexInfo.uId))
            {
                return idToPathMap[indexInfo.uId];
            }

            string detectedType = FileTypeDetector.DetectFileType(fileData);
            string extension = FileTypeDetector.GetExtensionFromFileType(detectedType);
            return $"0x{indexInfo.uId:X8}{extension}";
        }

        public static bool ExtractSingleFile(XPackFile currentPack, List<XPackIndexInfo> fileIndexList, int index, string outputFolder, Dictionary<uint, string> idToPathMap)
        {
            if (currentPack == null)
                return false;

            XPackIndexInfo indexInfo = fileIndexList[index];

            byte[]? fileData = currentPack.ReadElemFileByIndex(index);
            if (fileData == null || fileData.Length == 0)
            {
                return false;
            }

            // Use GetFileNameForExtraction to get the proper filename with mapping support
            string fileName = GetFileNameForExtraction(indexInfo, fileData, idToPathMap);
            string fullPath = Path.Combine(outputFolder, fileName.Replace('/', '\\'));

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(fullPath, fileData);
            return true;
        }

        public static string FormatFileSize(long size)
        {
            if (size < 1024)
                return $"{size} B";
            else if (size < 1024 * 1024)
                return $"{size / 1024.0:F2} KB";
            else if (size < 1024 * 1024 * 1024)
                return $"{size / (1024.0 * 1024.0):F2} MB";
            else
                return $"{size / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        public static string GetCompressionMethodName(int method)
        {
            if (method == (int)XPACK_METHOD.TYPE_NONE)
                return "None";
            else if (method == (int)XPACK_METHOD.TYPE_UCL)
                return "UCL";
            else if (method == (int)XPACK_METHOD.TYPE_UPL_NEW)
                return "UPL New";
            else if (method == (int)XPACK_METHOD.TYPE_BZIP2)
                return "BZIP2";
            else if ((method & (int)XPACK_METHOD.TYPE_FRAME) != 0)
            {
                int baseMethod = method & (int)XPACK_METHOD.TYPE_METHOD_FILTER;
                string baseMethodName = GetCompressionMethodName(baseMethod);
                return $"Frame-based ({baseMethodName})";
            }
            else
                return $"Unknown (0x{method:X8})";
        }
    }
}