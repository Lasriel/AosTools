using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace AosTools.Data {

    /// <summary>
    /// Decodes scr/ABM files.
    /// </summary>
    public class Decoder {

        private readonly AosFileInfo m_FileInfo;

        /// <summary>
        /// Creates a new decoder.
        /// </summary>
        /// <param name="fileInfo"> Optional: File info, without this you can only decode data. </param>
        public Decoder(AosFileInfo fileInfo = null) {
            m_FileInfo = fileInfo;
        }

        #region Decoder Methods

        /// <summary>
        /// <para> Decodes a file or directory of scr/ABM files. </para>
        /// <para> This requires you to pass in file info in the constructor. </para>
        /// </summary>
        public OperationOutcome Decode() {
            if (m_FileInfo == null) return new OperationOutcome(false, "Decoding failed, no file info was passed to decoder.");

            switch (m_FileInfo.FileMode) {
                case Mode.File:
                    return DecodeFile();
                case Mode.Directory:
                    return DecodeDirectory();
                default:
                    return new OperationOutcome(false, "Decoding failed, invalid file mode.");
            }
        }

        /// <summary>
        /// Decodes a single huffman encoded script file.
        /// </summary>
        /// <param name="encodedData"> Script file bytes. </param>
        /// <returns> Decoded bytes. </returns>
        public byte[] DecodeScript(byte[] encodedData) {
            using (Stream stream = new MemoryStream(encodedData)) {
                using (BinaryReader reader = new BinaryReader(stream)) {

                    // Read uncompressed file size from the first 4 bytes
                    uint uncompressedSize = reader.ReadUInt32();

                    // Variables for decoding
                    HuffmanDecode huffman = new HuffmanDecode();

                    // Rebuild huffman tree
                    huffman.arrayIndex = RebuildHuffmanTree(huffman, reader);

                    // Use the huffman tree to decode
                    return DecodeHuffman(huffman, reader, uncompressedSize);
                }
            }
        }

        /// <summary>
        /// Decodes a single encoded file.
        /// </summary>
        private OperationOutcome DecodeFile() {
            Stopwatch stopwatch = Stopwatch.StartNew();

            byte[] encodedData = File.ReadAllBytes(m_FileInfo.InputPath);

            byte[] decodedData = null;
            string outputExtension = m_FileInfo.FileExtension;

            string fileExtension = m_FileInfo.FileExtension;
            string fileName = m_FileInfo.FileNameNoExt;
            switch (fileExtension) {
                case ".scr":
                    decodedData = DecodeScript(encodedData);
                    outputExtension = ".txt";
                    break;
                case ".abm":
                    DataABM abm;
                    try {
                        abm = ImageABM.DecodeABM(encodedData, fileName);
                    } catch (Exception error) {
                        return new OperationOutcome(false, error.Message);
                    }

                    switch (abm.ImageType) {
                        case ABMType.Single:
                            decodedData = ImageBMP.CreateBMP(abm.FileHeader, abm.InfoHeader, abm.ColorData);
                            outputExtension = ".bmp";
                            break;

                        case ABMType.MultiFrame:

                            string jsonPath = Path.Combine(m_FileInfo.OutputPath, $"{fileName}.json");
                            File.WriteAllText(jsonPath, abm.AnimationJson.ToJson());

                            for (int j = 0; j < abm.Frames.Count; j++) {
                                string framePath = Path.Combine(m_FileInfo.OutputPath, $"{abm.Frames[j].FrameName}.bmp");
                                using (FileStream fileStream = File.Open(framePath, FileMode.Create, FileAccess.Write)) {
                                    using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                                        writer.Write(ImageBMP.CreateBMP(abm.FileHeader, abm.InfoHeader, abm.Frames[j].FrameData));
                                    }
                                }
                            }

                            stopwatch.Stop();
                            return new OperationOutcome(true, $"Successfully decoded file {m_FileInfo.FileName} in {stopwatch.ElapsedMilliseconds} ms");

                        case ABMType.Unknown:
                        case ABMType.NotImplemented:
                        default:
                            return new OperationOutcome(false, $"Decoding failed, unknown abm type!");
                    }

                    break;
                case ".msk":
                    decodedData = encodedData;
                    outputExtension = ".bmp";
                    break;
                default:
                    // Unknown file extension
                    return new OperationOutcome(false, $"Decoding failed, unexpected file extension [{fileExtension}]!");
            }

            if (decodedData == null || decodedData.Length <= 0) return new OperationOutcome(false, "Decoding failed, invalid data to decode.");


            string fileNameFull = fileName + outputExtension;
            string filePath = Path.Combine(m_FileInfo.OutputPath, fileNameFull);

            using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                    writer.Write(decodedData);
                }
            }

            stopwatch.Stop();
            return new OperationOutcome(true, $"Successfully decoded file {m_FileInfo.FileName} in {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Decodes all scr and ABM files in a directory.
        /// </summary>
        private OperationOutcome DecodeDirectory() {
            string[] filePaths = Directory.GetFiles(m_FileInfo.InputPath);
            if (filePaths.Length == 0) return new OperationOutcome(false, "Decode cancelled, input directory is empty.");

            Stopwatch stopwatch = Stopwatch.StartNew();

            StringBuilder stringBuilder = new StringBuilder();
            List<string> skippedFileNames = new List<string>();
            int filesSkipped = 0;
            int filesDecoded = 0;

            for (int i = 0; i < filePaths.Length; i++) {

                string inputPath = filePaths[i];

                string fileExtension = Path.GetExtension(inputPath);
                string fileName = Path.GetFileNameWithoutExtension(inputPath);

                byte[] decodedData = null;
                string outputExtension = fileExtension;

                switch (fileExtension) {
                    case ".scr":
                        decodedData = DecodeScript(File.ReadAllBytes(inputPath));
                        outputExtension = ".txt";
                        break;
                    case ".abm":
                        DataABM abm;
                        try {
                            abm = ImageABM.DecodeABM(File.ReadAllBytes(inputPath), fileName);
                        } catch (Exception) {
                            break;
                        }

                        switch (abm.ImageType) {
                            case ABMType.Single:
                                decodedData = ImageBMP.CreateBMP(abm.FileHeader, abm.InfoHeader, abm.ColorData);
                                outputExtension = ".bmp";
                                break;

                            case ABMType.MultiFrame:

                                string jsonPath = Path.Combine(m_FileInfo.OutputPath, $"{fileName}.json");
                                File.WriteAllText(jsonPath, abm.AnimationJson.ToJson());

                                for (int j = 0; j < abm.Frames.Count; j++) {
                                    string framePath = Path.Combine(m_FileInfo.OutputPath, $"{abm.Frames[j].FrameName}.bmp");
                                    using (FileStream fileStream = File.Open(framePath, FileMode.Create, FileAccess.Write)) {
                                        using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                                            writer.Write(ImageBMP.CreateBMP(abm.FileHeader, abm.InfoHeader, abm.Frames[j].FrameData));
                                        }
                                    }
                                }
                                continue; // Next file

                            case ABMType.Unknown:
                            case ABMType.NotImplemented:
                            default:
                                // Leave as abm
                                break;
                        }
                        break;
                    case ".msk":
                        decodedData = File.ReadAllBytes(inputPath);
                        outputExtension = ".bmp";
                        break;
                    default:
                        skippedFileNames.Add(Path.GetFileName(inputPath));
                        filesSkipped++;
                        continue;
                }

                if (decodedData == null) {
                    skippedFileNames.Add(Path.GetFileName(inputPath));
                    filesSkipped++;
                    continue;
                }

                string fileNameFull = fileName + outputExtension;
                string filePath = Path.Combine(m_FileInfo.OutputPath, fileNameFull);

                using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                    using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                        writer.Write(decodedData);
                    }
                }

                filesDecoded++;
            }

            stopwatch.Stop();

            string veryImportant = filesDecoded == 1 ? "file" : "files";
            stringBuilder.AppendLine($"Successfully decoded {filesDecoded} {veryImportant} in {stopwatch.ElapsedMilliseconds} ms");

            if (skippedFileNames.Count != 0) {
                stringBuilder.AppendLine($"Skipped files {filesSkipped}:");
                foreach (string fileName in skippedFileNames) {
                    stringBuilder.AppendLine(fileName);
                }
            }

            return new OperationOutcome(true, stringBuilder.ToString());
        }

        #endregion

        #region Huffman Decoding

        /// <summary>
        /// Variables needed for decoding.
        /// </summary>
        private class HuffmanDecode {

            /// <summary>
            /// Bit 0 nodes of the huffman tree.
            /// </summary>
            public uint[] bit0_array;

            /// <summary>
            /// Bit 1 nodes of the huffman tree.
            /// </summary>
            public uint[] bit1_array;

            /// <summary>
            /// Current index in the huffman tree arrays.
            /// </summary>
            public uint arrayIndex;

            /// <summary>
            /// Byte that is currently being read.
            /// </summary>
            public uint currentByte;

            /// <summary>
            /// Current bit shift.
            /// </summary>
            public int bitShift;

            public HuffmanDecode() {
                bit0_array = new uint[511];
                bit1_array = new uint[511];
                // 256 is the root internal node in the huffman tree, values above 256 are other internal nodes
                // Values stored in the leaf nodes are bytes so when being decoded we know that we found a leaf node when we drop below 256
                arrayIndex = 256;
                currentByte = 0;
                bitShift = 0;
            }
        }

        /// <summary>
        /// Rebuilds huffman tree recursively from the data that is stored at the beginning of the encoded file.
        /// </summary>
        /// <param name="huffman"> Decoding variables. </param>
        /// <param name="reader"> BinaryReader, stream position needs to be after first 4 bytes. </param>
        /// <returns> When rebuilding finishes returns index of the root node (256), otherwise returns either internal node index or leaf node byte. </returns>
        private uint RebuildHuffmanTree(HuffmanDecode huffman, BinaryReader reader) {
            uint nodeValue = 0;

            // When previous byte is processed, read next byte
            if (huffman.bitShift == 0) {
                huffman.currentByte = reader.ReadByte();
                huffman.bitShift = 8;
            }

            // Get next bit in the current byte
            huffman.bitShift -= 1;
            uint bit = (huffman.currentByte >> huffman.bitShift) & 1;

            if (bit == 1) { // Bit 1
                            // Internal node, continue traversing the tree and return index of this node
                uint internalNodeIndex = huffman.arrayIndex;

                huffman.arrayIndex += 1;

                if (internalNodeIndex < 511) {
                    huffman.bit0_array[internalNodeIndex] = RebuildHuffmanTree(huffman, reader);
                    huffman.bit1_array[internalNodeIndex] = RebuildHuffmanTree(huffman, reader);

                    nodeValue = internalNodeIndex;
                }

            } else { // Bit 0
                     // Leaf node, next 8-Bits are the value stored in the leaf node, returns leaf node value
                int bitShiftTemp = 8;
                uint result = 0;
                uint u1 = 1; // 0000 0001

                // Get bits that are left in the currently processed byte
                // For example:
                // Bit shift is 5 and current byte is 0111 0110
                // This would get the ---1 0110 part of the byte and shift it left by the bit shift resulting in 1011 0---
                while (bitShiftTemp > huffman.bitShift) {
                    uint bitMask1 = (u1 << huffman.bitShift) - 1;
                    uint work = bitMask1 & huffman.currentByte;

                    bitShiftTemp -= huffman.bitShift;

                    result |= work << bitShiftTemp;

                    huffman.currentByte = reader.ReadByte();
                    huffman.bitShift = 8;
                }

                huffman.bitShift -= bitShiftTemp;

                // Get rest of the leaf node bits and combine them with the result bits
                // For example:
                // Bit shift is 3 and current byte is 0110 1110
                // This would get the 011- ---- part of the byte and right shift it by the bit shift resulting in ---- -011
                // Then combine the results 1011 0--- and ---- -011 to form the leaf node value of 1011 0011
                uint bitMask2 = (u1 << bitShiftTemp) - 1;
                nodeValue = ((huffman.currentByte >> huffman.bitShift) & bitMask2) | result;
            }

            return nodeValue;
        }

        /// <summary>
        /// Starts decoding huffman coded binary using the created huffman tree.
        /// </summary>
        /// <param name="huffman"> Rebuilt huffman tree and decoding variables. </param>
        /// <param name="reader"> BinaryReader, stream position should be at the beginning of the encoded binary after tree is rebuilt. </param>
        /// <param name="uncompressedSize"> Size of the uncompressed file, in bytes. </param>
        /// <returns> Decoded bytes. </returns>
        private byte[] DecodeHuffman(HuffmanDecode huffman, BinaryReader reader, uint uncompressedSize) {
            if (uncompressedSize <= 0) return null;

            byte[] buffer = new byte[uncompressedSize];
            uint bufferIndex = 0;

            // Decode until uncompressed size is reached
            while (bufferIndex < uncompressedSize) {
                // Start from the root of the tree
                uint arrayIndex = huffman.arrayIndex;

                // Traverse the huffman tree, when arrayIndex drops below 256 we have found a leaf node
                while (arrayIndex >= 256) {

                    // When previous byte is processed, read next byte
                    if (huffman.bitShift == 0) {
                        huffman.currentByte = reader.ReadByte();
                        huffman.bitShift = 8;
                    }

                    // Get next bit in the current byte
                    huffman.bitShift -= 1;
                    uint bit = (huffman.currentByte >> huffman.bitShift) & 1;

                    if (bit == 1) { // bit 1
                        arrayIndex = huffman.bit1_array[arrayIndex];
                    } else { // bit 0
                        arrayIndex = huffman.bit0_array[arrayIndex];
                    }
                }

                // Write the leaf node byte to the buffer 
                buffer[bufferIndex++] = (byte)arrayIndex;
            }

            return buffer;
        }

        #endregion

    }

}