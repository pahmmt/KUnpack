//---------------------------------------------------------------------------
//	Đọc file đóng gói
//	Copyright : Kingsoft 2003
//	Author	:   Wooy(Wu yue)
//	CreateTime:	2003-9-16
//---------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace KUnpack.EngineSharp
{
    // Một Pack file có cấu trúc header:
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XPackFileHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] cSignature;        // Bốn byte của file header flag, cố định là chuỗi 'PACK'

        public uint uCount;              // Số lượng mục dữ liệu
        public uint uIndexTableOffset;   // Offset của bảng index
        public uint uDataOffset;         // Offset của dữ liệu
        public uint uCrc32;              // Checksum

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] cReserved;         // Các byte dành riêng
    }

    public static class XPackConstants
    {
        public const uint XPACKFILE_SIGNATURE_FLAG = 0x4b434150;  // 'PACK'
        public const int SPR_COMMENT_FLAG = 0x525053;  // 'SPR'
    }

    // Thông tin index của mỗi file con trong Pack
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XPackIndexInfo
    {
        public uint uId;                 // ID của file con
        public uint uOffset;             // Vị trí offset của file con trong package
        public int lSize;                // Kích thước gốc của file con
        public int lCompressSizeFlag;    // Kích thước file con sau khi nén và phương thức nén
                                         // Byte cao nhất biểu thị phương thức nén, xem XPACK_METHOD
                                         // Ba byte thấp biểu thị kích thước file con sau khi nén
    }

    // Phương thức nén của file package
    public enum XPACK_METHOD : uint
    {
        TYPE_NONE = 0x00000000,          // Không nén
        TYPE_UCL = 0x01000000,          // Nén UCL
        TYPE_UPL_NEW = 0x20000000,      // Load Pak mới
        TYPE_BZIP2 = 0x02000000,        // Nén bzip2
        TYPE_FRAME = 0x10000000,        // Sử dụng nén frame độc lập, chỉ có thể dùng khi file con là loại spr

        TYPE_METHOD_FILTER = 0x0f000000, // Bộ lọc marker
        TYPE_FILTER = 0xff000000,       // Bộ lọc marker
    }

    // Thông tin frame spr được lưu trong pak package - sử dụng tuple thay vì struct
    // public struct XPackSprFrameInfo { public int lCompressSize; public int lSize; } - Đã thay thế bằng (int, int)

    // Cấu trúc để mô tả tham chiếu đến file con trong package
    public struct XPackElemFileRef
    {
        public uint uId;                 // ID file
        public int nPackIndex;           // Index package
        public int nElemIndex;           // Index file con trong package
        public int nCacheIndex;          // Index cache
        public int nOffset;              // Offset di chuyển của file con
        public int nSize;                // Kích thước file con
    }

    public class XPackFile : IDisposable
    {
        private const int MAX_XPACKFILE_CACHE = 10;
        private const int NODE_INDEX_STORE_IN_RESERVED = 2;

        // Action để log messages ra UI
        public Action<string>? LogAction { get; set; }

        private void Log(string message)
        {
            LogAction?.Invoke(message);
        }

        // Cache dữ liệu file con
        private struct XPackElemFileCache
        {
            public byte[] pBuffer;       // Buffer lưu dữ liệu file con
            public uint uId;             // ID file con
            public int lSize;            // Kích thước file con
            public int nPackIndex;       // Từ package file nào
            public int nElemIndex;       // Vị trí trong danh sách index
            public uint uRefFlag;        // Marker tham chiếu gần đây
        }

        private static XPackElemFileCache[] ms_ElemFileCache = new XPackElemFileCache[MAX_XPACKFILE_CACHE];
        private static int ms_nNumElemFileCache = 0;

        private FileStream? m_hFile;                    // Handle file package
        private uint m_uFileSize;                       // Kích thước file package
        private int m_nElemFileCount;                   // Số lượng file con
        private int m_nSelfIndex;                       // Index của chính file package trong chuỗi package
        private XPackIndexInfo[]? m_pIndexList;         // Danh sách index file con
        private readonly object m_ReadCritical = new object();  // Điều khiển critical section khi thao tác file package
        private bool _disposed = false;

        public XPackFile()
        {
            m_hFile = null;
            m_uFileSize = 0;
            m_pIndexList = null;
            m_nElemFileCount = 0;
        }

        ~XPackFile()
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
                    Close();
                }
                _disposed = true;
            }
        }

        //-------------------------------------------------
        // Chức năng: Mở file package
        // Trả về: Thành công hay không
        //-------------------------------------------------
        public bool Open(string pszPackFileName, int nSelfIndex)
        {
            bool bResult = false;
            Close();
            lock (m_ReadCritical)
            {
                m_nSelfIndex = nSelfIndex;
                try
                {
                    m_hFile = new FileStream(pszPackFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    m_uFileSize = (uint)m_hFile.Length;

                    if (m_uFileSize == 0 || m_uFileSize <= Marshal.SizeOf<XPackFileHeader>())
                    {
                        return false;
                    }

                    // Đọc header file package
                    byte[] headerBytes = new byte[Marshal.SizeOf<XPackFileHeader>()];
                    if (m_hFile.Read(headerBytes, 0, headerBytes.Length) != headerBytes.Length)
                    {
                        return false;
                    }

                    XPackFileHeader header = ByteArrayToStructure<XPackFileHeader>(headerBytes);

                    // Kiểm tra tính hợp lệ của file package marker và nội dung
                    if (BitConverter.ToInt32(header.cSignature, 0) != XPackConstants.XPACKFILE_SIGNATURE_FLAG ||
                        header.uCount == 0 ||
                        header.uIndexTableOffset < Marshal.SizeOf<XPackFileHeader>() ||
                        header.uIndexTableOffset >= m_uFileSize ||
                        header.uDataOffset < Marshal.SizeOf<XPackFileHeader>() ||
                        header.uDataOffset >= m_uFileSize)
                    {
                        return false;
                    }

                    // Đọc bảng thông tin index
                    int dwListSize = Marshal.SizeOf<XPackIndexInfo>() * (int)header.uCount;
                    m_pIndexList = new XPackIndexInfo[header.uCount];

                    if (m_hFile.Seek(header.uIndexTableOffset, SeekOrigin.Begin) != header.uIndexTableOffset)
                    {
                        return false;
                    }

                    byte[] indexBytes = new byte[dwListSize];
                    if (m_hFile.Read(indexBytes, 0, dwListSize) != dwListSize)
                    {
                        return false;
                    }

                    // Chuyển đổi byte array thành struct array
                    for (int i = 0; i < header.uCount; i++)
                    {
                        int offset = i * Marshal.SizeOf<XPackIndexInfo>();
                        byte[] structBytes = new byte[Marshal.SizeOf<XPackIndexInfo>()];
                        Array.Copy(indexBytes, offset, structBytes, 0, structBytes.Length);
                        m_pIndexList[i] = ByteArrayToStructure<XPackIndexInfo>(structBytes);
                    }

                    m_nElemFileCount = (int)header.uCount;
                    bResult = true;
                }
                catch
                {
                    bResult = false;
                }
            }

            if (bResult == false)
                Close();

            return bResult;
        }

        //-------------------------------------------------
        // Chức năng: Đóng file package
        //-------------------------------------------------
        public void Close()
        {
            lock (m_ReadCritical)
            {
                if (m_pIndexList != null)
                {
                    // Xóa các file con trong cache (có thể) từ package này
                    for (int i = ms_nNumElemFileCache - 1; i >= 0; i--)
                    {
                        if (ms_ElemFileCache[i].nPackIndex == m_nSelfIndex)
                        {
                            FreeElemCache(i);
                            ms_nNumElemFileCache--;
                            for (int j = i; j < ms_nNumElemFileCache; j++)
                                ms_ElemFileCache[j] = ms_ElemFileCache[j + 1];
                        }
                    }
                    m_pIndexList = null;
                }
                m_nElemFileCount = 0;

                if (m_hFile != null)
                {
                    m_hFile.Close();
                    m_hFile = null;
                }
                m_uFileSize = 0;
            }
        }

        //-------------------------------------------------
        // Chức năng: Giải phóng nội dung của một node cache
        // Trả về: Thành công hay không
        //-------------------------------------------------
        private static void FreeElemCache(int nCacheIndex)
        {
            if (nCacheIndex < 0 || nCacheIndex >= ms_nNumElemFileCache)
                return;

            if (ms_ElemFileCache[nCacheIndex].pBuffer != null)
            {
                ms_ElemFileCache[nCacheIndex].pBuffer = null!;
            }
            ms_ElemFileCache[nCacheIndex].uId = 0;
            ms_ElemFileCache[nCacheIndex].lSize = 0;
            ms_ElemFileCache[nCacheIndex].uRefFlag = 0;
            ms_ElemFileCache[nCacheIndex].nPackIndex = -1;
        }

        //-------------------------------------------------
        // Chức năng: Đọc trực tiếp dữ liệu từ file package vào buffer
        // Trả về: Thành công hay không
        //-------------------------------------------------
        private bool DirectRead(byte[] pBuffer, uint uOffset, uint uSize)
        {
            bool bResult = false;
            if (m_hFile != null && uOffset + uSize <= m_uFileSize)
            {
                try
                {
                    if (m_hFile.Seek(uOffset, SeekOrigin.Begin) == uOffset)
                    {
                        int bytesRead = m_hFile.Read(pBuffer, 0, (int)uSize);
                        if (bytesRead == uSize)
                            bResult = true;
                    }
                }
                catch
                {
                    bResult = false;
                }
            }
            return bResult;
        }

        //-------------------------------------------------
        // Chức năng: Đọc file package với giải nén vào buffer
        // Tham số: pBuffer --> Con trỏ buffer
        //          uExtractSize --> Kích thước dữ liệu (mong muốn) sau khi giải nén, kích thước buffer pBuffer không nhỏ hơn số này
        //          lCompressType --> Lấy trực tiếp từ package kích thước gốc (/nén)
        //          uOffset --> Bắt đầu đọc từ vị trí offset này trong package
        //          uSize --> Kích thước dữ liệu (nén) đọc trực tiếp từ package
        // Trả về: Thành công hay không
        //-------------------------------------------------
        private bool ExtractRead(byte[] pBuffer, uint uExtractSize, int lCompressType, uint uOffset, uint uSize)
        {
            Log($"ExtractRead: CompType=0x{lCompressType:X}, CompSize={uSize}, ExtractSize={uExtractSize}");

            bool bResult = false;
            if (lCompressType == (int)XPACK_METHOD.TYPE_NONE)
            {
                Log("  → No compression, using DirectRead");
                if (uExtractSize == uSize)
                    bResult = DirectRead(pBuffer, uOffset, uSize);
            }
            else
            {
                byte[] pReadBuffer = new byte[uSize];
                if (pReadBuffer != null)
                {
                    bool isUclOrUpl = (lCompressType == (int)XPACK_METHOD.TYPE_UCL || lCompressType == (int)XPACK_METHOD.TYPE_UPL_NEW);
                    Log($"  → IsUCL/UPL: {isUclOrUpl} (UCL=0x{(int)XPACK_METHOD.TYPE_UCL:X}, UPL=0x{(int)XPACK_METHOD.TYPE_UPL_NEW:X})");

                    if (isUclOrUpl)
                    {
                        if (DirectRead(pReadBuffer, uOffset, uSize))
                        {
                            Log("  → DirectRead OK, decompressing...");

                            // Log first 16 bytes of compressed data
                            string hexDump = "  → First bytes: ";
                            for (int i = 0; i < Math.Min(16, pReadBuffer.Length); i++)
                            {
                                hexDump += $"{pReadBuffer[i]:X2} ";
                            }
                            Log(hexDump);

                            uint uDestLength;
                            bResult = UCLDecompression.ucl_nrv2b_decompress_8(pReadBuffer, uSize, pBuffer, out uDestLength, null);
                            Log($"  → Decompress result: {bResult}, size: {uDestLength}/{uExtractSize}");
                            if (bResult && uDestLength != uExtractSize)
                            {
                                Log($"  → ERROR: Size mismatch!");
                                bResult = false;
                            }
                        }
                        else
                        {
                            Log("  → DirectRead FAILED!");
                        }
                    }
                    else
                    {
                        Log($"  → Unsupported compression: 0x{lCompressType:X}");
                    }
                }
            }
            Log($"  → Result: {bResult}");
            return bResult;
        }

        //-------------------------------------------------
        // Chức năng: Tìm mục file con trong bảng index (tìm kiếm nhị phân)
        // Trả về: Nếu tìm thấy trả về vị trí trong bảng index (>=0), nếu không tìm thấy trả về -1
        //-------------------------------------------------
        private int FindElemFile(uint ulId)
        {
            int nBegin = 0;
            int nEnd = m_nElemFileCount - 1;
            while (nBegin <= nEnd)
            {
                int nMid = (nBegin + nEnd) / 2;
                if (ulId < m_pIndexList![nMid].uId)
                    nEnd = nMid - 1;
                else if (ulId > m_pIndexList[nMid].uId)
                    nBegin = nMid + 1;
                else
                    break;
            }
            return ((nBegin <= nEnd) ? (nBegin + nEnd) / 2 : -1);
        }

        //-------------------------------------------------
        // Chức năng: Tìm file con trong package
        // Tham số: uId --> ID của file con
        //          ElemRef --> Nếu tìm thấy thì điền thông tin liên quan của file con vào cấu trúc này
        // Trả về: Có tìm thấy hay không
        //-------------------------------------------------
        public bool FindElemFile(uint uId, ref XPackElemFileRef ElemRef)
        {
            ElemRef.nElemIndex = -1;
            if (uId != 0)
            {
                lock (m_ReadCritical)
                {
                    ElemRef.nCacheIndex = FindElemFileInCache(uId, -1);
                    if (ElemRef.nCacheIndex >= 0)
                    {
                        ElemRef.uId = uId;
                        ElemRef.nPackIndex = ms_ElemFileCache[ElemRef.nCacheIndex].nPackIndex;
                        ElemRef.nElemIndex = ms_ElemFileCache[ElemRef.nCacheIndex].nElemIndex;
                        ElemRef.nSize = ms_ElemFileCache[ElemRef.nCacheIndex].lSize;
                        ElemRef.nOffset = 0;
                    }
                    else
                    {
                        ElemRef.nElemIndex = FindElemFile(uId);
                        if (ElemRef.nElemIndex >= 0)
                        {
                            ElemRef.uId = uId;
                            ElemRef.nPackIndex = m_nSelfIndex;
                            ElemRef.nOffset = 0;
                            ElemRef.nSize = m_pIndexList![ElemRef.nElemIndex].lSize;
                        }
                    }
                }
            }
            return (ElemRef.nElemIndex >= 0);
        }

        //-------------------------------------------------
        // Chức năng: Cấp phát buffer và đọc nội dung file con trong package vào đó
        // Tham số: Index của file con trong package
        // Trả về: Thành công thì trả về con trỏ buffer, ngược lại trả về con trỏ null
        //-------------------------------------------------
        private byte[]? ReadElemFile(int nElemIndex)
        {
            if (nElemIndex < 0 || nElemIndex >= m_nElemFileCount)
                return null;

            int compressionType = unchecked((int)(m_pIndexList![nElemIndex].lCompressSizeFlag & (int)XPACK_METHOD.TYPE_FILTER));
            int compressedSize = unchecked((int)(m_pIndexList[nElemIndex].lCompressSizeFlag & (~(int)XPACK_METHOD.TYPE_FILTER)));
            uint offset = m_pIndexList[nElemIndex].uOffset;
            int originalSize = m_pIndexList[nElemIndex].lSize;

            Console.WriteLine($"ReadElemFile[{nElemIndex}]: OrigSize={originalSize}, CompType=0x{compressionType:X}, Offset=0x{offset:X}, CompSize={compressedSize}");

            // Check if TYPE_FRAME flag is set
            if ((compressionType & (int)XPACK_METHOD.TYPE_FRAME) != 0)
            {
                // TYPE_FRAME: Read stored data directly (SPR header + compressed frames)
                // For preview purposes, we read the raw stored data instead of full decompression
                Console.WriteLine($"ReadElemFile[{nElemIndex}]: TYPE_FRAME detected, reading stored data ({compressedSize} bytes)");
                Log($"  → TYPE_FRAME detected, reading stored data ({compressedSize} bytes)");

                byte[] pDataBuffer = new byte[compressedSize];
                if (DirectRead(pDataBuffer, offset, (uint)compressedSize))
                {
                    Console.WriteLine($"ReadElemFile[{nElemIndex}]: Success! Read {compressedSize} bytes (stored data)");
                    return pDataBuffer;
                }
                else
                {
                    Console.WriteLine($"ReadElemFile[{nElemIndex}]: DirectRead failed!");
                    return null;
                }
            }

            // Normal compression (NONE, UCL, UPL_NEW)
            byte[] pDataBuffer2 = new byte[originalSize];
            if (pDataBuffer2 != null)
            {
                bool success = ExtractRead(pDataBuffer2,
                        (uint)originalSize,
                        compressionType,
                        offset,
                        (uint)compressedSize);

                if (!success)
                {
                    Console.WriteLine($"ReadElemFile[{nElemIndex}]: ExtractRead failed!");
                    pDataBuffer2 = null!;
                }
                else
                {
                    Console.WriteLine($"ReadElemFile[{nElemIndex}]: Success! Read {originalSize} bytes");
                }
            }
            return pDataBuffer2;
        }

        //-------------------------------------------------
        // Chức năng: Tìm file con trong cache
        // Tham số: uId --> ID file con
        //          nDesireIndex --> Vị trí có thể trong cache
        // Trả về: Thành công thì trả về index node cache (>=0), thất bại thì trả về -1
        //-------------------------------------------------
        private int FindElemFileInCache(uint uId, int nDesireIndex)
        {
            if (nDesireIndex >= 0 && nDesireIndex < ms_nNumElemFileCache &&
                uId == ms_ElemFileCache[nDesireIndex].uId)
            {
                ms_ElemFileCache[nDesireIndex].uRefFlag = 0xffffffff;
                return nDesireIndex;
            }

            nDesireIndex = -1;
            for (int i = 0; i < ms_nNumElemFileCache; i++)
            {
                if (uId == ms_ElemFileCache[i].uId)
                {
                    ms_ElemFileCache[i].uRefFlag = 0xffffffff;
                    nDesireIndex = i;
                    break;
                }
            }
            return nDesireIndex;
        }

        //-------------------------------------------------
        // Chức năng: Thêm dữ liệu file con vào cache
        // Tham số: pBuffer --> Buffer chứa dữ liệu file con
        //          nElemIndex --> Index của file con trong package
        // Trả về: Vị trí index được thêm vào cache
        //-------------------------------------------------
        private int AddElemFileToCache(byte[] pBuffer, int nElemIndex)
        {
            if (nElemIndex < 0 || nElemIndex >= m_nElemFileCount)
                return -1;

            int nCacheIndex;
            if (ms_nNumElemFileCache < MAX_XPACKFILE_CACHE)
            {   // Tìm một vị trí trống
                nCacheIndex = ms_nNumElemFileCache++;
            }
            else
            {   // Giải phóng một node cache cũ
                nCacheIndex = 0;
                if (ms_ElemFileCache[0].uRefFlag != 0)
                    ms_ElemFileCache[0].uRefFlag--;
                for (int i = 1; i < MAX_XPACKFILE_CACHE; i++)
                {
                    if (ms_ElemFileCache[i].uRefFlag != 0)
                        ms_ElemFileCache[i].uRefFlag--;
                    if (ms_ElemFileCache[i].uRefFlag < ms_ElemFileCache[nCacheIndex].uRefFlag)
                        nCacheIndex = i;
                }
                FreeElemCache(nCacheIndex);
            }
            ms_ElemFileCache[nCacheIndex].pBuffer = pBuffer;
            ms_ElemFileCache[nCacheIndex].uId = m_pIndexList![nElemIndex].uId;
            ms_ElemFileCache[nCacheIndex].lSize = m_pIndexList[nElemIndex].lSize;
            ms_ElemFileCache[nCacheIndex].nPackIndex = m_nSelfIndex;
            ms_ElemFileCache[nCacheIndex].nElemIndex = nElemIndex;
            ms_ElemFileCache[nCacheIndex].uRefFlag = 0xffffffff;
            return nCacheIndex;
        }

        //-------------------------------------------------
        // Chức năng: Đọc dữ liệu với độ dài nhất định của file con vào buffer
        // Tham số: pBuffer --> Buffer dùng để đọc dữ liệu
        //          uSize --> Độ dài dữ liệu cần đọc
        // Trả về: Số byte đọc được thành công
        //-------------------------------------------------
        public int ElemFileRead(ref XPackElemFileRef ElemRef, byte[] pBuffer, uint uSize)
        {
            int nResult = 0;
            if (pBuffer != null && ElemRef.uId != 0 && ElemRef.nElemIndex >= 0)
            {
                lock (m_ReadCritical)
                {
                    // Xem có trong cache chưa
                    ElemRef.nCacheIndex = FindElemFileInCache(ElemRef.uId, ElemRef.nCacheIndex);

                    if (ElemRef.nCacheIndex < 0 &&                              // Không tìm thấy trong cache
                        ElemRef.nElemIndex < m_nElemFileCount &&
                        m_pIndexList![ElemRef.nElemIndex].uId == ElemRef.uId)
                    {
                        byte[]? pDataBuffer = ReadElemFile(ElemRef.nElemIndex);
                        if (pDataBuffer != null)
                            ElemRef.nCacheIndex = AddElemFileToCache(pDataBuffer, ElemRef.nElemIndex);
                    }

                    if (ElemRef.nCacheIndex >= 0 &&
                        // Ba mục dưới đây nên được kiểm tra mở rộng để tránh bị thay đổi bởi module bên ngoài, gây lỗi.
                        // Để hiệu quả có thể cân nhắc bỏ qua, nhưng cần bên ngoài tuân theo quy tắc không thay đổi nội dung ElemRef tùy tiện.
                        ElemRef.nPackIndex == ms_ElemFileCache[ElemRef.nCacheIndex].nPackIndex &&
                        ElemRef.nElemIndex == ms_ElemFileCache[ElemRef.nCacheIndex].nElemIndex &&
                        ElemRef.nSize == ms_ElemFileCache[ElemRef.nCacheIndex].lSize)
                    {
                        if (ElemRef.nOffset < 0)
                            ElemRef.nOffset = 0;
                        if (ElemRef.nOffset < ElemRef.nSize)
                        {
                            if (ElemRef.nOffset + (int)uSize <= ElemRef.nSize)
                                nResult = (int)uSize;
                            else
                                nResult = ElemRef.nSize - ElemRef.nOffset;

                            Array.Copy(ms_ElemFileCache[ElemRef.nCacheIndex].pBuffer!, ElemRef.nOffset, pBuffer, 0, nResult);
                            ElemRef.nOffset += nResult;
                        }
                        else
                        {
                            ElemRef.nOffset = ElemRef.nSize;
                        }
                    }
                }
            }
            return nResult;
        }

        //-------------------------------------------------
        // Chức năng: Lấy số lượng file con trong package
        // Trả về: Số lượng file con
        //-------------------------------------------------
        public int GetElemFileCount()
        {
            return m_nElemFileCount;
        }

        //-------------------------------------------------
        // Chức năng: Lấy thông tin index của file con theo index
        // Tham số: nIndex --> Index của file con
        // Trả về: Thông tin index hoặc null nếu index không hợp lệ
        //-------------------------------------------------
        public XPackIndexInfo? GetIndexInfo(int nIndex)
        {
            if (nIndex < 0 || nIndex >= m_nElemFileCount || m_pIndexList == null)
                return null;
            return m_pIndexList[nIndex];
        }

        //-------------------------------------------------
        // Chức năng: Đọc toàn bộ dữ liệu file con theo index
        // Tham số: nIndex --> Index của file con
        // Trả về: Dữ liệu file con hoặc null nếu lỗi
        //-------------------------------------------------
        public byte[]? ReadElemFileByIndex(int nIndex)
        {
            if (nIndex < 0 || nIndex >= m_nElemFileCount || m_pIndexList == null)
                return null;

            lock (m_ReadCritical)
            {
                return ReadElemFile(nIndex);
            }
        }

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

        //-------------------------------------------------
        // Chức năng: Đọc header file spr hoặc toàn bộ spr
        // Tham số: ElemRef --> Tham chiếu đến file con
        //          pOffsetTable --> Bảng offset (output)
        // Trả về: Con trỏ đến SPRHEAD hoặc null nếu thất bại
        //-------------------------------------------------
        public SPRHEAD? GetSprHeader(ref XPackElemFileRef ElemRef, out SPROFFS[]? pOffsetTable)
        {
            SPRHEAD? pSpr = null;
            bool bOk = false;

            pOffsetTable = null;
            if (ElemRef.uId == 0 || ElemRef.nElemIndex < 0)
                return null;

            lock (m_ReadCritical)
            {
                if (ElemRef.nElemIndex < m_nElemFileCount &&
                    m_pIndexList![ElemRef.nElemIndex].uId == ElemRef.uId)
                {
                    // Trước tiên kiểm tra id này là loại phương thức nén gì
                    if ((m_pIndexList[ElemRef.nElemIndex].lCompressSizeFlag & (int)XPACK_METHOD.TYPE_FRAME) == 0)
                    {
                        byte[]? pData = ReadElemFile(ElemRef.nElemIndex);
                        if (pData != null)
                        {
                            SPRHEAD header = ByteArrayToStructure<SPRHEAD>(pData);
                            if (BitConverter.ToInt32(header.Comment, 0) == XPackConstants.SPR_COMMENT_FLAG)
                            {
                                // Parse offset table
                                int offsetTableSize = header.Frames * Marshal.SizeOf<SPROFFS>();
                                int offsetTableOffset = Marshal.SizeOf<SPRHEAD>() + header.Colors * 3;

                                if (offsetTableOffset + offsetTableSize <= pData.Length)
                                {
                                    SPROFFS[] offsetTable = new SPROFFS[header.Frames];
                                    for (int i = 0; i < header.Frames; i++)
                                    {
                                        int offset = offsetTableOffset + i * Marshal.SizeOf<SPROFFS>();
                                        byte[] offsetBytes = new byte[Marshal.SizeOf<SPROFFS>()];
                                        Array.Copy(pData, offset, offsetBytes, 0, offsetBytes.Length);
                                        offsetTable[i] = ByteArrayToStructure<SPROFFS>(offsetBytes);
                                    }

                                    pSpr = header;
                                    pOffsetTable = offsetTable;
                                    bOk = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        SPRHEAD header;
                        byte[] headerBytes = new byte[Marshal.SizeOf<SPRHEAD>()];
                        if (DirectRead(headerBytes, m_pIndexList[ElemRef.nElemIndex].uOffset, (uint)headerBytes.Length))
                        {
                            header = ByteArrayToStructure<SPRHEAD>(headerBytes);
                            if (BitConverter.ToInt32(header.Comment, 0) == XPackConstants.SPR_COMMENT_FLAG)
                            {
                                uint u2ListSize = (uint)(header.Colors * 3 + header.Frames * sizeof(int) * 2); // 2 ints: compressSize + size
                                byte[] pSprData = new byte[Marshal.SizeOf<SPRHEAD>() + u2ListSize];

                                if (DirectRead(pSprData, m_pIndexList[ElemRef.nElemIndex].uOffset, (uint)pSprData.Length))
                                {
                                    Array.Copy(headerBytes, 0, pSprData, 0, headerBytes.Length);
                                    pSpr = ByteArrayToStructure<SPRHEAD>(pSprData);
                                    bOk = true;
                                }
                            }
                        }
                    }

                    if (pSpr.HasValue)
                    {
                        if (!bOk)
                        {
                            pSpr = null;
                        }
                    }
                }
            }
            return pSpr;
        }

        //-------------------------------------------------
        // Chức năng: Đọc dữ liệu một frame của spr được nén theo frame
        // Tham số: pSprHeader --> Header của sprite
        //          nFrame --> Số frame
        // Trả về: Con trỏ đến SPRFRAME hoặc null nếu thất bại
        //-------------------------------------------------
        public SPRFRAME? GetSprFrame(SPRHEAD? pSprHeader, int nFrame)
        {
            SPRFRAME? pFrame = null;
            if (pSprHeader.HasValue && nFrame >= 0 && nFrame < pSprHeader.Value.Frames)
            {
                lock (m_ReadCritical)
                {
                    // Tìm node index từ reserved field
                    int nNodeIndex = -1;
                    if (pSprHeader.Value.Reserved != null && pSprHeader.Value.Reserved.Length > NODE_INDEX_STORE_IN_RESERVED)
                    {
                        nNodeIndex = BitConverter.ToInt32(BitConverter.GetBytes(pSprHeader.Value.Reserved[NODE_INDEX_STORE_IN_RESERVED]), 0);
                    }

                    if (nNodeIndex >= 0 && nNodeIndex < m_nElemFileCount)
                    {
                        long lCompressType = m_pIndexList![nNodeIndex].lCompressSizeFlag;
                        if ((lCompressType & (int)XPACK_METHOD.TYPE_FRAME) != 0)
                        {
                            bool bOk = false;
                            lCompressType &= (int)XPACK_METHOD.TYPE_METHOD_FILTER;

                            // Tính toán offset của frame info
                            long lTempValue = Marshal.SizeOf<SPRHEAD>() + pSprHeader.Value.Colors * 3;

                            // Đọc thông tin frame
                            uint frameInfoOffset = m_pIndexList[nNodeIndex].uOffset + (uint)lTempValue;
                            byte[] frameInfoBytes = new byte[sizeof(int) * 2]; // 2 ints: compressSize + size

                            if (DirectRead(frameInfoBytes, frameInfoOffset + (uint)(nFrame * sizeof(int) * 2), (uint)frameInfoBytes.Length))
                            {
                                int compressSize = BitConverter.ToInt32(frameInfoBytes, 0);
                                int size = BitConverter.ToInt32(frameInfoBytes, sizeof(int));

                                // Tính toán offset của dữ liệu frame
                                uint uSrcOffset = m_pIndexList[nNodeIndex].uOffset + (uint)lTempValue + (uint)(pSprHeader.Value.Frames * sizeof(int) * 2);

                                // Tính offset cho frame cụ thể
                                for (int i = 0; i < nFrame; i++)
                                {
                                    byte[] tempFrameInfoBytes = new byte[sizeof(int) * 2];
                                    if (DirectRead(tempFrameInfoBytes, frameInfoOffset + (uint)(i * sizeof(int) * 2), (uint)tempFrameInfoBytes.Length))
                                    {
                                        int tempCompressSize = BitConverter.ToInt32(tempFrameInfoBytes, 0);
                                        uSrcOffset += (uint)tempCompressSize;
                                    }
                                }

                                // Đọc dữ liệu frame
                                int frameSize = size;
                                if (frameSize < 0)
                                {
                                    // Frame không nén
                                    frameSize = -frameSize;
                                    byte[] frameData = new byte[frameSize];
                                    if (DirectRead(frameData, uSrcOffset, (uint)frameSize))
                                    {
                                        pFrame = ByteArrayToStructure<SPRFRAME>(frameData);
                                        bOk = true;
                                    }
                                }
                                else
                                {
                                    // Frame đã nén
                                    byte[] compressedData = new byte[compressSize];
                                    if (DirectRead(compressedData, uSrcOffset, (uint)compressSize))
                                    {
                                        byte[] decompressedData = new byte[frameSize];
                                        if (UCLDecompression.ucl_nrv2b_decompress_8(compressedData, (uint)compressSize, decompressedData, out uint actualSize, null))
                                        {
                                            if (actualSize == frameSize)
                                            {
                                                pFrame = ByteArrayToStructure<SPRFRAME>(decompressedData);
                                                bOk = true;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!bOk)
                            {
                                pFrame = null;
                            }
                        }
                    }
                }
            }
            return pFrame;
        }
    }
}