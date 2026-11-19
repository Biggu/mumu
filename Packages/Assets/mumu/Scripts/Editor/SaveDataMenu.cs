using System.IO;
using UnityEditor;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 保存データのデバッグ用メニュー（mumu > Save Data）
    /// 保存ファイルは暗号化されているので、中身を見る・書き換える時は JSON に書き出して読み込む
    /// </summary>
    public static class SaveDataMenu
    {
        /// <summary>書き出す JSON（保存ファイルと同じフォルダ）</summary>
        private const string DebugJsonName = "save_debug.json";

        private static string debugJsonPath => Path.Combine(Application.persistentDataPath, DebugJsonName);

        /// <summary>
        /// 全部消して新しく始める（パグ1匹だけの状態）
        /// </summary>
        [MenuItem("mumu/Save Data/Reset")]
        public static void ResetSaveData()
        {
            if (!EditorUtility.DisplayDialog("Reset Save Data", "保存データを新規ユーザーの状態に戻します。よろしいですか？", "Reset", "Cancel"))
            {
                return;
            }
            SaveDataManager.ResetAll();
            Debug.Log($"[SaveDataMenu] Reset: {SaveDataManager.FilePath}");
        }

        /// <summary>
        /// 中身を読める JSON に書き出して開く
        /// </summary>
        [MenuItem("mumu/Save Data/Export JSON")]
        public static void ExportJson()
        {
            File.WriteAllText(debugJsonPath, SaveDataManager.ExportJson());
            Debug.Log($"[SaveDataMenu] Exported: {debugJsonPath}");
            EditorUtility.RevealInFinder(debugJsonPath);
        }

        /// <summary>
        /// 書き換えた JSON を読み込んで、暗号化して保存する
        /// </summary>
        [MenuItem("mumu/Save Data/Import JSON")]
        public static void ImportJson()
        {
            if (!File.Exists(debugJsonPath))
            {
                EditorUtility.DisplayDialog("Import JSON", $"{DebugJsonName} がありません。先に Export JSON してください。", "OK");
                return;
            }
            bool isImported = SaveDataManager.ImportJson(File.ReadAllText(debugJsonPath));
            EditorUtility.DisplayDialog("Import JSON", isImported ? "読み込んで保存しました。" : "JSON を読めませんでした。", "OK");
        }

        /// <summary>
        /// 保存先のフォルダを開く
        /// </summary>
        [MenuItem("mumu/Save Data/Open Folder")]
        public static void OpenFolder()
        {
            string path = SaveDataManager.FilePath;
            EditorUtility.RevealInFinder(File.Exists(path) ? path : Application.persistentDataPath);
        }
    }
}
