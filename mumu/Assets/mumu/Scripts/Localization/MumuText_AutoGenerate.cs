using System.Collections.Generic;

namespace mumu
{
    /// <summary>
    /// 文字列テーブルのキーから作るテキスト（自動生成。直接編集しない）
    /// </summary>
    public static class MumuText
    {
        /// <summary>
        /// 冒険 | Adventure
        /// </summary>
        public static string ADVENTURE()
        {
            return TextLocalizer.Get("ADVENTURE");
        }

        /// <summary>
        /// 今あるステージは全部クリア！ 今後のアップデートをお楽しみに。 最後のステージをもう一度遊べます。 | You cleared every stage! More stages are coming in future updates. You can replay the last stage.
        /// </summary>
        public static string ALL_CLEAR_BODY()
        {
            return TextLocalizer.Get("ALL_CLEAR_BODY");
        }

        /// <summary>
        /// 全部クリア！ | All Clear!
        /// </summary>
        public static string ALL_CLEAR_TITLE()
        {
            return TextLocalizer.Get("ALL_CLEAR_TITLE");
        }

        /// <summary>
        /// BGM | Music
        /// </summary>
        public static string BGM()
        {
            return TextLocalizer.Get("BGM");
        }

        /// <summary>
        /// 道をつないで、パグを助けだそう！ | Draw a path and save the pug!
        /// </summary>
        public static string CATCHPHRASE()
        {
            return TextLocalizer.Get("CATCHPHRASE");
        }

        /// <summary>
        /// CLEAR! | CLEAR!
        /// </summary>
        public static string CLEAR()
        {
            return TextLocalizer.Get("CLEAR");
        }

        /// <summary>
        /// 最初から やり直しますか？ | Start this stage over?
        /// </summary>
        public static string CONFIRM_RETRY()
        {
            return TextLocalizer.Get("CONFIRM_RETRY");
        }

        /// <summary>
        /// {count}マス戻りますか？ 戻ると「戻らずクリア」のボーナスはもらえないよ。 | Go back {count} tiles? You won&apos;t get the no-rewind clear bonus.
        /// </summary>
        /// <param name="_count"></param>
        public static string CONFIRM_REWIND(object _count)
        {
            return TextLocalizer.GetNamed("CONFIRM_REWIND", new Dictionary<string, object> { { "count", _count } });
        }

        /// <summary>
        /// 確認 | Confirm
        /// </summary>
        public static string CONFIRM_TITLE()
        {
            return TextLocalizer.Get("CONFIRM_TITLE");
        }

        /// <summary>
        /// タイトルに戻りますか？ 今の道は消えます。 | Back to the title? Your current path will be lost.
        /// </summary>
        public static string CONFIRM_TO_TITLE()
        {
            return TextLocalizer.Get("CONFIRM_TO_TITLE");
        }

        /// <summary>
        /// 今日のお散歩5回が終わったよ。 ここからはパグに会えない普通のお散歩です。 明日も5回チャンスがあるよ！ | You used today&apos;s 5 strolls. From now on, strolls won&apos;t have pugs to rescue. Come back tomorrow for 5 more chances!
        /// </summary>
        public static string DAILY_DONE_BODY()
        {
            return TextLocalizer.Get("DAILY_DONE_BODY");
        }

        /// <summary>
        /// 今日のパグチャンスは終わり | No more pug chances today
        /// </summary>
        public static string DAILY_DONE_TITLE()
        {
            return TextLocalizer.Get("DAILY_DONE_TITLE");
        }

        /// <summary>
        /// 未所持のみ | Not owned only
        /// </summary>
        public static string FILTER_UNOWNED()
        {
            return TextLocalizer.Get("FILTER_UNOWNED");
        }

        /// <summary>
        /// お庭 | Garden
        /// </summary>
        public static string GARDEN()
        {
            return TextLocalizer.Get("GARDEN");
        }

        /// <summary>
        /// 仲間 {count}匹 | Friends: {count}
        /// </summary>
        /// <param name="_count"></param>
        public static string GARDEN_COUNT(object _count)
        {
            return TextLocalizer.GetNamed("GARDEN_COUNT", new Dictionary<string, object> { { "count", _count } });
        }

