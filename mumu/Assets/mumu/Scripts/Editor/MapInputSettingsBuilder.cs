using UnityEditor;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップの操作の設定のアセット（Resources/Settings/MapInputSettings.asset）を作る。無い時だけ作る（中身の数値は残る）
    /// </summary>
    [InitializeOnLoad]
    public static class MapInputSettingsBuilder
    {
        private const string Folder = "Assets/mumu/Resources/Settings";
        private const string AssetPath = Folder + "/MapInputSettings.asset";

        /// <summary>
        /// コンパイル後に、無ければ作る
        /// </summary>
        static MapInputSettingsBuilder()
        {
            EditorApplication.delayCall += create;
        }

        /// <summary>
        /// 設定のアセットを作る（メニューからも作れる）
        /// </summary>
        [MenuItem("mumu/Create Map Input Settings")]
        public static void create()
        {
            if (AssetDatabase.LoadAssetAtPath<MapInputSettings>(AssetPath) != null)
            {
                return;
            }
            EditorAssetUtility.EnsureFolder(Folder);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MapInputSettings>(), AssetPath);
            AssetDatabase.SaveAssets();
        }
    }
}
