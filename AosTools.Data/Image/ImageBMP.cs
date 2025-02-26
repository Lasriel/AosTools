using System.IO;

namespace AosTools.Data {

    public class ImageBMP {

        private readonly BITMAPFILEHEADER m_FileHeader;
        private readonly BITMAPINFOHEADER m_InfoHeader;
        private readonly byte[] m_ColorData;

        public static byte[] CreateBMP(BITMAPFILEHEADER fileHeader, BITMAPINFOHEADER infoHeader, byte[] colorData) {
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    // Write file header
                    writer.Write(fileHeader.bfType);
                    writer.Write(fileHeader.bfSize);
                    writer.Write(fileHeader.bfReserved1);
                    writer.Write(fileHeader.bfReserved2);
                    writer.Write(fileHeader.bfOffBits);

                    // Write info header
                    writer.Write(infoHeader.biSize);
                    writer.Write(infoHeader.biWidth);
                    writer.Write(infoHeader.biHeight);
                    writer.Write(infoHeader.biPlanes);
                    writer.Write(infoHeader.biBitCount);
                    writer.Write(infoHeader.biCompression);
                    writer.Write(infoHeader.biSizeImage);
                    writer.Write(infoHeader.biXPelsPerMeter);
                    writer.Write(infoHeader.biYPelsPerMeter);
                    writer.Write(infoHeader.biClrUsed);
                    writer.Write(infoHeader.biClrImportant);

                    // Write color data
                    writer.Write(colorData);

                    return stream.ToArray();
                }
            }
        }

        // Create bmp files from header and color data
        public ImageBMP(BITMAPFILEHEADER fileHeader, BITMAPINFOHEADER infoHeader, byte[] colorData) {
            m_FileHeader = fileHeader;
            m_InfoHeader = infoHeader;
            m_ColorData = colorData;
        }

    }

}