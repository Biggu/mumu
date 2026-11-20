using System;

namespace mumu
{
    /// <summary>
    /// 設定ポップアップの内容（やりなおし・タイトルへは閉じた後に実行。null ならボタンを出さない）
    /// </summary>
    public class SettingsParam : BaseParam
    {
        /// <summary>やりなおし押下時の処理</summary>
        public readonly Action OnRetry;
        /// <summary>タイトルへ押下時の処理</summary>
        public readonly Action OnTitle;

        /// <summary>
        /// 内容を作る
        /// </summary>
        /// <param name="_onRetry"></param>
        /// <param name="_onTitle"></param>
        public SettingsParam(Action _onRetry, Action _onTitle)
        {
            OnRetry = _onRetry;
            OnTitle = _onTitle;
        }
    }
}
