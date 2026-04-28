/// <summary>
///
/// </summary>
namespace mumu
{
    public enum eGameMode
    {
        Easy,
        Normal,
        Hard,
        VeryHard,
        Extreme,
    }

    public enum eBlockType
    {
        Empty = 0,
        Walkable,
        Obstruct,
        Start,
        Goal,
    }

    public enum eBlockSubType
    {
        None,
        Waypoint,
    }
}