using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace mumu
{
    /// <summary>
    /// デバッグ用のマップ（通常のマップの CSV とは別の名前で保存する。デバッグメニューから遊ぶ）
    /// </summary>
    public static class DebugMapFile
    {
        /// <summary>保存先（Assets から）</summary>
        private const string AssetFolder = "Assets/mumu/Resources/MapData/debug";
        /// <summary>保存するファイルの名前（Resources での読み込み名）</summary>
        public const string MapFileName = "debug_map";
        public const string ListFileName = "debug_map_list";

        /// <summary>
        /// 今のマップをデバッグ用の CSV として保存する（通常のマップ一覧は変えない）
        /// </summary>
        /// <param name="_mapData"></param>
        public static void Save(MapData _mapData)
        {
            Directory.CreateDirectory(AssetFolder);
            string mapPath = $"{AssetFolder}/{MapFileName}.csv";
            string listPath = $"{AssetFolder}/{ListFileName}.csv";

            File.WriteAllText(mapPath, MapCsv.WriteMap(_mapData), MapCsv.FileEncoding);
            File.WriteAllText(listPath, MapCsv.WriteList(new List<MapInfo> { MapCsv.ToInfo(_mapData) }), MapCsv.FileEncoding);
            AssetDatabase.ImportAsset(mapPath);
            AssetDatabase.ImportAsset(listPath);
            UnityEngine.Debug.Log($"[DebugMapFile] Saved debug map: {mapPath}");
        }
    }
}
