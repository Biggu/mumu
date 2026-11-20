using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static mumu.EditorUiBuilder;

namespace mumu
{
    /// <summary>
    /// 部品のプレハブ（Resources/Prefabs/Common）を組み立てる。未作成ならコンパイル後に自動作成。作り直す時は mumu > Build Parts
    /// </summary>
    [InitializeOnLoad]
    public static class PartsPrefabBuilder
    {
        private const string Folder = "Assets/mumu/Resources/" + PartsLoaderManager.Folder;
        private const string StampKey = "PartsPrefabBuilder";
        /// <summary>箱だけの大きさ（実際の大きさは作った後にコードで決める）</summary>
        private const float PartSize = 64f;

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static PartsPrefabBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (!EditorBuildStamp.NeedsBuild(StampKey, true, getPaths()))
            {
                return;
            }
            Build();
        }

        /// <summary>
        /// 部品のプレハブを作り直す
        /// </summary>
        [MenuItem("mumu/Build Parts")]
        public static void Build()
        {
            LoadFont();
            EditorAssetUtility.EnsureFolder(Folder);
            foreach (ePart part in System.Enum.GetValues(typeof(ePart)))
            {
                save(buildPart(PartsLoaderManager.GetName(part)), PartsLoaderManager.GetName(part));
            }
            EditorBuildStamp.Save(StampKey);
            Debug.Log("[PartsPrefabBuilder] Parts built");
        }

        /// <summary>
        /// 作るプレハブのパス（部品の種類ごと）
        /// </summary>
        private static string[] getPaths()
        {
            ePart[] parts = (ePart[])System.Enum.GetValues(typeof(ePart));
            var paths = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                paths[i] = getPath(PartsLoaderManager.GetName(parts[i]));
            }
            return paths;
        }

        /// <summary>
        /// プレハブのパス
        /// </summary>
        /// <param name="_name"></param>
        private static string getPath(string _name) => $"{Folder}/{_name}.prefab";

        /// <summary>
        /// 画像だけの部品（Image の絵と大きさはコードで入れる）
        /// </summary>
        /// <param name="_name"></param>
        private static GameObject buildPart(string _name)
        {
            var go = new GameObject(_name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(PartSize, PartSize);
            Image image = AddImage(rect, null, Color.white, false);
            image.preserveAspect = true;
            return go;
        }

        /// <summary>
        /// プレハブとして保存して一時オブジェクトを破棄
        /// </summary>
        /// <param name="_root"></param>
        /// <param name="_name"></param>
        private static void save(GameObject _root, string _name)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(_root, getPath(_name));
            }
            finally
            {
                Object.DestroyImmediate(_root);
            }
        }
    }
}
