using UnityEditor;

namespace mumu
{
    /// <summary>
    /// エディタのアセット操作
    /// </summary>
    public static class EditorAssetUtility
    {
        /// <summary>
        /// フォルダを作成（親も含む）
        /// </summary>
        /// <param name="_path"></param>
        public static void EnsureFolder(string _path)
        {
            if (AssetDatabase.IsValidFolder(_path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(_path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(_path));
        }
    }
}
