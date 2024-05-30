using System.Collections.Concurrent;
using Manager.Shared.Entities;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// Defines a service that can handle video.
/// It should handle its video player surfaces.
/// </summary>
public interface IVideoBackendService : IBackendService
{
}