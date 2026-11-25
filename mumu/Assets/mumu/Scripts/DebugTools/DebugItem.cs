#if UNITY_EDITOR
using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// デバッグメニューの1項目（名前と処理）
    /// </summary>
    public class DebugItem
    {
        /// <summary>ボタンの名前</summary>
        public string Label { get; }
        private readonly Action action;

        /// <summary>
        /// 名前と処理を持つ
        /// </summary>
        /// <param name="_label"></param>
        /// <param name="_action"></param>
        public DebugItem(string _label, Action _action)
        {
            Label = _label;
            action = _action;
        }

        /// <summary>
        /// ボタンを押した時の処理
        /// </summary>
        public void Press()
        {
            action?.Invoke();
            Debug.Log($"[DebugMenu] {Label}");
        }
    }
}
#endif
