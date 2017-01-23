<!--Title:Working with Marten's Metadata Columns-->

When Marten generates a table for document storage it now adds several _metadata_ columns
that further describe the document:

1. `mt_last_modified` - a timestamp of the last time the document was modified
1. `mt_dotnet_type` - The `FullName` property of the actual .Net type persisted. This is strictly for information and is not used by Marten itself.
1. `mt_version` - A sequential Guid designating the revision of the document. Marten uses
   this column in its optimistic concurrency checks
1. `mt_doc_type` - document name (_document <[linkto:documentation/documents/advanced/hierarchies;title=hierarchies]> only_)
1. `mt_deleted` - a boolean flag representing deleted state (_<[linkto:documentation/documents/advanced/soft_deletes;title=soft deletes]> only_)
1. `mt_deleted_at` - a timestamp of the time the document was deleted (_<[linkto:documentation/documents/advanced/soft_deletes;title=soft deletes]> only_)

## Finding the Metadata for a Document

You can find the metadata values for a given document object with the following mechanism
on `IDocumentStore.Advanced`:

<[sample:resolving_metadata]>

