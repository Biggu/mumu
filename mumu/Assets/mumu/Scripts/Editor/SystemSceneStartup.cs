using UnityEditor;
using UnityEditor.SceneManagement;

namespace mumu
{
    /// <summary>
    /// エディタで再生する時は必ず system シーンから始める（マネージャーが無い状態で始まらないように）
    /// </summary>
    [InitializeOnLoad]
    public static class SystemSceneStartup
    {
        /// <summary>システムシーンのパス</summary>
        private const string ScenePath = "Assets/mumu/Scenes/system.unity";

        /// <summary>
        /// 再生開始シーンを system にする（ビルドには影響しない）
        /// </summary>
        static SystemSceneStartup()
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene != null && EditorSceneManager.playModeStartScene != scene)
            {
                EditorSceneManager.playModeStartScene = scene;
            }
        }
    }
}
