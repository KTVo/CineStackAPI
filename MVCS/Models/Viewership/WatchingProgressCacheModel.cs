namespace CineStackAPI.MVCS.Models.Viewership;
using System;
public sealed class WatchingProgressCacheModel
{
    public int PositionSeconds { get; set; }
    public int DurationSeconds { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime LastDatabaseSave { get; set; }

}