        /// <summary>
        /// まだ仲間がいないよ。 冒険やお散歩でパグを助けよう！ | No friends yet. Rescue pugs in Adventure or Stroll!
        /// </summary>
        public static string GARDEN_EMPTY()
        {
            return TextLocalizer.Get("GARDEN_EMPTY");
        }

        /// <summary>
        /// お休み | Rest
        /// </summary>
        public static string GARDEN_REST()
        {
            return TextLocalizer.Get("GARDEN_REST");
        }

        /// <summary>
        /// 仲間 {count}匹  骨 {bone} | Friends {count}  Bones {bone}
        /// </summary>
        /// <param name="_count"></param>
        /// <param name="_bone"></param>
        public static string GARDEN_STATUS(object _count, object _bone)
        {
            return TextLocalizer.GetNamed("GARDEN_STATUS", new Dictionary<string, object> { { "count", _count }, { "bone", _bone } });
        }

        /// <summary>
        /// 新しいギミック「{name}」 | New gimmick: {name}
        /// </summary>
        /// <param name="_name"></param>
        public static string GIMMICK_NEW_TITLE(object _name)
        {
            return TextLocalizer.GetNamed("GIMMICK_NEW_TITLE", new Dictionary<string, object> { { "name", _name } });
        }

        /// <summary>
        /// やめる | Cancel
        /// </summary>
        public static string HINT_CANCEL()
        {
            return TextLocalizer.Get("HINT_CANCEL");
        }

        /// <summary>
        /// ヒントを使う？ 骨 {bone} を使うよ | Use a hint? It costs {bone} bones
        /// </summary>
        /// <param name="_bone"></param>
        public static string HINT_CONFIRM(object _bone)
        {
            return TextLocalizer.GetNamed("HINT_CONFIRM", new Dictionary<string, object> { { "bone", _bone } });
        }

        /// <summary>
        /// 使う | Use
        /// </summary>
        public static string HINT_USE()
        {
            return TextLocalizer.Get("HINT_USE");
        }

        /// <summary>
        /// ヒントを出したよ。 のこりのヒントも使えるよ。 | A hint is revealed. You can use more hints too.
        /// </summary>
        public static string HINT_USED()
        {
            return TextLocalizer.Get("HINT_USED");
        }

        /// <summary>
        /// メニュー | Menu
        /// </summary>
        public static string MENU()
        {
            return TextLocalizer.Get("MENU");
        }

        /// <summary>
        /// いいえ | No
        /// </summary>
        public static string NO()
        {
            return TextLocalizer.Get("NO");
        }

        /// <summary>
        /// 今はできないよ | Not available right now
        /// </summary>
        public static string NOTICE_UNAVAILABLE()
        {
            return TextLocalizer.Get("NOTICE_UNAVAILABLE");
        }

        /// <summary>
        /// OK | OK
        /// </summary>
        public static string OK()
        {
            return TextLocalizer.Get("OK");
        }

        /// <summary>
        /// PERFECT! | PERFECT!
        /// </summary>
        public static string PERFECT()
        {
            return TextLocalizer.Get("PERFECT");
        }

        /// <summary>
        /// 遊ぶ | Play
        /// </summary>
        public static string PLAY()
        {
            return TextLocalizer.Get("PLAY");
        }

        /// <summary>
        /// 図鑑 | Pug Book
        /// </summary>
        public static string PUG_BOOK()
        {
            return TextLocalizer.Get("PUG_BOOK");
        }

        /// <summary>
        /// 持っている骨 {bone} | Bones: {bone}
        /// </summary>
        /// <param name="_bone"></param>
        public static string PUG_BOOK_BONES(object _bone)
        {
            return TextLocalizer.GetNamed("PUG_BOOK_BONES", new Dictionary<string, object> { { "bone", _bone } });
        }

        /// <summary>
        /// {name} を骨 {bone}個と交換して 仲間にする？ | Trade {bone} bones to adopt {name}?
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_bone"></param>
        public static string PUG_BUY_BODY(object _name, object _bone)
        {
            return TextLocalizer.GetNamed("PUG_BUY_BODY", new Dictionary<string, object> { { "name", _name }, { "bone", _bone } });
        }

