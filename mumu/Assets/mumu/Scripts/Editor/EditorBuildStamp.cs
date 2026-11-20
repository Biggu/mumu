using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEditor;

namespace mumu
{
    /// <summary>
    /// 自動生成（ビルダー）の作り直し判定。ビルダーのソースのハッシュを EditorPrefs に保存する
    /// </summary>
    public static class EditorBuildStamp
    {
        private const string KeyPrefix = "mumu.BuildStamp.";

        /// <summary>
        /// 作り直しが必要か（ソースが変わった時と、出力がない時だけ true。作成済みかは _isBuilt で渡す）
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_isBuilt"></param>
        /// <param name="_outputPaths"></param>
        /// <param name="_sourcePath"></param>
        public static bool NeedsBuild(string _key, bool _isBuilt, string[] _outputPaths, [CallerFilePath] string _sourcePath = "")
        {
            if (_outputPaths != null)
            {
                foreach (string path in _outputPaths)
                {
                    bool exists = AssetDatabase.LoadMainAssetAtPath(path) != null;
                    if (!exists)
                    {
                        return true;
                    }
                }
            }

            string current = hashOf(_sourcePath);
            string stored = EditorPrefs.GetString(KeyPrefix + _key, "");
            if (stored == current)
            {
                return false;
            }

            // 導入前に作られたものは、そのまま使う（作り直しで手直しを消さない）
            if (stored.Length == 0 && _isBuilt)
            {
                EditorPrefs.SetString(KeyPrefix + _key, current);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 今のソースで作ったことを記録する（作り直しに成功した時に呼ぶ）
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_sourcePath"></param>
        public static void Save(string _key, [CallerFilePath] string _sourcePath = "")
        {
            EditorPrefs.SetString(KeyPrefix + _key, hashOf(_sourcePath));
        }

        /// <summary>
        /// ソースファイルの SHA1（読めなければ空）
        /// </summary>
        /// <param name="_path"></param>
        private static string hashOf(string _path)
        {
            if (!File.Exists(_path))
            {
                return "";
            }
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(File.ReadAllBytes(_path));
                return BitConverter.ToString(hash);
            }
        }
    }
}
