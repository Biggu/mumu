using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace mumu
{
    /// <summary>
    /// ローカライズの初期設定（Locale・文字列テーブル）
    /// 未設定ならコンパイル後に自動実行。テキストの追加・修正は Window > Asset Management > Localization Tables で行う
    /// </summary>
    [InitializeOnLoad]
    public static class LocalizationSetup
    {
        /// <summary>文字列テーブル名</summary>
        public const string TableName = "UI";

        private const string SettingsPath = "Assets/mumu/Localization/TextSetting.asset";
        private const string LocaleFolder = "Assets/mumu/Localization/Locales";
        private const string TableFolder = "Assets/mumu/Localization/Tables";
        private const string StampKey = "LocalizationSetup";

        /// <summary>対応言語（先頭が日本語）</summary>
        private static readonly string[] LocaleCodes = { "ja", "en" };

        /// <summary>初期テキスト（キー, 日本語, 英語）</summary>
        private static readonly string[][] Entries =
        {
            new[] { "TITLE", "つなパグ", "Pug Path" },
            new[] { "START", "スタート", "Start" },
            new[] { "CATCHPHRASE", "パグまで道をつないで、たすけだそう！", "Draw a path and save the pug!" },
            new[] { "SETTINGS", "せってい", "Settings" },
            new[] { "SE", "効果音", "Sound" },
            new[] { "BGM", "BGM", "Music" },
            new[] { "MENU", "メニュー", "Menu" },
            new[] { "RETRY", "やりなおす", "Retry" },
            new[] { "TO_TITLE", "タイトルへ", "Title" },
            new[] { "ADVENTURE", "冒険", "Adventure" },
            new[] { "STROLL", "おさんぽ", "Stroll" },
            new[] { "STAGE", "ステージ {0}", "Stage {0}" },
            new[] { "OK", "OK", "OK" },
            new[] { "YES", "はい", "Yes" },
            new[] { "NO", "いいえ", "No" },
            new[] { "CONFIRM_TITLE", "かくにん", "Confirm" },
            new[] { "CONFIRM_RETRY", "さいしょから やりなおしますか？", "Start this stage over?" },
            new[] { "CONFIRM_TO_TITLE", "タイトルに もどりますか？\nいまの道は 消えます。", "Back to the title?\nYour current path will be lost." },
            new[] { "ALL_CLEAR_TITLE", "ぜんぶクリア！", "All Clear!" },
            new[] { "ALL_CLEAR_BODY", "いまある ステージは ぜんぶクリア！\n今後の アップデートを おたのしみに。\n最後のステージを もう一度 あそべます。", "You cleared every stage!\nMore stages are coming in future updates.\nYou can replay the last stage." },
            new[] { "GARDEN", "おにわ", "Garden" },
            new[] { "GARDEN_COUNT", "なかま {0}ひき", "Friends: {0}" },
            new[] { "GARDEN_EMPTY", "まだ なかまが いないよ。\n冒険や おさんぽで パグを たすけよう！", "No friends yet.\nRescue pugs in Adventure or Stroll!" },
            new[] { "UNKNOWN", "？？？", "???" },
            new[] { "CLEAR", "CLEAR!", "CLEAR!" },
            new[] { "PERFECT", "PERFECT!", "PERFECT!" },
            new[] { "PUG_JOINED_TITLE", "なかまが ふえた！", "New friend!" },
            new[] { "PUG_JOINED_BODY", "{0} が なかまに なった！\nおにわで 会えるよ。", "{0} joined you!\nSay hi in the Garden." },
            new[] { "STROLL_DAILY", "おさんぽ {0}/{1}", "Stroll {0}/{1}" },
            new[] { "DAILY_DONE_TITLE", "きょうの パグチャンス おしまい", "No more pug chances today" },
            new[] { "DAILY_DONE_BODY", "きょうの おさんぽ 5回が おわったよ。\nここからは パグに 会えない ふつうの おさんぽです。\nあしたも 5回 チャンスが あるよ！", "You used today's 5 strolls.\nFrom now on, strolls won't have pugs to rescue.\nCome back tomorrow for 5 more chances!" },
            new[] { "STAGE_RANGE", "ステージ {0}〜{1}", "Stages {0}-{1}" },
            new[] { "PLAY", "あそぶ", "Play" },
            new[] { "GARDEN_STATUS", "なかま {0}ひき  ほね {1}", "Friends {0}  Bones {1}" },
            new[] { "GIMMICK_NEW_TITLE", "あたらしい ギミック「{0}」", "New gimmick: {0}" },
            new[] { "PUG_BOOK", "ずかん", "Pug Book" },
            new[] { "PUG_BOOK_BONES", "もっている ほね {0}", "Bones: {0}" },
            new[] { "PUG_BUY_TITLE", "なかまに する？", "Adopt?" },
            new[] { "PUG_BUY_BODY", "{0} を ほね {1}こ と こうかんして\nなかまに する？", "Trade {1} bones\nto adopt {0}?" },
            new[] { "PUG_BUY_SHORT", "ほねが あと {0}こ たりないよ。\nおさんぽや 冒険で あつめよう！", "You need {0} more bones.\nCollect them on walks and adventures!" },
            new[] { "PUG_NOT_FOR_SALE", "おさんぽで あえるかも", "Meet on a stroll" },
            new[] { "PUG_DUPLICATE_TITLE", "もう なかまだよ！", "Already a friend!" },
            new[] { "PUG_DUPLICATE_BODY", "{0} は もう なかまだよ。\nかわりに ほね {1}こ もらったよ！", "{0} is already your friend.\nYou got {1} bones instead!" },
            new[] { "PUG_OWNED", "もってる", "Owned" },
            new[] { "PUG_EQUIP", "一緒にいく", "Come along" },
            new[] { "GARDEN_REST", "お休み", "Rest" },
            new[] { "PUG_EXCHANGE", "交換", "Exchange" },
            new[] { "PUG_TO_GARDEN", "庭へ", "To garden" },
            new[] { "NOTICE_UNAVAILABLE", "いまは できないよ", "Not available right now" },
            // TODO: 仮の文字（ミッションのボーナス表示。あとで差し替える）
            new[] { "WANDERFUL_BONUS", "[仮] ワンだふるボーナス！ ほね +{0}", "[TODO] Woof-derful Bonus! Bones +{0}" },
            new[] { "WANDERFUL_DETAIL", "[仮] もどらず パーフェクト クリア", "[TODO] No-return perfect clear" },
            // TODO: テーマのおはなし（仮の文字。テーマ番号ごとに書き直す）
            new[] { "THEME_STORY_1", "[仮] テーマ1の おはなし", "[TODO] Theme 1 story" },
            new[] { "THEME_STORY_2", "[仮] テーマ2の おはなし", "[TODO] Theme 2 story" },
            new[] { "THEME_STORY_3", "[仮] テーマ3の おはなし", "[TODO] Theme 3 story" },
            new[] { "THEME_STORY_4", "[仮] テーマ4の おはなし", "[TODO] Theme 4 story" },
            new[] { "THEME_STORY_5", "[仮] テーマ5の おはなし", "[TODO] Theme 5 story" },
            new[] { "THEME_STORY_6", "[仮] テーマ6の おはなし", "[TODO] Theme 6 story" },
            new[] { "THEME_STORY_7", "[仮] テーマ7の おはなし", "[TODO] Theme 7 story" },
            new[] { "THEME_STORY_8", "[仮] テーマ8の おはなし", "[TODO] Theme 8 story" },
            new[] { "THEME_STORY_9", "[仮] テーマ9の おはなし", "[TODO] Theme 9 story" },
            new[] { "THEME_STORY_10", "[仮] テーマ10の おはなし", "[TODO] Theme 10 story" },
            new[] { "THEME_STORY_11", "[仮] テーマ11の おはなし", "[TODO] Theme 11 story" },
            new[] { "THEME_STORY_12", "[仮] テーマ12の おはなし", "[TODO] Theme 12 story" },
            new[] { "THEME_STORY_13", "[仮] テーマ13の おはなし", "[TODO] Theme 13 story" },
            new[] { "THEME_STORY_14", "[仮] テーマ14の おはなし", "[TODO] Theme 14 story" },
            // TODO: ヒントの確認（仮の文字）
            new[] { "HINT_CONFIRM", "[仮] ヒントを つかう？ ほね {0} を つかうよ", "[TODO] Use a hint? It costs {0} bones" },
            new[] { "HINT_USE", "[仮] つかう", "[TODO] Use" },
            new[] { "HINT_CANCEL", "[仮] やめる", "[TODO] Cancel" },
            new[] { "PUG_UNEQUIP", "はずす", "Leave home" },
            new[] { "PUG_EQUIPPED", "いっしょ", "With you" },
            new[] { "TOAST_GATE", "カギを とってから とおろう", "Get the key first to open this gate" },
            new[] { "TOAST_CRUMBLE", "はしが こわれた！ もう もどれないよ（やりなおしは メニューから）", "The bridge broke! No going back (retry from the menu)" },
            new[] { "TOAST_CRUMBLE_LOCKED", "こわれた はしより まえには もどれないよ", "You can't go back past a broken bridge" },
        };

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static LocalizationSetup()
        {
            EditorApplication.delayCall += autoSetup;
        }

        /// <summary>
        /// 未設定なら実行
        /// </summary>
        private static void autoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorBuildStamp.NeedsBuild(StampKey, isReady(), null))
            {
                return;
            }
            EditorBuildStamp.Save(StampKey);
            Setup();
        }

        /// <summary>
        /// 設定済みか
        /// </summary>
        private static bool isReady()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null || collection == null)
            {
                return false;
            }

            // テキストを追加した時も再実行する
            foreach (string[] entry in Entries)
            {
                if (!collection.SharedData.Contains(entry[0]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// ローカライズを設定
        /// </summary>
        [MenuItem("mumu/Setup Localization")]
        public static void Setup()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                Debug.LogError($"[LocalizationSetup] LocalizationSettings not found: {SettingsPath}");
                return;
            }
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;

            var locales = new List<Locale>();
            foreach (string code in LocaleCodes)
            {
                locales.Add(ensureLocale(code));
            }

            StringTableCollection collection = ensureCollection(locales);
            foreach (string[] entry in Entries)
            {
                for (int i = 0; i < locales.Count; i++)
                {
                    setEntry(collection, locales[i], entry[0], entry[1 + i]);
                }
            }
            EditorUtility.SetDirty(collection.SharedData);

            AssetDatabase.SaveAssets();
            EditorBuildStamp.Save(StampKey);
            Debug.Log($"[LocalizationSetup] Localization ready: table '{TableName}', locales {string.Join(", ", LocaleCodes)}");
        }

        /// <summary>
        /// Locale を取得（なければ作成して登録）
        /// </summary>
        /// <param name="_code"></param>
        private static Locale ensureLocale(string _code)
        {
            var identifier = new LocaleIdentifier(_code);
            Locale locale = LocalizationEditorSettings.GetLocale(identifier);
            if (locale != null)
            {
                return locale;
            }

            EditorAssetUtility.EnsureFolder(LocaleFolder);
            string path = $"{LocaleFolder}/{_code}.asset";
            locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(identifier);
                AssetDatabase.CreateAsset(locale, path);
            }
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        /// <summary>
        /// 文字列テーブルを取得（なければ作成、足りない言語のテーブルも追加）
        /// </summary>
        /// <param name="_locales"></param>
        private static StringTableCollection ensureCollection(List<Locale> _locales)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (collection == null)
            {
                EditorAssetUtility.EnsureFolder(TableFolder);
                collection = LocalizationEditorSettings.CreateStringTableCollection(TableName, TableFolder, _locales);
            }

            foreach (Locale locale in _locales)
            {
                if (collection.GetTable(locale.Identifier) == null)
                {
                    collection.AddNewTable(locale.Identifier);
                }
            }
            return collection;
        }

        /// <summary>
        /// テキストを追加・更新
        /// </summary>
        /// <param name="_collection"></param>
        /// <param name="_locale"></param>
        /// <param name="_key"></param>
        /// <param name="_value"></param>
        private static void setEntry(StringTableCollection _collection, Locale _locale, string _key, string _value)
        {
            var table = (StringTable)_collection.GetTable(_locale.Identifier);
            table.AddEntry(_key, _value);
            EditorUtility.SetDirty(table);
        }

        /// <summary>
        /// TextMeshPro の文字をテーブルのキーにつなぐ（LocalizeStringEvent）
        /// </summary>
        /// <param name="_text"></param>
        /// <param name="_key"></param>
        public static void Bind(TMP_Text _text, string _key)
        {
            if (_text == null)
            {
                return;
            }

            var localize = _text.GetComponent<LocalizeStringEvent>();
            if (localize == null)
            {
                localize = _text.gameObject.AddComponent<LocalizeStringEvent>();
            }
            localize.StringReference.SetReference(TableName, _key);

            // 既存のつなぎ先を外して text に設定し直す
            while (localize.OnUpdateString.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(localize.OnUpdateString, 0);
            }
            var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), _text, "set_text");
            UnityEventTools.AddPersistentListener(localize.OnUpdateString, setter);
        }
    }
}
