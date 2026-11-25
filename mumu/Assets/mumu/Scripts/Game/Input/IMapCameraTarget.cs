using UnityEngine;

namespace mumu
{
    /// <summary>
    /// カメラ（MapViewController）が動かす地図。範囲と、中心に合わせる位置を返す
    /// </summary>
    public interface IMapCameraTarget
    {
        /// <summary>地図全体の範囲（mapRoot の親の座標系）</summary>
        Rect GetContentBounds();

        /// <summary>最初に中心へ合わせる位置（mapRoot の親の座標系）</summary>
        Vector2 GetFocusPosition();
    }
}
