using System;
using System.Text;

namespace AosTools.Data {

    /// <summary>
    /// Utility methods for handling binary data.
    /// </summary>
    public class DataUtility {

        /// <summary>
        /// Trims all null bytes (00) from the byte arrays end.
        /// </summary>
        /// <param name="array"> Byte array to trim. </param>
        /// <returns> Trimmed byte array. </returns>
        public static byte[] TrimEnd(byte[] array) {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);
            Array.Resize(ref array, lastIndex + 1);
            return array;
        }

        /// <summary>
        /// Converts a byte array to a string using ASCII encoding and trims null bytes.
        /// </summary>
        /// <param name="nameBytes"> Bytes to convert. </param>
        /// <returns> File name as string. </returns>
        public static string ConvertFileName(byte[] nameBytes) => Encoding.ASCII.GetString(TrimEnd(nameBytes));

        /// <summary>
        /// Converts a byte to string of bits.
        /// </summary>
        public static string ByteToString(byte b) => Convert.ToString(b, 2).PadLeft(8, '0');

        /// <summary>
        /// Converts uint to hex string.
        /// </summary>
        public static string UIntToHex(uint value) => String.Format("{0:X2}", value);

    }

}