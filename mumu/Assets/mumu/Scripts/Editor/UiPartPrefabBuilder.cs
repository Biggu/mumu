using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 新しい UI の部品（Resources/Prefabs/Common/float_text・filter_button）を作る。無ければコンパイル後に自動で作る
    /// </summary>
    [InitializeOnLoad]
    public static class UiPartPrefabBuilder
    {
        private const string FloatTextPath = "Assets/mumu/Resources/Prefabs/Common/float_text.prefab";
        private const string FilterButtonPath = "Assets/mumu/Resources/Prefabs/Common/filter_button.prefab";
        private const string TouchPadPath = "Assets/mumu/Resources/Prefabs/Common/touch_pad.prefab";
        private const string SummaryPath = "Assets/mumu/Resources/Prefabs/Common/clear_summary.prefab";
        private const string FontPath = "Assets/mumu/Fonts/mplus-1m-medium SDF.asset";
        private const string FontMaterialPath = "Assets/mumu/Fonts/mplus-1m-medium SDF_001.mat";

        /// <summary>
        /// コンパイル後に、無ければ作る
        /// </summary>
        static UiPartPrefabBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 無いものだけ作る
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FloatTextPath) == null || AssetDatabase.LoadAssetAtPath<GameObject>(FilterButtonPath) == null || AssetDatabase.LoadAssetAtPath<GameObject>(TouchPadPath) == null || AssetDatabase.LoadAssetAtPath<GameObject>(SummaryPath) == null)
            {
                Build();
            }
        }

        /// <summary>
        /// 結果の札の文字を1行作る
        /// </summary>
        private static void addSummaryLabel(Transform _parent, string _name, float _y, TMP_FontAsset _font, Material _material)
        {
            var go = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(440f, 60f);
            rect.anchoredPosition = new Vector2(0f, _y);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.font = _font;
            text.fontSharedMaterial = _material;
            text.fontSize = 40f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.29f, 0.17f, 0.06f);
            text.raycastTarget = false;
        }

        /// <summary>
        /// メニューから作る（作り直す）
        /// </summary>
        [MenuItem("mumu/Build UI Parts")]
        public static void Build()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);

            // 飛び出す文字（ほね・x2 など）
            var floatGo = new GameObject("float_text", typeof(RectTransform), typeof(TextMeshProUGUI));
            ((RectTransform)floatGo.transform).sizeDelta = new Vector2(200f, 60f);
            TMP_Text floatText = floatGo.GetComponent<TMP_Text>();
            floatText.font = font;
            floatText.fontSharedMaterial = material;
            floatText.fontSize = 40f;
            floatText.alignment = TextAlignmentOptions.Center;
            floatText.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(floatGo, FloatTextPath);
            Object.DestroyImmediate(floatGo);

            // 押せる小さなボタン（ラベル付き）
            var buttonGo = new GameObject("filter_button", typeof(RectTransform), typeof(Image), typeof(MumuButton));
            ((RectTransform)buttonGo.transform).sizeDelta = new Vector2(260f, 60f);
            Image image = buttonGo.GetComponent<Image>();
            image.color = Color.white;
            buttonGo.GetComponent<MumuButton>().targetGraphic = image;
            var labelGo = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelGo.GetComponent<TMP_Text>();
            label.font = font;
            label.fontSharedMaterial = material;
            label.fontSize = 30f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.29f, 0.17f, 0.06f);
            label.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(buttonGo, FilterButtonPath);
            Object.DestroyImmediate(buttonGo);

            // 指の下の淡い丸（絵は実行時に入れる。ボタンではないので押せない）
            var padGo = new GameObject("touch_pad", typeof(RectTransform), typeof(Image));
            ((RectTransform)padGo.transform).sizeDelta = new Vector2(120f, 120f);
            Image padImage = padGo.GetComponent<Image>();
            padImage.raycastTarget = false;
            // 中のつまみ（指について行く丸）
            var knobGo = new GameObject("knob", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(padGo.transform, false);
            ((RectTransform)knobGo.transform).sizeDelta = new Vector2(60f, 60f);
            knobGo.GetComponent<Image>().raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(padGo, TouchPadPath);
            Object.DestroyImmediate(padGo);

            // クリアの結果の札（ほね・★・もどったか）
            var summaryGo = new GameObject("clear_summary", typeof(RectTransform), typeof(Image), typeof(MumuButton));
            ((RectTransform)summaryGo.transform).sizeDelta = new Vector2(480f, 260f);
            Image summaryImage = summaryGo.GetComponent<Image>();
            summaryImage.color = new Color(1f, 0.97f, 0.9f, 0.92f);
            summaryImage.raycastTarget = true;
            summaryGo.GetComponent<MumuButton>().targetGraphic = summaryImage;
            addSummaryLabel(summaryGo.transform, "bones", 70f, font, material);
            addSummaryLabel(summaryGo.transform, "stars", 0f, font, material);
            addSummaryLabel(summaryGo.transform, "norewind", -70f, font, material);
            PrefabUtility.SaveAsPrefabAsset(summaryGo, SummaryPath);
            Object.DestroyImmediate(summaryGo);

            AssetDatabase.SaveAssets();
            Debug.Log("[UiPartPrefabBuilder] float_text・filter_button・touch_pad を作りました");
        }
    }
}
