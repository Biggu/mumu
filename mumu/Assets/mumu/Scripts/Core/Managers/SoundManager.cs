using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 効果音（Resources/Sound/SE/se_{6桁}）。番号どおりの名前は下の8つ、古い名前は同じ番号の別名
    /// </summary>
    public enum eSe
    {
        /// <summary>se_000001: クリック（ボタン）</summary>
        Click = 1,
        /// <summary>se_000002: 吠える1（おにわでパグを押した時）</summary>
        Bark1 = 2,
        /// <summary>se_000003: 吠える2（進めない時）</summary>
        Bark2 = 3,
        /// <summary>se_000004: ブロックが増える（1マス進めた）</summary>
        BlockGain = 4,
        /// <summary>se_000005: ブロックが増える2（チェックポイント通過・Goal 解放）</summary>
        BlockGain2 = 5,
        /// <summary>se_000006: ブロックが減る（巻き戻した）</summary>
        BlockMinus = 6,
        /// <summary>se_000007: 勝利のジングル（クリア）</summary>
        WinJingle = 7,
        /// <summary>se_000008: 鳥が飛ぶ（今は未使用）</summary>
        BirdFly = 8,
        /// <summary>se_000009: カギを拾った</summary>
        KeyGet = 9,
        /// <summary>se_000010: 扉が開いた</summary>
        GateOpen = 10,
        /// <summary>se_000011: ほね（宝箱）が増えた</summary>
        CoinGet = 11,
        /// <summary>se_000012: こわれる橋が壊れた</summary>
        CrackWood = 12,
        /// <summary>se_000013: ワープに乗った</summary>
        Warp = 13,
        /// <summary>se_000014: 星が1つずつ点いた（クリア）</summary>
        StarPing = 14,
        /// <summary>se_000015: 大きな星になった（もどらずクリア）</summary>
        Sparkle = 15,
        /// <summary>se_000016: もどらずボーナス（ほね +10）</summary>
        BonusCoin = 16,
        /// <summary>se_000017: PERFECT の王冠</summary>
        Fanfare = 17,
        /// <summary>se_000018: テーマのおはなし</summary>
        Chime = 18,
        /// <summary>se_000019: 通知（真ん中のお知らせ）</summary>
        NoticePing = 19,
        /// <summary>se_000020: 画面下のトースト</summary>
        ToastPop = 20,
        /// <summary>se_000021: ダイアログが開く</summary>
        PopupOpen = 21,
        /// <summary>se_000022: ダイアログが閉じる</summary>
        PopupClose = 22,
        /// <summary>se_000023: ヒントを使った</summary>
        HintChime = 23,
        /// <summary>se_000024: パグが寝る（とても小さい）</summary>
        SnoreSoft = 24,
        /// <summary>se_000025: うれしい吠え（パグが仲間になった）</summary>
        HappyBark = 25,
        /// <summary>se_000026: ステージマップのページをめくる</summary>
        PageFlip = 26,
        /// <summary>se_000027: 男の子が歩く（足音）</summary>
        Footstep = 27,
        /// <summary>se_000028: 注意（今日のおさんぽが終わった）</summary>
        AlertChime = 28,
        /// <summary>se_000029: 木の宝箱を拾った</summary>
        ChestWood = 29,
        /// <summary>se_000030: 銀の宝箱を拾った</summary>
        ChestSilver = 30,
        /// <summary>se_000031: 金の宝箱を拾った（豪華に）</summary>
        ChestGold = 31,
        /// <summary>se_000032: 宝箱を続けて拾った（連続音）</summary>
        ChestCombo = 32,
        /// <summary>se_000033: ボーナスステージが始まる</summary>
        BonusEnter = 33,
        /// <summary>se_000034: ボーナスステージをクリア（コインが降る）</summary>
        BonusClear = 34,
        /// <summary>se_000035: ヒントの2回目以降（少し高い音）</summary>
        HintReveal = 35,
        /// <summary>se_000036: 鍵のかかったステージを押した</summary>
        LockedTap = 36,
        /// <summary>se_000037: レアなパグが仲間になった</summary>
        RareJoin = 37,
        /// <summary>se_000038: 今日のおさんぽを始める</summary>
        StrollStart = 38,
        /// <summary>se_000039: 新しいテーマが始まる</summary>
        ThemeWhoosh = 39,
        /// <summary>se_000040: クリアしたステージをもう一度遊ぶ</summary>
        ReplayStart = 40,
        /// <summary>se_000041: 多く戻る時の確認が出る</summary>
        RewindConfirm = 41,

        // 既存の呼び出し名（同じ番号の別名）
        Step = BlockGain,
        Undo = BlockMinus,
        Checkpoint = BlockGain2,
        Refused = Bark2,
        Clear = WinJingle,
        ButtonClick = Click,
    }

    /// <summary>
    /// 効果音・BGM の再生
    /// </summary>
    public class SoundManager : MonoSingleton<SoundManager>
    {
        /// <summary>同時に鳴らせる数（ピッチ違いを重ねるため複数持つ）</summary>
        private const int SourceCount = 8;
        /// <summary>タイトルの BGM（bgm_000）</summary>
        public const int TitleBgmId = 0;
        /// <summary>テーマの BGM がない時の BGM（bgm_000）</summary>
        public const int DefaultBgmId = 0;

        private readonly Dictionary<eSe, AudioClip> clips = new Dictionary<eSe, AudioClip>();
        /// <summary>BGM（見つからなかった ID は null を覚える）</summary>
        private readonly Dictionary<int, AudioClip> bgmClips = new Dictionary<int, AudioClip>();
        private AudioSource[] sources = null;
        /// <summary>BGM の切り替えにかける時間（秒）</summary>
        private const float BgmFadeSeconds = 1f;
        /// <summary>今流れている BGM</summary>
        private AudioSource bgmSource = null;
        /// <summary>もう一方の BGM（切り替えの時、ここから新しい曲を流す）</summary>
        private AudioSource bgmOther = null;
        /// <summary>フェードの経過時間（秒）。負なら切り替え中ではない</summary>
        private float bgmFadeTime = -1f;
        /// <summary>フェードアウトを始めた時の音量</summary>
        private float bgmOutStartVolume = 1f;
        private int nextSource = 0;

        /// <summary>
        /// 効果音のパス
        /// </summary>
        /// <param name="_se"></param>
        public static string GetSePath(eSe _se) => $"Sound/SE/se_{(int)_se:000000}";

        /// <summary>
        /// BGM のパス（Resources/Sound/BGM/bgm_XXX）
        /// </summary>
        /// <param name="_id"></param>
        public static string GetBgmPath(int _id) => $"Sound/BGM/bgm_{_id:000}";

        /// <summary>
        /// AudioSource を生成
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            sources = new AudioSource[SourceCount];
            for (int i = 0; i < SourceCount; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }

            bgmSource = createBgmSource();
            bgmOther = createBgmSource();
            ApplySettings();
        }

        /// <summary>
        /// BGM 用の AudioSource を生成（ループ・自動再生なし）
        /// </summary>
        private AudioSource createBgmSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            return source;
        }

        /// <summary>
        /// 設定（効果音・BGM の ON/OFF）を反映
        /// </summary>
        public void ApplySettings()
        {
            bool mute = !GameSettings.IsBgmEnabled;
            if (bgmSource != null)
            {
                bgmSource.mute = mute;
            }
            if (bgmOther != null)
            {
                bgmOther.mute = mute;
            }
        }

        /// <summary>
        /// タイトルの BGM を再生
        /// </summary>
        public void PlayTitleBgm()
        {
            PlayBgm(TitleBgmId);
        }

        /// <summary>
        /// テーマの BGM を再生（bgm_{テーマ ID}、なければ bgm_000）
        /// </summary>
        /// <param name="_themeId"></param>
        public void PlayThemeBgm(int _themeId)
        {
            PlayBgm(_themeId);
        }

        /// <summary>
        /// BGM をループ再生（なければ bgm_000、同じ曲が流れていればそのまま。切り替えはフェード）
        /// </summary>
        /// <param name="_id"></param>
        public void PlayBgm(int _id)
        {
            AudioClip clip = getBgmClip(_id);
            if (clip == null)
            {
                clip = getBgmClip(DefaultBgmId);
            }
            if (clip == null || bgmSource == null || bgmOther == null || (bgmSource.clip == clip && bgmSource.isPlaying))
            {
                return;
            }

            // 今の BGM はフェードアウト、もう一方の AudioSource で新しい BGM をフェードイン
            bgmOther.Stop();
            AudioSource outgoing = bgmSource;
            bgmOutStartVolume = outgoing.volume;
            bgmSource = bgmOther;
            bgmOther = outgoing;
            bgmSource.clip = clip;
            bgmSource.volume = 0f;
            bgmSource.Play();
            bgmFadeTime = 0f;
        }

        /// <summary>
        /// BGM の切り替えのフェード（新しい曲を大きく、前の曲を小さく）
        /// </summary>
        private void Update()
        {
            if (bgmFadeTime < 0f)
            {
                return;
            }

            bgmFadeTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(bgmFadeTime / BgmFadeSeconds);
            bgmSource.volume = t;
            bgmOther.volume = bgmOutStartVolume * (1f - t);
            if (t >= 1f)
            {
                bgmOther.Stop();
                bgmFadeTime = -1f;
            }
        }

        /// <summary>
        /// BGM を取得（初回のみ読み込み、ない ID はエラーを出さず null）
        /// </summary>
        /// <param name="_id"></param>
        private AudioClip getBgmClip(int _id)
        {
            if (!bgmClips.TryGetValue(_id, out AudioClip clip))
            {
                clip = ResourceManager.LoadResource<AudioClip>(GetBgmPath(_id), false);
                bgmClips[_id] = clip;
            }
            return clip;
        }

        /// <summary>
        /// 効果音を再生
        /// </summary>
        /// <param name="_se"></param>
        /// <param name="_pitch"></param>
        public void PlaySe(eSe _se, float _pitch = 1f)
        {
            if (!GameSettings.IsSeEnabled)
            {
                return;
            }

            AudioClip clip = getClip(_se);
            if (clip == null || sources == null)
            {
                return;
            }

            AudioSource source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;

            source.pitch = _pitch;
            source.clip = clip;
            source.Play();
        }

        /// <summary>
        /// 効果音を取得（初回のみ読み込み）
        /// </summary>
        /// <param name="_se"></param>
        private AudioClip getClip(eSe _se)
        {
            if (!clips.TryGetValue(_se, out AudioClip clip))
            {
                clip = ResourceManager.Instance.Load<AudioClip>(GetSePath(_se));
                clips[_se] = clip;
            }
            return clip;
        }
    }
}