        /// <summary>
        /// 骨があと {bone}個足りないよ。 お散歩や冒険で集めよう！ | You need {bone} more bones. Collect them on walks and adventures!
        /// </summary>
        /// <param name="_bone"></param>
        public static string PUG_BUY_SHORT(object _bone)
        {
            return TextLocalizer.GetNamed("PUG_BUY_SHORT", new Dictionary<string, object> { { "bone", _bone } });
        }

        /// <summary>
        /// 仲間にする？ | Adopt?
        /// </summary>
        public static string PUG_BUY_TITLE()
        {
            return TextLocalizer.Get("PUG_BUY_TITLE");
        }

        /// <summary>
        /// {name} はもう仲間だよ。 かわりに骨 {bone}個もらったよ！ | {name} is already your friend. You got {bone} bones instead!
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_bone"></param>
        public static string PUG_DUPLICATE_BODY(object _name, object _bone)
        {
            return TextLocalizer.GetNamed("PUG_DUPLICATE_BODY", new Dictionary<string, object> { { "name", _name }, { "bone", _bone } });
        }

        /// <summary>
        /// もう仲間だよ！ | Already a friend!
        /// </summary>
        public static string PUG_DUPLICATE_TITLE()
        {
            return TextLocalizer.Get("PUG_DUPLICATE_TITLE");
        }

        /// <summary>
        /// 一緒に行く | Come along
        /// </summary>
        public static string PUG_EQUIP()
        {
            return TextLocalizer.Get("PUG_EQUIP");
        }

        /// <summary>
        /// 一緒 | With you
        /// </summary>
        public static string PUG_EQUIPPED()
        {
            return TextLocalizer.Get("PUG_EQUIPPED");
        }

        /// <summary>
        /// 交換 | Exchange
        /// </summary>
        public static string PUG_EXCHANGE()
        {
            return TextLocalizer.Get("PUG_EXCHANGE");
        }

        /// <summary>
        /// {name} が仲間になった！ お庭で会えるよ。 | {name} joined you! Say hi in the Garden.
        /// </summary>
        /// <param name="_name"></param>
        public static string PUG_JOINED_BODY(object _name)
        {
            return TextLocalizer.GetNamed("PUG_JOINED_BODY", new Dictionary<string, object> { { "name", _name } });
        }

        /// <summary>
        /// 仲間が増えた！ | New friend!
        /// </summary>
        public static string PUG_JOINED_TITLE()
        {
            return TextLocalizer.Get("PUG_JOINED_TITLE");
        }

        /// <summary>
        /// お散歩で会えるかも | Meet on a stroll
        /// </summary>
        public static string PUG_NOT_FOR_SALE()
        {
            return TextLocalizer.Get("PUG_NOT_FOR_SALE");
        }

        /// <summary>
        /// 持ってる | Owned
        /// </summary>
        public static string PUG_OWNED()
        {
            return TextLocalizer.Get("PUG_OWNED");
        }

        /// <summary>
        /// 庭へ | To garden
        /// </summary>
        public static string PUG_TO_GARDEN()
        {
            return TextLocalizer.Get("PUG_TO_GARDEN");
        }

        /// <summary>
        /// 外す | Leave home
        /// </summary>
        public static string PUG_UNEQUIP()
        {
            return TextLocalizer.Get("PUG_UNEQUIP");
        }

        /// <summary>
        /// やり直す | Retry
        /// </summary>
        public static string RETRY()
        {
            return TextLocalizer.Get("RETRY");
        }

        /// <summary>
        /// 効果音 | Sound
        /// </summary>
        public static string SE()
        {
            return TextLocalizer.Get("SE");
        }

        /// <summary>
        /// 設定 | Settings
        /// </summary>
        public static string SETTINGS()
        {
            return TextLocalizer.Get("SETTINGS");
        }

        /// <summary>
        /// ステージ {stage} | Stage {stage}
        /// </summary>
        /// <param name="_stage"></param>
        public static string STAGE(object _stage)
        {
            return TextLocalizer.GetNamed("STAGE", new Dictionary<string, object> { { "stage", _stage } });
        }

        /// <summary>
        /// ステージ {first}〜{last} | Stages {first}-{last}
        /// </summary>
        /// <param name="_first"></param>
        /// <param name="_last"></param>
        public static string STAGE_RANGE(object _first, object _last)
        {
            return TextLocalizer.GetNamed("STAGE_RANGE", new Dictionary<string, object> { { "first", _first }, { "last", _last } });
        }

