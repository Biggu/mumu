using UnityEditor;

namespace mumu
{
    /// <summary>
    /// MumuButton のインスペクタ（ButtonEditor だと独自フィールドが表示されないため既定の表示にする）
    /// </summary>
    [CustomEditor(typeof(MumuButton))]
    public class MumuButtonEditor : Editor
    {
    }
}
