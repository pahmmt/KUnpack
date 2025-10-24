//---------------------------------------------------------------------------
// Sword3 Engine (c) 1999-2000 by Kingsoft
//
// File:	KSprite.cs
// Date:	2000.09.18
// Code:	WangWei(Daphnis),Wooy
// Desc:	Sprite Class
//---------------------------------------------------------------------------

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KUnpack.EngineSharp
{
    // Cấu trúc header của sprite
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPRHEAD
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Comment;   // Văn bản chú thích (SPR\0)

        public ushort Width;     // Chiều rộng hình ảnh
        public ushort Height;    // Chiều cao hình ảnh
        public ushort CenterX;   // Dịch chuyển ngang của trọng tâm
        public ushort CenterY;   // Dịch chuyển dọc của trọng tâm
        public ushort Frames;    // Tổng số frame
        public ushort Colors;    // Số màu
        public ushort Directions; // Số hướng
        public ushort Interval;  // Khoảng cách mỗi frame (đơn vị là game frame)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public ushort[] Reserved; // Trường dành riêng (để sử dụng sau)
    }

    public static class KSpriteConstants
    {
        public const int SPR_COMMENT_FLAG = 0x525053;  // 'SPR'
    }

    //---------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPROFFS
    {
        public uint Offset;      // Offset của mỗi frame
        public uint Length;      // Độ dài của mỗi frame
    }

    //---------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPRFRAME
    {
        public ushort Width;     // Chiều rộng tối thiểu của frame
        public ushort Height;    // Chiều cao tối thiểu của frame
        public ushort OffsetX;   // Dịch chuyển ngang (tương đối với góc trên bên trái của hình gốc)
        public ushort OffsetY;   // Dịch chuyển dọc (tương đối với góc trên bên trái của hình gốc)
        // Dữ liệu hình ảnh nén RLE
    }

    //---------------------------------------------------------------------------
    // Cấu trúc palette 24-bit
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KPAL24
    {
        public byte Red;         // Thành phần màu đỏ
        public byte Green;      // Thành phần màu xanh lá
        public byte Blue;       // Thành phần màu xanh dương
    }

    //---------------------------------------------------------------------------
    // Palette 16-bit - sử dụng ushort trực tiếp thay vì struct
    // public struct KPAL16 { public ushort Value; } - Đã thay thế bằng ushort

    //---------------------------------------------------------------------------
    // Lớp quản lý bộ nhớ đơn giản
    public class KMemClass : IDisposable
    {
        private byte[]? m_lpMemPtr;
        private uint m_lpMemLen;
        private bool _disposed = false;

        public KMemClass()
        {
            m_lpMemPtr = null;
            m_lpMemLen = 0;
        }

        ~KMemClass()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    Free();
                }
                _disposed = true;
            }
        }

        public byte[]? Alloc(uint dwSize)
        {
            Free();
            if (dwSize > 0)
            {
                m_lpMemPtr = new byte[dwSize];
                m_lpMemLen = dwSize;
            }
            return m_lpMemPtr;
        }

        public void Free()
        {
            m_lpMemPtr = null;
            m_lpMemLen = 0;
        }

        public byte[]? GetMemPtr()
        { return m_lpMemPtr; }
    }

    //---------------------------------------------------------------------------
    // KPakFile đã được loại bỏ - sử dụng FileStream trực tiếp
    //---------------------------------------------------------------------------

    //---------------------------------------------------------------------------
    // Hàm tiện ích để so sánh bộ nhớ
    public static class KMemUtils
    {
        public static bool g_MemComp(byte[] buffer1, byte[] buffer2, int count)
        {
            if (buffer1 == null || buffer2 == null || count <= 0)
                return false;

            for (int i = 0; i < count && i < buffer1.Length && i < buffer2.Length; i++)
            {
                if (buffer1[i] != buffer2[i])
                    return false;
            }
            return true;
        }

        public static void g_Pal24ToPal16(KPAL24[] pPal24, ushort[] pPal16, int nColors)
        {
            for (int i = 0; i < nColors; i++)
            {
                ushort red = (ushort)(pPal24[i].Red >> 4);
                ushort green = (ushort)(pPal24[i].Green >> 4);
                ushort blue = (ushort)(pPal24[i].Blue >> 4);
                pPal16[i] = (ushort)((red << 8) | (green << 4) | blue);
            }
        }
    }

    //---------------------------------------------------------------------------
    // Lớp Sprite chính
    public class KSprite : IDisposable
    {
        // Constants
        private const int MaxSpriteSize = 4096;

        private const int DefaultCellPadding = 8;
        private const int AlphaNibbleMask = 0x0F;
        private const int AlphaShift = 4;
        private const int AlphaScaleFactor = 17;

        private KMemClass m_Buffer = new KMemClass();
        private KMemClass m_Palette = new KMemClass();
        private KPAL24[]? m_pPal24;
        private ushort[]? m_pPal16;
        private SPROFFS[]? m_pOffset;
        private byte[]? m_pSprite;
        private int m_nWidth;
        private int m_nHeight;
        private int m_nCenterX;
        private int m_nCenterY;
        private int m_nFrames;
        private int m_nColors;
        private int m_nDirections;
        private int m_nInterval;
        private bool _disposed = false;

        //---------------------------------------------------------------------------
        // Hàm: KSprite
        // Chức năng: Hàm tạo
        // Tham số: void
        // Trả về: void
        //---------------------------------------------------------------------------
        public KSprite()
        {
            m_nWidth = 0;
            m_nHeight = 0;
            m_nCenterX = 0;
            m_nCenterY = 0;
            m_nFrames = 0;
            m_nColors = 0;
            m_nDirections = 1;
            m_nInterval = 1;
            m_pPal24 = null;
            m_pPal16 = null;
            m_pOffset = null;
            m_pSprite = null;
        }

        ~KSprite()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    Free();
                }
                _disposed = true;
            }
        }

        //---------------------------------------------------------------------------
        // Hàm: Load
        // Chức năng: Tải
        // Tham số: FileName tên file
        // Trả về: TRUE－thành công
        //          FALSE－thất bại
        //---------------------------------------------------------------------------
        public bool Load(string fileName)
        {
            FileStream? file = null;
            SPRHEAD header;

            try
            {
                // mở file
                file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                uint fileSize = (uint)file.Length;

                // cấp phát bộ nhớ
                if (m_Buffer.Alloc(fileSize) == null)
                    return false;

                byte[]? pTemp = m_Buffer.GetMemPtr();
                if (pTemp == null)
                    return false;

                // đọc dữ liệu từ file
                file.Read(pTemp, 0, (int)fileSize);

                // kiểm tra header file và thiết lập thành viên sprite
                header = ByteArrayToStructure<SPRHEAD>(pTemp);
                byte[] sprComment = { (byte)'S', (byte)'P', (byte)'R', 0 };
                if (!KMemUtils.g_MemComp(header.Comment, sprComment, 3))
                    return false;

                // lấy thông tin sprite
                m_nWidth = header.Width;
                m_nHeight = header.Height;
                m_nCenterX = header.CenterX;
                m_nCenterY = header.CenterY;
                m_nFrames = header.Frames;
                m_nColors = header.Colors;
                m_nDirections = header.Directions;
                m_nInterval = header.Interval;

                // thiết lập con trỏ palette
                int offset = Marshal.SizeOf<SPRHEAD>();
                m_pPal24 = new KPAL24[m_nColors];
                for (int i = 0; i < m_nColors; i++)
                {
                    int palOffset = offset + i * Marshal.SizeOf<KPAL24>();
                    if (palOffset + Marshal.SizeOf<KPAL24>() <= pTemp.Length)
                    {
                        byte[] palBytes = new byte[Marshal.SizeOf<KPAL24>()];
                        Array.Copy(pTemp, palOffset, palBytes, 0, palBytes.Length);
                        m_pPal24[i] = ByteArrayToStructure<KPAL24>(palBytes);
                    }
                }

                // thiết lập con trỏ offset
                offset += m_nColors * Marshal.SizeOf<KPAL24>();
                m_pOffset = new SPROFFS[m_nFrames];
                for (int i = 0; i < m_nFrames; i++)
                {
                    int offOffset = offset + i * Marshal.SizeOf<SPROFFS>();
                    if (offOffset + Marshal.SizeOf<SPROFFS>() <= pTemp.Length)
                    {
                        byte[] offBytes = new byte[Marshal.SizeOf<SPROFFS>()];
                        Array.Copy(pTemp, offOffset, offBytes, 0, offBytes.Length);
                        m_pOffset[i] = ByteArrayToStructure<SPROFFS>(offBytes);
                    }
                }

                // thiết lập con trỏ sprite
                offset += m_nFrames * Marshal.SizeOf<SPROFFS>();
                m_pSprite = new byte[pTemp.Length - offset];
                Array.Copy(pTemp, offset, m_pSprite, 0, m_pSprite.Length);

                // tạo bảng màu
                MakePalette();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                file?.Close();
            }
        }

        //---------------------------------------------------------------------------
        // Hàm: Free
        // Chức năng: Giải phóng
        // Tham số: void
        // Trả về: void
        //---------------------------------------------------------------------------
        public void Free()
        {
            m_Buffer.Free();
            m_Palette.Free();
            m_nFrames = 0;
            m_nColors = 0;
            m_pPal24 = null;
            m_pPal16 = null;
            m_pOffset = null;
            m_pSprite = null;
        }

        //---------------------------------------------------------------------------
        // Hàm: MakePalette
        // Chức năng: Tạo palette
        // Tham số: void
        // Trả về: void
        //---------------------------------------------------------------------------
        public void MakePalette()
        {
            m_Palette.Alloc((uint)(m_nColors * sizeof(ushort)));
            byte[]? paletteBytes = m_Palette.GetMemPtr();
            if (paletteBytes == null)
                return;

            m_pPal16 = new ushort[m_nColors];
            if (m_pPal24 != null && m_pPal16 != null)
            {
                KMemUtils.g_Pal24ToPal16(m_pPal24, m_pPal16, m_nColors);
            }
        }

        //---------------------------------------------------------------------------
        // Hàm: RenderSpriteSheet
        // Chức năng: Render tất cả frames thành sprite sheet (grid layout)
        // Tham số: maxColumns - số cột tối đa (0 = auto tính)
        //          cellPadding - khoảng cách giữa các ô
        // Trả về: Bitmap chứa tất cả frames
        //---------------------------------------------------------------------------
        public Bitmap RenderSpriteSheet(int maxColumns = 0, int cellPadding = DefaultCellPadding)
        {
            if (m_nFrames == 0)
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);

            int columns = CalculateColumns(maxColumns);
            int rows = (int)Math.Ceiling(m_nFrames / (double)columns);

            int cellWidth = Math.Max(1, m_nWidth);
            int cellHeight = Math.Max(1, m_nHeight);
            int sheetWidth = columns * cellWidth + Math.Max(0, (columns + 1) * cellPadding);
            int sheetHeight = rows * cellHeight + Math.Max(0, (rows + 1) * cellPadding);

            var sheet = new Bitmap(sheetWidth, sheetHeight, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(sheet))
            {
                ConfigureGraphicsForPixelArt(graphics);

                for (int index = 0; index < m_nFrames; index++)
                {
                    DrawFrameToSheet(graphics, index, columns, cellWidth, cellHeight, cellPadding);
                }
            }

            return sheet;
        }

        private int CalculateColumns(int maxColumns)
        {
            if (maxColumns > 0)
                return Math.Min(maxColumns, m_nFrames);

            return Math.Max(1, (int)Math.Ceiling(Math.Sqrt(m_nFrames)));
        }

        private void ConfigureGraphicsForPixelArt(Graphics graphics)
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        }

        private void DrawFrameToSheet(Graphics graphics, int index, int columns, int cellWidth, int cellHeight, int cellPadding)
        {
            using (var frameBitmap = RenderFrame(index))
            {
                if (frameBitmap != null)
                {
                    int column = index % columns;
                    int row = index / columns;
                    int destX = cellPadding + column * (cellWidth + cellPadding);
                    int destY = cellPadding + row * (cellHeight + cellPadding);
                    var destination = new Rectangle(destX, destY, frameBitmap.Width, frameBitmap.Height);
                    graphics.DrawImage(frameBitmap, destination);
                }
            }
        }

        //---------------------------------------------------------------------------
        // Hàm: RenderFrame
        // Chức năng: Render frame thành Bitmap
        // Tham số: frameIndex - index của frame cần render
        // Trả về: Bitmap hoặc null nếu thất bại
        //---------------------------------------------------------------------------
        public Bitmap? RenderFrame(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= m_nFrames)
                return null;

            if (m_pOffset == null || m_pSprite == null || m_pPal24 == null)
                return null;

            try
            {
                // Lấy frame data
                int frameOffset = (int)m_pOffset[frameIndex].Offset;
                if (frameOffset >= m_pSprite.Length)
                    return null;

                byte[] frameBytes = new byte[m_pSprite.Length - frameOffset];
                Array.Copy(m_pSprite, frameOffset, frameBytes, 0, frameBytes.Length);

                SPRFRAME frame = ByteArrayToStructure<SPRFRAME>(frameBytes);

                // Tạo bitmap với kích thước của sprite
                int width = m_nWidth;
                int height = m_nHeight;

                if (!IsValidSpriteSize(width, height))
                    return null;

                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                // Decode RLE sprite data
                int dataOffset = Marshal.SizeOf<SPRFRAME>();
                DecodeRLESprite(bitmap, frameBytes, dataOffset, frame, m_pPal24);

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        //---------------------------------------------------------------------------
        // Hàm: DecodeRLESprite
        // Chức năng: Decode dữ liệu RLE sprite và vẽ lên bitmap
        // Tham số: bitmap - bitmap đích
        //          spriteData - dữ liệu sprite
        //          offset - offset bắt đầu dữ liệu RLE
        //          frame - thông tin frame
        //          palette - bảng màu
        // Trả về: void
        //---------------------------------------------------------------------------
        private void DecodeRLESprite(Bitmap bitmap, byte[] spriteData, int offset, SPRFRAME frame, KPAL24[] palette)
        {
            BitmapData? bitmapData = null;
            try
            {
                bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                unsafe
                {
                    byte* ptr = (byte*)bitmapData.Scan0;
                    int stride = bitmapData.Stride;
                    int bufferLength = stride * bitmap.Height;

                    // Clear to transparent
                    for (int i = 0; i < bufferLength; i++)
                    {
                        ptr[i] = 0;
                    }

                    int pos = offset;
                    int originX = frame.OffsetX;
                    int originY = frame.OffsetY;

                    // Decode RLE: Format là [runLength][alpha] rồi đến [palette indices...]
                    for (int y = 0; y < frame.Height; y++)
                    {
                        int remaining = frame.Width;
                        int localX = 0;

                        while (remaining > 0)
                        {
                            // Đọc runLength và alpha
                            if (pos + 2 > spriteData.Length)
                                break;

                            byte runLength = spriteData[pos++];
                            byte alpha = spriteData[pos++];

                            if (runLength == 0)
                                continue;

                            // Nếu alpha = 0, skip transparent pixels
                            if (alpha == 0)
                            {
                                localX += runLength;
                                remaining -= runLength;
                                continue;
                            }

                            // Đọc palette indices
                            if (pos + runLength > spriteData.Length)
                                break;

                            for (int i = 0; i < runLength; i++)
                            {
                                byte paletteIndex = spriteData[pos++];
                                int destX = originX + localX;
                                int destY = originY + y;

                                if (destX >= 0 && destX < bitmap.Width && destY >= 0 && destY < bitmap.Height)
                                {
                                    byte finalAlpha = ResolveAlpha(paletteIndex, alpha);
                                    if (finalAlpha > 0 && paletteIndex < palette.Length)
                                    {
                                        KPAL24 color = palette[paletteIndex];
                                        int pixelIndex = destY * stride + destX * 4;
                                        ptr[pixelIndex + 0] = color.Blue;
                                        ptr[pixelIndex + 1] = color.Green;
                                        ptr[pixelIndex + 2] = color.Red;
                                        ptr[pixelIndex + 3] = finalAlpha;
                                    }
                                }

                                localX++;
                                remaining--;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (bitmapData != null)
                    bitmap.UnlockBits(bitmapData);
            }
        }

        //---------------------------------------------------------------------------
        // Hàm: IsValidSpriteSize
        // Chức năng: Kiểm tra kích thước sprite có hợp lệ không
        //---------------------------------------------------------------------------
        private static bool IsValidSpriteSize(int width, int height)
        {
            return width > 0 && height > 0 && width <= MaxSpriteSize && height <= MaxSpriteSize;
        }

        //---------------------------------------------------------------------------
        // Hàm: ResolveAlpha
        // Chức năng: Giải mã alpha từ nibble cao (4 bits)
        // Tham số: paletteIndex - index của palette
        //          alpha - byte alpha từ RLE stream
        // Trả về: alpha cuối cùng (0-255)
        //---------------------------------------------------------------------------
        private static byte ResolveAlpha(byte paletteIndex, byte alpha)
        {
            int nibble = (alpha >> AlphaShift) & AlphaNibbleMask;
            if (nibble <= 0)
                return 0;

            int scaled = nibble * AlphaScaleFactor;
            return (byte)Math.Clamp(scaled, 0, 255);
        }

        //---------------------------------------------------------------------------
        // Hàm: NextFrame
        // Chức năng: Lấy frame tiếp theo
        // Tham số: nFrame		frame hiện tại
        // Trả về: frame tiếp theo
        //---------------------------------------------------------------------------
        public int NextFrame(int nFrame)
        {
            nFrame++;
            if (nFrame >= m_nFrames)
                nFrame = 0;
            return nFrame;
        }

        //---------------------------------------------------------------------------
        // Hàm: GetFrame
        // Chức năng: Lấy Sprite Frame
        // Tham số: nFrame	frame
        // Trả về: void
        //---------------------------------------------------------------------------
        public byte[]? GetFrame(int nFrame)
        {
            byte[]? pSprite = m_pSprite;

            // kiểm tra phạm vi frame
            if (nFrame < 0 || nFrame >= m_nFrames)
                return null;

            // đến frame
            if (m_pOffset == null || pSprite == null)
                return null;

            int frameOffset = (int)m_pOffset[nFrame].Offset;
            if (frameOffset >= pSprite.Length)
                return null;

            byte[] frameData = new byte[pSprite.Length - frameOffset];
            Array.Copy(pSprite, frameOffset, frameData, 0, frameData.Length);

            return frameData;
        }

        // Getter methods
        public int GetWidth()
        { return m_nWidth; }

        public int GetHeight()
        { return m_nHeight; }

        public int GetCenterX()
        { return m_nCenterX; }

        public int GetCenterY()
        { return m_nCenterY; }

        public int GetFrames()
        { return m_nFrames; }

        public int GetColors()
        { return m_nColors; }

        public int GetDirections()
        { return m_nDirections; }

        public int GetInterval()
        { return m_nInterval; }

        public byte[]? GetPalette()
        { return m_Palette.GetMemPtr(); }

        public KPAL24[]? Get24Palette()
        { return m_pPal24; }

        // Helper method để chuyển đổi byte array thành struct
        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}