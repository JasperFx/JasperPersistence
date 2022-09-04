using System;
using System.Threading.Tasks;
using Marten;
using Marten.Metadata;
using Marten.Schema;
using Marten.Storage;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace DocumentDbTests.Metadata;

public class metadata_marker_interfaces : IntegrationContext
{
    public metadata_marker_interfaces(DefaultStoreFixture fixture) : base(fixture)
    {
    }

    internal static void manually_wire_soft_deleted_metadata()
    {
        #region sample_manually_wire_soft_deleted_metadata

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection("some connection string");

            opts.Schema.For<ASoftDeletedDoc>().Metadata(m =>
            {
                m.IsSoftDeleted.MapTo(x => x.IsDeleted);
                m.SoftDeletedAt.MapTo(x => x.DeletedWhen);
            });
        });

        #endregion
    }

    [Fact]
    public void implementing_ITenanted_makes_a_document_conjoined_tenanted()
    {
        var mapping = theStore.Options.Storage.MappingFor(typeof(MyTenantedDoc));
        mapping.TenancyStyle.ShouldBe(TenancyStyle.Conjoined);
        mapping.Metadata.TenantId.Enabled.ShouldBeTrue();
        mapping.Metadata.TenantId.Member.Name.ShouldBe(nameof(ITenanted.TenantId));
    }

    [Fact]
    public void implementing_IVersioned_makes_a_document_versioned()
    {
        var mapping = theStore.Options.Storage.MappingFor(typeof(MyVersionedDoc));
        mapping.UseOptimisticConcurrency.ShouldBeTrue();
        mapping.Metadata.Version.Member.Name.ShouldBe(nameof(IVersioned.Version));
        mapping.Metadata.Version.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task using_IVersioned_end_to_end()
    {
        var doc1 = new MyVersionedDoc();
        theSession.Store(doc1);
        await theSession.SaveChangesAsync();

        doc1.Version.ShouldNotBe(Guid.Empty);

        using var query = theStore.QuerySession();
        var doc2 = await query.LoadAsync<MyVersionedDoc>(doc1.Id);
        doc2.Version.ShouldBe(doc1.Version);
    }

    [Fact]
    public void implementing_ISoftDeleted_makes_a_document_soft_deleted()
    {
        var mapping = theStore.Options.Storage.MappingFor(typeof(MySoftDeletedDoc));
        mapping.DeleteStyle.ShouldBe(DeleteStyle.SoftDelete);

        mapping.Metadata.IsSoftDeleted.Member.Name.ShouldBe(nameof(ISoftDeleted.Deleted));
        mapping.Metadata.IsSoftDeleted.Enabled.ShouldBeTrue();
        mapping.Metadata.SoftDeletedAt.Member.Name.ShouldBe(nameof(ISoftDeleted.DeletedAt));
        mapping.Metadata.SoftDeletedAt.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void implementing_ITracked_makes_a_document_have_metadata()
    {
        var mapping = theStore.Options.Storage.MappingFor(typeof(MyTrackedDoc));

        mapping.Metadata.CorrelationId.Enabled.ShouldBeTrue();
        mapping.Metadata.CorrelationId.Member.Name.ShouldBe(nameof(ITracked.CorrelationId));

        mapping.Metadata.CausationId.Enabled.ShouldBeTrue();
        mapping.Metadata.CausationId.Member.Name.ShouldBe(nameof(ITracked.CausationId));

        mapping.Metadata.LastModifiedBy.Enabled.ShouldBeTrue();
        mapping.Metadata.LastModifiedBy.Member.Name.ShouldBe(nameof(ITracked.LastModifiedBy));
    }

    [Fact]
    public async Task using_ITracked_end_to_end()
    {
        theSession.CausationId = "cause";
        theSession.CorrelationId = "correlation";
        theSession.LastModifiedBy = "me";

        var doc = new MyTrackedDoc();
        theSession.Store(doc);
        await theSession.SaveChangesAsync();

        doc.CausationId.ShouldBe("cause");
        doc.CorrelationId.ShouldBe("correlation");
        doc.LastModifiedBy.ShouldBe("me");

        using var query = theStore.QuerySession();

        var doc2 = await query.LoadAsync<MyTrackedDoc>(doc.Id);

        doc2.CausationId.ShouldBe("cause");
        doc2.CorrelationId.ShouldBe("correlation");
        doc2.LastModifiedBy.ShouldBe("me");
    }
}

#region sample_MyVersionedDoc

public class MyVersionedDoc: IVersioned
{
    public Guid Id { get; set; }
    public Guid Version { get; set; }
}

#endregion

#region sample_implementing_ISoftDeleted

public class MySoftDeletedDoc: ISoftDeleted
{
    // Always have to have an identity of some sort
    public Guid Id { get; set; }

    // Is the document deleted? From ISoftDeleted
    public bool Deleted { get; set; }

    // When was the document deleted? From ISoftDeleted
    public DateTimeOffset? DeletedAt { get; set; }
}

#endregion

#region sample_ASoftDeletedDoc

public class ASoftDeletedDoc
{
    // Always have to have an identity of some sort
    public Guid Id { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedWhen { get; set; }
}

#endregion



#region sample_MyTrackedDoc

public class MyTrackedDoc: ITracked
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; }
    public string CausationId { get; set; }
    public string LastModifiedBy { get; set; }
}

#endregion

public class MyTenantedDoc: ITenanted
{
    public Guid Id { get; set; }
    public string TenantId { get; set; }
}