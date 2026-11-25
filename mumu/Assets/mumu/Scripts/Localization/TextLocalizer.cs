using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace mumu
{
    /// <summary>
    /// コードから使うローカライズ文字（UI テーブル）
    /// </summary>
    public static class TextLocalizer
    {
        public const string TableName = "UI";

        /// <summary>引数なしの文字（言語が変わったら消す）</summary>
        private static readonly Dictionary<string, string> plainCache = new Dictionary<string, string>();
        /// <summary>日本語か（言語が変わったら消す）</summary>
        private static bool? isJapaneseCache = null;

        /// <summary>
        /// 言語が変わった時に保存を消す
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void subscribe()
        {
            plainCache.Clear();
            isJapaneseCache = null;
            LocalizationSettings.SelectedLocaleChanged += onLocaleChanged;
        }

        /// <summary>
        /// 言語が変わった時の保存の消去
        /// </summary>
        /// <param name="_locale"></param>
        private static void onLocaleChanged(Locale _locale)
        {
            plainCache.Clear();
            isJapaneseCache = null;
        }

        /// <summary>
        /// UI テーブルを先に読み込む（起動時に1回。読み込む前に文字を取るとキーのまま出る）
        /// </summary>
        public static async UniTask PreloadAsync()
        {
            await LocalizationSettings.InitializationOperation;
            await LocalizationSettings.StringDatabase.GetTableAsync(TableName);
        }

        /// <summary>
        /// キーの文字（{0} は _args の番号、見つからなければキーのまま）
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_args"></param>
        public static string Get(string _key, params object[] _args)
        {
            bool isPlain = _args == null || _args.Length == 0;
            if (isPlain && plainCache.TryGetValue(_key, out string cached))
            {
                return cached;
            }

            string text = find(_key, isPlain ? null : _args);
            if (string.IsNullOrEmpty(text))
            {
                return _key;
            }
            if (isPlain)
            {
                plainCache[_key] = text;
            }
            return text;
        }

        /// <summary>
        /// キーの文字（{name} などは名前で埋める、見つからなければキーのまま）
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_args"></param>
        public static string GetNamed(string _key, IDictionary<string, object> _args)
        {
            string text = find(_key, new object[] { _args });
            return string.IsNullOrEmpty(text) ? _key : text;
        }

        /// <summary>
        /// テーブルから文字を取り、Smart String の埋め込みをする（読み込み前は null）
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_args"></param>
        private static string find(string _key, object[] _args)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString(TableName, _key, _args);
        }

        /// <summary>
        /// 今の言語が日本語か（言語が決まるまでは保存しない）
        /// </summary>
        public static bool IsJapanese()
        {
            if (isJapaneseCache.HasValue)
            {
                return isJapaneseCache.Value;
            }

            var locale = LocalizationSettings.SelectedLocale;
            if (locale == null)
            {
                return true;
            }
            isJapaneseCache = locale.Identifier.Code.StartsWith("ja");
            return isJapaneseCache.Value;
        }
    }
}
