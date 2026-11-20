using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// tier5・6 のパグのまわりに出るキラキラ（tier5 は金、tier6 は虹色）。パグの子に置くので一緒に動く
    /// </summary>
    public class PugAura : MonoBehaviour
    {
        /// <summary>この tier 以上でキラキラを出す</summary>
        public const int MinTier = 5;
        private const string SpritePath = "Sprites/UI/Kit/kit_icon_star";
        private const float SpawnInterval = 0.18f;
        private const float Lifetime = 0.9f;
        private const float Rise = 24f;
        private const int MaxAlive = 8;
        private static readonly Color GoldColor = new Color(1f, 0.86f, 0.3f);

        private static Sprite sprite = null;

        private class Spark
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Start;
            public float Age;
            public float Size;
        }

        private readonly List<Spark> sparks = new List<Spark>();
        private int tier = 0;
        private float size = 44f;
        private float nextSpawn = 0f;

        /// <summary>
        /// キラキラをつける（tier が MinTier 未満なら何もしない。同じ相手には1つだけ）
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_tier"></param>
        /// <param name="_size"></param>
        public static PugAura Attach(RectTransform _target, int _tier, float _size)
        {
            if (_target == null || _tier < MinTier)
            {
                return null;
            }
            PugAura aura = _target.GetComponentInChildren<PugAura>();
            if (aura == null)
            {
                var go = new GameObject("aura", typeof(RectTransform));
                go.transform.SetParent(_target, false);
                var rect = (RectTransform)go.transform;
                // 相手の pivot（パグは足元）を基準にする
                rect.anchorMin = rect.anchorMax = _target.pivot;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.zero;
                aura = go.AddComponent<PugAura>();
            }
            aura.tier = _tier;
            aura.size = _size;
            return aura;
        }

        /// <summary>
        /// 出して、上がりながら消す
        /// </summary>
        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            nextSpawn -= delta;
            if (nextSpawn <= 0f && sparks.Count < MaxAlive)
            {
                nextSpawn = SpawnInterval;
                spawn();
            }

            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = sparks[i];
                spark.Age += delta;
                float t = spark.Age / Lifetime;
                if (t >= 1f)
                {
                    Destroy(spark.Rect.gameObject);
                    sparks.RemoveAt(i);
                    continue;
                }
                spark.Rect.anchoredPosition = spark.Start + new Vector2(0f, Rise * t);
                float scale = Mathf.Sin(t * Mathf.PI);
                spark.Rect.localScale = new Vector3(scale, scale, 1f);
                spark.Rect.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                Color color = tier >= CharacterMaster.SecretTier ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.5f + spark.Start.x * 0.01f, 1f), 0.45f, 1f) : GoldColor;
                color.a = 1f - t * t;
                spark.Image.color = color;
            }
        }

        /// <summary>
        /// パグのまわりに1つ出す
        /// </summary>
        private void spawn()
        {
            SpriteUtility.LoadCached(ref sprite, SpritePath);

            var go = new GameObject("spark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            float sparkSize = size * Random.Range(0.2f, 0.32f);
            image.rectTransform.sizeDelta = new Vector2(sparkSize, sparkSize);

            // パグの体のまわり（足元が基準なので少し上）
            Vector2 start = new Vector2(Random.Range(-0.5f, 0.5f) * size, Random.Range(0.1f, 0.85f) * size);
            sparks.Add(new Spark { Rect = image.rectTransform, Image = image, Start = start, Size = sparkSize });
        }
    }
}
