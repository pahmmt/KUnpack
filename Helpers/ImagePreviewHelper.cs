namespace KUnpack.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ hiển thị xem trước ảnh, bao gồm cả tệp con trỏ (cursor)
    /// </summary>
    public static class ImagePreviewHelper
    {
        private const int HotspotMarkerSize = 6;
        private const int HotspotDotSize = 3;

        public static void ClearPictureBox(PictureBox pictureBox)
        {
            if (pictureBox.Image != null)
            {
                var oldImage = pictureBox.Image;
                pictureBox.Image = null;
                oldImage.Dispose();
            }
        }

        public static byte[] ConvertCursorToIcon(byte[] data)
        {
            byte[] iconData = new byte[data.Length];
            Array.Copy(data, iconData, data.Length);
            iconData[2] = 0x01; // Đổi type từ CUR (0x02) sang ICO (0x01)
            return iconData;
        }

        public static Bitmap CreateCursorPreviewBitmap(Bitmap cursorImage, int hotspotX, int hotspotY)
        {
            var previewBitmap = new Bitmap(cursorImage.Width, cursorImage.Height);
            using (var graphics = Graphics.FromImage(previewBitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(cursorImage, 0, 0);

                if (hotspotX < cursorImage.Width && hotspotY < cursorImage.Height)
                {
                    DrawHotspotMarker(graphics, hotspotX, hotspotY, cursorImage.Width, cursorImage.Height);
                }
            }
            return previewBitmap;
        }

        private static void DrawHotspotMarker(Graphics graphics, int x, int y, int maxWidth, int maxHeight)
        {
            using (var pen = new Pen(Color.Red, 2))
            {
                int x1 = Math.Max(0, x - HotspotMarkerSize);
                int x2 = Math.Min(maxWidth - 1, x + HotspotMarkerSize);
                graphics.DrawLine(pen, x1, y, x2, y);

                int y1 = Math.Max(0, y - HotspotMarkerSize);
                int y2 = Math.Min(maxHeight - 1, y + HotspotMarkerSize);
                graphics.DrawLine(pen, x, y1, x, y2);

                int dotOffset = HotspotDotSize / 2;
                graphics.FillEllipse(Brushes.Red, x - dotOffset, y - dotOffset, HotspotDotSize, HotspotDotSize);
            }
        }

        public static Bitmap? LoadCursorAsBitmap(byte[] data, out ushort hotspotX, out ushort hotspotY)
        {
            hotspotX = 0;
            hotspotY = 0;

            if (data.Length < 22)
                return null;

            hotspotX = BitConverter.ToUInt16(data, 10);
            hotspotY = BitConverter.ToUInt16(data, 12);

            byte[] iconData = ConvertCursorToIcon(data);

            using (var ms = new MemoryStream(iconData))
            using (var icon = new Icon(ms))
            {
                return icon.ToBitmap();
            }
        }
    }
}