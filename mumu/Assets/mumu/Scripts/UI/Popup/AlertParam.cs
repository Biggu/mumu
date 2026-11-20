using UnityEngine;

namespace mumu
{
    /// <summary>
    /// アラート（OK、または はい・いいえ）の内容
    /// </summary>
    public class AlertParam : BaseParam
    {
        /// <summary>見出し</summary>
        public readonly string Title;
        /// <summary>本文</summary>
        public readonly string Body;
        /// <summary>いいえを出すか</summary>
        public readonly bool HasNo;
        /// <summary>アイコンのコマ送り（null で非表示）</summary>
        public readonly Sprite[] IconFrames;
        /// <summary>アイコンのオーラの段階</summary>
        public readonly int AuraTier;

        /// <summary>
        /// 内容を作る（文字は呼ぶ側で翻訳したものを渡す）
        /// </summary>
        /// <param name="_title"></param>
        /// <param name="_body"></param>
        /// <param name="_hasNo"></param>
        /// <param name="_iconFrames"></param>
        /// <param name="_auraTier"></param>
        public AlertParam(string _title, string _body, bool _hasNo, Sprite[] _iconFrames = null, int _auraTier = 0)
        {
            Title = _title;
            Body = _body;
            HasNo = _hasNo;
            IconFrames = _iconFrames;
            AuraTier = _auraTier;
        }
    }
}
