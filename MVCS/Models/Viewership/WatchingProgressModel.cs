namespace CineStackAPI.MVCS.Models.Viewership;

using System.ComponentModel.DataAnnotations.Schema;

[Table("WatchingProgress")]
public sealed class WatchingProgressModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid ContentId { get; set; }

    public int PositionSeconds { get; set; }
    public int DurationSeconds { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime LastWatchedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
