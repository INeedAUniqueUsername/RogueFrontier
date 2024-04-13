using System.IO;
using System.IO.Compression;
using System.Text;

namespace Common;

public static class Space {
    public static void CopyTo(Stream src, Stream dest) {
        byte[] bytes = new byte[4096];
        int cnt;
        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
            dest.Write(bytes, 0, cnt);
        }
    }

    public static byte[] Zip(string str) {
        var bytes = Encoding.UTF8.GetBytes(str);

        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream()) {
            using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                //msi.CopyTo(gs);
                CopyTo(msi, gs);
            }

            return mso.ToArray();
        }
    }
    public static string Unzip(MemoryStream input) {
        using (var mso = new MemoryStream()) {
            input.Position = 0;
            using (var gs = new GZipStream(input, CompressionMode.Decompress)) {
                CopyTo(gs, mso);
                //gs.CopyTo(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }

    public static string Unzip(byte[] bytes) {
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream()) {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                //gs.CopyTo(mso);
                CopyTo(gs, mso);
            }

            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }
}
