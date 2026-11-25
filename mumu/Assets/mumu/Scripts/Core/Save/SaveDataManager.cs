using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 保存データの読み書き（Application.persistentDataPath/save.dat、SaveCrypto で暗号化）
    /// </summary>
    public static class SaveDataManager
    {
        public const string FileName = "save.dat";
        /// <summary>前の版の暗号化していない保存ファイル（初回に save.dat へ移して消す）</summary>
        public const string LegacyFileName = "save.json";
        /// <summary>読めなかった保存ファイルを残す時の名前</summary>
        private const string BrokenSuffix = ".broken";
        /// <summary>新しく始めた人に最初からいるパグ</summary>
        public const int DefaultPugId = 1;

        /// <summary>前の版で PlayerPrefs に保存していたキー（初回に save.json へ移す）</summary>
        private const string OldSeKey = "mumu.settings.se";
        private const string OldBgmKey = "mumu.settings.bgm";
        private const string OldStageIndexKey = "mumu.stage.index";
        private const string OldClearedKey = "mumu.stage.cleared";
        private const string OldRecordKeyPrefix = "mumu.stage.record.";
        private const string OldPugsKey = "mumu.pugs";
        private const int OldPerfectFlag = 1 << 4;
        /// <summary>version 1 のパグツー・パグワンの ID（version 2 で 500・501 に移った）</summary>
        private const int OldPugTwoId = 150;
        private const int OldPugOneId = 151;
        private const int PugTwoId = 500;
        private const int PugOneId = 501;

        private static UserSaveData data = null;

        /// <summary>保存先</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string legacyPath => Path.Combine(Application.persistentDataPath, LegacyFileName);

        /// <summary>保存データ（初回に読み込み、なければ作る）</summary>
        public static UserSaveData Data
        {
            get
            {
                if (data == null)
                {
                    data = load();
                }
                return data;
            }
        }

        /// <summary>
        /// 保存（書きかけで壊れないよう一時ファイルに書いてから差し替える）
        /// </summary>
        public static void Save()
        {
            string path = FilePath;
            string temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, SaveCrypto.Encrypt(JsonUtility.ToJson(Data)));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(temp, path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveDataManager] Save failed: {exception.Message}");
            }
        }

        /// <summary>
        /// 中身を JSON で取り出す（デバッグ用）
        /// </summary>
        public static string ExportJson()
        {
            return JsonUtility.ToJson(Data, true);
        }

        /// <summary>
        /// JSON で中身を差し替えて保存（デバッグ用、読めなければ false）
        /// </summary>
        /// <param name="_json"></param>
        public static bool ImportJson(string _json)
        {
            UserSaveData imported = parse(_json);
            if (imported == null)
            {
                return false;
            }
            data = imported;
            Save();
            return true;
        }

        /// <summary>
        /// 全部消して新しく始める
        /// </summary>
        public static void ResetAll()
        {
            data = createNew();
            Save();
        }

        /// <summary>
        /// 読み込み（ファイルがなければ前の版の PlayerPrefs から移すか、新しく作る）
        /// </summary>
        private static UserSaveData load()
        {
            string path = FilePath;
            if (File.Exists(path))
            {
                UserSaveData loaded = null;
                try
                {
                    if (SaveCrypto.TryDecrypt(File.ReadAllText(path), out string json))
                    {
                        loaded = parse(json);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[SaveDataManager] Load failed: {exception.Message}");
                }

                if (loaded != null)
                {
                    return loaded;
                }

                // 壊れている・書き換えられている時は残しておいて新しく始める
                Debug.LogError("[SaveDataManager] Save data is broken or modified. Starting new data.");
                keepBroken(path);
            }
            else if (File.Exists(legacyPath))
            {
                // 前の版の暗号化していないファイルを移す
                UserSaveData legacy = parse(File.ReadAllText(legacyPath));
                if (legacy != null)
                {
                    data = legacy;
                    Save();
                    File.Delete(legacyPath);
                    Debug.Log("[SaveDataManager] Moved save.json into encrypted save.dat");
                    return data;
                }
            }

            data = hasOldPlayerPrefs() ? importOldPlayerPrefs() : createNew();
            Save();
            return data;
        }

        /// <summary>
        /// JSON → 保存データ（読めなければ null）
        /// </summary>
        /// <param name="_json"></param>
        private static UserSaveData parse(string _json)
        {
            try
            {
                UserSaveData parsed = JsonUtility.FromJson<UserSaveData>(_json);
                if (parsed != null)
                {
                    migrate(parsed);
                }
                return parsed;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveDataManager] Parse failed: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 読めなかったファイルを別名で残す
        /// </summary>
        /// <param name="_path"></param>
        private static void keepBroken(string _path)
        {
            try
            {
                string broken = _path + BrokenSuffix;
                if (File.Exists(broken))
                {
                    File.Delete(broken);
                }
                File.Move(_path, broken);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveDataManager] Could not keep the broken file: {exception.Message}");
            }
        }

        /// <summary>
        /// 新しく始めた人のデータ（パグ1匹つき）
        /// </summary>
        private static UserSaveData createNew()
        {
            var created = new UserSaveData();
            created.pugs.Add(DefaultPugId);
            created.gardenPugs.Add(DefaultPugId);
            return created;
        }

        /// <summary>
        /// 古い形式を今の形式に変換（バージョンを上げた時にここへ追加する）
        /// </summary>
        /// <param name="_data"></param>
        private static void migrate(UserSaveData _data)
        {
            if (_data.stageRecords == null)
            {
                _data.stageRecords = new List<StageRecordData>();
            }
            if (_data.pugs == null)
            {
                _data.pugs = new List<int>();
            }
            if (_data.seenGimmicks == null)
            {
                _data.seenGimmicks = new List<string>();
            }

            // version 2: パグツー・パグワンの ID が 150・151 → 500・501 に変わった。同じ子は1匹だけにして、重なった分はほねにする
            if (_data.version < 2)
            {
                var unique = new List<int>();
                foreach (int id in _data.pugs)
                {
                    int newId = id == OldPugTwoId ? PugTwoId : (id == OldPugOneId ? PugOneId : id);
                    if (unique.Contains(newId))
                    {
                        CharacterData character = CharacterMaster.Get(newId);
                        _data.bones += character != null ? character.DuplicateBones : 0;
                    }
                    else
                    {
                        unique.Add(newId);
                    }
                }
                _data.pugs = unique;
            }
            // version 3: おにわに入れる子を決めるようにした（持っている子の先頭から最大 MaxGarden 匹）
            if (_data.version < 3)
            {
                _data.gardenPugs = new List<int>();
                foreach (int id in _data.pugs)
                {
                    if (_data.gardenPugs.Count < UserSaveData.MaxGarden)
                    {
                        _data.gardenPugs.Add(id);
                    }
                }
            }
            if (_data.gardenPugs == null)
            {
                _data.gardenPugs = new List<int>();
            }
            sanitizeGarden(_data);
            _data.version = UserSaveData.CurrentVersion;
        }
        /// <summary>
        /// おにわの子を整える（持っていない子は外す、同じ子は1回だけ、上限まで、1匹もいなければ先頭の子を入れる）
        /// </summary>
        /// <param name="_data"></param>
        private static void sanitizeGarden(UserSaveData _data)
        {
            var result = new List<int>();
            foreach (int id in _data.gardenPugs)
            {
                if (_data.pugs.Contains(id) && !result.Contains(id) && result.Count < UserSaveData.MaxGarden)
                {
                    result.Add(id);
                }
            }
            if (result.Count == 0 && _data.pugs.Count > 0)
            {
                result.Add(_data.pugs[0]);
            }
            _data.gardenPugs = result;
        }


        /// <summary>
        /// 前の版の PlayerPrefs が残っているか
        /// </summary>
        private static bool hasOldPlayerPrefs()
        {
            return PlayerPrefs.HasKey(OldSeKey) || PlayerPrefs.HasKey(OldBgmKey) || PlayerPrefs.HasKey(OldStageIndexKey)
                || PlayerPrefs.HasKey(OldClearedKey) || PlayerPrefs.HasKey(OldPugsKey);
        }

        /// <summary>
        /// 前の版の PlayerPrefs を移して消す（仲間が1匹もいなければデフォルトのパグを入れる）
        /// </summary>
        private static UserSaveData importOldPlayerPrefs()
        {
            var imported = new UserSaveData
            {
                isSeEnabled = PlayerPrefs.GetInt(OldSeKey, 1) == 1,
                isBgmEnabled = PlayerPrefs.GetInt(OldBgmKey, 1) == 1,
                stageIndex = Mathf.Max(0, PlayerPrefs.GetInt(OldStageIndexKey, 0)),
                clearedCount = Mathf.Max(0, PlayerPrefs.GetInt(OldClearedKey, 0)),
            };

            // 記録はクリアしたステージまで（その先は保存されていない）
            int recordMax = Mathf.Max(imported.clearedCount, imported.stageIndex + 1);
            for (int stage = 1; stage <= recordMax; stage++)
            {
                string key = OldRecordKeyPrefix + stage;
                if (!PlayerPrefs.HasKey(key))
                {
                    continue;
                }
                int value = PlayerPrefs.GetInt(key, 0);
                imported.stageRecords.Add(new StageRecordData { stage = stage, stars = value & (OldPerfectFlag - 1), isPerfect = (value & OldPerfectFlag) != 0 });
                PlayerPrefs.DeleteKey(key);
            }

            foreach (string value in PlayerPrefs.GetString(OldPugsKey, "").Split(','))
            {
                if (int.TryParse(value, out int id))
                {
                    imported.pugs.Add(id);
                }
            }
            if (imported.pugs.Count == 0)
            {
                imported.pugs.Add(DefaultPugId);
            }

            sanitizeGarden(imported);

            foreach (string key in new[] { OldSeKey, OldBgmKey, OldStageIndexKey, OldClearedKey, OldPugsKey })
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            Debug.Log("[SaveDataManager] Imported old PlayerPrefs into save.json");
            return imported;
        }
    }
}
