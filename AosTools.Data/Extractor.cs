using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace AosTools.Data {

    /// <summary>
    /// Extracts files from an aos archive.
    /// </summary>
    public class Extractor {

        private readonly AosFileInfo m_FileInfo;
        private readonly bool m_NoDecode;

        /// <summary>
        /// Creates a new extractor.
        /// </summary>
        /// <param name="fileInfo"> File info. </param>
        /// <param name="noDecode"> When set to true, disables automatic decoding on scr/ABM files. </param>
        public Extractor(AosFileInfo fileInfo, bool noDecode = false) {
            m_FileInfo = fileInfo;
            m_NoDecode = noDecode;
        }

        /// <summary>
        /// Extracts all files from an aos archive.
        /// </summary>
        public OperationOutcome Extract() {

            Stopwatch stopwatch = Stopwatch.StartNew();

            uint entryCount = 0;

            using (FileStream stream = File.Open(m_FileInfo.InputPath, FileMode.Open)) {
                using (BinaryReader reader = new BinaryReader(stream)) {

                    // Read archive header
                    AOSHEADER hdr = ReadHeader(reader);

                    // Calculate archive entry count
                    entryCount = hdr.indexSize / AOSENTRY.k_EntryBytes;

                    // Read archive entries
                    AOSENTRY[] entries = ReadEntries(entryCount, reader);

                    // Sanity check on stream position
                    if (stream.Position != hdr.dataOffset) {
                        stream.Seek(hdr.dataOffset, SeekOrigin.Begin);
                    }

                    // Write entries to files
                    WriteEntryFiles(entries, reader, stream, hdr);

                }

            }

            stopwatch.Stop();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Successfully extracted archive {Path.GetFileName(m_FileInfo.InputPath)} in {stopwatch.ElapsedMilliseconds} ms");
            stringBuilder.AppendLine($"Files extracted {entryCount}");

            return new OperationOutcome(true, stringBuilder.ToString());
        }

        /// <summary>
        /// Reads aos file header.
        /// </summary>
        /// <param name="reader"> BinaryReader which has it's stream at position 0. </param>
        /// <returns> Header struct. </returns>
        private AOSHEADER ReadHeader(BinaryReader reader) {
            AOSHEADER hdr = new AOSHEADER();

            // Magic, 4 bytes
            hdr.fileSignature = reader.ReadUInt32();
            // Data offset, 4 bytes
            hdr.dataOffset = reader.ReadUInt32();
            // Index size, 4 bytes
            hdr.indexSize = reader.ReadUInt32();
            // Archive name, 261 bytes
            hdr.archiveName = reader.ReadBytes(AOSHEADER.k_ArchiveNameBytes);

            return hdr;
        }

        /// <summary>
        /// Reads all entries in the aos archive.
        /// </summary>
        /// <param name="entryCount"> How many entries exist in the archive. </param>
        /// <param name="reader"> BinaryReader which has it's stream at position after header. </param>
        /// <returns> Entry struct array. </returns>
        private AOSENTRY[] ReadEntries(uint entryCount, BinaryReader reader) {
            AOSENTRY[] entries = new AOSENTRY[entryCount];

            for (int i = 0; i < entryCount; i++) {
                entries[i].fileName = reader.ReadBytes(AOSENTRY.k_EntryNameBytes);
                entries[i].offset = reader.ReadUInt32();
                entries[i].size = reader.ReadUInt32();
            }

            return entries;
        }

        /// <summary>
        /// Writes entries to disk.
        /// </summary>
        /// <param name="entries"> Entry information. </param>
        /// <param name="reader"> BinaryReader which has it's stream at position at archive data offset. </param>
        /// <param name="stream"> FileStream for the archive. </param>
        /// <param name="hdr"> AOS file header. </param>
        private void WriteEntryFiles(AOSENTRY[] entries, BinaryReader reader, FileStream stream, AOSHEADER hdr) {

            Decoder decoder = new Decoder();

            uint baseOffset = hdr.dataOffset;

            // Get archive name without extension and combine it with output path
            string archiveName = DataUtility.ConvertFileName(hdr.archiveName);
            archiveName = Path.GetFileNameWithoutExtension(archiveName);
            string outputPath = Path.Combine(m_FileInfo.OutputPath, archiveName);
            Directory.CreateDirectory(outputPath);

            for (int i = 0; i < entries.Length; i++) {

                // Sanity check stream position with offset in entry information
                uint dataOffset = baseOffset + entries[i].offset;
                if (stream.Position != dataOffset) {
                    stream.Seek(dataOffset, SeekOrigin.Begin);
                }

                int bytesToRead = Convert.ToInt32(entries[i].size);
                byte[] buffer = reader.ReadBytes(bytesToRead);

                string fileName = DataUtility.ConvertFileName(entries[i].fileName);

                // Decode scr/ABM files
                if (!m_NoDecode) {
                    string fileExtension = Path.GetExtension(fileName);
                    switch (fileExtension) {
                        case ".scr":
                            buffer = decoder.DecodeScript(buffer);
                            fileName = Path.GetFileNameWithoutExtension(fileName) + ".txt";
                            break;
                        case ".abm":
                            DataABM abm;
                            try {
                                abm = ImageABM.DecodeABM(buffer, Path.GetFileNameWithoutExtension(fileName));
                            } catch (Exception) {
                                break;
                            }

                            switch (abm.ImageType) {
                                case ABMType.Single:
                                    buffer = ImageBMP.CreateBMP(abm.FileHeader, abm.InfoHeader, abm.ColorData);
                                    fileName = Path.GetFileNameWithoutExtension(fileName) + ".bmp";
                                    break;

                                case ABMType.MultiFrame:

                                    string jsonPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(fileName)}.json");
                                    File.WriteAllText(jsonPath, abm.AnimationJson.ToJson());

                                    for (int j = 0; j < abm.Frames.Count; j++) {
                                        string framePath = Path.Combine(outputPath, $"{abm.Frames[j].FrameName}.bmp");
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
                            fileName = Path.GetFileNameWithoutExtension(fileName) + ".bmp";
                            break;
                        default:
                            // No decoding needed
                            break;
                    }
                }

                string filePath = Path.Combine(outputPath, fileName);

                using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                    using (BinaryWriter writer = new BinaryWriter(fileStream)) {
                        writer.Write(buffer);
                    }
                }

            }
        }

    }

}
