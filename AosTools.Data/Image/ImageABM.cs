using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Reflection;

namespace AosTools.Data {

    public enum ABMType {
        Unknown,
        NotImplemented,
        Single,
        MultiFrame
    }

    public class DataABM {

        public ABMType ImageType = ABMType.Unknown;

        // Header data
        public BITMAPFILEHEADER FileHeader;
        public BITMAPINFOHEADER InfoHeader;
        public ABMANIMHEADER AnimHeader;

        // Animation image data
        public AnimHeaderJson AnimationJson;
        public List<AnimationFrame> Frames;

        // Single image data
        public byte[] ColorData;

        /// <summary>
        /// Initializes new empty abm data.
        /// </summary>
        public DataABM() {
            FileHeader = new BITMAPFILEHEADER();
            InfoHeader = new BITMAPINFOHEADER();
            AnimHeader = new ABMANIMHEADER();
            Frames = new List<AnimationFrame>();
        }
    }

    public class AnimationFrame {
        public string FrameName;
        public byte[] FrameData;
    }

    public class AnimHeaderJson {

        public string AosToolsVersion { get; set; }
        public string[] AnimationFrameNames { get; set; }
        public ABMANIMHEADER AnimationHeader { get; set; }

        public AnimHeaderJson(string version, string[] frameNames, ABMANIMHEADER animHeader) {
            AosToolsVersion = version;
            AnimationFrameNames = frameNames;
            AnimationHeader = animHeader;
        }

        public string ToJson(Formatting formatting = Formatting.Indented) {
            return JsonConvert.SerializeObject(this, formatting);
        }

        // FromJson, read json file

    }

    public class ImageABM {

