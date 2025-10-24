//---------------------------------------------------------------------------
//	NRV2B (UCL) Decompression Library
//	Original: Copyright Kingsoft 2003, Author: Wooy(Wu yue)
//	C# Port: 2024
//	Modern Implementation: KUnpackGUI
//---------------------------------------------------------------------------

namespace KUnpack.EngineSharp
{
    /// <summary>
    /// Provides decompression for NRV2B (UCL) compressed data used in Kingsoft PAK archives.
    /// </summary>
    /// <remarks>
    /// NRV2B is a variant of the UCL compression algorithm that uses bit-packed literals and LZ77-style back-references.
    /// This implementation is compatible with Kingsoft's PAK format compression.
    /// </remarks>
    public static class UCLDecompression
    {
        private const uint MaxMatchOffset = 0x00FFFFFFu + 3u;

        /// <summary>
        /// Giải nén dữ liệu UCL nrv2b (legacy interface for XPackFile compatibility)
        /// </summary>
        /// <param name="src">Dữ liệu nguồn đã nén</param>
        /// <param name="srcLen">Độ dài dữ liệu nguồn</param>
        /// <param name="dst">Buffer đích để chứa dữ liệu giải nén</param>
        /// <param name="dstLen">Độ dài buffer đích (sẽ được cập nhật với độ dài thực tế)</param>
        /// <param name="wrkmem">Buffer làm việc (có thể null)</param>
        /// <returns>true nếu thành công, false nếu thất bại</returns>
        public static bool ucl_nrv2b_decompress_8(byte[] src, uint srcLen, byte[] dst, out uint dstLen, byte[]? wrkmem)
        {
            dstLen = 0;

            if (src == null || dst == null || srcLen == 0)
                return false;

            try
            {
                int written = Decompress(src.AsSpan(0, (int)srcLen), dst.AsSpan());
                dstLen = (uint)written;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Decompresses NRV2B compressed data.
        /// </summary>
        /// <param name="source">The compressed source data.</param>
        /// <param name="destination">The destination buffer for decompressed data.</param>
        /// <returns>The number of bytes written to the destination buffer.</returns>
        /// <exception cref="InvalidDataException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item>The destination buffer is too small</item>
        /// <item>The compressed data is corrupted or truncated</item>
        /// <item>Match offsets or lengths are out of valid range</item>
        /// </list>
        /// </exception>
        public static int Decompress(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length == 0)
            {
                if (source.Length == 0)
                {
                    return 0;
                }

                throw new InvalidDataException("Destination buffer is empty while source still contains data.");
            }

            uint bitBuffer = 0;
            int srcIndex = 0;
            int dstIndex = 0;
            uint lastMatchOffset = 1;

            while (true)
            {
                uint matchOffset;
                uint matchLength;

                while (GetBit(ref bitBuffer, source, ref srcIndex) != 0)
                {
                    byte literal = ReadByte(source, ref srcIndex);
                    if (dstIndex >= destination.Length)
                    {
                        throw new InvalidDataException("NRV2B decompression would overrun the destination buffer while copying literals.");
                    }

                    destination[dstIndex++] = literal;
                }

                matchOffset = 1;
                do
                {
                    matchOffset = matchOffset * 2u + (uint)GetBit(ref bitBuffer, source, ref srcIndex);
                    if (matchOffset > MaxMatchOffset)
                    {
                        throw new InvalidDataException("NRV2B match offset exceeded maximum range.");
                    }
                }
                while (GetBit(ref bitBuffer, source, ref srcIndex) == 0);

                if (matchOffset == 2)
                {
                    matchOffset = lastMatchOffset;
                }
                else
                {
                    byte suffix = ReadByte(source, ref srcIndex);
                    matchOffset = (matchOffset - 3u) * 256u + suffix;
                    if (matchOffset == 0xFFFFFFFFu)
                    {
                        break;
                    }

                    lastMatchOffset = ++matchOffset;
                }

                matchLength = (uint)GetBit(ref bitBuffer, source, ref srcIndex);
                matchLength = matchLength * 2u + (uint)GetBit(ref bitBuffer, source, ref srcIndex);
                if (matchLength == 0)
                {
                    matchLength++;
                    do
                    {
                        matchLength = matchLength * 2u + (uint)GetBit(ref bitBuffer, source, ref srcIndex);
                        if (matchLength >= (uint)destination.Length)
                        {
                            throw new InvalidDataException("NRV2B decompression would overrun the destination buffer while expanding a match.");
                        }
                    }
                    while (GetBit(ref bitBuffer, source, ref srcIndex) == 0);
                    matchLength += 2u;
                }

                if (matchOffset > 0xD00u)
                {
                    matchLength += 1u;
                }

                int bytesToCopy = checked((int)matchLength + 1);
                if (dstIndex + bytesToCopy > destination.Length)
                {
                    throw new InvalidDataException("NRV2B decompression would overrun the destination buffer while copying a match.");
                }

                if (matchOffset > (uint)dstIndex)
                {
                    throw new InvalidDataException("NRV2B decompression encountered a look-behind beyond the already produced output.");
                }

                int matchSourceIndex = dstIndex - (int)matchOffset;
                for (int i = 0; i < bytesToCopy; i++)
                {
                    destination[dstIndex++] = destination[matchSourceIndex++];
                }
            }

            if (srcIndex != source.Length)
            {
                if (srcIndex < source.Length)
                {
                    throw new InvalidDataException("NRV2B decompression finished before the input stream was fully consumed.");
                }

                throw new InvalidDataException("NRV2B decompression read past the end of the input stream.");
            }

            return dstIndex;
        }

        public static int Decompress(ReadOnlySpan<byte> source, byte[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            return Decompress(source, destination.AsSpan());
        }

        public static byte[] Decompress(ReadOnlySpan<byte> source, int expectedOutputLength)
        {
            if (expectedOutputLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedOutputLength));
            }

            byte[] buffer = new byte[expectedOutputLength];
            int written = Decompress(source, buffer);
            if (written != expectedOutputLength)
            {
                throw new InvalidDataException($"NRV2B decompression produced {written} bytes, but {expectedOutputLength} bytes were expected.");
            }

            return buffer;
        }

        private static int GetBit(ref uint bitBuffer, ReadOnlySpan<byte> source, ref int srcIndex)
        {
            if ((bitBuffer & 0x7Fu) != 0)
            {
                bitBuffer *= 2u;
            }
            else
            {
                bitBuffer = (uint)(ReadByte(source, ref srcIndex) * 2 + 1);
            }

            return (int)((bitBuffer >> 8) & 1u);
        }

        private static byte ReadByte(ReadOnlySpan<byte> source, ref int index)
        {
            if (index >= source.Length)
            {
                throw new InvalidDataException("NRV2B decompression attempted to read past the end of the input stream.");
            }

            return source[index++];
        }
    }
}