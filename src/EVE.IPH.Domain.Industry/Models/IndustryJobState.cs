namespace EVE.IPH.Domain.Industry.Models;

public enum IndustryJobState
{
    Pending = 0,
    InProgress = 1,
    Complete = 2,
    Completed = 3,
    Cancelled = 4,
    Paused = 5,
    Ready = 6,
    Reverted = 7,
    Unknown = 8,
}