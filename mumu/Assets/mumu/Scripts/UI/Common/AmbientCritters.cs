using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ときどきテーマの生き物（ちょうちょ・かもめ…）が画面を横切る演出
    /// </summary>
    public class AmbientCritters : MonoBehaviour
    {
        private const string SheetPath = "Sprites/Common/critters";
        private const float MinInterval = 6f;
        private const float MaxInterval = 14f;
        private const float Size = 56f;
        private const float FrameTime = 0.15f;

        private eCritter critter = eCritter.None;
        private Sprite[] frames = null;
        private RectTransform area = null;

        /// <summary>
        /// 生成（その画面の上、_siblingIndex の位置に全画面で置く。生き物がいないテーマなら null）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_siblingIndex"></param>
        /// <param name="_themeId"></param>
        public static AmbientCritters Create(Transform _parent, int _siblingIndex, int _themeId)
        {
            eCritter critter = ThemeDefine.GetCritter(_themeId);
            if (critter == eCritter.None)
            {
                return null;
            }

            Sprite[] frames = SpriteUtility.FilterByPrefix(ResourceManager.Instance.LoadAll<Sprite>(SheetPath), $"critter_{critter.ToString().ToLowerInvariant()}_");
            if (frames.Length == 0)
            {
                return null;
            }

            var go = new GameObject("AmbientCritters", typeof(RectTransform));
            go.transform.SetParent(_parent, false);
            go.transform.SetSiblingIndex(Mathf.Clamp(_siblingIndex, 0, _parent.childCount - 1));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var critters = go.AddComponent<AmbientCritters>();
            critters.critter = critter;
            critters.frames = frames;
            critters.area = rect;
            critters.loopAsync(critters.destroyCancellationToken).Forget();
            return critters;
        }

        /// <summary>
        /// 少し待っては1匹横切らせる
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid loopAsync(CancellationToken _token)
        {
            while (true)
            {
                if (await UniTask.Delay(System.TimeSpan.FromSeconds(Random.Range(MinInterval, MaxInterval)), cancellationToken: _token).SuppressCancellationThrow())
                {
                    return;
                }
                await crossAsync(_token);
            }
        }

        /// <summary>
        /// 1匹が左右どちらかから反対側へ横切る（種類ごとに動き方を変える）
        /// </summary>
        /// <param name="_token"></param>
        private async UniTask crossAsync(CancellationToken _token)
        {
            Rect rect = area.rect;
            bool isLeftToRight = Random.value < 0.5f;
            float margin = Size;
            float startX = isLeftToRight ? rect.xMin - margin : rect.xMax + margin;
            float endX = isLeftToRight ? rect.xMax + margin : rect.xMin - margin;

            float duration;
            float baseY;
            switch (critter)
            {
                case eCritter.Butterfly:
                    duration = 8f;
                    baseY = Random.Range(rect.yMin * 0.3f, rect.yMax * 0.5f);
                    break;
                case eCritter.Gull:
                    duration = 4.5f;
                    baseY = Random.Range(rect.yMax * 0.3f, rect.yMax * 0.6f);
                    break;
                case eCritter.Bunny:
                    duration = 6f;
                    baseY = rect.yMin * 0.55f;
                    break;
                default:
                    duration = 6f;
                    baseY = Random.Range(rect.yMin * 0.3f, rect.yMax * 0.5f);
                    break;
            }

            var go = new GameObject(critter.ToString(), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            RectTransform critterRect = image.rectTransform;
            critterRect.sizeDelta = new Vector2(Size, Size);
            // 絵は右向き（ちょうちょ・キャンディは向きなし）
            critterRect.localScale = new Vector3(isLeftToRight ? 1f : -1f, 1f, 1f);

            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                float t = time / duration;
                float x = Mathf.Lerp(startX, endX, t);
                float y = baseY;
                int frame = Mathf.FloorToInt(time / FrameTime) % frames.Length;
                float angle = 0f;
                switch (critter)
                {
                    case eCritter.Butterfly:
                        y += Mathf.Sin(time * 2.2f) * 40f + Mathf.Sin(time * 5f) * 8f;
                        break;
                    case eCritter.Gull:
                        y += Mathf.Sin(time * 1.5f) * 12f;
                        frame = Mathf.FloorToInt(time / (FrameTime * 2f)) % frames.Length;
                        break;
                    case eCritter.Bunny:
                        // ぴょんぴょん跳ねる。空中は2コマ目
                        float hop = Mathf.Abs(Mathf.Sin(time * Mathf.PI * 1.6f));
                        y += hop * 40f;
                        frame = hop > 0.3f ? 1 : 0;
                        break;
                    case eCritter.Candy:
                        y += Mathf.Sin(time * 1.2f) * 20f;
                        angle = time * 90f;
                        break;
                    case eCritter.Plane:
                        y += Mathf.Sin(time * 1.4f) * 25f - t * 60f;
                        angle = Mathf.Cos(time * 1.4f) * 8f;
                        break;
                }

                critterRect.anchoredPosition = new Vector2(x, y);
                critterRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                image.sprite = frames[frame];
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
            }

            if (go != null)
            {
                Destroy(go);
            }
        }
    }
}
