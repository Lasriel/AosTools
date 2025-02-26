namespace AosTools.Data {

    /// <summary>
    /// AOS file header.
    /// </summary>
    public struct AOSHEADER {
        // 260 is the limit defined by MAX_PATH in windows + null termination byte at the end
        public const int k_ArchiveNameBytes = 261;
        public const uint k_HeaderBytes = 273;

        public uint fileSignature; // File signature 4 bytes, should be usually 00 00 00 00
        public uint dataOffset; // Offset where archive header & entry index end and entry data start
        public uint indexSize; // Size of the entry index in bytes
        public byte[] archiveName; // Archive name as 261 bytes
        // Total size 273 bytes
    }

    /// <summary>
    /// AOS archive entry.
    /// </summary>
    public struct AOSENTRY {
        public const int k_EntryNameBytes = 32;
        public const uint k_EntryBytes = 40;

        public byte[] fileName; // File name as 32 bytes
        public uint offset; // Data offset, starts from 0, each iteration last entry size gets added to the offset
        public uint size; // File size in bytes
        // Total size 40 bytes
    }

    /// <summary>
    /// Standard bitmap file header defined here:
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapfileheader"/>
    /// </summary>
    public struct BITMAPFILEHEADER {
        public ushort bfType; // BM (42 4D)
        public uint bfSize; // File size
        public ushort bfReserved1; // Always 0 (00 00)
        public ushort bfReserved2; // Always 0 (00 00)
        public uint bfOffBits; // Offset where color data starts
        // Total size 14 bytes
    }

    /// <summary>
    /// Standard bitmap info header defined here:
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader"/>
    /// </summary>
    public struct BITMAPINFOHEADER {
        public uint biSize; // Info header size
        public int biWidth; // Image width in pixels
        public int biHeight; // Image height in pixels
        public ushort biPlanes; // Always 1 (01 00)
        public ushort biBitCount; // Bits per pixel
        public uint biCompression; // Compression mode
        public uint biSizeImage; // Size of the color data
        public int biXPelsPerMeter; // X resolution
        public int biYPelsPerMeter; // Y resolution
        public uint biClrUsed; // Number of colors in the color table, if biBitCount is 8 color table is always present
        public uint biClrImportant; // Number of important colors in color table, 0 = all
        // Total size 40 bytes
    }

    /// <summary>
    /// Header used by Animated ABM files.
    /// </summary>
    public struct ABMANIMHEADER {
        public ushort abmType; // ABM image type, matches biBitCount usually
        public ushort animMode; // Animation mode, 01 00 = does not animate (single frame anim) | 02 00 = animates
        public uint frameCount; // Amount of frames in the file
        public uint frameSequenceSize; // Size of the frame sequence in bytes
        public uint[] frameOffsets; // Same amount of offsets than there are frames, index 0 is the base offset
        public ushort[] frameSequence; // Index based frame sequence
        // Total size varies by frame count and animation sequence
    }

    /// <summary>
    /// Output data for operations.
    /// </summary>
    public readonly struct OperationOutcome {

        /// <summary>
        /// Whether the operation ended successfully.
        /// </summary>
        public readonly bool Success;

        /// <summary>
        /// Optional. Message to pass to caller.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Creates a new operation outcome.
        /// </summary>
        /// <param name="success"> Was this operation successful. </param>
        /// <param name="msg"> Message to the caller. </param>
        public OperationOutcome(bool success, string msg = "") {
            Success = success;
            Message = msg;
        }

    }
}