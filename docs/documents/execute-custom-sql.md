# Execute custom SQL in session

Use `QueueSqlCommand(string sql, params object[] parameterValues)`  method to register and execute any custom/arbitrary SQL commands with the underlying unit of work, as part of the batched commands within `IDocumentSession`. "?" placeholders can be used to denote parameter values.

<!-- snippet: sample_QueueSqlCommand -->
<a id='snippet-sample_queuesqlcommand'></a>
```cs
theSession.QueueSqlCommand("insert into names (name) values ('Jeremy')");
theSession.QueueSqlCommand("insert into names (name) values ('Babu')");
theSession.Store(Target.Random());
theSession.QueueSqlCommand("insert into names (name) values ('Oskar')");
theSession.Store(Target.Random());
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/CoreTests/executing_arbitrary_sql_as_part_of_transaction.cs#L34-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_queuesqlcommand' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
