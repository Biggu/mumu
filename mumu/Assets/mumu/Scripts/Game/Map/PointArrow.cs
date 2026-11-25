using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 経路の先端を指す下向き矢印（上下にふわふわ動く）
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PointArrow : MonoBehaviour
    {
        public const string SpritePath = "Sprites/Common/point_arrow";

        private const float FollowSpeed = 18f;
        private const float BobHeight = 5f;
        private const float BobSpeed = 6f;

        private RectTransform rect = null;
        private Vector2 target = Vector2.zero;
        private Vector2 position = Vector2.zero;
        private bool hasTarget = false;

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_size"></param>
        public static PointArrow Create(Transform _parent, float _size)
        {
            Image image = UiFactory.CreateImage("PointArrow", _parent, ResourceManager.Instance.Load<Sprite>(SpritePath), _size);
            var arrow = image.gameObject.AddComponent<PointArrow>();
            arrow.rect = image.rectTransform;
            arrow.rect.pivot = new Vector2(0.5f, 0f);
            return arrow;
        }

        /// <summary>
        /// 指す位置（矢印の先端）
        /// </summary>
        /// <param name="_position"></param>
        public void SetTarget(Vector2 _position)
        {
            target = _position;
            if (!hasTarget)
            {
                hasTarget = true;
                position = _position;
            }
        }

        /// <summary>
        /// 表示切り替え
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetVisible(bool _isVisible)
        {
            gameObject.SetActive(_isVisible);
        }

        /// <summary>
        /// 追従と上下の揺れ
        /// </summary>
        private void Update()
        {
            position = Vector2.Lerp(position, target, 1f - Mathf.Exp(-FollowSpeed * Time.unscaledDeltaTime));
            float bob = (Mathf.Sin(Time.unscaledTime * BobSpeed) * 0.5f + 0.5f) * BobHeight;
            rect.anchoredPosition = position + new Vector2(0f, bob);
        }
    }
}
