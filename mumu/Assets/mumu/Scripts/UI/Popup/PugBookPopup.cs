using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ずかん（全キャラクターのグリッド。ほねと交換してなかまにできる）
    /// </summary>
    public class PugBookPopup : MonoBehaviour
    {
        public const string PrefabName = "pug_book";

        private const float OpenPunchAmount = 0.05f;
        private const float OpenPunchDuration = 0.2f;
        private const float IconSize = 120f;
        private const float StarSize = 22f;
        private const float NameFontSize = 24f;
        private const float SmallFontSize = 22f;
        private const float PriceFontSize = 24f;
        private static readonly Vector2 PriceButtonSize = new Vector2(160f, 54f);
        private static readonly Color TextBrown = new Color(0.29f, 0.17f, 0.06f);
        private static readonly Color NewColor = new Color(0.95f, 0.35f, 0.55f);
        private static readonly Color EquippedColor = new Color(0.25f, 0.6f, 0.95f);
        private static readonly Color DisabledColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color[] TierBackColors =
        {
            new Color(1f, 1f, 1f, 0f),
            new Color(0.7f, 0.9f, 1f, 0.45f),
            new Color(0.85f, 0.7f, 1f, 0.45f),
            new Color(1f, 0.85f, 0.35f, 0.6f),
            new Color(1f, 0.6f, 0.5f, 0.6f),
            new Color(1f, 0.55f, 0.85f, 0.7f),
        };
        private static readonly Color SilhouetteColor = new Color(0f, 0f, 0f, 0.5f);

        private const string StarPath = "Sprites/UI/ui_star_on";
        private const string BonePath = "Sprites/Common/gimmick_bone";
        private const string ButtonPath = "Sprites/UI/ui_button";
        private const string PanelPath = "Sprites/UI/ui_panel";

        /// <summary>中身のパネル（開く時に跳ねる）</summary>
        [SerializeField]
        private RectTransform panel = null;
        /// <summary>閉じるボタン</summary>
        [SerializeField]
        private MumuButton closeButton = null;
        /// <summary>背景の暗幕（押すと閉じる）</summary>
        [SerializeField]
        private MumuButton dimButton = null;
        /// <summary>持っているほね</summary>
        [SerializeField]
        private TMP_Text boneText = null;
        /// <summary>キャラクターを並べるグリッド</summary>
        [SerializeField]
        private RectTransform content = null;
        /// <summary>tier のタブ（★1〜★6、押した tier の子だけ並べる）</summary>
        [SerializeField]
        private MumuButton[] tierTabs = null;

        private static readonly Color TabColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color TabSelectedColor = Color.white;
        private int selectedTier = CharacterMaster.MinTier;

        /// <summary>
        /// 1匹分の表示
        /// </summary>
        private class Item
        {
            public CharacterData Character;
            /// <summary>とくべつな子のシルエットを外すため</summary>
            public Image Icon;
            public TMP_Text NameText;
            public TMP_Text CountText;
            public GameObject NewBadge;
            public MumuButton PriceButton;
            public Image PriceButtonImage;
            /// <summary>持っている時の「もってる」</summary>
            public GameObject OwnedLabel;
            /// <summary>連れていく子の「いっしょ」</summary>
            public GameObject EquippedBadge;
            /// <summary>交換できない子の「おさんぽで あえるかも」</summary>
            public GameObject SecretLabel;
        }

        private readonly List<Item> items = new List<Item>();
        private UniTaskCompletionSource closeSource = null;
        private bool isExchanged = false;
        private bool isConfirming = false;

        /// <summary>
        /// 開いて閉じるまで待つ（交換したら true）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_token"></param>
        public static async UniTask<bool> OpenAsync(Transform _parent, CancellationToken _token)
        {
            PugBookPopup popup = await PopupLoader.InstantiateAsync<PugBookPopup>(PrefabName, _parent, _token);
            if (popup == null)
            {
                return false;
            }

            popup.setup();
            await popup.closeSource.Task.AttachExternalCancellation(_token).SuppressCancellationThrow();
            bool isExchanged = popup.isExchanged;
            if (popup != null)
            {
                Destroy(popup.gameObject);
            }
            return isExchanged;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void setup()
        {
            closeSource = new UniTaskCompletionSource();
            closeButton.SetOnClick(onClickCloseButton);
            if (dimButton != null)
            {
                dimButton.SetClickSeEnabled(false);
                dimButton.SetOnClick(onClickCloseButton);
            }

            if (tierTabs != null)
            {
                for (int i = 0; i < tierTabs.Length; i++)
                {
                    int tier = CharacterMaster.MinTier + i;
                    tierTabs[i].SetOnClick(() => onClickTab(tier));
                }
            }
            showTier(selectedTier);
            UiTween.PunchScale(panel, Vector3.one, OpenPunchAmount, OpenPunchDuration, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// tier のタブ押下時の処理
        /// </summary>
        /// <param name="_tier"></param>
        private void onClickTab(int _tier)
        {
            if (!isConfirming)
            {
                showTier(_tier);
            }
        }

        /// <summary>
        /// その tier の子だけ並べ直す（一度に全員のカードを作らない）
        /// </summary>
        /// <param name="_tier"></param>
        private void showTier(int _tier)
        {
            selectedTier = _tier;
            foreach (Item item in items)
            {
                Destroy(item.Icon.transform.parent.gameObject);
            }
            items.Clear();
            foreach (CharacterData character in CharacterMaster.All)
            {
                if (character.Tier == _tier)
                {
                    items.Add(createItem(character));
                }
            }
            if (tierTabs != null)
            {
                for (int i = 0; i < tierTabs.Length; i++)
                {
                    tierTabs[i].image.color = CharacterMaster.MinTier + i == _tier ? TabSelectedColor : TabColor;
                }
            }
            content.anchoredPosition = Vector2.zero;
            refresh();
        }

        /// <summary>
        /// ほね・持っているか・交換できるかを反映
        /// </summary>
        private void refresh()
        {
            int bones = SaveDataManager.Data.bones;
            boneText.text = TextLocalizer.Get("PUG_BOOK_BONES", bones);
            foreach (Item item in items)
            {
                int count = PugCollection.Count(item.Character.Id);
                item.CountText.text = "";
                item.NewBadge.SetActive(count == 0 && !item.Character.IsSecret);
                item.EquippedBadge.SetActive(count > 0 && item.Character.Id == PugCollection.EquippedId);
                item.OwnedLabel.SetActive(count > 0);
                if (item.SecretLabel != null)
                {
                    item.SecretLabel.SetActive(count == 0);
                }

                // とくべつな子は持つまでシルエットと ？？？
                bool isHidden = item.Character.IsSecret && count == 0;
                item.Icon.color = isHidden ? SilhouetteColor : Color.white;
                item.NameText.text = isHidden ? TextLocalizer.Get("UNKNOWN") : item.Character.Name;

                // 持っている子は交換できない（ボタンの代わりに「もってる」）
                if (item.PriceButton != null)
                {
                    item.PriceButton.gameObject.SetActive(count == 0);
                    bool canBuy = bones >= item.Character.Price;
                    item.PriceButtonImage.color = canBuy ? Color.white : DisabledColor;
                }
                if (!isHidden && item.Character.Tier >= PugAura.MinTier)
                {
                    PugAura.Attach(item.Icon.rectTransform, item.Character.Tier, IconSize);
                }
            }
        }

        /// <summary>
        /// 1匹分（tier の色の台・パグ・名前・★・持っている数・NEW・交換ボタン）
        /// </summary>
        /// <param name="_character"></param>
        private Item createItem(CharacterData _character)
        {
            var item = new Item { Character = _character };
            Image back = createImage("item", content, ResourceManager.Instance.Load<Sprite>(PanelPath), Vector2.zero);
            back.type = Image.Type.Sliced;
            back.preserveAspect = false;
            Transform root = back.transform;

            Image tierBack = createImage("tier", root, null, Vector2.zero);
            tierBack.color = TierBackColors[Mathf.Clamp(_character.Tier, CharacterMaster.MinTier, CharacterMaster.MaxTier) - 1];
            stretch(tierBack.rectTransform, 8f);

            // ずかんはアイコン集から（キャラごとの画像は読み込まない）
            Image icon = createImage("icon", root, PugIcons.Get(_character.Id), new Vector2(IconSize, IconSize));
            place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f));
            item.Icon = icon;

            TMP_Text nameText = createText("name", root, _character.Name, NameFontSize, TextBrown);
            item.NameText = nameText;
            place(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -IconSize - 8f));
            nameText.rectTransform.sizeDelta = new Vector2(180f, 30f);

            // tier の数だけ★
            Sprite star = ResourceManager.Instance.Load<Sprite>(StarPath);
            for (int i = 0; i < _character.Tier; i++)
            {
                Image starImage = createImage("star", root, star, new Vector2(StarSize, StarSize));
                place(starImage.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - (_character.Tier - 1) * 0.5f) * StarSize, -IconSize - 42f));
            }

            item.CountText = createText("count", root, "", SmallFontSize, TextBrown);
            place(item.CountText.rectTransform, Vector2.one, new Vector2(-10f, -8f));
            item.CountText.alignment = TextAlignmentOptions.TopRight;
            item.CountText.rectTransform.sizeDelta = new Vector2(80f, 30f);

            TMP_Text newText = createText("new", root, "NEW", SmallFontSize, NewColor);
            place(newText.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -8f));
            newText.alignment = TextAlignmentOptions.TopLeft;
            newText.rectTransform.sizeDelta = new Vector2(80f, 30f);
            item.NewBadge = newText.gameObject;

            TMP_Text equipped = createText("equipped", root, TextLocalizer.Get("PUG_EQUIPPED"), SmallFontSize, EquippedColor);
            place(equipped.rectTransform, Vector2.one, new Vector2(-10f, -8f));
            equipped.alignment = TextAlignmentOptions.TopRight;
            equipped.rectTransform.sizeDelta = new Vector2(100f, 30f);
            item.EquippedBadge = equipped.gameObject;

            TMP_Text owned = createText("owned", root, TextLocalizer.Get("PUG_OWNED"), PriceFontSize, TextBrown);
            place(owned.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f));
            owned.rectTransform.sizeDelta = new Vector2(180f, 32f);
            item.OwnedLabel = owned.gameObject;

            // とくべつな子は交換できない
            if (!_character.IsForSale)
            {
                TMP_Text secret = createText("secret", root, TextLocalizer.Get("PUG_NOT_FOR_SALE"), SmallFontSize, NewColor);
                place(secret.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f));
                secret.rectTransform.sizeDelta = new Vector2(180f, 30f);
                item.SecretLabel = secret.gameObject;
                return item;
            }

            // 交換ボタン（ほねのアイコン＋値段）
            Image buttonImage = createImage("price", root, ResourceManager.Instance.Load<Sprite>(ButtonPath), PriceButtonSize);
            buttonImage.type = Image.Type.Sliced;
            buttonImage.preserveAspect = false;
            buttonImage.raycastTarget = true;
            place(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f));
            Image bone = createImage("bone", buttonImage.transform, ResourceManager.Instance.Load<Sprite>(BonePath), new Vector2(40f, 24f));
            place(bone.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 3f));
            TMP_Text price = createText("label", buttonImage.transform, _character.Price.ToString(), PriceFontSize, TextBrown);
            stretch(price.rectTransform, 0f);
            price.rectTransform.offsetMin = new Vector2(48f, 0f);
            price.rectTransform.anchoredPosition += new Vector2(0f, 3f);

            var button = buttonImage.gameObject.AddComponent<MumuButton>();
            button.targetGraphic = buttonImage;
            int id = _character.Id;
            button.SetOnClick(() => onClickPriceButton(id));
            item.PriceButton = button;
            item.PriceButtonImage = buttonImage;
            return item;
        }

        /// <summary>
        /// 交換ボタン押下時の処理
        /// </summary>
        /// <param name="_characterId"></param>
        private void onClickPriceButton(int _characterId)
        {
            confirmExchangeAsync(_characterId).Forget();
        }

        /// <summary>
        /// 確認してからほねと交換し、なかまになった子を見せる
        /// </summary>
        /// <param name="_characterId"></param>
        private async UniTaskVoid confirmExchangeAsync(int _characterId)
        {
            CharacterData character = CharacterMaster.Get(_characterId);
            if (isConfirming || character == null || !character.IsForSale || PugCollection.IsOwned(_characterId))
            {
                return;
            }
            CharacterSprites sprites = CharacterSprites.Load(character);
            Sprite[] sitFrames = sprites != null ? sprites.Sit : null;
            Transform dialogParent = transform.parent;

            isConfirming = true;
            if (SaveDataManager.Data.bones < character.Price)
            {
                await MessageDialog.ShowTextAsync(dialogParent, TextLocalizer.Get("PUG_BUY_TITLE"), TextLocalizer.Get("PUG_BUY_SHORT", character.Price - SaveDataManager.Data.bones), false, sitFrames, destroyCancellationToken);
                isConfirming = false;
                return;
            }

            bool isYes = await MessageDialog.ShowTextAsync(dialogParent, TextLocalizer.Get("PUG_BUY_TITLE"), TextLocalizer.Get("PUG_BUY_BODY", character.Name, character.Price), true, sitFrames, destroyCancellationToken);
            if (isYes && CharacterMaster.Exchange(_characterId))
            {
                isExchanged = true;
                refresh();
                SoundManager sound = SoundManager.Instance;
                if (sound != null)
                {
                    sound.PlaySe(eSe.Clear);
                }
                await MessageDialog.ShowTextAsync(dialogParent, TextLocalizer.Get("PUG_JOINED_TITLE"), TextLocalizer.Get("PUG_JOINED_BODY", character.Name), false, sprites != null ? sprites.Happy : null, destroyCancellationToken);
            }
            isConfirming = false;
        }

        /// <summary>
        /// 閉じるボタン押下時の処理
        /// </summary>
        private void onClickCloseButton()
        {
            if (!isConfirming)
            {
                closeSource.TrySetResult();
            }
        }

        /// <summary>
        /// 画像を作る
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        private static Image createImage(string _name, Transform _parent, Sprite _sprite, Vector2 _size)
        {
            var go = new GameObject(_name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = _sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = _size;
            return image;
        }

        /// <summary>
        /// 文字を作る
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_text"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        private static TMP_Text createText(string _name, Transform _parent, string _text, float _size, Color _color)
        {
            var go = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = _text;
            text.fontSize = _size;
            text.color = _color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>
        /// アンカーと pivot を同じ所にして置く
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_anchor"></param>
        /// <param name="_position"></param>
        private static void place(RectTransform _rect, Vector2 _anchor, Vector2 _position)
        {
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = _anchor;
            _rect.anchoredPosition = _position;
        }

        /// <summary>
        /// 親いっぱい（_inset だけ内側）
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_inset"></param>
        private static void stretch(RectTransform _rect, float _inset)
        {
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = new Vector2(_inset, _inset);
            _rect.offsetMax = new Vector2(-_inset, -_inset);
        }
    }
}
