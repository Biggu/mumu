using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 冒険のステージマップ（10 ステージで1ページ、パグが次のステージまで歩く）
    /// </summary>
    public class StageMapView : ViewBase
    {
        private const int StagesPerPage = 10;
        private const float NodeSize = 96f;
        private const float NodeFontSize = 40f;
        private const float SmallStarSize = 26f;
        private const float PawSize = 18f;
        private const float PawSpacing = 34f;
        /// <summary>ノードを並べる範囲の余白（上下）</summary>
        private const float PathMarginY = 120f;
        /// <summary>くねくねの横幅（範囲の幅に対する割合）</summary>
        private const float PathSwingRate = 0.3f;
        private const float PugSize = 64f;
        private const float PugOffsetY = 34f;
        private const float WalkDuration = 0.9f;
        private const float CurrentPulseSpeed = 4f;
        private const float CurrentPulseScale = 0.08f;
        /// <summary>次のページへ進んだ時にページが下から上がってくる時間</summary>
        private const float PageSlideDuration = 0.7f;
        /// <summary>次のページへ進んだ時、パグが最初のノードの下のこの距離から走ってくる</summary>
        private const float PageEnterDistance = 160f;
        private const float TitlePunchAmount = 0.3f;
        private const float TitlePunchDuration = 0.3f;

        private const string NodeSpritePath = "Sprites/UI/base_ui/ui_button_round";
        private const string StarOnPath = "Sprites/UI/base_ui/ui_star_on";
        private const string StarOffPath = "Sprites/UI/base_ui/ui_star_off";
        private const string CrownPath = "Sprites/UI/base_ui/ui_crown_on";
        private const string PawPath = "Sprites/Common/paw";
        private const string LockPrefabPath = "Prefabs/Common/stage_lock";
        private const string PageButtonPath = "Prefabs/Common/stage_page_button";
        private const string BonusIconPath = "Prefabs/Common/bonus_icon";
        private const string BonusSpriteName = "gimmick_bone";
        private const string ArrowLeftPath = "Sprites/UI/ui_kit_02/kit_icon_arrow_left";
        private const string ArrowRightPath = "Sprites/UI/ui_kit_02/kit_icon_arrow_right";
        /// <summary>ページを送る矢印の、画面の端からの距離</summary>
        private const float PageButtonMargin = 60f;

        private static readonly Color ClearedColor = new Color(0.55f, 0.85f, 0.55f);
        private static readonly Color CurrentColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color LockedColor = new Color(0.75f, 0.75f, 0.75f);
        private static readonly Color NodeTextColor = new Color(0.29f, 0.17f, 0.06f);
        private static readonly Color PawReachedColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color PawLockedColor = new Color(1f, 1f, 1f, 0.35f);

        /// <summary>背景（今のステージのテーマ）</summary>
        [SerializeField]
        private Image bgImage = null;
        /// <summary>ノードを並べる範囲</summary>
        [SerializeField]
        private RectTransform nodeRoot = null;
        /// <summary>タイトルへ戻るボタン</summary>
        [SerializeField]
        private MumuButton backButton = null;
        /// <summary>あそぶボタン</summary>
        [SerializeField]
        private MumuButton playButton = null;
        /// <summary>見出し（ステージ 1〜10）</summary>
        [SerializeField]
        private TMP_Text titleText = null;

        private readonly List<Vector2> nodePositions = new List<Vector2>();
        /// <summary>ページの中身の親（ページを動かす時に使う）</summary>
        private RectTransform pageRoot = null;
        private RectTransform currentNode = null;
        private RectTransform pug = null;
        private SpriteFrameAnimation pugAnimation = null;
        private PugSprites pugSprites = null;
        private bool isStarting = false;
        /// <summary>今見ているページ（0 始まり）</summary>
        private int shownPage = 0;
        /// <summary>最後に見ていたページ（アプリを起動し直すと -1 に戻り、最新のページを出す）</summary>
        private static int lastViewedPage = -1;
        /// <summary>今のステージのページ（これより新しいページへは行けない）</summary>
        private int currentPage = 0;
        private List<MapInfo> mapList = null;
        /// <summary>パグが道を走っている間は、ページを送れない</summary>
        private bool isWalking = false;
        private MumuButton olderButton = null;
        private MumuButton newerButton = null;

        /// <summary>
        /// 初期化
        /// </summary>
        protected override async UniTask onInitView()
        {
            if (backButton != null)
            {
                backButton.SetOnClick(onClickBackButton);
            }
            if (playButton != null)
            {
                playButton.SetOnClick(onClickPlayButton);
            }

            List<MapInfo> list = MapCsv.LoadList();
            int current = StageProgress.GetCurrentIndex();
            mapList = list;
            currentPage = current / StagesPerPage;
            shownPage = lastViewedPage >= 0 ? Mathf.Min(lastViewedPage, currentPage) : currentPage;
            lastViewedPage = shownPage;
            if (list.Count > 0)
            {
                await setupBackgroundAsync(list[Mathf.Min(current, list.Count - 1)].ThemeId);
                buildPage(list, shownPage, current);
                createPageButtons();
            }
            await base.onInitView();

            walkThenUnlockAsync(GameSession.WalkFromIndex, current).Forget();
            GameSession.WalkFromIndex = -1;
        }

        /// <summary>
        /// 今のステージのノードを脈打たせる
        /// </summary>
        private void Update()
        {
            if (currentNode != null)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * CurrentPulseSpeed) * CurrentPulseScale;
                currentNode.localScale = new Vector3(scale, scale, 1f);
            }
        }

        /// <summary>
        /// 背景と BGM（今のステージのテーマ）
        /// </summary>
        /// <param name="_themeId"></param>
        private async UniTask setupBackgroundAsync(int _themeId)
        {
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlayThemeBgm(_themeId);
            }

            ThemeSprites sprites = await ThemeSprites.LoadAsync(_themeId, destroyCancellationToken);
            if (sprites != null)
            {
                ThemeBackground.Show(bgImage, sprites);
                AmbientParticles.Create(transform, bgImage != null ? bgImage.transform.GetSiblingIndex() + 1 : 0, _themeId);
            }
        }

        /// <summary>
        /// 今のステージがあるページのノードと道を並べる
        /// </summary>
        /// <param name="_list"></param>
        /// <param name="_current"></param>
        private void buildPage(List<MapInfo> _list, int _page, int _current)
        {
            nodePositions.Clear();
            currentNode = null;
            pug = null;
            pugAnimation = null;
            int first = _page * StagesPerPage;
            int last = Mathf.Min(first + StagesPerPage, _list.Count) - 1;
            if (titleText != null)
            {
                titleText.text = MumuText.STAGE_RANGE(first + 1, last + 1);
            }

            var page = new GameObject("page", typeof(RectTransform));
            page.transform.SetParent(nodeRoot, false);
            pageRoot = (RectTransform)page.transform;
            pageRoot.anchorMin = Vector2.zero;
            pageRoot.anchorMax = Vector2.one;
            pageRoot.offsetMin = pageRoot.offsetMax = Vector2.zero;

            Rect area = nodeRoot.rect;
            for (int i = first; i <= last; i++)
            {
                float t = StagesPerPage > 1 ? (float)(i - first) / (StagesPerPage - 1) : 0f;
                float y = Mathf.Lerp(area.yMin + PathMarginY, area.yMax - PathMarginY, t);
                float x = Mathf.Sin((i - first) * 1.1f) * area.width * PathSwingRate;
                nodePositions.Add(new Vector2(x, y));
            }

            // 道（線）→ ノード → パグの順に重ねる
            for (int i = 1; i < nodePositions.Count; i++)
            {
                bool isReached = first + i <= _current;
                createTrail(nodePositions[i - 1], nodePositions[i], isReached ? PawReachedColor : PawLockedColor);
            }
            for (int i = first; i <= last; i++)
            {
                RectTransform node = createNode(i, _current, nodePositions[i - first]);
                if (i == _current)
                {
                    currentNode = node;
                }
            }
            // パグは今のステージのページにだけ置く
            if (_current / StagesPerPage == _page)
            {
                createPug(nodePositions[_current - first]);
            }
        }

        /// <summary>
        /// ノード間を線でつなぐ（ノードの下に敷く。ノードが線の端を隠す）
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        /// <param name="_color"></param>
        private void createTrail(Vector2 _from, Vector2 _to, Color _color)
        {
            const float LineWidth = 10f;
            Vector2 diff = _to - _from;
            float length = diff.magnitude;
            Image line = UiFactory.CreateImage("trail", pageRoot, null, new Vector2(length, LineWidth));
            line.color = _color;
            line.raycastTarget = false;
            line.rectTransform.anchoredPosition = (_from + _to) * 0.5f;
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

            // 線の上に肉球を並べる（線の色と違う白にして見えるように）
            Sprite paw = ResourceManager.Instance.Load<Sprite>(PawPath);
            int count = Mathf.FloorToInt((length - NodeSize) / PawSpacing);
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg - 90f;
            for (int i = 1; i <= count; i++)
            {
                float t = (NodeSize * 0.5f + i * PawSpacing) / length;
                Vector2 side = new Vector2(-diff.y, diff.x).normalized * (i % 2 == 0 ? 5f : -5f);
                Image image = UiFactory.CreateImage("paw", pageRoot, paw, new Vector2(PawSize, PawSize));
                image.color = Color.white;
                image.raycastTarget = false;
                image.rectTransform.anchoredPosition = Vector2.Lerp(_from, _to, t) + side;
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>
        /// ステージのノード（番号・★・王冠・報酬のパグ）
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="_current"></param>
        /// <param name="_position"></param>
        private RectTransform createNode(int _index, int _current, Vector2 _position)
        {
            int number = _index + 1;
            bool isCleared = _index < StageProgress.ClearedCount;
            bool isCurrent = _index == _current;
            bool isBonus = mapList != null && _index < mapList.Count && mapList[_index].IsBonus;

            Image node = UiFactory.CreateImage($"stage{number}", pageRoot, ResourceManager.Instance.Load<Sprite>(NodeSpritePath), new Vector2(NodeSize, NodeSize));
            node.color = isCurrent ? CurrentColor : (isCleared ? ClearedColor : LockedColor);
            node.rectTransform.anchoredPosition = _position;

            // 今のステージは押して始められる。クリアしたステージは、もう一度遊べる（進みは変わらない）
            // ボーナスステージは一度だけ（クリアしたら押せない）
            if (isCurrent || (isCleared && !isBonus))
            {
                node.raycastTarget = true;
                var button = node.gameObject.AddComponent<MumuButton>();
                button.targetGraphic = node;
                if (isCurrent)
                {
                    button.SetOnClick(onClickPlayButton);
                }
                else
                {
                    int replayIndex = _index;
                    button.SetOnClick(() => onClickReplay(replayIndex));
                }
            }

            // ボーナスステージは番号の代わりにほねの絵
            if (isBonus)
            {
                createBonusIcon(node.transform);
            }
            else
            {
                createText(node.transform, number.ToString(), NodeFontSize);
            }

            // まだ開いていないステージには鍵の印（Prefab から出す）
            if (!isCleared && !isCurrent)
            {
                GameObject lockPrefab = ResourceManager.Instance.Load<GameObject>(LockPrefabPath);
                if (lockPrefab != null)
                {
                    Transform lockIcon = UnityEngine.Object.Instantiate(lockPrefab, node.transform, false).transform;
                    lockIcon.localPosition = Vector3.zero;
                }
            }

            if (isCleared)
            {
                int stars = StageRecord.GetStars(number);
                if (stars == 3 && StageRecord.IsNoRewind(number))
                {
                    // 地図の上は動かさない絵（1コマ目）
                    Image bigStar = UiFactory.CreateImage("bigStar", node.transform, RainbowStar.Still, SmallStarSize * 2f);
                    bigStar.rectTransform.anchoredPosition = new Vector2(0f, -NodeSize * 0.5f - 6f);
                }
                else
                {
                    for (int s = 0; s < 3; s++)
                    {
                        Image star = UiFactory.CreateImage("star", node.transform, ResourceManager.Instance.Load<Sprite>(s < stars ? StarOnPath : StarOffPath), new Vector2(SmallStarSize, SmallStarSize));
                        star.rectTransform.anchoredPosition = new Vector2((s - 1) * SmallStarSize, -NodeSize * 0.5f - 6f);
                    }
                }
                if (StageRecord.IsPerfect(number))
                {
                    Image crown = UiFactory.CreateImage("crown", node.transform, ResourceManager.Instance.Load<Sprite>(CrownPath), new Vector2(SmallStarSize * 1.4f, SmallStarSize * 1.4f));
                    crown.rectTransform.anchoredPosition = new Vector2(NodeSize * 0.4f, NodeSize * 0.4f);
                }
            }

            // 鍵のかかったステージを押すと、まだ遊べないことを音で知らせる
            if (!isCurrent && !isCleared)
            {
                node.raycastTarget = true;
                var lockedButton = node.gameObject.AddComponent<MumuButton>();
                lockedButton.targetGraphic = node;
                lockedButton.SetOnClick(onClickLocked);
            }

            return node.rectTransform;
        }

        /// <summary>
        /// 男の子のパグ（今のステージの上に座る）
        /// </summary>
        /// <param name="_position"></param>
        private void createPug(Vector2 _position)
        {
            pugSprites = PugSprites.Load();
            if (pugSprites == null)
            {
                return;
            }
            // 連れていくパグがいればその子が歩く
            CharacterSprites equipped = CharacterSprites.Load(CharacterMaster.Get(PugCollection.EquippedId));
            if (equipped != null)
            {
                pugSprites = pugSprites.WithBoy(equipped);
            }

            GameObject go = PartsLoaderManager.Instance.Create(ePart.StagePug, pageRoot);
            if (go == null)
            {
                return;
            }
            Image image = go.GetComponent<Image>();
            image.sprite = pugSprites.BoySit[0];
            image.rectTransform.sizeDelta = new Vector2(PugSize, PugSize);
            pug = image.rectTransform;
            pug.pivot = new Vector2(0.5f, 0.08f);
            pug.anchoredPosition = _position + new Vector2(0f, PugOffsetY);
            pugAnimation = image.gameObject.AddComponent<SpriteFrameAnimation>();
            pugAnimation.Play(pugSprites.BoySit, 3f);
            PugAura.Attach(pug, pugSprites.BoyTier, PugSize);
        }

        /// <summary>
        /// クリアしたステージから今のステージまで走る（前のページから来た時はページが上がってきて、下から走ってくる）
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        private async UniTask walkAsync(int _from, int _to)
        {
            int first = _to / StagesPerPage * StagesPerPage;
            if (pug == null || _from < 0 || _from >= _to)
            {
                return;
            }

            Vector2 end = nodePositions[_to - first] + new Vector2(0f, PugOffsetY);
            Vector2 start;
            if (_from < first)
            {
                // 次のエリアへ: パグを下に隠して、ページを下から上げる
                start = end - new Vector2(0f, PageEnterDistance);
                pug.anchoredPosition = start;
                pageRoot.anchoredPosition = new Vector2(0f, -nodeRoot.rect.height);
                if (await waitFadeInAsync() || await slidePageAsync())
                {
                    return;
                }
                if (titleText != null)
                {
                    UiTween.PunchScale(titleText.transform, Vector3.one, TitlePunchAmount, TitlePunchDuration, destroyCancellationToken).Forget();
                }
            }
            else
            {
                start = nodePositions[_from - first] + new Vector2(0f, PugOffsetY);
                pug.anchoredPosition = start;
                if (await waitFadeInAsync())
                {
                    return;
                }
            }
            pug.localScale = new Vector3(end.x < start.x ? -1f : 1f, 1f, 1f);
            CommonUtility.PlaySe(eSe.Footstep);
            pugAnimation.Play(pugSprites.BoyRun, 12f);

            for (float time = 0f; time < WalkDuration; time += Time.unscaledDeltaTime)
            {
                float t = time / WalkDuration;
                pug.anchoredPosition = Vector2.Lerp(start, end, t) + new Vector2(0f, Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f)) * 6f);
                if (await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken).SuppressCancellationThrow())
                {
                    return;
                }
            }

            pug.anchoredPosition = end;
            pug.localScale = Vector3.one;
            pugAnimation.Play(pugSprites.BoySit, 3f);
            UiTween.PunchScale(pug, Vector3.one, 0.3f, 0.3f, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// 歩き終わるまでページを送れないようにして歩かせる
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        private async UniTaskVoid walkThenUnlockAsync(int _from, int _to)
        {
            isWalking = true;
            await walkAsync(_from, _to);
            isWalking = false;
        }

        /// <summary>
        /// 黒フェードが明けるのを待つ（中断されたら true）
        /// </summary>
        private UniTask<bool> waitFadeInAsync()
        {
            return UniTask.Delay(System.TimeSpan.FromSeconds(FadeScreen.DefaultDuration), cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
        }

        /// <summary>
        /// ページを下から上げる（中断されたら true）
        /// </summary>
        private async UniTask<bool> slidePageAsync()
        {
            CommonUtility.PlaySe(eSe.PageFlip);
            float height = nodeRoot.rect.height;
            for (float time = 0f; time < PageSlideDuration; time += Time.unscaledDeltaTime)
            {
                float t = 1f - Mathf.Pow(1f - time / PageSlideDuration, 3f);
                pageRoot.anchoredPosition = new Vector2(0f, -height * (1f - t));
                if (await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken).SuppressCancellationThrow())
                {
                    return true;
                }
            }
            pageRoot.anchoredPosition = Vector2.zero;
            return false;
        }

        /// <summary>
        /// ボーナスステージのほねの絵（Prefab から出す）
        /// </summary>
        /// <param name="_parent"></param>
        private void createBonusIcon(Transform _parent)
        {
            GameObject prefab = ResourceManager.Instance.Load<GameObject>(BonusIconPath);
            if (prefab == null)
            {
                return;
            }
            GameObject go = UnityEngine.Object.Instantiate(prefab, _parent, false);
            Image image = go.GetComponentInChildren<Image>();
            Sprite[] frames = GimmickSprites.LoadFrames(BonusSpriteName);
            if (image != null && frames.Length > 0)
            {
                image.sprite = frames[0];
            }
        }

        /// <summary>
        /// 文字を作る（親いっぱい）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_text"></param>
        /// <param name="_size"></param>
        private static void createText(Transform _parent, string _text, float _size)
        {
            var go = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = _text;
            text.fontSize = _size;
            text.color = NodeTextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        /// <summary>
        /// 鍵のかかったステージを押した時の処理（まだ遊べない音を鳴らす）
        /// </summary>
        private void onClickLocked()
        {
            CommonUtility.PlaySe(eSe.LockedTap);
        }

        /// <summary>
        /// クリアしたステージを押した時の処理（もう一度遊ぶ）
        /// </summary>
        /// <param name="_index"></param>
        private void onClickReplay(int _index)
        {
            if (isStarting || isWalking || (mapList != null && _index < mapList.Count && mapList[_index].IsBonus))
            {
                return;
            }
            isStarting = true;
            CommonUtility.PlaySe(eSe.ReplayStart);
            GameSession.StartReplay(_index);
            ViewManager.Instance.ToGameWithFade().Forget();
        }

        /// <summary>
        /// ページの矢印を作る（Prefab から出す。左は前のページ、右は次のページ）
        /// </summary>
        private void createPageButtons()
        {
            olderButton = createPageButton(ArrowLeftPath, 0f, PageButtonMargin, () => showPage(shownPage - 1));
            newerButton = createPageButton(ArrowRightPath, 1f, -PageButtonMargin, () => showPage(shownPage + 1));
            updatePageButtons();
        }

        /// <summary>
        /// ページの矢印を1つ作る
        /// </summary>
        /// <param name="_sprite"></param>
        /// <param name="_anchorX">0 = 左端、1 = 右端</param>
        /// <param name="_offsetX"></param>
        /// <param name="_onClick"></param>
        private MumuButton createPageButton(string _sprite, float _anchorX, float _offsetX, System.Action _onClick)
        {
            GameObject prefab = ResourceManager.Instance.Load<GameObject>(PageButtonPath);
            if (prefab == null || nodeRoot == null)
            {
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, nodeRoot, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(_anchorX, 0.5f);
            rect.anchoredPosition = new Vector2(_offsetX, 0f);
            var image = go.GetComponentInChildren<Image>();
            if (image != null)
            {
                image.sprite = ResourceManager.Instance.Load<Sprite>(_sprite);
            }
            var button = go.GetComponent<MumuButton>();
            if (button != null)
            {
                button.SetOnClick(_onClick);
            }
            return button;
        }

        /// <summary>
        /// 前後のページの矢印を、ページがあるときだけ出す（矢印は前に出す）
        /// </summary>
        private void updatePageButtons()
        {
            if (olderButton != null)
            {
                olderButton.gameObject.SetActive(shownPage > 0);
                olderButton.transform.SetAsLastSibling();
            }
            if (newerButton != null)
            {
                newerButton.gameObject.SetActive(shownPage < currentPage);
                newerButton.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 指定のページを表示する（パグが歩いている間は送らない）
        /// </summary>
        /// <param name="_page"></param>
        private void showPage(int _page)
        {
            if (isWalking || isStarting || mapList == null || _page == shownPage || _page < 0 || _page > currentPage)
            {
                return;
            }
            shownPage = _page;
            lastViewedPage = _page;
            if (pageRoot != null)
            {
                UnityEngine.Object.Destroy(pageRoot.gameObject);
            }
            buildPage(mapList, shownPage, StageProgress.GetCurrentIndex());
            updatePageButtons();
        }

        /// <summary>
        /// あそぶボタン押下時の処理
        /// </summary>
        private void onClickPlayButton()
        {
            if (isStarting)
            {
                return;
            }
            isStarting = true;
            GameSession.StartAdventure();
            ViewManager.Instance.ToGameWithFade().Forget();
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