        /// <summary>
        /// ステージ {stage}　{theme} | Stage {stage}  {theme}
        /// </summary>
        /// <param name="_stage"></param>
        /// <param name="_theme"></param>
        public static string STAGE_THEME(object _stage, object _theme)
        {
            return TextLocalizer.GetNamed("STAGE_THEME", new Dictionary<string, object> { { "stage", _stage }, { "theme", _theme } });
        }

        /// <summary>
        /// スタート | Start
        /// </summary>
        public static string START()
        {
            return TextLocalizer.Get("START");
        }

        /// <summary>
        /// お散歩 | Stroll
        /// </summary>
        public static string STROLL()
        {
            return TextLocalizer.Get("STROLL");
        }

        /// <summary>
        /// 今日 あと {num}/{total}回 | {num}/{total} left today
        /// </summary>
        /// <param name="_num"></param>
        /// <param name="_total"></param>
        public static string STROLL_CHANCES(object _num, object _total)
        {
            return TextLocalizer.GetNamed("STROLL_CHANCES", new Dictionary<string, object> { { "num", _num }, { "total", _total } });
        }

        /// <summary>
        /// お散歩 {num}/{total} | Stroll {num}/{total}
        /// </summary>
        /// <param name="_num"></param>
        /// <param name="_total"></param>
        public static string STROLL_DAILY(object _num, object _total)
        {
            return TextLocalizer.GetNamed("STROLL_DAILY", new Dictionary<string, object> { { "num", _num }, { "total", _total } });
        }

        /// <summary>
        /// ほね +{bone} | Bones +{bone}
        /// </summary>
        /// <param name="_bone"></param>
        public static string SUMMARY_BONES(object _bone)
        {
            return TextLocalizer.GetNamed("SUMMARY_BONES", new Dictionary<string, object> { { "bone", _bone } });
        }

        /// <summary>
        /// もどらずクリア | No-rewind clear
        /// </summary>
        public static string SUMMARY_NOREWIND()
        {
            return TextLocalizer.Get("SUMMARY_NOREWIND");
        }

        /// <summary>
        /// もどってクリア | Rewound clear
        /// </summary>
        public static string SUMMARY_REWOUND()
        {
            return TextLocalizer.Get("SUMMARY_REWOUND");
        }

        /// <summary>
        /// ★ {star}/3 | ★ {star}/3
        /// </summary>
        /// <param name="_star"></param>
        public static string SUMMARY_STARS(object _star)
        {
            return TextLocalizer.GetNamed("SUMMARY_STARS", new Dictionary<string, object> { { "star", _star } });
        }

        /// <summary>THEME_NAME_N のキー（N は番号）</summary>
        private static readonly string[] keys_THEME_NAME = { "THEME_NAME_0", "THEME_NAME_1", "THEME_NAME_2", "THEME_NAME_3", "THEME_NAME_4", "THEME_NAME_5", "THEME_NAME_6", "THEME_NAME_7", "THEME_NAME_8", "THEME_NAME_9", "THEME_NAME_10", "THEME_NAME_11", "THEME_NAME_12", "THEME_NAME_13", "THEME_NAME_14", "THEME_NAME_15", "THEME_NAME_16", "THEME_NAME_17", "THEME_NAME_18", "THEME_NAME_19", "THEME_NAME_20", "THEME_NAME_21", "THEME_NAME_22", "THEME_NAME_23", "THEME_NAME_24", "THEME_NAME_25", "THEME_NAME_26", "THEME_NAME_27", "THEME_NAME_28", "THEME_NAME_29", "THEME_NAME_30", "THEME_NAME_31", "THEME_NAME_32", "THEME_NAME_33", "THEME_NAME_34", "THEME_NAME_35", "THEME_NAME_36", "THEME_NAME_37", "THEME_NAME_38", "THEME_NAME_39", "THEME_NAME_40" };

        /// <summary>
        /// THEME_NAME_N の文字（N は _index）
        /// </summary>
        /// <param name="_index"></param>
        public static string THEME_NAME(int _index)
        {
            return TextLocalizer.Get(_index >= 0 && _index < keys_THEME_NAME.Length ? keys_THEME_NAME[_index] : "THEME_NAME_" + _index);
        }

