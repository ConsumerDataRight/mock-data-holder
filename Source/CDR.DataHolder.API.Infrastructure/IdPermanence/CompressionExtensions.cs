using System.IO;
using System.IO.Compression;

namespace CDR.DataHolder.API.Infrastructure.IdPermanence
{
    public static class CompressionExtensions
    {
        /// <summary>
        /// Compresses a byte array and returns a deflate compressed, byte array.
        /// </summary>
        /// <param name="uncompressedString">String to compress</param>
        public static byte[] Compress(this byte[] uncompressedString)
        {
            byte[] compressedBytes;

            using (var uncompressedStream = new MemoryStream(uncompressedString))
            {
                using (var compressedStream = new MemoryStream())
                {
                    // setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
                    // this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
                    // although MSDN documentation states that ToArray() can be called on a closed MemoryStream, I don't want to rely on that very odd behavior should it ever change
                    using (var compressorStream = new DeflateStream(compressedStream, CompressionMode.Compress, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }

                    // call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
                    compressedBytes = compressedStream.ToArray();
                }
            }

            return compressedBytes;
        }

        /// <summary>
        /// Decompresses a deflate compressed, byte array and returns an uncompressed byte array.
        /// </summary>
        /// <param name="compressedString">String to decompress.</param>
        public static byte[] Decompress(this byte[] compressedString)
        {
            byte[] decompressedBytes;

            var compressedStream = new MemoryStream(compressedString);

            using (var decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    decompressorStream.CopyTo(decompressedStream);

                    decompressedBytes = decompressedStream.ToArray();
                }
            }

            return decompressedBytes;
        }
    }
}
