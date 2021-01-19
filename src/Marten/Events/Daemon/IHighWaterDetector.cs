using System;
using System.Threading;
using System.Threading.Tasks;

namespace Marten.Events.Daemon
{
    internal interface IHighWaterDetector
    {
        Task<HighWaterStatistics> DetectInSafeZone(DateTimeOffset safeTimestamp, CancellationToken token);
        Task<HighWaterStatistics> Detect(CancellationToken token);
    }
}