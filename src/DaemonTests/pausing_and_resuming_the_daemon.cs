using System.Threading.Tasks;
using Confluent.Kafka;
using DaemonTests.TestingSupport;
using JasperFx.Core;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Marten.Testing.Harness;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace DaemonTests;

public class pausing_and_resuming_the_daemon
{
    [Fact]
    public async Task stop_and_resume_from_the_host_extensions()
    {
        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMarten(opts =>
                {
                    opts.Connection(ConnectionSource.ConnectionString);
                    opts.DatabaseSchemaName = "coordinator";

                    opts.Projections.Add<TestingSupport.TripProjection>(ProjectionLifecycle.Async);
                }).AddAsyncDaemon(DaemonMode.Solo);
            }).StartAsync();

        await host.PauseAllDaemonsAsync();

        await host.ResumeAllDaemonsAsync();

        await using var session = host.DocumentStore().LightweightSession();
        var id = session.Events.StartStream<TestingSupport.TripProjection>(new TripStarted()).Id;

        await session.SaveChangesAsync();

        await host.WaitForNonStaleProjectionDataAsync(15.Seconds());

        var trip = await session.LoadAsync<Trip>(id);
        trip.ShouldNotBeNull();
    }

}
