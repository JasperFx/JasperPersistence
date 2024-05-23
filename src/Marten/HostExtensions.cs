using System;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events;
using Marten.Events.Daemon.Coordination;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Marten;

public static class HostExtensions
{
    /// <summary>
    /// Testing helper to pause all projection daemons in the system and completely
    /// disable the daemon projection assignments
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static Task PauseAllDaemonsAsync(this IHost host)
    {
        var coordinator =  host.Services.GetRequiredService<IProjectionCoordinator>();
        return coordinator.PauseAsync();
    }

    /// <summary>
    /// Testing helper to resume all projection daemons in the system and restart
    /// the daemon projection assignments
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static Task ResumeAllDaemonsAsync(this IHost host)
    {
        var coordinator =  host.Services.GetRequiredService<IProjectionCoordinator>();
        return coordinator.ResumeAsync();
    }

    /// <summary>
    /// Retrieve the Marten document store for this IHost
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static IDocumentStore DocumentStore(this IHost host)
    {
        return host.Services.GetRequiredService<IDocumentStore>();
    }

    /// <summary>
    /// Clean off all Marten data in the default DocumentStore for this host
    /// </summary>
    /// <param name="host"></param>
    public static async Task CleanAllMartenDataAsync(this IHost host)
    {
        var store = host.DocumentStore();
        await store.Advanced.Clean.DeleteAllDocumentsAsync(CancellationToken.None).ConfigureAwait(false);
        await store.Advanced.Clean.DeleteAllEventDataAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Call DocumentStore.ResetAllData() on the document store in this host
    /// </summary>
    /// <param name="host"></param>
    public static Task ResetAllMartenDataAsync(this IHost host)
    {
        var store = host.DocumentStore();
        return store.Advanced.ResetAllData(CancellationToken.None);
    }
}
