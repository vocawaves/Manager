namespace Manager.Shared.Entities;

/// <summary>
/// A standardized wrapper for selectable media streams.
/// </summary>
public class SelectableMediaStream
{
    /// <summary>
    /// Identifier of the media stream. Defined by the backend.
    /// </summary>
    public required string Identifier { get; init; }
    /// <summary>
    /// Name of the media stream. Displayed to the user.
    /// </summary>
    public required string Name { get; init; }
}