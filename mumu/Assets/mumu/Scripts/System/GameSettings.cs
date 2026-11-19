namespace mumu
{
    /// <summary>
    /// ユーザー設定（SaveDataManager に保存）
    /// </summary>
    public static class GameSettings
    {
        /// <summary>効果音を鳴らすか</summary>
        public static bool IsSeEnabled
        {
            get => SaveDataManager.Data.isSeEnabled;
            set
            {
                SaveDataManager.Data.isSeEnabled = value;
                SaveDataManager.Save();
            }
        }

        /// <summary>BGM を鳴らすか</summary>
        public static bool IsBgmEnabled
        {
            get => SaveDataManager.Data.isBgmEnabled;
            set
            {
                SaveDataManager.Data.isBgmEnabled = value;
                SaveDataManager.Save();
            }
        }
    }
}