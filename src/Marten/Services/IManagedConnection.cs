using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace Marten.Services
{

    public interface IManagedConnection: IDisposable
    {
        void Execute(SqlCommand cmd, Action<SqlCommand> action = null);

        void Execute(Action<SqlCommand> action);

        T Execute<T>(Func<SqlCommand, T> func);

        T Execute<T>(SqlCommand cmd, Func<SqlCommand, T> func);

        Task ExecuteAsync(Func<SqlCommand, CancellationToken, Task> action, CancellationToken token = default(CancellationToken));

        Task ExecuteAsync(SqlCommand cmd, Func<SqlCommand, CancellationToken, Task> action, CancellationToken token = default(CancellationToken));

        Task<T> ExecuteAsync<T>(Func<SqlCommand, CancellationToken, Task<T>> func, CancellationToken token = default(CancellationToken));

        Task<T> ExecuteAsync<T>(SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> func, CancellationToken token = default(CancellationToken));

        void Commit();

        void Rollback();

        SqlConnection Connection { get; }

        int RequestCount { get; }

        void BeginTransaction();

        bool InTransaction();

        Task BeginTransactionAsync(CancellationToken token);

        Task CommitAsync(CancellationToken token);

        Task RollbackAsync(CancellationToken token);

        void BeginSession();
    }
}
