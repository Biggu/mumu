using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace mumu
{
    /// <summary>
    /// 日本語ドットフォント（DotGothic16）の TMP フォントアセット作成と適用
    /// 未作成ならコンパイル後に自動実行
    /// </summary>
    [InitializeOnLoad]
    public static class FontSetup
    {
        private const string FontFolder = "Assets/mumu/Fonts/DotGothic16";
        private const string SourceFontPath = FontFolder + "/DotGothic16-Regular.ttf";
        private const string FontAssetPath = FontFolder + "/DotGothic16 TMP.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string PrefabFolder = "Assets/mumu/Resources";
        private const string StampKey = "FontSetup";

        /// <summary>サンプリングサイズ（フォントの設計サイズ 16px の倍数にしてドットを崩さない）</summary>
        private const int SamplingPointSize = 32;
        private const int AtlasPadding = 2;
        private const int AtlasSize = 1024;

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static FontSetup()
        {
            EditorApplication.delayCall += autoSetup;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoSetup()
        {
            bool isBuilt = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null;
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorBuildStamp.NeedsBuild(StampKey, isBuilt, new[] { FontAssetPath }))
            {
                return;
            }
            EditorBuildStamp.Save(StampKey);
            Setup();
        }

        /// <summary>
        /// フォントアセットを作成して全テキストに適用
        /// </summary>
        [MenuItem("mumu/Setup Font")]
        public static void Setup()
        {
            TMP_FontAsset fontAsset = loadOrCreateFontAsset();
            if (fontAsset == null)
            {
                return;
            }

            setDefaultFont(fontAsset);
            int count = applyToPrefabs(fontAsset);
            AssetDatabase.SaveAssets();
            EditorBuildStamp.Save(StampKey);
            Debug.Log($"[FontSetup] DotGothic16 applied ({count} texts)");
        }

        /// <summary>
        /// フォントアセットを読み込む（なければ作成）
        /// </summary>
        private static TMP_FontAsset loadOrCreateFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset != null)
            {
                return fontAsset;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[FontSetup] Font not found: {SourceFontPath}");
                return null;
            }

            // ドットフォントなので SDF ではなくビットマップで描画（必要な文字は実行時に追加される）
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, SamplingPointSize, AtlasPadding, GlyphRenderMode.RASTER_HINTED,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            fontAsset.name = "DotGothic16 TMP";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = fontAsset.name + " Atlas";
            atlas.filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        /// <summary>
        /// TMP の既定フォントにする（新しく作るテキストにも適用される）
        /// </summary>
        /// <param name="_fontAsset"></param>
        private static void setDefaultFont(TMP_FontAsset _fontAsset)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"[FontSetup] TMP Settings not found: {TmpSettingsPath}");
                return;
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = _fontAsset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// プレハブ内の全テキストのフォントを差し替える
        /// </summary>
        /// <param name="_fontAsset"></param>
        private static int applyToPrefabs(TMP_FontAsset _fontAsset)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (text.font == _fontAsset)
                        {
                            continue;
                        }
                        text.font = _fontAsset;
                        text.fontSharedMaterial = _fontAsset.material;
                        changed = true;
                        count++;
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return count;
        }
    }
}
