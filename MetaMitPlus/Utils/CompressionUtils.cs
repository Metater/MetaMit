using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MetaMitPlus.Utils
{
    internal class CompressionUtils
    {
        internal static byte[] Zip(byte[] unzippedData)
        {
            using (var msi = new MemoryStream(unzippedData))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }
                return mso.ToArray();
            }
        }
        internal static byte[] Unzip(byte[] zippedData)
        {
            using (var msi = new MemoryStream(zippedData))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return mso.ToArray();
            }
        }
    }
}
