using System.IO;
using System.Collections;

namespace AosTools.Data {

    /// <summary>
    /// <para> An extension class to BinaryWriter which allows writing bits to a stream. </para>
    /// <para> Writes bits in Big Endian byte order. </para>
    /// <para> Remember to call Flush method when you finish writing to clear and write any unwritten buffer. </para>
    /// </summary>
    public class BitWriter : BinaryWriter {

        /// <summary>
        /// Endianness.
        /// </summary>
        public enum ByteOrder {
            BigEndian,
            LittleEndian
        }

        private bool[] m_CurrentByte = new bool[8];
        private byte m_CurrentBitIndex = 0;
        private BitArray m_BitArray;

        public BitWriter(Stream stream) : base(stream) { }

        /// <summary>
        /// <para> Clears all buffers for the current writer and causes any buffered data to be written. </para>
        /// <para> Unwritten part of the bit buffer will be written as 0's. </para>
        /// </summary>
        public override void Flush() {
            // Write byte only when at least one bit has been written in it
            if (m_CurrentBitIndex != 0) base.Write(ConvertToByte(m_CurrentByte));
            base.Flush();
        }

        /// <summary>
        /// <para> Writes a bit to the memory. </para>
        /// <para> When full byte is written, the byte will be written to the stream. </para>
        /// </summary>
        /// <param name="value"> <see langword="true"/> = 1 | <see langword="false"/> = 0 </param>
        public void BitWrite(bool value) {
            // Write the next bit
            m_CurrentByte[m_CurrentBitIndex] = value;
            m_CurrentBitIndex++;

            // When full byte is written use BinaryWriter to write the byte to the stream
            // and create a new byte to write bits to
            if (m_CurrentBitIndex == 8) {
                base.Write(ConvertToByte(m_CurrentByte));
                this.m_CurrentBitIndex = 0;
                this.m_CurrentByte = new bool[8];
            }
        }

        /// <summary>
        /// <para> Writes a <see langword="bool"/> array to the stream. </para>
        /// <para> Each <see langword="bool"/> is written as a single bit. </para>
        /// </summary>
        /// <param name="values"> <see langword="bool"/> array to write. </param>
        public void BitWrite(bool[] values) {
            for (int i = 0; i < values.Length; i++) {
                this.BitWrite(values[i]);
            }
        }

        /// <summary>
        /// <para> Writes a <see langword="byte"/> to the stream. </para>
        /// <para> BitWriter writes this data type in big endian format. </para>
        /// </summary>
        /// <param name="value"> <see langword="byte"/> to write. </param>
        public void BitWrite(byte value) {
            m_BitArray = new BitArray(new byte[] { value });
            for (byte i = 8; i > 0; i--) {
                this.BitWrite(m_BitArray[i - 1]);
            }
            m_BitArray = null;
        }

        /// <summary>
        /// <para> Writes a <see langword="byte"/> buffer to the stream. </para>
        /// <para> BitWriter writes this data type in big endian format. </para>
        /// </summary>
        /// <param name="buffer"> <see langword="byte"/> buffer. </param>
        public void BitWrite(byte[] buffer) {
            for (int i = 0; i < buffer.Length; i++) {
                this.BitWrite((byte)buffer[i]);
            }
        }

        /// <summary>
        /// Converts 8 bool array to a byte.
        /// </summary>
        /// <param name="bools"> Boolean array. </param>
        /// <param name="endianness"> Byte order to use. </param>
        /// <returns> Converted byte. </returns>
        public static byte ConvertToByte(bool[] bools, ByteOrder endianness = ByteOrder.BigEndian) {
            byte b = 0; // 0000 0000
            byte bitShift = 0;

            byte b1 = 1; // 0000 0001
            byte b128 = 128; // 1000 0000

            if (endianness == ByteOrder.BigEndian) {
                for (int i = 0; i < 8; i++) {
                    if (bools[i]) {
                        // Write 1-Bit to the current shift position, big endian
                        b |= (byte)((b128) >> bitShift);
                    }
                    bitShift++;
                }
            } else {
                for (int i = 0; i < 8; i++) {
                    if (bools[i]) {
                        // Write 1-Bit to the current shift position, little endian
                        b |= (byte)((b1) << bitShift);
                    }
                    bitShift++;
                }
            }

            return b;
        }
    }

}