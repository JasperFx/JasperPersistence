using System;
using System.Threading;
using System.Threading.Tasks;
using Marten.Services;
using Npgsql;
using Weasel.Postgresql;

namespace Marten.Events.Daemon.HighWater
{
    internal class HighWaterDetector: IHighWaterDetector
    {
        private readonly ISingleQueryRunner _runner;
        private readonly NpgsqlCommand _updateStatus;
        private readonly NpgsqlParameter _newSeq;
        private readonly GapDetector _gapDetector;
        private readonly HighWaterStatisticsDetector _highWaterStatisticsDetector;

        public HighWaterDetector(ISingleQueryRunner runner, EventGraph graph)
        {
            _runner = runner;
            _gapDetector = new GapDetector(graph);
            _highWaterStatisticsDetector = new HighWaterStatisticsDetector(graph);

            _updateStatus =
                new NpgsqlCommand($"select {graph.DatabaseSchemaName}.mt_mark_event_progression('{ShardState.HighWaterMark}', :seq);");
            _newSeq = _updateStatus.AddNamedParameter("seq", 0L);

        }

        public async Task<HighWaterStatistics> DetectInSafeZone(CancellationToken token)
        {
            var statistics = await loadCurrentStatistics(token).ConfigureAwait(false);

            // Skip gap and find next safe sequence
            _gapDetector.Start = statistics.SafeStartMark + 1;

            var safeSequence = await _runner.Query(_gapDetector, token).ConfigureAwait(false);
            if (safeSequence.HasValue)
            {
                statistics.SafeStartMark = safeSequence.Value;
            }

            await calculateHighWaterMark(statistics, token).ConfigureAwait(false);

            return statistics;
        }


        public async Task<HighWaterStatistics> Detect(CancellationToken token)
        {
            var statistics = await loadCurrentStatistics(token).ConfigureAwait(false);


            await calculateHighWaterMark(statistics, token).ConfigureAwait(false);

            return statistics;
        }

        private async Task calculateHighWaterMark(HighWaterStatistics statistics, CancellationToken token)
        {
            // If the last high water mark is the same as the highest number
            // assigned from the sequence, then the high water mark cannot
            // have changed
            if (statistics.LastMark == statistics.HighestSequence)
            {
                statistics.CurrentMark = statistics.LastMark;
            }
            else if (statistics.HighestSequence == 0)
            {
                statistics.CurrentMark = statistics.LastMark = 0;
            }
            else
            {
                statistics.CurrentMark = await findCurrentMark(statistics, token).ConfigureAwait(false);
            }

            if (statistics.HasChanged)
            {
                _newSeq.Value = statistics.CurrentMark;
                await _runner.SingleCommit(_updateStatus, token).ConfigureAwait(false);

                if (!statistics.LastUpdated.HasValue)
                {
                    var current = await loadCurrentStatistics(token).ConfigureAwait(false);
                    statistics.LastUpdated = current.LastUpdated;
                }
            }
        }

        private async Task<HighWaterStatistics> loadCurrentStatistics(CancellationToken token)
        {
            return await _runner.Query(_highWaterStatisticsDetector, token).ConfigureAwait(false);
        }

        private async Task<long> findCurrentMark(HighWaterStatistics statistics, CancellationToken token)
        {
            // look for the current mark
            _gapDetector.Start = statistics.SafeStartMark;
            var current = await _runner.Query(_gapDetector, token).ConfigureAwait(false);

            if (current.HasValue) return current.Value;

            // This happens when the agent is restarted with persisted
            // state, and has no previous current mark.
            if (statistics.CurrentMark == 0 && statistics.LastMark > 0)
            {
                return statistics.LastMark;
            }

            return statistics.CurrentMark;
        }

    }
}
