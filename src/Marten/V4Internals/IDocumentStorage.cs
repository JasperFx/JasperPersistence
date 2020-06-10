using System;
using Marten.Linq;
using Marten.Linq.Fields;
using Marten.Schema;
using Marten.Storage;
using Remotion.Linq;

namespace Marten.V4Internals
{
    public interface IDocumentStorage<T>
    {
        IFieldMapping Fields { get; }

        Type IdType { get; }
        Guid? VersionFor(T document, IMartenSession session);

        void Store(IMartenSession session, T document);
        void Store(IMartenSession session, T document, Guid? version);

        void Eject(IMartenSession session, T document);

        IStorageOperation Update(T document, IMartenSession session, ITenant tenant);
        IStorageOperation Insert(T document, ITenant tenant);
        IStorageOperation Upsert(T document, IMartenSession session, ITenant tenant);
        IStorageOperation Override(T document, IMartenSession session, ITenant tenant);


        IStorageOperation DeleteForDocument(T document);
        IStorageOperation DeleteForWhere(IWhereFragment where);


        // These actually need to be here, because there's some branching
        // logic we can eliminate
        IWhereFragment FilterDocuments(QueryModel model, IWhereFragment query);

        IWhereFragment DefaultWhereFragment();
    }

    public abstract class DirtyTrackingDocumentStorage<T, TId>: DocumentStorage<T, TId>
    {
        public DirtyTrackingDocumentStorage(IQueryableDocument document): base(document)
        {
        }
    }
}
