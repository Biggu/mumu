using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 紙吹雪演出
    /// </summary>
    public static class ConfettiEffect
    {
        private const float Gravity = 260f;
        private const float MinLife = 2.5f;
        private const float MaxLife = 3.5f;
        private static readonly Vector2 PieceSize = new Vector2(10f, 16f);

        private static readonly Color[] Colors =
        {
            new Color(1f, 0.35f, 0.35f),
            new Color(1f, 0.8f, 0.2f),
            new Color(0.4f, 0.85f, 0.4f),
            new Color(0.35f, 0.65f, 1f),
            new Color(0.85f, 0.5f, 1f),
        };

        /// <summary>
        /// 画面上から紙吹雪を降らせる
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_count"></param>
        /// <param name="_token"></param>
        public static void Play(RectTransform _parent, int _count, CancellationToken _token)
        {
            Rect area = _parent.rect;

            for (int i = 0; i < _count; i++)
            {
                var piece = new GameObject("confetti", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)piece.transform;
                rect.SetParent(_parent, false);
                rect.SetAsLastSibling();
                rect.sizeDelta = PieceSize;

                var image = piece.GetComponent<Image>();
                image.color = Colors[Random.Range(0, Colors.Length)];
                image.raycastTarget = false;

                var start = new Vector2(Random.Range(area.xMin, area.xMax), area.yMax + Random.Range(10f, 200f));
                var velocity = new Vector2(Random.Range(-80f, 80f), Random.Range(-150f, -40f));
                float spin = Random.Range(-360f, 360f);
                float life = Random.Range(MinLife, MaxLife);

                pieceAsync(rect, start, velocity, spin, life, _token).Forget();
            }
        }

        /// <summary>
        /// 1枚分の落下
        /// </summary>
        /// <param name="_piece"></param>
        /// <param name="_position"></param>
        /// <param name="_velocity"></param>
        /// <param name="_spin"></param>
        /// <param name="_life"></param>
        /// <param name="_token"></param>
        private static async UniTaskVoid pieceAsync(RectTransform _piece, Vector2 _position, Vector2 _velocity, float _spin, float _life, CancellationToken _token)
        {
            float time = 0f;
            while (time < _life && _piece != null)
            {
                float deltaTime = Time.unscaledDeltaTime;
                time += deltaTime;
                _velocity.y -= Gravity * deltaTime;
                _position += _velocity * deltaTime;

                _piece.anchoredPosition = _position;
                _piece.localRotation = Quaternion.Euler(0f, 0f, _spin * time);

                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
            }

            if (_piece != null)
            {
                Object.Destroy(_piece.gameObject);
            }
        }
    }
}
