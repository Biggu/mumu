using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 演出の親を管理
    /// </summary>
    public class EffectManager : MonoSingleton<EffectManager>
    {
        /// <summary>演出の親</summary>
        [SerializeField]
        private RectTransform effectRoot = null;

        /// <summary>演出の親</summary>
        public RectTransform EffectRoot => effectRoot;
    }
}