        public static DataABM DecodeABM(byte[] data, string fileName) {

            DataABM abm = new DataABM();

            using (MemoryStream stream = new MemoryStream(data)) {
                using (BinaryReader reader = new BinaryReader(stream)) {

                    abm.FileHeader.bfType = reader.ReadUInt16();
                    if (abm.FileHeader.bfType != 0x4D42) {
                        throw new InvalidDataException($"Invalid ABM file signature on decode.");
                    }
                    abm.FileHeader.bfSize = reader.ReadUInt32(); // Confirm this when creating bmp from decoded color data
                    abm.FileHeader.bfReserved1 = reader.ReadUInt16();
                    abm.FileHeader.bfReserved2 = reader.ReadUInt16();
                    abm.FileHeader.bfOffBits = reader.ReadUInt32();

                    abm.InfoHeader.biSize = reader.ReadUInt32();
                    if (abm.InfoHeader.biSize != 0x28) {
                        throw new InvalidDataException($"Invalid ABM info header size {abm.InfoHeader.biSize}");
                    }
                    abm.InfoHeader.biWidth = reader.ReadInt32();
                    abm.InfoHeader.biHeight = reader.ReadInt32();
                    abm.InfoHeader.biPlanes = reader.ReadUInt16();
                    abm.InfoHeader.biBitCount = reader.ReadUInt16();
                    abm.InfoHeader.biCompression = reader.ReadUInt32(); // Check this when creating bmp
                    abm.InfoHeader.biSizeImage = reader.ReadUInt32(); // Check this when creating bmp
                    abm.InfoHeader.biXPelsPerMeter = reader.ReadInt32();
                    abm.InfoHeader.biYPelsPerMeter = reader.ReadInt32();
                    abm.InfoHeader.biClrUsed = reader.ReadUInt32(); // Check this when creating bmp
                    abm.InfoHeader.biClrImportant = reader.ReadUInt32(); // Check this when creating bmp

                    uint unpackedSize;

                    switch (abm.InfoHeader.biBitCount) {
                        case 1: // Encoded animated bitmap 24 bit color first frame, other frames are 32 bit color
                            abm.InfoHeader.biBitCount = 32;
                            abm.ImageType = ABMType.MultiFrame;

                            abm.AnimHeader = ReadAnimationHeader(reader);
                            if (abm.AnimHeader.frameCount <= 0) {
                                throw new InvalidDataException($"Invalid animated abm frame count {abm.AnimHeader.frameCount}");
                            }

                            // Unpack the first frame
                            unpackedSize = (uint)(abm.InfoHeader.biWidth * abm.InfoHeader.biHeight * 32 / 8);
                            stream.Seek(abm.AnimHeader.frameOffsets[0], SeekOrigin.Begin);
                            AnimationFrame firstFrame = new AnimationFrame();
                            firstFrame.FrameName = $"{fileName}#{0:D3}";

                            firstFrame.FrameData = new byte[unpackedSize];
                            for (int i = 0; i < unpackedSize; i += 4) {
                                firstFrame.FrameData[i] = reader.ReadByte();
                                firstFrame.FrameData[i + 1] = reader.ReadByte();
                                firstFrame.FrameData[i + 2] = reader.ReadByte();
                                firstFrame.FrameData[i + 3] = 0xFF;
                            }

                            firstFrame.FrameData = FlipColorBuffer(firstFrame.FrameData, abm.InfoHeader.biWidth, abm.InfoHeader.biHeight, 32);
                            abm.Frames.Add(firstFrame);

                            unpackedSize = (uint)(abm.InfoHeader.biWidth * abm.InfoHeader.biHeight * abm.InfoHeader.biBitCount / 8);
                            // Unpack rest of the frames if there are any
                            for (int i = 1; i < abm.AnimHeader.frameCount; i++) {
                                stream.Seek(abm.AnimHeader.frameOffsets[i], SeekOrigin.Begin);
                                AnimationFrame frame = new AnimationFrame();
                                frame.FrameName = $"{fileName}#{i:D3}";
                                frame.FrameData = Unpack32(reader, unpackedSize);
                                frame.FrameData = FlipColorBuffer(frame.FrameData, abm.InfoHeader.biWidth, abm.InfoHeader.biHeight, abm.InfoHeader.biBitCount);
                                abm.Frames.Add(frame);
                            }

                            abm.AnimationJson = new AnimHeaderJson(
                                Assembly.GetExecutingAssembly().GetName().Version.ToString(3),
                                GetFrameNameArray(abm.Frames),
                                abm.AnimHeader
                            );

                            break;
                        case 2: // Encoded animated bitmap 32 bit color, transparent frames
                            abm.InfoHeader.biBitCount = 32;
                            abm.ImageType = ABMType.MultiFrame;

                            abm.AnimHeader = ReadAnimationHeader(reader);
                            if (abm.AnimHeader.frameCount <= 0) {
                                throw new InvalidDataException($"Invalid animated abm frame count {abm.AnimHeader.frameCount}");
                            }

                            unpackedSize = (uint)(abm.InfoHeader.biWidth * abm.InfoHeader.biHeight * abm.InfoHeader.biBitCount / 8);

                            abm.Frames = UnpackFrames(reader, abm, unpackedSize, fileName);

                            abm.AnimationJson = new AnimHeaderJson(
                                Assembly.GetExecutingAssembly().GetName().Version.ToString(3),
                                GetFrameNameArray(abm.Frames),
                                abm.AnimHeader
                            );

                            break;
                        case 3: // Encoded animated bitmap 32 bit color, opaque frames
                            abm.InfoHeader.biBitCount = 32;
                            abm.ImageType = ABMType.MultiFrame;

                            abm.AnimHeader = ReadAnimationHeader(reader);
                            if (abm.AnimHeader.frameCount <= 0) {
                                throw new InvalidDataException($"Invalid animated abm frame count {abm.AnimHeader.frameCount}");
                            }

                            unpackedSize = (uint)(abm.InfoHeader.biWidth * abm.InfoHeader.biHeight * abm.InfoHeader.biBitCount / 8);

                            abm.Frames = UnpackFrames(reader, abm, unpackedSize, fileName);

                            abm.AnimationJson = new AnimHeaderJson(
                                Assembly.GetExecutingAssembly().GetName().Version.ToString(3),
                                GetFrameNameArray(abm.Frames),
                                abm.AnimHeader
                            );

                            break;
                        case 8: // Encoded alpha mask
                            abm.ImageType = ABMType.NotImplemented;
                            // Have not found a file for this yet but there are signs that it exists inside the engine
                            break;
                        case 24: // Encoded opaque bitmap
                            abm.ImageType = ABMType.Single;
                            stream.Seek(abm.FileHeader.bfOffBits, SeekOrigin.Begin);
                            unpackedSize = (uint)(abm.InfoHeader.biWidth * abm.InfoHeader.biHeight * abm.InfoHeader.biBitCount / 8);
                            abm.ColorData = Unpack24(reader, unpackedSize);
                            abm.ColorData = FlipColorBuffer(abm.ColorData, abm.InfoHeader.biWidth, abm.InfoHeader.biHeight, abm.InfoHeader.biBitCount);
                            break;
                        case 32: // Encoded transparent bitmap
                            abm.ImageType = ABMType.Single;
                            stream.Seek(abm.FileHeader.bfOffBits, SeekOrigin.Begin);
                            unpackedSize = (uint)(abm.InfoHeader.biWidth * abm.InfoHeader.biHeight * abm.InfoHeader.biBitCount / 8);
                            abm.ColorData = Unpack32(reader, unpackedSize);
                            abm.ColorData = FlipColorBuffer(abm.ColorData, abm.InfoHeader.biWidth, abm.InfoHeader.biHeight, abm.InfoHeader.biBitCount);
                            break;
                        default:
                            abm.ImageType = ABMType.Unknown;
                            break;
                    }

                    return abm;
                }
            }
        }

