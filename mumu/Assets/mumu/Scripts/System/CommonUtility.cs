using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 共通処理（コンポーネント取得・効果音・打ち上げ）
    /// </summary>
    public static class CommonUtility
    {
        /// <summary>
        /// コンポーネントを取得し、無ければ追加する
        /// </summary>
        /// <param name="_gameObject"></param>
        public static T GetOrAddComponent<T>(GameObject _gameObject) where T : Component
        {
            T component = _gameObject.GetComponent<T>();
            if (component == null)
            {
                component = _gameObject.AddComponent<T>();
            }
            return component;
        }


        /// <summary>
        /// 大きさを一度跳ねさせる（基準の大きさは 1）
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_amount"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static void Punch(Transform _target, float _amount, float _duration, CancellationToken _token)
        {
            Punch(_target, Vector3.one, _amount, _duration, _token);
        }


        /// <summary>
        /// 基準の大きさから一度跳ねさせる
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_baseScale"></param>
        /// <param name="_amount"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static void Punch(Transform _target, Vector3 _baseScale, float _amount, float _duration, CancellationToken _token)
        {
            UiTween.PunchScale(_target, _baseScale, _amount, _duration, _token).Forget();
        }



        /// <summary>
        /// 効果音を鳴らす（SoundManager がなければ何もしない）
        /// </summary>
        /// <param name="_se"></param>
        /// <param name="_pitch"></param>
        public static void PlaySe(eSe _se, float _pitch = 1f)
        {
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(_se, _pitch);
            }
        }

    }
}
