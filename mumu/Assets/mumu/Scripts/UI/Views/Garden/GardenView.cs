using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// おにわ（仲間のパグが遊ぶ箱庭と、おにわに入っている子の操作）
    /// </summary>
    public class GardenView : ViewBase
    {
        private const float PugSize = 44f;
        /// <summary>おにわを開いた時の倍率（100%）</summary>
        private const float GardenStartScale = 1f;
        /// <summary>仲良し・ついて歩くを判定する間隔（秒）</summary>
        private const float SocialInterval = 1f;
        /// <summary>判定ごとに誰かがついて歩き始める確率</summary>
        private const float FollowRate = 0.12f;
        /// <summary>同じ子が続けてハートを出さない時間（秒）</summary>
        private const float HeartCooldown = 6f;
        private const float HeartOffsetY = 48f;
        private const float HeartRise = 36f;
        private const float HeartSize = 28f;
        private const float HeartDuration = 1f;

        /// <summary>箱庭のブロックの親</summary>
        [SerializeField]
        private GameObject mapRoot = null;
        /// <summary>箱庭の親（カメラで拡大縮小・移動する）</summary>
        [SerializeField]
        private RectTransform mapGroup = null;
        /// <summary>背景（押すと箱庭を動かせる）</summary>
        [SerializeField]
        private Image bgImage = null;
        /// <summary>タイトルへ戻るボタン</summary>
        [SerializeField]
        private MumuButton backButton = null;
        /// <summary>仲間の数</summary>
        [SerializeField]
        private TMP_Text countText = null;
        /// <summary>仲間がいない時の案内</summary>
        [SerializeField]
        private TMP_Text emptyText = null;
        /// <summary>ずかんを開くボタン</summary>
        [SerializeField]
        private MumuButton bookButton = null;

        private readonly List<GardenPug> pugs = new List<GardenPug>();
        private readonly Dictionary<GardenPug, float> heartTimes = new Dictionary<GardenPug, float>();
        private GardenMap gardenMap = null;
        private MapViewController viewController = null;
        private GardenInputController input = null;
        private float nextSocialTime = 0f;
        private bool isBookOpen = false;
        /// <summary>前回並べた子（並び順が変わった時だけ並べ直す）</summary>
        private readonly List<GardenPug> placedPugs = new List<GardenPug>();
        /// <summary>前回並べた時の、各子のブロック</summary>
        private readonly List<Block> placedBlocks = new List<Block>();
        private readonly List<Block> blockBuffer = new List<Block>();

        /// <summary>
        /// 初期化
        /// </summary>
        protected override async UniTask onInitView()
        {
            if (backButton != null)
            {
                backButton.SetOnClick(onClickBackButton);
            }
            if (bookButton != null)
            {
                bookButton.SetOnClick(onClickBookButton);
            }

            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlayThemeBgm(GardenMap.ThemeId);
            }

            IReadOnlyList<int> owned = PugCollection.Owned;
            if (countText != null)
            {
                countText.text = MumuText.GARDEN_STATUS(owned.Count, SaveDataManager.Data.bones);
            }
            if (emptyText != null)
            {
                emptyText.transform.parent.gameObject.SetActive(owned.Count == 0);
            }

            await setupGardenAsync();
            cheerRandomPug();
            await base.onInitView();
        }

        /// <summary>
        /// クリア後の最初のおにわで、ランダムな子が跳ねてハートを出す
        /// </summary>
        private void cheerRandomPug()
        {
            if (!SaveDataManager.Data.gardenCheerPending || pugs.Count == 0)
            {
                return;
            }
            SaveDataManager.Data.gardenCheerPending = false;
            SaveDataManager.Save();

            GardenPug pug = pugs[Random.Range(0, pugs.Count)];
            pug.Cheer();
            CommonUtility.PlaySe(eSe.Bark1);
            Vector2 above = ((RectTransform)pug.transform).anchoredPosition + new Vector2(0f, HeartOffsetY);
            HeartEffect.Play((RectTransform)mapRoot.transform, above, new Vector2(0f, HeartRise), HeartSize, HeartDuration, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// 描画順（立っているブロックのすぐ後ろ）と、仲良し・ついて歩く
        /// </summary>
        private void LateUpdate()
        {
            if (gardenMap == null || pugs.Count == 0)
            {
                return;
            }

            // 手前の子から順に「ブロックのすぐ後ろ」へ入れると、同じブロックの子は奥から手前の順に並ぶ
            pugs.Sort((_a, _b) => _a.Depth.CompareTo(_b.Depth));
            placeIfChanged();

            if (Time.unscaledTime >= nextSocialTime)
            {
                nextSocialTime = Time.unscaledTime + SocialInterval;
                showFriendHearts();
                startFollow();
            }
        }

        /// <summary>
        /// 子の並び順かブロックが変わった時だけ、ブロックのすぐ後ろへ並べ直す
        /// </summary>
        private void placeIfChanged()
        {
            blockBuffer.Clear();
            foreach (GardenPug pug in pugs)
            {
                blockBuffer.Add(gardenMap.GetBlock(pug.AnchorCell));
            }
            if (isSamePlacement())
            {
                return;
            }

            for (int i = 0; i < pugs.Count; i++)
            {
                if (blockBuffer[i] != null)
                {
                    IsoDepth.PlaceAfter(pugs[i].transform, blockBuffer[i].transform);
                }
            }
            placedPugs.Clear();
            placedPugs.AddRange(pugs);
            placedBlocks.Clear();
            placedBlocks.AddRange(blockBuffer);
        }

        /// <summary>
        /// 前回と同じ並び・同じブロックか
        /// </summary>
        private bool isSamePlacement()
        {
            if (placedPugs.Count != pugs.Count)
            {
                return false;
            }
            for (int i = 0; i < pugs.Count; i++)
            {
                if (placedPugs[i] != pugs[i] || placedBlocks[i] != blockBuffer[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// となり同士で座っている2匹がハートを出す
        /// </summary>
        private void showFriendHearts()
        {
            for (int i = 0; i < pugs.Count; i++)
            {
                for (int j = i + 1; j < pugs.Count; j++)
                {
                    GardenPug a = pugs[i], b = pugs[j];
                    Vector2Int diff = a.Cell - b.Cell;
                    if (!a.IsIdle || !b.IsIdle || Mathf.Abs(diff.x) + Mathf.Abs(diff.y) > 1 || !canHeart(a) || !canHeart(b))
                    {
                        continue;
                    }

                    heartTimes[a] = heartTimes[b] = Time.unscaledTime;
                    Vector2 middle = (((RectTransform)a.transform).anchoredPosition + ((RectTransform)b.transform).anchoredPosition) * 0.5f;
                    HeartEffect.Play((RectTransform)mapRoot.transform, middle + new Vector2(0f, HeartOffsetY), new Vector2(0f, HeartRise), HeartSize, HeartDuration, destroyCancellationToken).Forget();
                }
            }
        }

        /// <summary>
        /// ハートを出してから時間が経ったか
        /// </summary>
        /// <param name="_pug"></param>
        private bool canHeart(GardenPug _pug)
        {
            return !heartTimes.TryGetValue(_pug, out float time) || Time.unscaledTime - time >= HeartCooldown;
        }

        /// <summary>
        /// ときどき座っている子がほかの子のところへついて歩く
        /// </summary>
        private void startFollow()
        {
            if (pugs.Count < 2 || Random.value >= FollowRate)
            {
                return;
            }

            GardenPug follower = pugs[Random.Range(0, pugs.Count)];
            GardenPug leader = pugs[Random.Range(0, pugs.Count)];
            if (follower != leader)
            {
                follower.Follow(leader);
            }
        }

        /// <summary>
        /// 今日の箱庭を作り、カメラと操作をつなぎ、おにわに入っている子を放す
        /// </summary>
        private async UniTask setupGardenAsync()
        {
            if (mapRoot == null || mapGroup == null)
            {
                return;
            }

            var map = new GardenMap();
            if (!await map.InitAsync(mapRoot, destroyCancellationToken))
            {
                return;
            }
            gardenMap = map;

            ThemeBackground.Show(bgImage, map.Sprites);
            bgImage.raycastTarget = true;
            attachCamera(map);
            AmbientParticles.Create(mapGroup.parent, mapGroup.GetSiblingIndex() + 1, GardenMap.ThemeId);
            AmbientCritters.Create(mapGroup.parent, mapGroup.GetSiblingIndex() + 2, GardenMap.ThemeId);

            var cells = new List<Vector2Int>(map.WalkableCells);
            foreach (int id in PugCollection.GardenIds)
            {
                if (cells.Count == 0)
                {
                    break;
                }
                CharacterData character = CharacterMaster.Get(id);
                CharacterSprites sprites = CharacterSprites.Load(character);
                if (sprites == null)
                {
                    continue;
                }

                // なるべく別のマスから始める
                int index = Random.Range(0, cells.Count);
                Vector2Int cell = cells[index];
                if (cells.Count > 1)
                {
                    cells.RemoveAt(index);
                }
                GardenPug pug = GardenPug.Create(mapRoot.transform, character, sprites, map, cell, PugSize);
                pug.SetOnTapped(onPugTapped);
                pug.SetEquipped(id == PugCollection.EquippedId);
                pugs.Add(pug);
            }
        }

        /// <summary>
        /// カメラ（拡大縮小・移動）を箱庭につなぐ
        /// </summary>
        /// <param name="_map"></param>
        private void attachCamera(GardenMap _map)
        {
            // 操作のコンポーネントは Garden プレハブに入っている（無ければビルダーで作り直す）
            viewController = mapGroup.GetComponent<MapViewController>();
            input = mapGroup.parent.GetComponent<GardenInputController>();
            if (viewController != null && input != null)
            {
                viewController.Setup((RectTransform)transform, _map, GardenStartScale);
                input.Setup(viewController);
            }
            else
            {
                Debug.LogError("[GardenView] 箱庭の操作が付いていません（mumu > Build Garden Screen で作り直す）");
            }
        }

        /// <summary>
        /// パグをタップした（跳ねて、操作の確認を出す）
        /// </summary>
        /// <param name="_pug"></param>
        private void onPugTapped(GardenPug _pug)
        {
            showPugActionAsync(_pug).Forget();
        }

        /// <summary>
        /// パグの操作の確認を出す（一緒にいく・お休み）
        /// </summary>
        /// <param name="_pug"></param>
        private async UniTaskVoid showPugActionAsync(GardenPug _pug)
        {
            int id = _pug.Character.Id;
            bool isEquipped = id == PugCollection.EquippedId;
            bool canRest = PugCollection.GardenIds.Count > 1;

            var param = new ConfirmParam(
                _pug.Character.Name,
                (isEquipped ? MumuText.PUG_UNEQUIP() : MumuText.PUG_EQUIP()), true, () => onEquipSelected(_pug),
                MumuText.GARDEN_REST(), canRest, () => onRestSelected(_pug));
            await PopupManager.Instance.Open(ePopup.Confirm, param, destroyCancellationToken);
        }

        /// <summary>
        /// 一緒にいく・はずすを押した（連れていく子は1匹、はずすと男の子に戻る）。確認はもう一度出す
        /// </summary>
        /// <param name="_pug"></param>
        private void onEquipSelected(GardenPug _pug)
        {
            int id = _pug.Character.Id;
            PugCollection.Equip(id == PugCollection.EquippedId ? PugCollection.DefaultPlayerId : id);
            foreach (GardenPug pug in pugs)
            {
                pug.SetEquipped(pug.Character.Id == PugCollection.EquippedId);
            }
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
            showPugActionAsync(_pug).Forget();
        }

        /// <summary>
        /// お休みを押した（おにわから出す。最後の1匹は出せない）
        /// </summary>
        /// <param name="_pug"></param>
        private void onRestSelected(GardenPug _pug)
        {
            if (!PugCollection.RemoveFromGarden(_pug.Character.Id))
            {
                return;
            }

            pugs.Remove(_pug);
            heartTimes.Remove(_pug);
            Object.Destroy(_pug.gameObject);
        }

        /// <summary>
        /// ずかんボタン押下時の処理
        /// </summary>
        private void onClickBookButton()
        {
            openBookAsync().Forget();
        }

        /// <summary>
        /// ずかんを開く（交換・庭へで変わったら、おにわを開き直す）
        /// </summary>
        private async UniTaskVoid openBookAsync()
        {
            if (isBookOpen)
            {
                return;
            }
            isBookOpen = true;
            bool isChanged = await PopupManager.Instance.Open(ePopup.PugBook, new PugBookParam(), destroyCancellationToken);
            isBookOpen = false;
            if (isChanged && !destroyCancellationToken.IsCancellationRequested)
            {
                ViewManager.Instance.ToGarden().Forget();
            }
        }

        /// <summary>
        /// 戻るボタン押下時の処理
        /// </summary>
        private void onClickBackButton()
        {
            ViewManager.Instance.ToTitle().Forget();
        }
    }
}
