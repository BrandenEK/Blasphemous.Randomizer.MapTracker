
namespace Blasphemous.Randomizer.MapTracker;

/// <summary>
/// Which locations have been collected at this cell
/// </summary>
public enum CollectionStatus
{
    /// <summary> Not present </summary>
    Untracked,
    /// <summary> Gray </summary>
    Finished,
    /// <summary> Red </summary>
    AllUnreachable,
    /// <summary> Orange </summary>
    SomeReachable,
    /// <summary> Green </summary>
    AllReachable,
    /// <summary> Purple </summary>
    HintedAllUnreachable,
    /// <summary> Cyan </summary>
    HintedSomeReachable,
    /// <summary> Blue </summary>
    HintedReachable,
}
