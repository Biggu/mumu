using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace mumu
{
    /// <summary>
    /// 保存データの暗号化（AES-256-CBC、保存ごとにランダムな IV、改ざんチェックに HMAC-SHA256）
    /// 形式: "MUMU1" + Base64(IV 16 バイト + 暗号文 + HMAC 32 バイト)
    /// </summary>
    public static class SaveCrypto
    {
        private const string Header = "MUMU1";
        private const int IvSize = 16;
        private const int MacSize = 32;

        // TODO: リリース前に秘密の文字列を変える（変えると今までの保存データは読めなくなる）
        private static readonly char[] Secret = { 't', 's', 'u', 'n', 'a', '-', 'p', 'u', 'g', '#', '2', '0', '2', '6', '!', 'm', 'u', 'm', 'u' };

        private static byte[] aesKey = null;
        private static byte[] macKey = null;

        /// <summary>
        /// 文字列を暗号化
        /// </summary>
        /// <param name="_plain"></param>
        public static string Encrypt(string _plain)
        {
            prepareKeys();
            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                byte[] plain = Encoding.UTF8.GetBytes(_plain);
                byte[] cipher;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                }

                using (var stream = new MemoryStream())
                {
                    stream.Write(aes.IV, 0, IvSize);
                    stream.Write(cipher, 0, cipher.Length);
                    byte[] body = stream.ToArray();
                    byte[] mac = computeMac(body, 0, body.Length);
                    stream.Write(mac, 0, MacSize);
                    return Header + Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        /// <summary>
        /// 復号（形式が違う・改ざんされている時は false）
        /// </summary>
        /// <param name="_text"></param>
        /// <param name="_plain"></param>
        public static bool TryDecrypt(string _text, out string _plain)
        {
            _plain = null;
            if (string.IsNullOrEmpty(_text) || !_text.StartsWith(Header, StringComparison.Ordinal))
            {
                return false;
            }

            prepareKeys();
            byte[] data;
            try
            {
                data = Convert.FromBase64String(_text.Substring(Header.Length).Trim());
            }
            catch (FormatException)
            {
                return false;
            }
            if (data.Length < IvSize + MacSize + 16)
            {
                return false;
            }

            int bodyLength = data.Length - MacSize;
            byte[] expected = computeMac(data, 0, bodyLength);
            if (!isSame(expected, data, bodyLength))
            {
                return false;
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] iv = new byte[IvSize];
                    Buffer.BlockCopy(data, 0, iv, 0, IvSize);
                    aes.IV = iv;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] plain = decryptor.TransformFinalBlock(data, IvSize, bodyLength - IvSize);
                        _plain = Encoding.UTF8.GetString(plain);
                        return true;
                    }
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        /// 秘密の文字列から暗号用と改ざんチェック用の鍵を作る（初回のみ）
        /// </summary>
        private static void prepareKeys()
        {
            if (aesKey != null)
            {
                return;
            }
            using (var sha = SHA256.Create())
            {
                string secret = new string(Secret);
                aesKey = sha.ComputeHash(Encoding.UTF8.GetBytes("aes:" + secret));
                macKey = sha.ComputeHash(Encoding.UTF8.GetBytes("mac:" + secret));
            }
        }

        /// <summary>
        /// 改ざんチェックの値
        /// </summary>
        /// <param name="_data"></param>
        /// <param name="_offset"></param>
        /// <param name="_count"></param>
        private static byte[] computeMac(byte[] _data, int _offset, int _count)
        {
            using (var hmac = new HMACSHA256(macKey))
            {
                return hmac.ComputeHash(_data, _offset, _count);
            }
        }

        /// <summary>
        /// 末尾の改ざんチェックの値が一致するか（時間で中身が分からないよう全部比べる）
        /// </summary>
        /// <param name="_expected"></param>
        /// <param name="_data"></param>
        /// <param name="_offset"></param>
        private static bool isSame(byte[] _expected, byte[] _data, int _offset)
        {
            int diff = 0;
            for (int i = 0; i < MacSize; i++)
            {
                diff |= _expected[i] ^ _data[_offset + i];
            }
            return diff == 0;
        }
    }
}
