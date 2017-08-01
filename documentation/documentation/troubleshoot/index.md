<!--Title:FAQ & Troubleshooting-->

**What are the different document sessions?**

| From `IDocumentStore` | Characteristics | Use |
|-----------------------|-----------------------|---|
| `OpenSession`         | **Defaults** to session that tracks objects by their identity, `DocumentTracking.IdentityOnly`, with isolation level of *Read Committed*. | Reading & writing data. Objects within a session are cached by their identity. Updates to objects are explicitly controlled through session operations (`IDocumentSession.Update`, `IDocumentSession.Store`). With the defaults, incurs lower overhead than `DirtyTrackedSession`. |
| `LightweightSession`  | No change tracking, `DocumentTracking.None`, with the default isolation level of *Read Committed*. | Reading & writing data. No caching of objects is done within a session, e.g. repeated loads using the same document identity yield separate objects, each hydrated from the database. In case of updates to such objects, the last object to be stored will overwrite any pending changes from previously stored objects of the same identity. Can incur lower overhead than tracked sessions. |
| `DirtyTrackedSession` | Track all changes to objects, `DocumentTracking.DirtyTracking`, with the default isolation level of *Read Committed*. | Reading & writing data. Tracks all changes to objects loaded through a session. Upon save (`IDocumentSession.SaveChanges`), Marten updates the changed objects without requiring explicit calls to `IDocumentSession.Update` or `IDocumentSession.Store`. Incurs the largest overhead of tracked sessions.  |
| `QuerySession`        | No identity mapping with the default isolation level of *Read Committed*.   | Reading data, i.e. no insert or update operations are exposed. |

**How do I serialize to Camel case?**

While it's possible to accommodate any serialization schemes by implementing a custom `ISerializer`, Marten's built-in serializer (Json.Net) can be set to serialize to Camel case through `StoreOptions.UseDefaultSerialization`:

<[sample:sample-serialize-to-camelcase]> 	

**How do I disable PLV8?**

If you don't want PLV8 (required for Javascript transformations) related items in your database schema, you can disable PLV8 alltogether by setting `StoreOptions.PLV8Enabled` to false.

