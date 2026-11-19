using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 効果音（Resources/Sound/SE/se_{6桁}、900001 以降はシステム音）
    /// </summary>
    public enum eSe
    {
        /// <summary>se_000001: 経路を1マス進めた</summary>
        Step = 1,
        /// <summary>se_000002: 経路を巻き戻した</summary>
        Undo = 2,
        /// <summary>se_000003: チェックポイント通過・Goal 解放</summary>
        Checkpoint = 3,
        /// <summary>se_000004: 手数不足で Goal に入れなかった</summary>
        Refused = 4,
        /// <summary>se_000005: クリア</summary>
        Clear = 5,
        /// <summary>se_900001: ボタンを押した</summary>
        ButtonClick = 900001,
    }

    /// <summary>
    /// 効果音・BGM の再生
    /// </summary>
    public class SoundManager : MonoSingleton<SoundManager>
    {
        /// <summary>同時に鳴らせる数（ピッチ違いを重ねるため複数持つ）</summary>
        private const int SourceCount = 8;
        /// <summary>タイトルの BGM（bgm_999）</summary>
        public const int TitleBgmId = 999;
        /// <summary>テーマの BGM がない時の BGM（bgm_000）</summary>
        public const int DefaultBgmId = 0;

        private readonly Dictionary<eSe, AudioClip> clips = new Dictionary<eSe, AudioClip>();
        /// <summary>BGM（見つからなかった ID は null を覚える）</summary>
        private readonly Dictionary<int, AudioClip> bgmClips = new Dictionary<int, AudioClip>();
        private AudioSource[] sources = null;
        private AudioSource bgmSource = null;
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

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            ApplySettings();
        }

        /// <summary>
        /// 設定（効果音・BGM の ON/OFF）を反映
        /// </summary>
        public void ApplySettings()
        {
            if (bgmSource != null)
            {
                bgmSource.mute = !GameSettings.IsBgmEnabled;
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
        /// BGM をループ再生（なければ bgm_000、同じ曲が流れていればそのまま）
        /// </summary>
        /// <param name="_id"></param>
        public void PlayBgm(int _id)
        {
            AudioClip clip = getBgmClip(_id);
            if (clip == null)
            {
                clip = getBgmClip(DefaultBgmId);
            }
            if (clip == null || bgmSource == null || (bgmSource.clip == clip && bgmSource.isPlaying))
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        /// <summary>
        /// BGM を取得（初回のみ読み込み、ない ID はエラーを出さず null）
        /// </summary>
        /// <param name="_id"></param>
        private AudioClip getBgmClip(int _id)
        {
            if (!bgmClips.TryGetValue(_id, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>(GetBgmPath(_id));
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
