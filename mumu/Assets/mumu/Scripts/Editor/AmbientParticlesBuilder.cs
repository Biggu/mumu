using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 雰囲気の粒のプレハブを作る。見本 ambient_001 をコピーして、色と粒の形（丸・四角）だけテーマに合わせる。未作成のものだけ作る（作った後は Prefab を直接編集する）
    /// </summary>
    [InitializeOnLoad]
    public static class AmbientParticlesBuilder
    {
        private const string FolderAsset = "Assets/mumu/Resources/Prefabs/Ambient";
        private const string PrefabFormat = FolderAsset + "/ambient_{0:000}.prefab";
        private const string RoundTexturePath = "Assets/mumu/Resources/Sprites/Common/ambient_round.png";
        private const string RoundMaterialPath = FolderAsset + "/ambient_round.mat";
        /// <summary>見本のテーマ ID（ambient_001 をコピーする）</summary>
        private const int TemplateId = 1;
        /// <summary>夜のテーマの ID に足すと昼の ID になる数（ThemeDefine と同じ）</summary>
        private const int NightOffset = 19;
        /// <summary>丸い粒を使う昼のテーマ（雪・氷・泡・ほたる）。ほかは見本の四角</summary>
        private static readonly HashSet<int> RoundThemes = new HashSet<int> { 3, 13, 16, 17 };

        /// <summary>
        /// コンパイル後に、作られていないプレハブがあれば作る
        /// </summary>
        static AmbientParticlesBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成のプレハブがある時だけ作る
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (ThemeIds().Any(_id => AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(PrefabFormat, _id)) == null))
            {
                Build();
            }
        }

        /// <summary>
        /// メニューから作る（作成済みのプレハブは上書きしない）
        /// </summary>
        [MenuItem("mumu/Build Ambient Effects")]
        public static void BuildFromMenu()
        {
            Build();
            Debug.Log("[AmbientParticlesBuilder] 未作成の雰囲気プレハブを作りました");
        }

        /// <summary>
        /// 雰囲気のプレハブを作る（既にあるものは残す）
        /// </summary>
        public static void Build()
        {
            string templatePath = string.Format(PrefabFormat, TemplateId);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(templatePath) == null)
            {
                Debug.LogError($"[AmbientParticlesBuilder] 見本がありません: {templatePath}");
                return;
            }

            Material round = getOrCreateRoundMaterial();
            foreach (int id in ThemeIds())
            {
                string path = string.Format(PrefabFormat, id);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                {
                    continue;
                }
                if (!AssetDatabase.CopyAsset(templatePath, path))
                {
                    Debug.LogError($"[AmbientParticlesBuilder] コピーできません: {path}");
                    continue;
                }
                createFromTemplate(id, path, round);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 雰囲気のある全テーマ ID（粒の数が 0 のテーマは除く）
        /// </summary>
        private static IOrderedEnumerable<int> ThemeIds()
        {
            return ThemeDefine.ThemeIds
                .Where(_id => ThemeDefine.GetAmbient(_id).Count > 0)
                .OrderBy(_id => _id);
        }

        /// <summary>
        /// 夜のテーマなら昼の ID を返す（昼はそのまま）
        /// </summary>
        /// <param name="_id"></param>
        private static int dayOf(int _id)
        {
            return _id > NightOffset ? _id - NightOffset : _id;
        }

        /// <summary>
        /// コピーしたプレハブの名前・色・粒の形をテーマに合わせて保存する（ほかの設定は見本のまま）
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_path"></param>
        /// <param name="_round"></param>
        private static void createFromTemplate(int _id, string _path, Material _round)
        {
            ThemeDefine.AmbientSettings settings = ThemeDefine.GetAmbient(_id);
            Color[] colors = settings.Colors != null && settings.Colors.Length > 0 ? settings.Colors : new[] { Color.white };

            GameObject root = PrefabUtility.LoadPrefabContents(_path);
            root.name = $"ambient_{_id:000}";
            ParticleSystem effect = root.GetComponent<ParticleSystem>();
            var main = effect.main;
            main.startColor = new ParticleSystem.MinMaxGradient(colors[0], colors[colors.Length - 1]);
            if (RoundThemes.Contains(dayOf(_id)))
            {
                effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = _round;
            }
            PrefabUtility.SaveAsPrefabAsset(root, _path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>
        /// 丸い粒の素材を作る（テクスチャは点フィルタのドット絵）。あれば残す
        /// </summary>
        private static Material getOrCreateRoundMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RoundMaterialPath);
            if (material != null)
            {
                return material;
            }

            var importer = AssetImporter.GetAtPath(RoundTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            material = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RoundTexturePath),
            };
            AssetDatabase.CreateAsset(material, RoundMaterialPath);
            return material;
        }
    }
}
