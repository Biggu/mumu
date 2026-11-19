using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 画面プレハブを組み立てる共通処理
    /// </summary>
    public static class EditorUiBuilder
    {
        public const string UiFolder = "Assets/mumu/Resources/Sprites/UI/";
        private const string FontPath = "Assets/mumu/Fonts/DotGothic16/DotGothic16 TMP.asset";

        public static readonly Color TextBrown = new Color(0.29f, 0.17f, 0.06f);
        public static readonly Color SettingsColor = new Color(0.42f, 0.78f, 0.64f);
        public static readonly Color CloseColor = new Color(0.92f, 0.4f, 0.4f);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);

        private static TMP_FontAsset font = null;

        /// <summary>
        /// フォントを読み込む（組み立ての最初に呼ぶ）
        /// </summary>
        public static void LoadFont()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        /// <summary>
        /// RectTransform 付きの GameObject を作成
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        public static RectTransform Create(string _name, Transform _parent)
        {
            var go = new GameObject(_name, typeof(RectTransform));
            go.layer = _parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(_parent, false);
            return rect;
        }

        /// <summary>
        /// 親いっぱいに広げる
        /// </summary>
        /// <param name="_rect"></param>
        public static RectTransform Stretch(RectTransform _rect)
        {
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            return _rect;
        }

        /// <summary>
        /// アンカー・位置・サイズを設定（pivot はアンカーと同じ）
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_anchorMin"></param>
        /// <param name="_anchorMax"></param>
        /// <param name="_position"></param>
        /// <param name="_size"></param>
        public static void SetRect(RectTransform _rect, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _position, Vector2 _size)
        {
            _rect.anchorMin = _anchorMin;
            _rect.anchorMax = _anchorMax;
            _rect.pivot = (_anchorMin + _anchorMax) * 0.5f;
            _rect.anchoredPosition = _position;
            _rect.sizeDelta = _size;
        }

        /// <summary>
        /// Image を追加
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_sprite"></param>
        /// <param name="_color"></param>
        /// <param name="_raycast"></param>
        /// <param name="_type"></param>
        public static Image AddImage(RectTransform _rect, Sprite _sprite, Color _color, bool _raycast, Image.Type _type = Image.Type.Simple)
        {
            var image = _rect.gameObject.AddComponent<Image>();
            image.sprite = _sprite;
            image.type = _type;
            image.color = _color;
            image.raycastTarget = _raycast;
            image.preserveAspect = _type == Image.Type.Simple && _sprite != null;
            return image;
        }

        /// <summary>
        /// 中央にアイコンを追加
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        public static Image AddIcon(RectTransform _parent, string _sprite, float _size, Color _color)
        {
            RectTransform icon = Create("icon", _parent);
            SetRect(icon, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(_size, _size));
            return AddImage(icon, LoadSprite(_sprite), _color, false);
        }

        /// <summary>
        /// ボタンを追加
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_sprite"></param>
        /// <param name="_color"></param>
        /// <param name="_type"></param>
        /// <param name="_label"></param>
        public static MumuButton AddButton(RectTransform _rect, Sprite _sprite, Color _color, Image.Type _type, TMP_Text _label)
        {
            Image image = AddImage(_rect, _sprite, _color, true, _type);
            var button = _rect.gameObject.AddComponent<MumuButton>();
            button.targetGraphic = image;
            if (_label != null)
            {
                SetField(button, "textComponent", _label);
            }
            return button;
        }

        /// <summary>
        /// ローカライズされた文字を追加
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_key"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        public static TMP_Text AddText(RectTransform _rect, string _key, float _size, Color _color)
        {
            TMP_Text text = AddPlainText(_rect, _key, _size, _color);
            LocalizationSetup.Bind(text, _key);
            return text;
        }

        /// <summary>
        /// ローカライズしない文字を追加（数字など）
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_text"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        public static TMP_Text AddPlainText(RectTransform _rect, string _text, float _size, Color _color)
        {
            var text = _rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            text.text = _text;
            text.fontSize = _size;
            text.color = _color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// UI 画像を読み込む
        /// </summary>
        /// <param name="_name"></param>
        public static Sprite LoadSprite(string _name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + _name + ".png");
            if (sprite == null)
            {
                Debug.LogWarning($"[EditorUiBuilder] Sprite not found: {UiFolder}{_name}.png");
            }
            return sprite;
        }

        /// <summary>
        /// シリアライズフィールドに設定
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_field"></param>
        /// <param name="_value"></param>
        public static void SetField(Object _target, string _field, Object _value)
        {
            var serialized = new SerializedObject(_target);
            SerializedProperty property = serialized.FindProperty(_field);
            if (property == null)
            {
                Debug.LogWarning($"[EditorUiBuilder] Field not found: {_target.GetType().Name}.{_field}");
                return;
            }
            property.objectReferenceValue = _value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 配列のシリアライズフィールドに設定
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_field"></param>
        /// <param name="_values"></param>
        public static void SetArrayField(Object _target, string _field, Object[] _values)
        {
            var serialized = new SerializedObject(_target);
            SerializedProperty property = serialized.FindProperty(_field);
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"[EditorUiBuilder] Array field not found: {_target.GetType().Name}.{_field}");
                return;
            }
            property.arraySize = _values.Length;
            for (int i = 0; i < _values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = _values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 設定行（アイコン・文字・切り替えボタン）
        /// </summary>
        /// <param name="_panel"></param>
        /// <param name="_name"></param>
        /// <param name="_icon"></param>
        /// <param name="_key"></param>
        /// <param name="_y"></param>
        public static MumuButton BuildToggleRow(Transform _panel, string _name, string _icon, string _key, float _y)
        {
            var center = Vector2.one * 0.5f;
            RectTransform row = Create(_name, _panel);
            SetRect(row, center, center, new Vector2(0f, _y), new Vector2(500f, 96f));

            RectTransform icon = Create("icon", row);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(64f, 64f));
            AddImage(icon, LoadSprite(_icon), TextBrown, false);

            RectTransform label = Create("label", row);
            SetRect(label, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 0f), new Vector2(220f, 80f));
            AddText(label, _key, 32, TextBrown).alignment = TextAlignmentOptions.Left;

            RectTransform toggle = Create("toggle", row);
            SetRect(toggle, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-80f, 0f), new Vector2(144f, 72f));
            return AddButton(toggle, LoadSprite("ui_toggle_on"), Color.white, Image.Type.Simple, null);
        }

        /// <summary>
        /// 閉じるボタン（パネルの右上）
        /// </summary>
        /// <param name="_panel"></param>
        public static MumuButton BuildCloseButton(RectTransform _panel)
        {
            RectTransform close = Create("closeButton", _panel);
            SetRect(close, Vector2.one, Vector2.one, new Vector2(-12f, -12f), new Vector2(80f, 80f));
            MumuButton button = AddButton(close, LoadSprite("ui_button_round"), CloseColor, Image.Type.Simple, null);
            AddIcon(close, "ui_icon_close", 44f, Color.white);
            return button;
        }

        /// <summary>
        /// 押すと閉じる暗幕
        /// </summary>
        /// <param name="_popup"></param>
        public static MumuButton BuildDim(RectTransform _popup)
        {
            RectTransform dim = Create("dim", _popup);
            SetRect(dim, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(4000f, 4000f));
            MumuButton button = AddButton(dim, null, DimColor, Image.Type.Simple, null);
            button.transition = Selectable.Transition.None;
            return button;
        }
    }
}
