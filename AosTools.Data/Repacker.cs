using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace AosTools.Data {

    /// <summary>
    /// Repacks files to an aos archive.
    /// </summary>
    public class Repacker {

        private readonly AosFileInfo m_FileInfo;
        private readonly bool m_NoEncode;

        /// <summary>
        /// Creates a new repacker.
        /// </summary>
        /// <param name="fileInfo"> File info. </param>
        /// <param name="noEncode"> When set to true, disables automatic encoding on txt/ABM files. </param>
        public Repacker(AosFileInfo fileInfo, bool noEncode = false) {
            m_FileInfo = fileInfo;
            m_NoEncode = noEncode;
        }

        /// <summary>
        /// Repacks all files in a directory to an aos archive.
        /// </summary>
        public OperationOutcome Repack() {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string[] filePaths = Directory.GetFiles(m_FileInfo.InputPath);
            int fileCount = filePaths.Length;
            if (fileCount == 0) return new OperationOutcome(false, "Repack cancelled, input directory is empty.");

            // Check that we have valid file names to work with
            if (!ValidateFileNames(filePaths)) {
                return new OperationOutcome(false, $"Repack cancelled, file names cannot exceed {AOSENTRY.k_EntryNameBytes} characters including the file extenstion.");
            }

            string outputFileName = new DirectoryInfo(m_FileInfo.InputPath).Name + ".aos";
            string outputFilePath = Path.Combine(m_FileInfo.OutputPath, outputFileName);

            AOSHEADER hdr = CreateHeader(fileCount, outputFileName);
            AOSENTRY[] entries = new AOSENTRY[fileCount];

            using (FileStream fileStream = File.Open(outputFilePath, FileMode.Create, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fileStream)) {

                    WriteHeader(writer, hdr);

                    // Write empty index
                    writer.Write(new byte[hdr.indexSize]);

                    WriteEntries(writer, filePaths, entries);

                    WriteEntryIndex(writer, entries);

                }
            }

            FileInfo outputFile = new FileInfo(outputFilePath);

            stopwatch.Stop();

            StringBuilder stringBuilder = new StringBuilder();
            string veryImportant = fileCount == 1 ? "file" : "files";
            stringBuilder.AppendLine($"Successfully repacked {fileCount} {veryImportant} in {stopwatch.ElapsedMilliseconds} ms");
            stringBuilder.AppendLine($"Total archive size: {outputFile.Length} bytes");

            return new OperationOutcome(true, stringBuilder.ToString());
        }

        /// <summary>
        /// Creates aos file header.
        /// </summary>
        /// <param name="fileCount"> Amount of files to repack. </param>
        /// <param name="fileName"> Archive file name. </param>
        /// <returns> Header struct. </returns>
        private AOSHEADER CreateHeader(int fileCount, string fileName) {
            AOSHEADER hdr = new AOSHEADER();

            hdr.fileSignature = 0; // 00 00 00 00

            hdr.indexSize = AOSENTRY.k_EntryBytes * Convert.ToUInt32(fileCount);

            hdr.dataOffset = AOSHEADER.k_HeaderBytes + hdr.indexSize;

            hdr.archiveName = new byte[AOSHEADER.k_ArchiveNameBytes];
            byte[] nameBytes = DataUtility.ConvertFileName(fileName);
            for (int i = 0; i < nameBytes.Length; i++) {
                hdr.archiveName[i] = nameBytes[i];
            }

            return hdr;
        }

        /// <summary>
        /// Writes the archive header.
        /// </summary>
        /// <param name="writer"> Binary writer, stream should be at position 0. </param>
        /// <param name="hdr"> Header struct. </param>
        private void WriteHeader(BinaryWriter writer, AOSHEADER hdr) {
            writer.Write(hdr.fileSignature);
            writer.Write(hdr.dataOffset);
            writer.Write(hdr.indexSize);
            writer.Write(hdr.archiveName);
        }

        /// <summary>
        /// Writes entry files to the archive, updates entry info and encodes files if necessary.
        /// </summary>
        /// <param name="writer"> BinaryWriter, stream position should be after entry index. </param>
        /// <param name="filePaths"> File paths. </param>
        /// <param name="entries"> Empty entry info. </param>
        private void WriteEntries(BinaryWriter writer, string[] filePaths, AOSENTRY[] entries) {
            int entryCount = filePaths.Length;
            uint currentOffset = 0;

            Encoder encoder = new Encoder();

            for (int i = 0; i < entryCount; i++) {

                string fileName = Path.GetFileName(filePaths[i]);
                byte[] buffer = File.ReadAllBytes(filePaths[i]);

                // Check if file needs to be encoded before repacking
                if (!m_NoEncode) {
                    string fileExtension = Path.GetExtension(fileName);
                    switch (fileExtension) {
                        case ".txt":
                            buffer = encoder.EncodeScript(buffer);
                            fileName = Path.GetFileNameWithoutExtension(fileName) + ".scr";
                            break;
                        case ".abm":
                            buffer = encoder.EncodeABM(buffer);
                            break;
                        default:
                            // No encoding needed
                            break;
                    }
                }

                // Update entry struct file name
                entries[i].fileName = new byte[AOSENTRY.k_EntryNameBytes];
                byte[] nameBytes = DataUtility.ConvertFileName(fileName);
                for (int j = 0; j < nameBytes.Length; j++) {
                    entries[i].fileName[j] = nameBytes[j];
                }

                // Update entry struct size and offset
                entries[i].size = Convert.ToUInt32(buffer.Length);
                entries[i].offset = currentOffset;
                currentOffset += entries[i].size;

                // Write file buffer
                writer.Write(buffer);
            }

        }

        /// <summary>
        /// Writes the entry index.
        /// </summary>
        /// <param name="writer"> BinaryWriter. </param>
        /// <param name="entries"> Entry info. </param>
        private void WriteEntryIndex(BinaryWriter writer, AOSENTRY[] entries) {
            Stream stream = writer.BaseStream;

            // Set stream position to entry index beginning
            stream.Seek(AOSHEADER.k_HeaderBytes, SeekOrigin.Begin);

            // Write entry info
            for (int i = 0; i < entries.Length; i++) {
                writer.Write(entries[i].fileName);
                writer.Write(entries[i].offset);
                writer.Write(entries[i].size);
            }
        }

        /// <summary>
        /// Validates file names that they do not exceed entry info name bytes.
        /// </summary>
        /// <param name="filePaths"> Files to validate. </param>
        /// <returns> <see langword="true"/> when file names are valid, otherwise <see langword="false"/>. </returns>
        private bool ValidateFileNames(string[] filePaths) {
            for (int i = 0; i < filePaths.Length; i++) {
                string fileName = Path.GetFileName(filePaths[i]);
                if (fileName.Length > AOSENTRY.k_EntryNameBytes) return false;
            }
            return true;
        }

    }

}