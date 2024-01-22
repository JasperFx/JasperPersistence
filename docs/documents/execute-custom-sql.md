# Execute custom SQL in session

Use `QueueSqlCommand(string sql, params object[] parameterValues)` method to register and execute any custom/arbitrary SQL commands with the underlying unit of work, as part of the batched commands within `IDocumentSession`. 

`?` placeholders can be used to denote parameter values. Postgres [type casts `::`](https://www.postgresql.org/docs/15/sql-expressions.html#SQL-SYNTAX-TYPE-CASTS) can be applied to the parameter if needed.

<!-- snippet: sample_QueueSqlCommand -->
<a id='snippet-sample_queuesqlcommand'></a>
```cs
TheSession.QueueSqlCommand("insert into names (name) values ('Jeremy')");
TheSession.QueueSqlCommand("insert into names (name) values ('Babu')");
TheSession.Store(Target.Random());
TheSession.QueueSqlCommand("insert into names (name) values ('Oskar')");
TheSession.Store(Target.Random());
var json = "{ \"answer\": 42 }";
TheSession.QueueSqlCommand("insert into data (raw_value) values (?::jsonb)", json);
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/CoreTests/executing_arbitrary_sql_as_part_of_transaction.cs#L39-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_queuesqlcommand' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