        private static ABMANIMHEADER ReadAnimationHeader(BinaryReader reader) {
            ABMANIMHEADER animHeader = new ABMANIMHEADER();
            animHeader.abmType = reader.ReadUInt16();
            animHeader.animMode = reader.ReadUInt16();
            animHeader.frameCount = reader.ReadUInt32();
            animHeader.frameSequenceSize = reader.ReadUInt32();

            animHeader.frameOffsets = new uint[animHeader.frameCount];
            for (int i = 0; i < animHeader.frameCount; i++) {
                animHeader.frameOffsets[i] = reader.ReadUInt32();
            }

            int frameSequenceLenght = (int)(animHeader.frameSequenceSize / 2);

            animHeader.frameSequence = new ushort[frameSequenceLenght];
            for (int i = 0; i < frameSequenceLenght; i++) {
                animHeader.frameSequence[i] = reader.ReadUInt16();
            }

            return animHeader;
        }

        private static List<AnimationFrame> UnpackFrames(BinaryReader reader, DataABM abm, uint unpackedSize, string fileName) {
            List<AnimationFrame> frames = new List<AnimationFrame>();
            Stream stream = reader.BaseStream;

            for (int i = 0; i < abm.AnimHeader.frameCount; i++) {
                stream.Seek(abm.AnimHeader.frameOffsets[i], SeekOrigin.Begin);
                byte[] buffer;
                AnimationFrame frame = new AnimationFrame();

                // There is a weird limit of 127 different frames so only 3 digit numbers are needed
                frame.FrameName = $"{fileName}#{i:D3}";

                switch (abm.InfoHeader.biBitCount) {
                    case 8:
                        throw new NotImplementedException();
                    case 24:
                        buffer = Unpack24(reader, unpackedSize);
                        buffer = FlipColorBuffer(buffer, abm.InfoHeader.biWidth, abm.InfoHeader.biHeight, abm.InfoHeader.biBitCount);
                        break;
                    case 32:
                        buffer = Unpack32(reader, unpackedSize);
                        buffer = FlipColorBuffer(buffer, abm.InfoHeader.biWidth, abm.InfoHeader.biHeight, abm.InfoHeader.biBitCount);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                frame.FrameData = buffer;
                frames.Add(frame);
            }

            return frames;
        }

        private static byte[] Unpack24(BinaryReader reader, uint unpackedSize) {
            byte[] colorBuffer = new byte[unpackedSize];

            int i = 0;

            while (i < unpackedSize) {
                byte value = reader.ReadByte();

                switch (value) {
                    case 0x00:
                        byte count = reader.ReadByte();
                        if (count == 0x00) continue;
                        i += count;
                        break;
                    case 0xFF:
                        int count2 = reader.ReadByte();
                        if (count2 == 0x00) continue;
                        reader.Read(colorBuffer, i, count2);
                        i += count2;
                        break;
                    default:
                        colorBuffer[i++] = reader.ReadByte();
                        break;
                }
            }

            return colorBuffer;
        }

        private static byte[] Unpack32(BinaryReader reader, uint unpackedSize) {
            byte[] colorBuffer = new byte[unpackedSize];

            int i = 0;
            int colorComponent = 0;

            while (i < unpackedSize) {
                byte value = reader.ReadByte();

                switch (value) {
                    case 0x00:
                        byte count = reader.ReadByte();
                        if (count == 0x00) continue;
                        for (int j = 0; j < count; ++j) {
                            ++i;
                            if (++colorComponent == 3) {
                                ++i;
                                colorComponent = 0;
                            }
                        }
                        break;
                    case 0xFF:
                        byte count2 = reader.ReadByte();
                        if (count2 == 0x00) continue;
                        for (int j = 0; j < count2 && i < unpackedSize; ++j) {
                            colorBuffer[i++] = reader.ReadByte();
                            if (++colorComponent == 3) {
                                colorBuffer[i++] = 0xFF;
                                colorComponent = 0;
                            }
                        }
                        break;
                    default:
                        colorBuffer[i++] = reader.ReadByte();
                        if (++colorComponent == 3) {
                            colorBuffer[i++] = value;
                            colorComponent = 0;
                        }
                        break;
                }
            }

            return colorBuffer;
        }

        private static byte[] FlipColorBuffer(byte[] colorBuffer, int width, int height, ushort realBitCount) {
            byte[] flippedBuffer = new byte[colorBuffer.Length];
            int stride = width * (realBitCount / 8);
            int flippedOffset = 0;

            for (int j = 0; j < height; j++) {
                int strideOffset = colorBuffer.Length - stride - flippedOffset;
                ArraySegment<byte> strideSegment = new ArraySegment<byte>(colorBuffer, strideOffset, stride);
                strideSegment.CopyTo(flippedBuffer, flippedOffset);
                flippedOffset += stride;
            }

            return flippedBuffer;
        }

        private static string[] GetFrameNameArray(List<AnimationFrame> frames) {
            int count = frames.Count;
            string[] names = new string[count];

            for (int i = 0; i < count; i++) {
                names[i] = frames[i].FrameName;
            }

            return names;
        }
    }
}