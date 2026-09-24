namespace Kmila.Shared.Models;
/// <summary>
/// Represents the different time scales that can be used in the application.
/// </summary>
public enum TimeScale : int
{
    /// <summary>
    /// Microseconds
    /// </summary>
    US,
    /// <summary>
    /// Miliseconds
    /// </summary>
    MS,
    /// <summary>
    /// Nanoseconds
    /// </summary>
    NS,
    /// <summary>
    /// Seconds
    /// </summary>
    S,
    /// <summary>
    /// Minutes
    /// </summary>
    M,
    /// <summary>
    /// Hours
    /// </summary>
    H
}