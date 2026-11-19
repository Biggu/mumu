using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// テーマの雰囲気演出（設定は ThemeDefine）
    /// TODO: 仮実装。Overlay Canvas では ParticleSystem が UI の裏に隠れるため Image で代用している
    /// </summary>
    public class AmbientParticles : MonoBehaviour
    {
        /// <summary>
        /// 1粒の状態
        /// </summary>
        private class Piece
        {
            public RectTransform Rect;
            public float Speed;
            public float SwayFrequency;
            public float Phase;
            public float Spin;
            public float BaseX;
            public float Y;
        }

        private RectTransform area = null;
        private ThemeDefine.AmbientSettings settings = null;
        private Piece[] pieces = null;

        /// <summary>
        /// 全画面に生成（指定の兄弟順に配置）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_siblingIndex"></param>
        /// <param name="_themeId"></param>
        public static AmbientParticles Create(Transform _parent, int _siblingIndex, int _themeId)
        {
            var go = new GameObject("ambient", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(_siblingIndex);

            var ambient = go.AddComponent<AmbientParticles>();
            ambient.Setup(_themeId);
            return ambient;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_themeId"></param>
        public void Setup(int _themeId)
        {
            area = (RectTransform)transform;
            settings = ThemeDefine.GetAmbient(_themeId);

            pieces = new Piece[settings.Count];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = createPiece();
                respawn(pieces[i], Random.Range(area.rect.yMin, area.rect.yMax));
            }
        }

        /// <summary>
        /// 移動
        /// </summary>
        private void Update()
        {
            if (pieces == null || pieces.Length == 0)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            Rect rect = area.rect;
            float margin = settings.Size.y * 2f;
            float direction = settings.Rise ? 1f : -1f;

            foreach (Piece piece in pieces)
            {
                piece.Y += piece.Speed * direction * deltaTime;
                if (!settings.Rise && piece.Y < rect.yMin - margin)
                {
                    respawn(piece, rect.yMax + margin);
                }
                else if (settings.Rise && piece.Y > rect.yMax + margin)
                {
                    respawn(piece, rect.yMin - margin);
                }

                float x = piece.BaseX + Mathf.Sin(time * piece.SwayFrequency + piece.Phase) * settings.Sway;
                piece.Rect.anchoredPosition = new Vector2(x, piece.Y);
                if (settings.Spin)
                {
                    piece.Rect.localRotation = Quaternion.Euler(0f, 0f, time * piece.Spin + piece.Phase * Mathf.Rad2Deg);
                }
            }
        }

        /// <summary>
        /// 1粒生成
        /// </summary>
        private Piece createPiece()
        {
            var go = new GameObject("piece", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(area, false);
            rect.sizeDelta = settings.Size * Random.Range(0.7f, 1.2f);

            var image = go.GetComponent<Image>();
            image.color = settings.Colors[Random.Range(0, settings.Colors.Length)];
            image.raycastTarget = false;

            return new Piece { Rect = rect };
        }

        /// <summary>
        /// 画面外から出し直す
        /// </summary>
        /// <param name="_piece"></param>
        /// <param name="_y"></param>
        private void respawn(Piece _piece, float _y)
        {
            Rect rect = area.rect;
            _piece.Y = _y;
            _piece.BaseX = Random.Range(rect.xMin, rect.xMax);
            _piece.Speed = Random.Range(settings.MinSpeed, settings.MaxSpeed);
            _piece.SwayFrequency = Random.Range(0.8f, 1.6f);
            _piece.Phase = Random.Range(0f, Mathf.PI * 2f);
            _piece.Spin = Random.Range(-120f, 120f);
        }
    }
}
