namespace mumu
{
    /// <summary>
    /// 共通の定数
    /// </summary>
    public static class CommonDefine
    {
        /// <summary>★・王冠・ほねの跳ねる大きさ</summary>
        public const float StarPunchAmount = 0.5f;
        /// <summary>★・王冠・ほねの跳ねる時間（秒）</summary>
        public const float StarPunchDuration = 0.3f;
        /// <summary>★を1つずつ点けるまでの間隔（秒）</summary>
        public const float ClearStarInterval = 0.15f;
        /// <summary>クリア時の紙吹雪の数</summary>
        public const int ConfettiCount = 60;
        /// <summary>クリア時に寄った画面に映るマップの幅（2匹のパグのアップ）</summary>
        public const float ClearZoomWidth = 150f;
        /// <summary>クリア時に寄る時間（秒）</summary>
        public const float ClearZoomDuration = 0.8f;
        /// <summary>クリア時の寄る位置（上面の中心から2匹の体の中心まで）</summary>
        public const float ClearZoomOffsetY = 18f;
        /// <summary>クリア演出の後、次のステージへ進むまでの時間（秒）</summary>
        public const float NextStageDelay = 2f;
        /// <summary>ステージ札をめくる時間（秒）</summary>
        public const float StampFlipDuration = 0.25f;
        /// <summary>ステージ札が跳ねる大きさ</summary>
        public const float StampPunchAmount = 0.6f;
        /// <summary>ステージ札が跳ねる時間（秒）</summary>
        public const float StampPunchDuration = 0.3f;
        /// <summary>PERFECT の時に出すハートの数</summary>
        public const int PerfectHeartCount = 16;
        /// <summary>PERFECT のハートが飛ぶ距離</summary>
        public const float PerfectHeartRadius = 260f;
        /// <summary>報酬ダイアログを出すまでの時間（秒）</summary>
        public const float RewardDialogDelay = 0.6f;
        /// <summary>もどらずクリアのボーナス（ほね、初めて達成した時だけ）</summary>
        public const int MissionBonusBones = 10;
        /// <summary>ヒント1回分のほね（今回が何回目かを掛けた分がかかる）</summary>
        public const int HintCost = 5;
        /// <summary>大きな★の大きさ（★3つを1つにまとめる時）</summary>
        public const float BigStarSize = 96f;
        /// <summary>このティア以上のパグが仲間になった時は、ファンファーレ</summary>
        public const int RareJoinTier = 4;
    }
}
