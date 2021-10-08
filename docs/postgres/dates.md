# Working with Dates

SQL Server has functions for modifying a date, such as `DATEADD(…)`

Postgres has an interval type which allows you to add/remove in a much more semantic manner.

For example, in SQL Server to add 5 days to the current date we might do:

```sql
SELECT DATEADD(day, 5, getutcdate())
```

In Postgres we would achieve the same like so:

```sql
SELECT now() + ‘5 days’::interval;
```
