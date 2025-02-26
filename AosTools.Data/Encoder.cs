using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AosTools.Data {

    /// <summary>
    /// Encodes txt/ABM files.
    /// </summary>
    public class Encoder {

        private readonly AosFileInfo m_FileInfo;

        /// <summary>
        /// Creates a new encoder.
        /// </summary>
        /// <param name="fileInfo"> File info. </param>
        public Encoder(AosFileInfo fileInfo = null) {
            m_FileInfo = fileInfo;
        }

        /// <summary>
        /// Starts encoding.
        /// </summary>
        public OperationOutcome Encode() {
            if (m_FileInfo == null) return new OperationOutcome(false, "Encoding failed, no file info was passed to encoder.");

            switch (m_FileInfo.FileMode) {
                case Mode.File:
                    return EncodeFile();
                case Mode.Directory:
                    return EncodeDirectory();
                default:
                    return new OperationOutcome(false, "Encoding failed, invalid file mode.");
            }
        }

        /// <summary>
        /// Encodes a single text file using huffman coding.
        /// </summary>
        /// <param name="data"> Data to encode. </param>
        /// <returns> Huffman encoded data. </returns>
        public byte[] EncodeScript(byte[] data) {
            long fileSize;
            HuffmanEncoder<byte> huffman;

            using (MemoryStream inputStream = new MemoryStream(data)) {
                using (BinaryReader reader = new BinaryReader(inputStream)) {

                    // File size that is used for decoding huffman encoded scripts
                    fileSize = inputStream.Length;

                    // Count all unique bytes
                    Dictionary<byte, uint> byteCounts = CountFileBytes(reader, fileSize);

                    // Build huffman tree
                    huffman = new HuffmanEncoder<byte>(byteCounts);

                    // Reset stream position
                    inputStream.Position = 0;

                    using (MemoryStream outputStream = new MemoryStream()) {
                        using (BitWriter writer = new BitWriter(outputStream)) {

                            // Write uncompressed size
                            writer.Write(Convert.ToUInt32(fileSize));

                            // Writes the entire huffman tree recursively
                            WriteNode(writer, huffman.RootNode);

                            // Write encoded bytes
                            while (inputStream.Position != fileSize) {
                                byte readByte = reader.ReadByte();
                                writer.BitWrite(huffman.Encode(readByte));
                            }

                            // Write possible trailing bytes and return bytes that were written in memory stream
                            writer.Flush();
                            return outputStream.ToArray();
                        }
                    }

                }
            }
        }

        public byte[] EncodeABM(byte[] encodedData) {
            // ToDo
            return encodedData;
        }

        /// <summary>
        /// Encodes a single file.
        /// </summary>
        private OperationOutcome EncodeFile() {
            Stopwatch stopwatch = Stopwatch.StartNew();

            byte[] fileData = File.ReadAllBytes(m_FileInfo.InputPath);

            byte[] encodedData;
            string outputExtension;

            string fileExtension = m_FileInfo.FileExtension;
            switch (fileExtension) {
                case ".txt":
                    encodedData = EncodeScript(fileData);
                    outputExtension = ".scr";
                    break;
                case ".abm":
                    encodedData = EncodeABM(fileData);
                    outputExtension = ".abm";
                    break;
                default:
                    // Unknown file extension
                    return new OperationOutcome(false, $"Encoding failed, unexpected file extension [{fileExtension}]!");
            }

            if (encodedData == null || encodedData.Length <= 0) return new OperationOutcome(false, "Encoding failed, invalid data to encode.");

            string fileName = m_FileInfo.FileNameNoExt;
            string fileNameFull = fileName + outputExtension;
            string filePath = Path.Combine(m_FileInfo.OutputPath, fileNameFull);

            using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                    writer.Write(encodedData);
                }
            }

            stopwatch.Stop();
            return new OperationOutcome(true, $"Successfully encoded file {m_FileInfo.FileName} in {stopwatch.ElapsedMilliseconds} ms");

        }

        /// <summary>
        /// Encodes all files in a directory.
        /// </summary>
        private OperationOutcome EncodeDirectory() {
            string[] filePaths = Directory.GetFiles(m_FileInfo.InputPath);
            if (filePaths.Length == 0) return new OperationOutcome(false, "Encode cancelled, input directory is empty.");

            Stopwatch stopwatch = Stopwatch.StartNew();

            StringBuilder stringBuilder = new StringBuilder();
            List<string> skippedFileNames = new List<string>();
            int filesSkipped = 0;
            int filesEncoded = 0;

            for (int i = 0; i < filePaths.Length; i++) {

                string inputPath = filePaths[i];

                byte[] encodedData;
                string outputExtension;

                string fileExtension = Path.GetExtension(inputPath);
                switch (fileExtension) {
                    case ".txt":
                        encodedData = EncodeScript(File.ReadAllBytes(inputPath));
                        outputExtension = ".scr";
                        break;
                    case ".abm":
                        encodedData = EncodeABM(File.ReadAllBytes(inputPath));
                        outputExtension = ".abm";
                        break;
                    default:
                        skippedFileNames.Add(Path.GetFileName(inputPath));
                        filesSkipped++;
                        continue;
                }

                if (encodedData == null || encodedData.Length <= 0) {
                    skippedFileNames.Add(Path.GetFileName(inputPath));
                    filesSkipped++;
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                string fileNameFull = fileName + outputExtension;
                string filePath = Path.Combine(m_FileInfo.OutputPath, fileNameFull);

                using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                    using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                        writer.Write(encodedData);
                    }
                }

                filesEncoded++;
            }

            stopwatch.Stop();

            string veryImportant = filesEncoded == 1 ? "file" : "files";
            stringBuilder.AppendLine($"Successfully encoded {filesEncoded} {veryImportant} in {stopwatch.ElapsedMilliseconds} ms");

            if (skippedFileNames.Count != 0) {
                stringBuilder.AppendLine($"Skipped files {filesSkipped}:");
                foreach (string fileName in skippedFileNames) {
                    stringBuilder.AppendLine(fileName);
                }
            }

            return new OperationOutcome(true, stringBuilder.ToString());
        }

        /// <summary>
        /// Recursively writes huffman tree as binary.
        /// </summary>
        /// <param name="writer"> BitWriter used for writing nodes and leaf node values. </param>
        /// <param name="node"> Start node should always be the root node of the tree, otherwise nodes get passed in recursively. </param>
        private void WriteNode(BitWriter writer, HuffmanNode<byte> node) {
            // When leaf node is encountered, write 0-Bit followed by leaf node value
            if (node.IsLeaf) {
                writer.BitWrite(false);
                writer.BitWrite(node.Value);
                return;
            }

            // When internal node is enountered, write 1-Bit and continue traversing the tree
            writer.BitWrite(true);

            if (node.LeftChild != null) WriteNode(writer, node.LeftChild);
            if (node.RightChild != null) WriteNode(writer, node.RightChild);
        }

        /// <summary>
        /// Counts all the unique bytes in a file.
        /// </summary>
        /// <param name="reader"> BinaryReader using stream of the file which to read. </param>
        /// <param name="fileSize"> Full size of the file/stream. </param>
        /// <returns> Dictionary of bytes and their counts. </returns>
        private Dictionary<byte, uint> CountFileBytes(BinaryReader reader, long fileSize) {
            Dictionary<byte, uint> byteCounts = new Dictionary<byte, uint>();
            Stream stream = reader.BaseStream;

            if (stream.Position != 0) {
                stream.Seek(0, SeekOrigin.Begin);
            }

            while (stream.Position != fileSize) {
                byte readByte = reader.ReadByte();
                if (!byteCounts.ContainsKey(readByte)) {
                    byteCounts[readByte] = 0;
                }
                byteCounts[readByte]++;
            }

            return byteCounts;
        }

    }

}