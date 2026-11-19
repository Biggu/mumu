using UnityEngine.Localization.Settings;

namespace mumu
{
    /// <summary>
    /// コードから使うローカライズ文字（UI テーブル）
    /// </summary>
    public static class TextLocalizer
    {
        public const string TableName = "UI";

        /// <summary>
        /// キーの文字（{0} などは _args で埋める、見つからなければキーのまま）
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_args"></param>
        public static string Get(string _key, params object[] _args)
        {
            string text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, _key);
            if (string.IsNullOrEmpty(text))
            {
                text = _key;
            }
            return _args != null && _args.Length > 0 ? string.Format(text, _args) : text;
        }

        /// <summary>
        /// 今の言語が日本語か
        /// </summary>
        public static bool IsJapanese()
        {
            var locale = LocalizationSettings.SelectedLocale;
            return locale == null || locale.Identifier.Code.StartsWith("ja");
        }
    }
}
