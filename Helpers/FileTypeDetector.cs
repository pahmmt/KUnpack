using System.Text;
using Ude;

namespace KUnpack.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ phát hiện loại tệp dựa trên chữ ký và nội dung
    /// </summary>
    public static class FileTypeDetector
    {
        private const int TextDetectionCheckLength = 256;
        private const int FileTypePreviewLength = 100;

        public static string DetectFileType(byte[] data)
        {
            if (data == null || data.Length < 4)
                return "Unknown";

            string extension = GuessExtension(data);

            return extension switch
            {
                ".spr" => "SPR (Sprite)",
                ".png" or ".jpg" or ".bmp" or ".ico" or ".cur" or ".tga" => extension[1..].ToUpper() + " (Image)",
                ".wav" or ".mp3" or ".ogg" => extension[1..].ToUpper() + " (Audio)",
                ".ini" or ".xml" or ".txt" => extension[1..].ToUpper() + " (Text)",
                ".bin" => LooksLikeText(data) ? "Text" : "Binary",
                _ => extension[1..].ToUpper()
            };
        }

        public static string GetExtensionFromFileType(string detectedType)
        {
            if (detectedType.Contains("("))
            {
                string ext = detectedType.Split('(')[0].Trim().ToLower();
                if (!string.IsNullOrEmpty(ext))
                    return "." + ext;
            }

            if (detectedType == "Text")
                return ".txt";

            return ".dat";
        }

        private static string GuessExtension(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
                return ".bin";

            if (IsSpriteFile(data))
                return ".spr";

            if (IsPngFile(data))
                return ".png";

            if (IsJpegFile(data))
                return ".jpg";

            if (IsBmpFile(data))
                return ".bmp";

            if (LooksLikeIcon(data))
                return data[2] == 0x01 ? ".ico" : ".cur";

            if (LooksLikeTga(data))
                return ".tga";

            if (LooksLikeWav(data))
                return ".wav";

            if (LooksLikeMp3(data))
                return ".mp3";

            if (LooksLikeOgg(data))
                return ".ogg";

            if (LooksLikeText(data))
                return GuessTextExtension(data);

            return ".bin";
        }

        public static bool IsSpriteFile(ReadOnlySpan<byte> data) =>
            data.Length >= 3 && data[0] == 'S' && data[1] == 'P' && data[2] == 'R';

        public static bool IsCursorFile(ReadOnlySpan<byte> data) =>
            data.Length >= 6 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x02 && data[3] == 0x00;

        private static bool IsPngFile(ReadOnlySpan<byte> data) =>
            data.Length >= 8 && data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G' &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;

        private static bool IsJpegFile(ReadOnlySpan<byte> data) =>
            data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;

        private static bool IsBmpFile(ReadOnlySpan<byte> data) =>
            data.Length >= 2 && data[0] == 'B' && data[1] == 'M';

        private static bool LooksLikeIcon(ReadOnlySpan<byte> data)
        {
            if (data.Length < 22)
                return false;

            if (data[0] != 0x00 || data[1] != 0x00 || data[3] != 0x00)
                return false;

            if (data[2] is not (0x01 or 0x02))
                return false;

            ushort entryCount = (ushort)(data[4] | (data[5] << 8));
            if (entryCount == 0)
                return false;

            int entryOffset = 6;
            if (data.Length < entryOffset + 16)
                return false;

            uint imageOffset = (uint)(data[entryOffset + 12] |
                                      (data[entryOffset + 13] << 8) |
                                      (data[entryOffset + 14] << 16) |
                                      (data[entryOffset + 15] << 24));
            if (imageOffset >= data.Length || data.Length < imageOffset + 8)
                return false;

            bool bmpPayload = data[(int)imageOffset] == 0x28 && data[(int)imageOffset + 1] == 0x00;
            bool pngPayload = data[(int)imageOffset] == 0x89 && data[(int)imageOffset + 1] == 'P';

            return bmpPayload || pngPayload;
        }

        private static bool LooksLikeTga(ReadOnlySpan<byte> data)
        {
            if (data.Length < 18)
                return false;

            byte imageType = data[2];
            if ((imageType is >= 1 and <= 3) || (imageType is >= 9 and <= 11))
            {
                byte colorMapType = data[1];
                if (colorMapType is 0 or 1)
                    return true;
            }

            return false;
        }

        private static bool LooksLikeWav(ReadOnlySpan<byte> data)
        {
            if (data.Length < 12)
                return false;

            return data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
                   data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E';
        }

        private static bool LooksLikeMp3(ReadOnlySpan<byte> data)
        {
            if (data.Length < 3)
                return false;

            if (data[0] == 'I' && data[1] == 'D' && data[2] == '3')
                return true;

            return data[0] == 0xFF && (data[1] & 0xE0) == 0xE0;
        }

        private static bool LooksLikeOgg(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
                return false;

            return data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S';
        }

        private static bool LooksLikeText(ReadOnlySpan<byte> data)
        {
            int checkLen = Math.Min(data.Length, 1024);
            if (checkLen == 0)
                return false;

            var detector = new CharsetDetector();

            using (var ms = new MemoryStream(data[..checkLen].ToArray()))
            {
                detector.Feed(ms);
                detector.DataEnd();
            }

            return detector.Charset != null && detector.Confidence > 0.5;
        }

        private static string GuessTextExtension(ReadOnlySpan<byte> data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                return ".txt";

            int previewLength = Math.Min(data.Length, FileTypePreviewLength);
            string preview = Encoding.ASCII.GetString(data[..previewLength]);

            if (preview.Contains('[') && preview.Contains(']'))
                return ".ini";

            if (preview.Contains("<?xml", StringComparison.OrdinalIgnoreCase))
                return ".xml";

            return ".txt";
        }
    }
}