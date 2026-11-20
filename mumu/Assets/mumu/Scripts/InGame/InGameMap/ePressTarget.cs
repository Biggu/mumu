namespace mumu
{
    /// <summary>
    /// 押した場所の種類
    /// </summary>
    public enum ePressTarget
    {
        /// <summary>背景（マップ移動）</summary>
        Background,
        /// <summary>経路外のブロック（マップ移動）</summary>
        Block,
        /// <summary>経路の先端付近の経路外ブロック（押すと経路を伸ばすドラッグを始める）</summary>
        NearHead,
        /// <summary>経路上（経路ドラッグ）</summary>
        PathHead,
    }
}
