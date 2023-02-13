using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Marten.Exceptions;
using Npgsql;

namespace Marten.Services;

[Obsolete("Being replaced by IConnectionLifetime")]
internal class TransactionState: IDisposable, IAsyncDisposable
{
    private readonly int _commandTimeout;
    private readonly IsolationLevel _isolationLevel;
    private readonly CommandRunnerMode _mode;
    private readonly bool _ownsConnection;

    public TransactionState(CommandRunnerMode mode, IsolationLevel isolationLevel, int? commandTimeout,
        NpgsqlConnection connection, bool ownsConnection, NpgsqlTransaction transaction = null)
    {
        _mode = mode;
        _isolationLevel = isolationLevel;
        _ownsConnection = ownsConnection;
        Transaction = transaction;
        Connection = connection;
        _commandTimeout = commandTimeout ?? Connection.CommandTimeout;
    }

    public TransactionState(IConnectionFactory factory, CommandRunnerMode mode, IsolationLevel isolationLevel,
        int? commandTimeout, bool ownsConnection)
    {
        _mode = mode;
        _isolationLevel = isolationLevel;
        _ownsConnection = ownsConnection;
        Connection = factory.Create();
        _commandTimeout = commandTimeout ?? Connection.CommandTimeout;
    }

    public bool IsOpen => Connection.State != ConnectionState.Closed;

    public NpgsqlTransaction Transaction { get; private set; }

    public NpgsqlConnection Connection { get; }

    public async ValueTask DisposeAsync()
    {
        if (_mode != CommandRunnerMode.External)
        {
            if (Transaction != null)
            {
                await Transaction.DisposeAsync().ConfigureAwait(false);

                Transaction = null;
            }
        }

        if (_ownsConnection)
        {
            await Connection.CloseAsync().ConfigureAwait(false);

            await Connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_mode != CommandRunnerMode.External)
        {
            Transaction?.Dispose();
            Transaction = null;
        }

        if (_ownsConnection)
        {
            Connection.Close();
            Connection.Dispose();
        }
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        Connection.Open();
    }

    public Task OpenAsync(CancellationToken token)
    {
        if (IsOpen)
        {
            return Task.CompletedTask;
        }

        return Connection.OpenAsync(token);
    }

    public void BeginTransaction()
    {
        if (Transaction != null || _mode == CommandRunnerMode.External)
        {
            return;
        }

        if (_mode == CommandRunnerMode.Transactional || _mode == CommandRunnerMode.ReadOnly)
        {
            Transaction = Connection.BeginTransaction(_isolationLevel);
        }

        if (_mode == CommandRunnerMode.ReadOnly)
        {
            using (var cmd = new NpgsqlCommand("SET TRANSACTION READ ONLY;"))
            {
                Apply(cmd);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void Apply(NpgsqlCommand cmd)
    {
        cmd.Connection = Connection;
        if (Transaction != null)
        {
            cmd.Transaction = Transaction;
        }

        cmd.CommandTimeout = _commandTimeout;
    }

    public void Commit()
    {
        if (_mode != CommandRunnerMode.External)
        {
            Transaction?.Commit();
            Transaction?.Dispose();
            Transaction = null;
        }

        if (_ownsConnection)
        {
            Connection.Close();
        }
    }

    public async Task CommitAsync(CancellationToken token)
    {
        if (Transaction != null && _mode != CommandRunnerMode.External)
        {
            await Transaction.CommitAsync(token).ConfigureAwait(false);
            await Transaction.DisposeAsync().ConfigureAwait(false);
            Transaction = null;
        }

        if (_ownsConnection)
        {
            await Connection.CloseAsync().ConfigureAwait(false);
        }
    }

    public void Rollback()
    {
        if (Transaction != null && _mode != CommandRunnerMode.External)
        {
            try
            {
                Transaction.Rollback();
                Transaction.Dispose();
                Transaction = null;
            }
            catch (Exception e) when (e is ObjectDisposedException || e is InvalidOperationException)
            {
                // Transaction already disposed or completed
            }
            catch (Exception e)
            {
                throw new RollbackException(e);
            }
            finally
            {
                if (_ownsConnection)
                {
                    Connection.Close();
                }
            }
        }
    }

    public async Task RollbackAsync(CancellationToken token)
    {
        if (Transaction != null && _mode != CommandRunnerMode.External)
        {
            try
            {
                await Transaction.RollbackAsync(token).ConfigureAwait(false);
                await Transaction.DisposeAsync().ConfigureAwait(false);
                Transaction = null;
            }
            catch (Exception e) when (e is ObjectDisposedException || e is InvalidOperationException)
            {
                // Transaction already disposed or completed
            }
            catch (Exception e)
            {
                throw new RollbackException(e);
            }
            finally
            {
                if (_ownsConnection)
                {
                    await Connection.CloseAsync().ConfigureAwait(false);
                }
            }
        }
    }

    public NpgsqlCommand CreateCommand()
    {
        var cmd = Connection.CreateCommand();
        if (Transaction != null)
        {
            cmd.Transaction = Transaction;
        }

        return cmd;
    }
}
