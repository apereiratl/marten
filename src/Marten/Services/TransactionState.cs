using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Marten.Services
{
    public class TransactionState: IDisposable
    {
        private readonly CommandRunnerMode _mode;
        private readonly IsolationLevel _isolationLevel;
        private readonly int _commandTimeout;
        private readonly bool _ownsConnection;

        public TransactionState(CommandRunnerMode mode, IsolationLevel isolationLevel, int? commandTimeout, SqlConnection connection, bool ownsConnection, SqlTransaction transaction = null)
        {
            _mode = mode;
            _isolationLevel = isolationLevel;
            _ownsConnection = ownsConnection;
            Transaction = transaction;
            Connection = connection;
            _commandTimeout = commandTimeout.GetValueOrDefault();
        }

        public TransactionState(IConnectionFactory factory, CommandRunnerMode mode, IsolationLevel isolationLevel, int? commandTimeout, bool ownsConnection)
        {
            _mode = mode;
            _isolationLevel = isolationLevel;
            _ownsConnection = ownsConnection;
            Connection = factory.Create();
            _commandTimeout = commandTimeout.GetValueOrDefault();
        }

        public bool IsOpen => Connection.State != ConnectionState.Closed;

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

        public IMartenSessionLogger Logger { get; set; } = NulloMartenLogger.Flyweight;

        public void BeginTransaction()
        {
            if (Transaction != null || _mode == CommandRunnerMode.External)
                return;

            if (_mode == CommandRunnerMode.Transactional || _mode == CommandRunnerMode.ReadOnly)
            {
                Transaction = Connection.BeginTransaction(_isolationLevel);
            }

            if (_mode == CommandRunnerMode.ReadOnly)
            {
                using (var cmd = new SqlCommand("SET TRANSACTION READ ONLY;"))
                {
                    Apply(cmd);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Apply(SqlCommand cmd)
        {
            cmd.Connection = Connection;
            if (Transaction != null)
                cmd.Transaction = Transaction;
            cmd.CommandTimeout = _commandTimeout;
        }

        public SqlTransaction Transaction { get; private set; }

        public SqlConnection Connection { get; }

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
                catch (Exception e)
                {
                    throw new RollbackException(e);
                }
                finally
                {
                    Connection.Close();
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
                catch (Exception e)
                {
                    throw new RollbackException(e);
                }
                finally
                {
                    await Connection.CloseAsync().ConfigureAwait(false);
                }
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

        public SqlCommand CreateCommand()
        {
            var cmd = Connection.CreateCommand();
            if (Transaction != null)
                cmd.Transaction = Transaction;

            return cmd;
        }
    }

    public class RollbackException: Exception
    {
        public RollbackException(Exception innerException) : base("Failed while trying to rollback an exception", innerException)
        {
        }
    }
}
