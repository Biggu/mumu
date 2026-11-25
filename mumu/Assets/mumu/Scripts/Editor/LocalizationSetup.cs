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
        /// <summary>文章を書き直した時は、この名前を変えると次のコンパイルで再設定される</summary>
        private const string StampKey = "LocalizationSetup_hint_used";

        /// <summary>対応言語（先頭が日本語）</summary>
        public static readonly string[] LocaleCodes = { "ja", "en" };

        /// <summary>初期テキスト（キー, 日本語, 英語）</summary>
        private static readonly string[][] Entries =
        {
            new[] { "TITLE", "つなパグ", "Pug Path" },
            new[] { "START", "スタート", "Start" },
            new[] { "CATCHPHRASE", "道をつないで、パグを助けだそう！", "Draw a path and save the pug!" },
            new[] { "SETTINGS", "設定", "Settings" },
            new[] { "SE", "効果音", "Sound" },
            new[] { "BGM", "BGM", "Music" },
            new[] { "MENU", "メニュー", "Menu" },
            new[] { "RETRY", "やり直す", "Retry" },
            new[] { "TO_TITLE", "タイトルへ", "Title" },
            new[] { "ADVENTURE", "冒険", "Adventure" },
            new[] { "STROLL", "お散歩", "Stroll" },
            new[] { "STAGE", "ステージ {stage}", "Stage {stage}" },
            new[] { "OK", "OK", "OK" },
            new[] { "YES", "はい", "Yes" },
            new[] { "NO", "いいえ", "No" },
            new[] { "CONFIRM_TITLE", "確認", "Confirm" },
            new[] { "CONFIRM_RETRY", "最初から やり直しますか？", "Start this stage over?" },
            new[] { "CONFIRM_TO_TITLE", "タイトルに戻りますか？\n今の道は消えます。", "Back to the title?\nYour current path will be lost." },
            new[] { "ALL_CLEAR_TITLE", "全部クリア！", "All Clear!" },
            new[] { "ALL_CLEAR_BODY", "今あるステージは全部クリア！\n今後のアップデートをお楽しみに。\n最後のステージをもう一度遊べます。", "You cleared every stage!\nMore stages are coming in future updates.\nYou can replay the last stage." },
            new[] { "GARDEN", "お庭", "Garden" },
            new[] { "GARDEN_COUNT", "仲間 {count}匹", "Friends: {count}" },
            new[] { "GARDEN_EMPTY", "まだ仲間がいないよ。\n冒険やお散歩でパグを助けよう！", "No friends yet.\nRescue pugs in Adventure or Stroll!" },
            new[] { "UNKNOWN", "？？？", "???" },
            new[] { "CLEAR", "CLEAR!", "CLEAR!" },
            new[] { "PERFECT", "PERFECT!", "PERFECT!" },
            new[] { "PUG_JOINED_TITLE", "仲間が増えた！", "New friend!" },
            new[] { "PUG_JOINED_BODY", "{name} が仲間になった！\nお庭で会えるよ。", "{name} joined you!\nSay hi in the Garden." },
            new[] { "STROLL_DAILY", "お散歩 {num}/{total}", "Stroll {num}/{total}" },
            new[] { "DAILY_DONE_TITLE", "今日のパグチャンスは終わり", "No more pug chances today" },
            new[] { "DAILY_DONE_BODY", "今日のお散歩5回が終わったよ。\nここからはパグに会えない普通のお散歩です。\n明日も5回チャンスがあるよ！", "You used today's 5 strolls.\nFrom now on, strolls won't have pugs to rescue.\nCome back tomorrow for 5 more chances!" },
            new[] { "STAGE_RANGE", "ステージ {first}〜{last}", "Stages {first}-{last}" },
            new[] { "PLAY", "遊ぶ", "Play" },
            new[] { "GARDEN_STATUS", "仲間 {count}匹  骨 {bone}", "Friends {count}  Bones {bone}" },
            new[] { "CONFIRM_REWIND", "{count}マス戻りますか？\n戻ると「戻らずクリア」のボーナスはもらえないよ。", "Go back {count} tiles?\nYou won't get the no-rewind clear bonus." },
            new[] { "GIMMICK_NEW_TITLE", "新しいギミック「{name}」", "New gimmick: {name}" },
            new[] { "PUG_BOOK", "図鑑", "Pug Book" },
            new[] { "PUG_BOOK_BONES", "持っている骨 {bone}", "Bones: {bone}" },
            new[] { "PUG_BUY_TITLE", "仲間にする？", "Adopt?" },
            new[] { "PUG_BUY_BODY", "{name} を骨 {bone}個と交換して\n仲間にする？", "Trade {bone} bones\nto adopt {name}?" },
            new[] { "PUG_BUY_SHORT", "骨があと {bone}個足りないよ。\nお散歩や冒険で集めよう！", "You need {bone} more bones.\nCollect them on walks and adventures!" },
            new[] { "PUG_NOT_FOR_SALE", "お散歩で会えるかも", "Meet on a stroll" },
            new[] { "PUG_DUPLICATE_TITLE", "もう仲間だよ！", "Already a friend!" },
            new[] { "PUG_DUPLICATE_BODY", "{name} はもう仲間だよ。\nかわりに骨 {bone}個もらったよ！", "{name} is already your friend.\nYou got {bone} bones instead!" },
            new[] { "PUG_OWNED", "持ってる", "Owned" },
            new[] { "PUG_EQUIP", "一緒に行く", "Come along" },
            new[] { "GARDEN_REST", "お休み", "Rest" },
            new[] { "PUG_EXCHANGE", "交換", "Exchange" },
            new[] { "PUG_TO_GARDEN", "庭へ", "To garden" },
            new[] { "NOTICE_UNAVAILABLE", "今はできないよ", "Not available right now" },
            new[] { "WANDERFUL_BONUS", "ワンダフルボーナス！ 骨 +{bone}", "Woof-derful Bonus! Bones +{bone}" },
            new[] { "WANDERFUL_DETAIL", "戻らずパーフェクトクリア", "Perfect clear without rewinding" },
            // テーマのおはなし（文章はまだ。テーマごとに書き直す）
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
            // テーマ 15〜33（仮の文章。あとで書き直す）
            new[] { "THEME_STORY_15", "[仮] テーマ15の おはなし", "[TODO] Theme 15 story" },
            new[] { "THEME_STORY_16", "[仮] テーマ16の おはなし", "[TODO] Theme 16 story" },
            new[] { "THEME_STORY_17", "[仮] テーマ17の おはなし", "[TODO] Theme 17 story" },
            new[] { "THEME_STORY_18", "[仮] テーマ18の おはなし", "[TODO] Theme 18 story" },
            new[] { "THEME_STORY_19", "[仮] テーマ19の おはなし", "[TODO] Theme 19 story" },
            new[] { "THEME_STORY_20", "[仮] テーマ20の おはなし", "[TODO] Theme 20 story" },
            new[] { "THEME_STORY_21", "[仮] テーマ21の おはなし", "[TODO] Theme 21 story" },
            new[] { "THEME_STORY_22", "[仮] テーマ22の おはなし", "[TODO] Theme 22 story" },
            new[] { "THEME_STORY_23", "[仮] テーマ23の おはなし", "[TODO] Theme 23 story" },
            new[] { "THEME_STORY_24", "[仮] テーマ24の おはなし", "[TODO] Theme 24 story" },
            new[] { "THEME_STORY_25", "[仮] テーマ25の おはなし", "[TODO] Theme 25 story" },
            new[] { "THEME_STORY_26", "[仮] テーマ26の おはなし", "[TODO] Theme 26 story" },
            new[] { "THEME_STORY_27", "[仮] テーマ27の おはなし", "[TODO] Theme 27 story" },
            new[] { "THEME_STORY_28", "[仮] テーマ28の おはなし", "[TODO] Theme 28 story" },
            new[] { "THEME_STORY_29", "[仮] テーマ29の おはなし", "[TODO] Theme 29 story" },
            new[] { "THEME_STORY_30", "[仮] テーマ30の おはなし", "[TODO] Theme 30 story" },
            new[] { "THEME_STORY_31", "[仮] テーマ31の おはなし", "[TODO] Theme 31 story" },
            new[] { "THEME_STORY_32", "[仮] テーマ32の おはなし", "[TODO] Theme 32 story" },
            new[] { "THEME_STORY_33", "[仮] テーマ33の おはなし", "[TODO] Theme 33 story" },
            // テーマの名前（札に出す）
            new[] { "THEME_NAME_1", "そうげん", "Meadow" },
            new[] { "THEME_NAME_2", "なつ", "Summer" },
            new[] { "THEME_NAME_3", "ふゆ", "Winter" },
            new[] { "THEME_NAME_4", "あき", "Autumn" },
            new[] { "THEME_NAME_5", "はる", "Spring" },
            new[] { "THEME_NAME_6", "さくら", "Sakura" },
            new[] { "THEME_NAME_7", "さばく", "Desert" },
            new[] { "THEME_NAME_8", "ケーキ", "Cake" },
            new[] { "THEME_NAME_9", "プリン", "Pudding" },
            new[] { "THEME_NAME_10", "まち", "City" },
            new[] { "THEME_NAME_11", "むら", "Village" },
            new[] { "THEME_NAME_12", "うみ", "Sea" },
            new[] { "THEME_NAME_13", "こおり", "Ice" },
            new[] { "THEME_NAME_14", "オフィス", "Office" },
            new[] { "THEME_NAME_15", "宇宙", "Space" },
            new[] { "THEME_NAME_16", "海の底", "Underwater" },
            new[] { "THEME_NAME_17", "月の森", "Moon Forest" },
            new[] { "THEME_NAME_18", "お城", "Castle" },
            new[] { "THEME_NAME_19", "山", "Mountain" },
            new[] { "THEME_NAME_20", "夜のそうげん", "Night Meadow" },
            new[] { "THEME_NAME_21", "夜のなつ", "Night Summer" },
            new[] { "THEME_NAME_22", "夜のふゆ", "Night Winter" },
            new[] { "THEME_NAME_23", "夜のあき", "Night Autumn" },
            new[] { "THEME_NAME_24", "夜のはる", "Night Spring" },
            new[] { "THEME_NAME_25", "夜のさくら", "Night Sakura" },
            new[] { "THEME_NAME_26", "夜のさばく", "Night Desert" },
            new[] { "THEME_NAME_27", "夜のケーキ", "Night Cake" },
            new[] { "THEME_NAME_28", "夜のプリン", "Night Pudding" },
            new[] { "THEME_NAME_29", "夜のまち", "Night City" },
            new[] { "THEME_NAME_30", "夜のむら", "Night Village" },
            new[] { "THEME_NAME_31", "夜のうみ", "Night Sea" },
            new[] { "THEME_NAME_32", "夜のこおり", "Night Ice" },
            new[] { "THEME_NAME_33", "夜のオフィス", "Night Office" },
            new[] { "THEME_NAME_40", "ボーナス", "Bonus" },
            new[] { "STAGE_THEME", "ステージ {stage}　{theme}", "Stage {stage}  {theme}" },
            new[] { "STROLL_CHANCES", "今日 あと {num}/{total}回", "{num}/{total} left today" },
            new[] { "FILTER_UNOWNED", "未所持のみ", "Not owned only" },
            new[] { "SUMMARY_BONES", "ほね +{bone}", "Bones +{bone}" },
            new[] { "SUMMARY_STARS", "★ {star}/3", "★ {star}/3" },
            new[] { "SUMMARY_NOREWIND", "もどらずクリア", "No-rewind clear" },
            new[] { "SUMMARY_REWOUND", "もどってクリア", "Rewound clear" },
            new[] { "HINT_CONFIRM", "ヒントを使う？ 骨 {bone} を使うよ", "Use a hint? It costs {bone} bones" },
            new[] { "HINT_USE", "使う", "Use" },
            new[] { "HINT_USED", "ヒントを出したよ。\nのこりのヒントも使えるよ。", "A hint is revealed.\nYou can use more hints too." },
            new[] { "HINT_CANCEL", "やめる", "Cancel" },
            new[] { "PUG_UNEQUIP", "外す", "Leave home" },
            new[] { "PUG_EQUIPPED", "一緒", "With you" },
            new[] { "TOAST_GATE", "カギを取ってから通ろう", "Get the key first to open this gate" },
            new[] { "TOAST_CRUMBLE", "橋が壊れた！ もう戻れないよ（やり直しはメニューから）", "The bridge broke! No going back (retry from the menu)" },
            new[] { "TOAST_CRUMBLE_LOCKED", "壊れた橋より前には戻れないよ", "You can't go back past a broken bridge" },
            new[] { "TOAST_ONEWAY", "この向きからは通れないよ", "You can't pass from this side" },
            new[] { "TOAST_KEY", "カギを手に入れた！ 同じ色の扉が開くよ", "Got the key! The gate of the same colour will open" },
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
            StringTableEntry entry = table.AddEntry(_key, _value);
            // {name} や {0} を使う文字は Smart String として登録する（通常の文字は String.Format で埋めるため）
            entry.IsSmart = _value.Contains("{");
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