        /// <summary>THEME_STORY_N のキー（N は番号）</summary>
        private static readonly string[] keys_THEME_STORY = { "THEME_STORY_0", "THEME_STORY_1", "THEME_STORY_2", "THEME_STORY_3", "THEME_STORY_4", "THEME_STORY_5", "THEME_STORY_6", "THEME_STORY_7", "THEME_STORY_8", "THEME_STORY_9", "THEME_STORY_10", "THEME_STORY_11", "THEME_STORY_12", "THEME_STORY_13", "THEME_STORY_14", "THEME_STORY_15", "THEME_STORY_16", "THEME_STORY_17", "THEME_STORY_18", "THEME_STORY_19", "THEME_STORY_20", "THEME_STORY_21", "THEME_STORY_22", "THEME_STORY_23", "THEME_STORY_24", "THEME_STORY_25", "THEME_STORY_26", "THEME_STORY_27", "THEME_STORY_28", "THEME_STORY_29", "THEME_STORY_30", "THEME_STORY_31", "THEME_STORY_32", "THEME_STORY_33" };

        /// <summary>
        /// THEME_STORY_N の文字（N は _index）
        /// </summary>
        /// <param name="_index"></param>
        public static string THEME_STORY(int _index)
        {
            return TextLocalizer.Get(_index >= 0 && _index < keys_THEME_STORY.Length ? keys_THEME_STORY[_index] : "THEME_STORY_" + _index);
        }

        /// <summary>
        /// つなパグ | Pug Path
        /// </summary>
        public static string TITLE()
        {
            return TextLocalizer.Get("TITLE");
        }

        /// <summary>
        /// 橋が壊れた！ もう戻れないよ（やり直しはメニューから） | The bridge broke! No going back (retry from the menu)
        /// </summary>
        public static string TOAST_CRUMBLE()
        {
            return TextLocalizer.Get("TOAST_CRUMBLE");
        }

        /// <summary>
        /// 壊れた橋より前には戻れないよ | You can&apos;t go back past a broken bridge
        /// </summary>
        public static string TOAST_CRUMBLE_LOCKED()
        {
            return TextLocalizer.Get("TOAST_CRUMBLE_LOCKED");
        }

        /// <summary>
        /// カギを取ってから通ろう | Get the key first to open this gate
        /// </summary>
        public static string TOAST_GATE()
        {
            return TextLocalizer.Get("TOAST_GATE");
        }

        /// <summary>
        /// カギを手に入れた！ 同じ色の扉が開くよ | Got the key! The gate of the same colour will open
        /// </summary>
        public static string TOAST_KEY()
        {
            return TextLocalizer.Get("TOAST_KEY");
        }

        /// <summary>
        /// この向きからは通れないよ | You can&apos;t pass from this side
        /// </summary>
        public static string TOAST_ONEWAY()
        {
            return TextLocalizer.Get("TOAST_ONEWAY");
        }

        /// <summary>
        /// タイトルへ | Title
        /// </summary>
        public static string TO_TITLE()
        {
            return TextLocalizer.Get("TO_TITLE");
        }

        /// <summary>
        /// ？？？ | ???
        /// </summary>
        public static string UNKNOWN()
        {
            return TextLocalizer.Get("UNKNOWN");
        }

        /// <summary>
        /// ワンダフルボーナス！ 骨 +{bone} | Woof-derful Bonus! Bones +{bone}
        /// </summary>
        /// <param name="_bone"></param>
        public static string WANDERFUL_BONUS(object _bone)
        {
            return TextLocalizer.GetNamed("WANDERFUL_BONUS", new Dictionary<string, object> { { "bone", _bone } });
        }

        /// <summary>
        /// 戻らずパーフェクトクリア | Perfect clear without rewinding
        /// </summary>
        public static string WANDERFUL_DETAIL()
        {
            return TextLocalizer.Get("WANDERFUL_DETAIL");
        }

        /// <summary>
        /// はい | Yes
        /// </summary>
        public static string YES()
        {
            return TextLocalizer.Get("YES");
        }

    }
}
