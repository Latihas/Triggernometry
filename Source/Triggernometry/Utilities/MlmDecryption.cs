using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Triggernometry.Utilities;

/// <summary> https://github.com/Magic-Xin/DecryptionMLM </summary>
public static class MlmDecryption {
    private static Encoding encoding = Encoding.UTF8;

    public static string DecryptDES(string decryptString, string key) {
        decryptString = decryptString.Replace('@', '/');
        var bytes = ProcessDES(Convert.FromBase64String(decryptString), key, false);
        return encoding.GetString(bytes);
    }

    private static byte[] ProcessDES(byte[] data, string key, bool isEncrypt) {
        using (var cryptoServiceProvider = DES.Create()) {
            var array1 = Md5(key);
            var array2 = new ArraySegment<byte>(array1, 0, 8).ToArray();
            var array3 = new ArraySegment<byte>(array1, 8, 8).ToArray();
            var transform = isEncrypt ? cryptoServiceProvider.CreateEncryptor(array2, array3) : cryptoServiceProvider.CreateDecryptor(array2, array3);
            using (var memoryStream = new MemoryStream()) {
                using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write)) {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }
    }

    public static byte[] Md5(string str) {
        using (var md5 = MD5.Create()) {
            return md5.ComputeHash(Encoding.UTF8.GetBytes(str));
        }
    }

    public static string TryDecrypt(string data) {
        if (data.StartsWith("mlm-")) // 加密：解密后进入下个 if
        {
            data = data.Remove(0, 4);
            data = DecryptDES(data, "mlm");
        }
        if (data.Contains("</MlmAction>")) // 只替换了 Action，没加密
        {
            data = Regex.Replace(data, @"\bMlmAction\b", "Action");
            data = data.Replace("<TriggernometryExport", "<TriggernometryExport PluginVersion=\"1.1.7.1\"");
        }
        return data;
    }
}