using System;

namespace mumu
{
    /// <summary>
    /// 2つのボタンの確認の内容（押すと閉じて、押したボタンの処理を実行。無効なボタンは押せない）
    /// </summary>
    public class ConfirmParam : BaseParam
    {
        /// <summary>見出し</summary>
        public readonly string Title;
        /// <summary>左のボタンの文字</summary>
        public readonly string FirstLabel;
        /// <summary>左のボタンを押せるか</summary>
        public readonly bool FirstEnabled;
        /// <summary>左のボタンを押した後の処理</summary>
        public readonly Action OnFirst;
        /// <summary>右のボタンの文字</summary>
        public readonly string SecondLabel;
        /// <summary>右のボタンを押せるか</summary>
        public readonly bool SecondEnabled;
        /// <summary>右のボタンを押した後の処理</summary>
        public readonly Action OnSecond;

        /// <summary>
        /// 内容を作る（文字は呼ぶ側で翻訳したものを渡す）
        /// </summary>
        /// <param name="_title"></param>
        /// <param name="_firstLabel"></param>
        /// <param name="_firstEnabled"></param>
        /// <param name="_onFirst"></param>
        /// <param name="_secondLabel"></param>
        /// <param name="_secondEnabled"></param>
        /// <param name="_onSecond"></param>
        public ConfirmParam(string _title, string _firstLabel, bool _firstEnabled, Action _onFirst, string _secondLabel, bool _secondEnabled, Action _onSecond)
        {
            Title = _title;
            FirstLabel = _firstLabel;
            FirstEnabled = _firstEnabled;
            OnFirst = _onFirst;
            SecondLabel = _secondLabel;
            SecondEnabled = _secondEnabled;
            OnSecond = _onSecond;
        }
    }
}
