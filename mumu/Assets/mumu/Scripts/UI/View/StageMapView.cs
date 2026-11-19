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

        private const string NodeSpritePath = "Sprites/UI/ui_button_round";
        private const string StarOnPath = "Sprites/UI/ui_star_on";
        private const string StarOffPath = "Sprites/UI/ui_star_off";
        private const string CrownPath = "Sprites/UI/ui_crown_on";
        private const string PawPath = "Sprites/Common/paw";

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
            if (list.Count > 0)
            {
                await setupBackgroundAsync(list[Mathf.Min(current, list.Count - 1)].ThemeId);
                buildPage(list, current);
            }
            await base.onInitView();

            walkAsync(GameSession.WalkFromIndex, current).Forget();
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
                UiImageUtility.SetTiled(bgImage, sprites.Background);
                AmbientParticles.Create(transform, bgImage != null ? bgImage.transform.GetSiblingIndex() + 1 : 0, _themeId);
            }
        }

        /// <summary>
        /// 今のステージがあるページのノードと道を並べる
        /// </summary>
        /// <param name="_list"></param>
        /// <param name="_current"></param>
        private void buildPage(List<MapInfo> _list, int _current)
        {
            int first = _current / StagesPerPage * StagesPerPage;
            int last = Mathf.Min(first + StagesPerPage, _list.Count) - 1;
            if (titleText != null)
            {
                titleText.text = TextLocalizer.Get("STAGE_RANGE", first + 1, last + 1);
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

            // 道（肉球）→ ノード → パグの順に重ねる
            Sprite paw = ResourceManager.Instance.Load<Sprite>(PawPath);
            for (int i = 1; i < nodePositions.Count; i++)
            {
                bool isReached = first + i <= _current;
                createTrail(paw, nodePositions[i - 1], nodePositions[i], isReached ? PawReachedColor : PawLockedColor);
            }
            for (int i = first; i <= last; i++)
            {
                RectTransform node = createNode(i, _current, nodePositions[i - first]);
                if (i == _current)
                {
                    currentNode = node;
                }
            }
            createPug(nodePositions[_current - first]);
        }

        /// <summary>
        /// ノード間に肉球を並べる
        /// </summary>
        /// <param name="_sprite"></param>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        /// <param name="_color"></param>
        private void createTrail(Sprite _sprite, Vector2 _from, Vector2 _to, Color _color)
        {
            Vector2 diff = _to - _from;
            float length = diff.magnitude;
            int count = Mathf.FloorToInt((length - NodeSize) / PawSpacing);
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg - 90f;
            for (int i = 1; i <= count; i++)
            {
                float t = (NodeSize * 0.5f + i * PawSpacing) / length;
                Vector2 side = new Vector2(-diff.y, diff.x).normalized * (i % 2 == 0 ? 5f : -5f);
                Image image = createImage("paw", pageRoot, _sprite, new Vector2(PawSize, PawSize));
                image.color = _color;
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

            Image node = createImage($"stage{number}", pageRoot, ResourceManager.Instance.Load<Sprite>(NodeSpritePath), new Vector2(NodeSize, NodeSize));
            node.color = isCurrent ? CurrentColor : (isCleared ? ClearedColor : LockedColor);
            node.rectTransform.anchoredPosition = _position;

            // 今のステージだけ押して始められる
            if (isCurrent)
            {
                node.raycastTarget = true;
                var button = node.gameObject.AddComponent<MumuButton>();
                button.targetGraphic = node;
                button.SetOnClick(onClickPlayButton);
            }

            createText(node.transform, number.ToString(), NodeFontSize);

            if (isCleared)
            {
                int stars = StageRecord.GetStars(number);
                for (int s = 0; s < 3; s++)
                {
                    Image star = createImage("star", node.transform, ResourceManager.Instance.Load<Sprite>(s < stars ? StarOnPath : StarOffPath), new Vector2(SmallStarSize, SmallStarSize));
                    star.rectTransform.anchoredPosition = new Vector2((s - 1) * SmallStarSize, -NodeSize * 0.5f - 6f);
                }
                if (StageRecord.IsPerfect(number))
                {
                    Image crown = createImage("crown", node.transform, ResourceManager.Instance.Load<Sprite>(CrownPath), new Vector2(SmallStarSize * 1.4f, SmallStarSize * 1.4f));
                    crown.rectTransform.anchoredPosition = new Vector2(NodeSize * 0.4f, NodeSize * 0.4f);
                }
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

            Image image = createImage("pug", pageRoot, pugSprites.BoySit[0], new Vector2(PugSize, PugSize));
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
        private async UniTaskVoid walkAsync(int _from, int _to)
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
        /// 画像を作る（中央基準）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        private static Image createImage(string _name, Transform _parent, Sprite _sprite, Vector2 _size)
        {
            var go = new GameObject(_name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = _size;
            Image image = go.GetComponent<Image>();
            image.sprite = _sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
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
