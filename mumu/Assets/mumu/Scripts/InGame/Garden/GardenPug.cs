using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace mumu
{
    /// <summary>
    /// おにわを歩きまわるパグ（座る・歩く・お昼寝・ついて歩く。タップで跳ねて、操作の表示を出す）
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GardenPug : MonoBehaviour, IPointerClickHandler
    {
        private const float SitFps = 3f;
        private const float WalkFps = 8f;
        private const float HappyFps = 8f;
        private const float WalkSpeed = 45f;
        private const float MinSitTime = 1.5f;
        private const float MaxSitTime = 5f;
        /// <summary>ついて歩く時の最大マス数</summary>
        private const int MaxFollowSteps = 6;
        private const float HappyDuration = 1.2f;
        private const float HopHeight = 16f;
        /// <summary>座り終わった時にお昼寝する確率</summary>
        private const float SleepRate = 0.15f;
        private const float MinSleepTime = 4f;
        private const float MaxSleepTime = 8f;
        /// <summary>お昼寝の時に見せるコマ（座りの目を閉じたコマ）</summary>
        private const int SleepFrameIndex = 2;
        /// <summary>Zzz の出る間隔</summary>
        private const float ZzzInterval = 0.9f;
        /// <summary>連れていく子の印（頭の上の★）</summary>
        private const string EquippedMarkPath = "Sprites/UI/ui_star_on";
        private const float EquippedMarkSize = 22f;
        private const float EquippedMarkY = 46f;

        private readonly GardenPugZzz zzz = new GardenPugZzz();

        private enum eState
        {
            Sit,
            Walk,
            Happy,
            Sleep,
        }

        private CharacterSprites sprites = null;
        private CharacterData character = null;
        private GardenMap map = null;
        /// <summary>タップされた時の処理（操作の表示を出す）</summary>
        private Action<GardenPug> onTapped = null;
        private GameObject equippedMark = null;
        private readonly GardenPugRoute route = new GardenPugRoute();
        private SpriteFrameAnimation frameAnimation = null;
        private Image image = null;
        private RectTransform rect = null;

        private Vector2Int cell = Vector2Int.zero;
        private Vector2 position = Vector2.zero;
        private eState state = eState.Sit;
        private float stateTime = 0f;
        private float sitTime = 0f;
        private float sleepTime = 0f;
        private float nextZzzTime = 0f;

        /// <summary>描画順に使う画面上の高さ（低いほど手前）</summary>
        public float Depth => position.y;
        /// <summary>今いるマス</summary>
        public Vector2Int Cell => cell;
        /// <summary>描画順の基準のマス（歩いている時は今のマスと次のマスの手前の方）</summary>
        public Vector2Int AnchorCell
        {
            get
            {
                if (state != eState.Walk || !route.HasNext)
                {
                    return cell;
                }
                Block current = map.GetBlock(cell);
                Block next = map.GetBlock(route.Next);
                return IsoDepth.Front(current, next) == next ? route.Next : cell;
            }
        }
        /// <summary>座っていて、ほかのことを始められるか</summary>
        public bool IsIdle => state == eState.Sit;
        /// <summary>キャラクター</summary>
        public CharacterData Character => character;

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_character"></param>
        /// <param name="_sprites"></param>
        /// <param name="_map"></param>
        /// <param name="_cell"></param>
        /// <param name="_size"></param>
        public static GardenPug Create(Transform _parent, CharacterData _character, CharacterSprites _sprites, GardenMap _map, Vector2Int _cell, float _size)
        {
            var go = new GameObject($"pug_{_character.Id}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            var pug = go.AddComponent<GardenPug>();
            pug.setup(_character, _sprites, _map, _cell, _size);
            return pug;
        }

        /// <summary>
        /// タップで起きて喜ぶ（操作の表示は onTapped で出す）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerClick(PointerEventData _eventData)
        {
            route.Clear();
            setState(eState.Happy);
            CommonUtility.PlaySe(eSe.Checkpoint);
            onTapped?.Invoke(this);
        }

        /// <summary>
        /// タップされた時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnTapped(Action<GardenPug> _action)
        {
            onTapped = _action;
        }

        /// <summary>
        /// 連れていく子の印を出すか
        /// </summary>
        /// <param name="_isEquipped"></param>
        public void SetEquipped(bool _isEquipped)
        {
            equippedMark.SetActive(_isEquipped);
        }

        /// <summary>
        /// 喜んで跳ねる（クリアのお祝い）
        /// </summary>
        public void Cheer()
        {
            route.Clear();
            setState(eState.Happy);
        }

        /// <summary>
        /// ほかの子のところまでついて歩く（座っている時だけ、行けなければ何もしない）
        /// </summary>
        /// <param name="_leader"></param>
        public bool Follow(GardenPug _leader)
        {
            if (!IsIdle || _leader == null || _leader == this)
            {
                return false;
            }
            if (!route.PlanFollow(cell, _leader.Cell, map, MaxFollowSteps))
            {
                return false;
            }

            setState(eState.Walk);
            return true;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_character"></param>
        /// <param name="_sprites"></param>
        /// <param name="_map"></param>
        /// <param name="_cell"></param>
        /// <param name="_size"></param>
        private void setup(CharacterData _character, CharacterSprites _sprites, GardenMap _map, Vector2Int _cell, float _size)
        {
            sprites = _sprites;
            character = _character;
            map = _map;
            cell = _cell;
            position = map.GetTopFacePosition(cell);

            image = GetComponent<Image>();
            UiFactory.SetPugImage(image, true);
            frameAnimation = gameObject.AddComponent<SpriteFrameAnimation>();

            // 足元を上面の中心に合わせる（Goal のパグと同じ）
            rect = (RectTransform)transform;
            UiFactory.SetFootRect(rect, _size);

            equippedMark = UiFactory.CreateImage("equipped", transform, ResourceManager.Instance.Load<Sprite>(EquippedMarkPath), EquippedMarkSize).gameObject;
            var markRect = (RectTransform)equippedMark.transform;
            markRect.anchorMin = markRect.anchorMax = new Vector2(0.5f, 0f);
            markRect.anchoredPosition = new Vector2(0f, EquippedMarkY);
            equippedMark.SetActive(false);
            PugAura.Attach(rect, _character.Tier, _size);

            // 起動直後に全員が同時に動かないようずらす
            sitTime = Random.Range(0f, MaxSitTime);
            setState(eState.Sit);
            apply(0f);
        }

        /// <summary>
        /// 座る・歩く・喜ぶ・お昼寝
        /// </summary>
        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            stateTime += delta;
            float hop = 0f;

            switch (state)
            {
                case eState.Sit:
                    if (stateTime >= sitTime)
                    {
                        if (Random.value < SleepRate)
                        {
                            sleepTime = Random.Range(MinSleepTime, MaxSleepTime);
                            setState(eState.Sleep);
                        }
                        else
                        {
                            planWalk();
                        }
                    }
                    break;
                case eState.Walk:
                    walk(delta);
                    break;
                case eState.Happy:
                    hop = Mathf.Abs(Mathf.Sin(stateTime / HappyDuration * Mathf.PI * 2f)) * HopHeight;
                    if (stateTime >= HappyDuration)
                    {
                        sitAgain();
                    }
                    break;
                case eState.Sleep:
                    if (stateTime >= nextZzzTime)
                    {
                        nextZzzTime = stateTime + ZzzInterval;
                        zzz.Spawn(transform, rect.localScale.x, destroyCancellationToken);
                    }
                    if (stateTime >= sleepTime)
                    {
                        sitAgain();
                    }
                    break;
            }
            apply(hop);
        }

        /// <summary>
        /// しばらく座る
        /// </summary>
        private void sitAgain()
        {
            sitTime = Random.Range(MinSitTime, MaxSitTime);
            setState(eState.Sit);
        }

        /// <summary>
        /// となりの草へ数マス歩く（行けなければ座ったまま）
        /// </summary>
        private void planWalk()
        {
            if (!route.PlanWalk(cell, map))
            {
                sitAgain();
                return;
            }
            setState(eState.Walk);
        }

        /// <summary>
        /// 道に沿って歩く（着いたら座る）
        /// </summary>
        /// <param name="_delta"></param>
        private void walk(float _delta)
        {
            float step = WalkSpeed * _delta;
            while (step > 0f && route.HasNext)
            {
                Vector2 target = map.GetTopFacePosition(route.Next);
                Vector2 toTarget = target - position;
                float distance = toTarget.magnitude;
                if (Mathf.Abs(toTarget.x) > 0.01f)
                {
                    rect.localScale = new Vector3(toTarget.x < 0f ? -1f : 1f, 1f, 1f);
                }

                if (distance <= step)
                {
                    position = target;
                    cell = route.Dequeue();
                    step -= distance;
                }
                else
                {
                    position += toTarget / distance * step;
                    step = 0f;
                }
            }

            if (!route.HasNext)
            {
                sitAgain();
            }
        }

        /// <summary>
        /// 状態を変えてアニメーションを切り替え
        /// </summary>
        /// <param name="_state"></param>
        private void setState(eState _state)
        {
            state = _state;
            stateTime = 0f;
            nextZzzTime = 0f;
            switch (_state)
            {
                case eState.Walk:
                    play(sprites.Walk, WalkFps);
                    break;
                case eState.Happy:
                    rect.localScale = Vector3.one;
                    play(sprites.Happy, HappyFps);
                    break;
                case eState.Sleep:
                    CommonUtility.PlaySe(eSe.SnoreSoft);
                    // 目を閉じたコマで止める
                    frameAnimation.Play(new[] { sprites.Sit[Mathf.Min(SleepFrameIndex, sprites.Sit.Length - 1)] }, SitFps);
                    break;
                default:
                    rect.localScale = Vector3.one;
                    play(sprites.Sit, SitFps);
                    break;
            }
        }

        /// <summary>
        /// コマ送りを切り替え（同じなら続ける）
        /// </summary>
        /// <param name="_frames"></param>
        /// <param name="_fps"></param>
        private void play(Sprite[] _frames, float _fps)
        {
            frameAnimation.PlayIfChanged(_frames, _fps);
        }

        /// <summary>
        /// 位置を反映
        /// </summary>
        /// <param name="_hop"></param>
        private void apply(float _hop)
        {
            rect.anchoredPosition = position + new Vector2(0f, _hop);
        }
    }
}
