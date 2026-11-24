using UnityEngine;

namespace mumu
{
    /// <summary>
    /// テーマの雰囲気演出（粒は ParticleSystem。プレハブは Resources/Prefabs/Ambient/ambient_{id:000}、親は EffectManager の effectRoot）
    /// </summary>
    public class AmbientParticles : MonoBehaviour
    {
        /// <summary>プレハブのパス（ID の前まで）</summary>
        private const string PrefabPath = "Prefabs/Ambient/ambient_";

        /// <summary>今出している雰囲気演出（同時に1つだけ）</summary>
        private static AmbientParticles current = null;

        /// <summary>
        /// 雰囲気演出を出す（前の演出は消す）。_parent と _siblingIndex は使わない（親は effectRoot）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_siblingIndex"></param>
        /// <param name="_themeId"></param>
        public static AmbientParticles Create(Transform _parent, int _siblingIndex, int _themeId)
        {
            if (current != null)
            {
                UnityEngine.Object.Destroy(current.gameObject);
                current = null;
            }

            RectTransform effectRoot = EffectManager.Instance.EffectRoot;
            if (effectRoot == null || ThemeDefine.GetAmbient(_themeId).Count == 0)
            {
                return null;
            }

            GameObject prefab = ResourceManager.Instance.Load<GameObject>(PrefabPath + _themeId.ToString("000"));
            if (prefab == null)
            {
                Debug.LogWarning($"[AmbientParticles] プレハブがありません: {PrefabPath}{_themeId:000}");
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, effectRoot, false);
            AmbientParticles ambient = go.GetComponent<AmbientParticles>();
            ambient.Setup(effectRoot);
            current = ambient;
            return ambient;
        }

        /// <summary>
        /// 粒を出す範囲を画面の大きさに合わせる（粒の動きはプレハブの設定）
        /// </summary>
        /// <param name="_area"></param>
        public void Setup(RectTransform _area)
        {
            transform.localPosition = _area.rect.center;
            ParticleSystem effect = GetComponent<ParticleSystem>();
            ParticleSystem.ShapeModule shape = effect.shape;
            shape.scale = new Vector3(_area.rect.width, _area.rect.height, 0f);
        }
    }
}
