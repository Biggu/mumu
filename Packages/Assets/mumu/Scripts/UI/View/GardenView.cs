using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// おにわ（仲間のパグが遊ぶ箱庭と、全キャラクターの一覧）
    /// </summary>
    public class GardenView : ViewBase
    {
        /// <summary>箱庭が画面に占める割合（横）</summary>
        private const float StageFitRateX = 0.95f;
        /// <summary>箱庭が画面に占める割合（縦）</summary>
        private const float StageFitRateY = 0.6f;
        /// <summary>箱庭の中心のずらし量（下の一覧を避けて上へ）</summary>
        private static readonly Vector2 StageOffset = new Vector2(0f, 60f);
        /// <summary>一度に出すパグの上限（多すぎると重なるため）</summary>
        private const int MaxPugs = 30;
        private const float PugSize = 44f;
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
        /// <summary>箱庭の親（画面に合わせて拡大）</summary>
        [SerializeField]
        private RectTransform mapGroup = null;
        /// <summary>背景</summary>
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
        private float nextSocialTime = 0f;
        private bool isBookOpen = false;

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
                countText.text = TextLocalizer.Get("GARDEN_STATUS", owned.Count, SaveDataManager.Data.bones);
            }
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(owned.Count == 0);
            }

            await setupGardenAsync(owned);
            await base.onInitView();
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
            foreach (GardenPug pug in pugs)
            {
                Block block = gardenMap.GetBlock(pug.AnchorCell);
                if (block != null)
                {
                    IsoDepth.PlaceAfter(pug.transform, block.transform);
                }
            }

            if (Time.unscaledTime >= nextSocialTime)
            {
                nextSocialTime = Time.unscaledTime + SocialInterval;
                showFriendHearts();
                startFollow();
            }
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
        /// 箱庭を作って仲間を放す
        /// </summary>
        /// <param name="_owned"></param>
        private async UniTask setupGardenAsync(IReadOnlyList<int> _owned)
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

            UiImageUtility.SetTiled(bgImage, map.Sprites.Background);
            fitStage(map.GetContentBounds());
            AmbientParticles.Create(mapGroup.parent, mapGroup.GetSiblingIndex() + 1, GardenMap.ThemeId);
            AmbientCritters.Create(mapGroup.parent, mapGroup.GetSiblingIndex() + 2, GardenMap.ThemeId);


            var cells = new List<Vector2Int>(map.WalkableCells);
            for (int i = 0; i < _owned.Count && i < MaxPugs && cells.Count > 0; i++)
            {
                CharacterData character = CharacterMaster.Get(_owned[i]);
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
                pug.SetOnEquip(onEquipPug);
                pug.SetEquipped(character.Id == PugCollection.EquippedId);
                pugs.Add(pug);
            }
        }

        /// <summary>
        /// 箱庭を画面に合わせる
        /// </summary>
        /// <param name="_bounds"></param>
        private void fitStage(Rect _bounds)
        {
            Rect view = ((RectTransform)transform).rect;
            float scale = Mathf.Min(view.width * StageFitRateX / _bounds.width, view.height * StageFitRateY / _bounds.height);
            mapGroup.localScale = new Vector3(scale, scale, 1f);
            mapGroup.anchoredPosition = StageOffset - _bounds.center * scale;
        }

        /// <summary>
        /// つれていく・はずすを押した時（連れていく子は1匹、はずすと男の子に戻る）
        /// </summary>
        /// <param name="_pug"></param>
        private void onEquipPug(GardenPug _pug)
        {
            int id = _pug.Character.Id == PugCollection.EquippedId ? PugCollection.DefaultPlayerId : _pug.Character.Id;
            PugCollection.Equip(id);
            foreach (GardenPug pug in pugs)
            {
                pug.SetEquipped(pug.Character.Id == PugCollection.EquippedId);
            }
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
        }

        /// <summary>
        /// ずかんボタン押下時の処理
        /// </summary>
        private void onClickBookButton()
        {
            openBookAsync().Forget();
        }

        /// <summary>
        /// ずかんを開く（交換したら新しい子が出るよう、おにわを開き直す）
        /// </summary>
        private async UniTaskVoid openBookAsync()
        {
            if (isBookOpen)
            {
                return;
            }
            isBookOpen = true;
            bool isExchanged = await PugBookPopup.OpenAsync(transform, destroyCancellationToken);
            isBookOpen = false;
            if (isExchanged && !destroyCancellationToken.IsCancellationRequested)
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
