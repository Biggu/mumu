using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ずかん（全キャラクターのグリッド。ほねと交換してなかまにできる）
    /// </summary>
    public class PugBookPopup : PopupBase
    {
        public const string PrefabName = "pug_book";

        private const float OpenPunchAmount = 0.05f;
        private const float OpenPunchDuration = 0.2f;
        private const string ScrollItemPrefabPath = "Prefabs/Common/pug_book_scroll_item";
        private const string FilterButtonPath = "Prefabs/Common/filter_button";
        /// <summary>フィルターボタンの、パネルの角からの距離</summary>
        private const float FilterButtonMargin = 20f;

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

        /// <summary>1匹分の表示</summary>
        private class Item
        {
            public PugBookScrollItem View;
            /// <summary>キャラクター ID（未所持だけ出す時に使う）</summary>
            public int Id;
        }

        /// <summary>未所持のパグだけ出すか</summary>
        private bool isUnownedOnly = false;
        /// <summary>「未所持のみ」の切り替えボタン</summary>
        private MumuButton filterButton = null;
        /// <summary>ボタンの色（選ばれている時と、ふつう）</summary>
        private static readonly Color FilterOnColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color FilterOffColor = Color.white;

        /// <summary>いま並んでいる tier のカード</summary>
        private List<Item> items = new List<Item>();
        /// <summary>tier ごとに作ったカード（一度作ったら作り直さない）</summary>
        private readonly Dictionary<int, List<Item>> tierItems = new Dictionary<int, List<Item>>();
        private PugBookScrollItem scrollItemPrefab = null;
        private bool isExchanged = false;
        private bool isConfirming = false;

        /// <summary>
        /// 初期化（ずかんは内容を受け取らない）
        /// </summary>
        /// <param name="_param"></param>
        public override void Setup(BaseParam _param)
        {
            setup();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void setup()
        {
            closeButton.SetOnClick(onClickCloseButton);
            filterButton = createFilterButton();
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
            scrollItemPrefab = ResourceManager.Instance.Load<PugBookScrollItem>(ScrollItemPrefabPath);
            if (scrollItemPrefab == null)
            {
                return;
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
            foreach (List<Item> cards in tierItems.Values)
            {
                setCardsActive(cards, false);
            }
            if (!tierItems.TryGetValue(_tier, out items))
            {
                items = new List<Item>();
                foreach (CharacterData character in CharacterMaster.All)
                {
                    if (character.Tier == _tier)
                    {
                        items.Add(createItem(character));
                    }
                }
                tierItems[_tier] = items;
            }
            applyFilter(items);
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
        /// カードの表示を切り替える
        /// </summary>
        /// <param name="_cards"></param>
        /// <param name="_isActive"></param>
        private void applyFilter(List<Item> _cards)
        {
            foreach (Item item in _cards)
            {
                item.View.gameObject.SetActive(!isUnownedOnly || !PugCollection.IsOwned(item.Id));
            }
        }

        /// <summary>
        /// 「未所持のみ」ボタンを作る（Prefabs/Common/filter_button から出す）
        /// </summary>
        private MumuButton createFilterButton()
        {
            GameObject prefab = ResourceManager.Instance.Load<GameObject>(FilterButtonPath);
            if (prefab == null || panel == null)
            {
                return null;
            }
            GameObject go = Instantiate(prefab, panel, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-FilterButtonMargin, -FilterButtonMargin);
            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = MumuText.FILTER_UNOWNED();
            }
            var button = go.GetComponent<MumuButton>();
            if (button != null)
            {
                button.image.color = FilterOffColor;
                button.SetOnClick(onClickFilterButton);
            }
            return button;
        }

        /// <summary>
        /// 「未所持のみ」の切り替え
        /// </summary>
        private void onClickFilterButton()
        {
            isUnownedOnly = !isUnownedOnly;
            if (filterButton != null)
            {
                filterButton.image.color = isUnownedOnly ? FilterOnColor : FilterOffColor;
            }
            showTier(selectedTier);
        }

        private static void setCardsActive(List<Item> _cards, bool _isActive)
        {
            foreach (Item item in _cards)
            {
                item.View.gameObject.SetActive(_isActive);
            }
        }

        /// <summary>
        /// ほね・持っているか・交換できるかを反映
        /// </summary>
        private void refresh()
        {
            int bones = SaveDataManager.Data.bones;
            boneText.text = MumuText.PUG_BOOK_BONES(bones);
            foreach (Item item in items)
            {
                item.View.Refresh(bones, PugCollection.EquippedId);
            }
        }

        /// <summary>スクロールアイテムのプレハブを作成してキャラクター情報を渡す</summary>
        /// <param name="_character"></param>
        private Item createItem(CharacterData _character)
        {
            PugBookScrollItem view = Instantiate(scrollItemPrefab, content, false);
            view.Setup(_character, onClickPriceButton, onClickOwnedItem);
            return new Item { View = view, Id = _character.Id };
        }

        /// <summary>
        /// 交換ボタン押下時の処理
        /// </summary>
        /// <param name="_characterId"></param>
        private void onClickPriceButton(int _characterId)
        {
            showActionAsync(_characterId).Forget();
        }

        /// <summary>
        /// 持っている子のカード押下時の処理
        /// </summary>
        /// <param name="_characterId"></param>
        private void onClickOwnedItem(int _characterId)
        {
            showActionAsync(_characterId).Forget();
        }

        /// <summary>
        /// 2つの確認（交換・庭へ）を出す。持っているか、ほねが足りるか、庭が一杯かで押せるかを決める
        /// </summary>
        /// <param name="_characterId"></param>
        private async UniTaskVoid showActionAsync(int _characterId)
        {
            CharacterData character = CharacterMaster.Get(_characterId);
            if (character == null)
            {
                return;
            }
            bool isOwned = PugCollection.IsOwned(_characterId);
            bool isEquipped = _characterId == PugCollection.EquippedId;
            bool canExchange = character.IsForSale && !isOwned && SaveDataManager.Data.bones >= character.Price;
            bool canGarden = isOwned && !PugCollection.IsInGarden(_characterId) && PugCollection.GardenIds.Count < UserSaveData.MaxGarden;
            string firstLabel = isOwned
                ? (isEquipped ? MumuText.PUG_UNEQUIP() : MumuText.PUG_EQUIP())
                : MumuText.PUG_EXCHANGE();
            bool firstEnabled = isOwned || canExchange;
            System.Action firstAction = isOwned
                ? () => equipCharacter(_characterId)
                : () => confirmExchangeAsync(_characterId).Forget();

            var param = new ConfirmParam(
                character.Name,
                firstLabel, firstEnabled, firstAction,
                MumuText.PUG_TO_GARDEN(), canGarden, () => addToGarden(_characterId));
            await PopupManager.Instance.Open(ePopup.Confirm, param, destroyCancellationToken);
        }

        /// <summary>
        /// 確認後に連れていく子を切り替える
        /// </summary>
        /// <param name="_characterId"></param>
        private void equipCharacter(int _characterId)
        {
            PugCollection.Equip(_characterId == PugCollection.EquippedId ? PugCollection.DefaultPlayerId : _characterId);
            refresh();
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
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
            PopupManager popupManager = PopupManager.Instance;

            isConfirming = true;
            if (SaveDataManager.Data.bones < character.Price)
            {
                await popupManager.Open(ePopup.Alert, new AlertParam(MumuText.PUG_BUY_TITLE(), MumuText.PUG_BUY_SHORT(character.Price - SaveDataManager.Data.bones), false, sitFrames), destroyCancellationToken);
                isConfirming = false;
                return;
            }

            bool isYes = await popupManager.Open(ePopup.Alert, new AlertParam(MumuText.PUG_BUY_TITLE(), MumuText.PUG_BUY_BODY(character.Name, character.Price), true, sitFrames), destroyCancellationToken);
            if (isYes && CharacterMaster.Exchange(_characterId))
            {
                isExchanged = true;
                refresh();
                SoundManager sound = SoundManager.Instance;
                if (sound != null)
                {
                    sound.PlaySe(eSe.Clear);
                }
                await popupManager.Open(ePopup.Alert, new AlertParam(MumuText.PUG_JOINED_TITLE(), MumuText.PUG_JOINED_BODY(character.Name), false, sprites != null ? sprites.Happy : null), destroyCancellationToken);
            }
            isConfirming = false;
        }

        /// <summary>
        /// 持っている子をおにわに入れる（入ったら、おにわを開き直せるよう変更を伝える）
        /// </summary>
        /// <param name="_characterId"></param>
        private void addToGarden(int _characterId)
        {
            if (PugCollection.AddToGarden(_characterId))
            {
                isExchanged = true;
                refresh();
            }
        }

        /// <summary>
        /// 閉じるボタン押下時の処理（交換した時は true で閉じる）
        /// </summary>
        private void onClickCloseButton()
        {
            if (!isConfirming)
            {
                close(isExchanged);
            }
        }

    }
}
