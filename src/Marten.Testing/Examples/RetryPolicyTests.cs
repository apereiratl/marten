using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Marten.Testing.Examples
{
    // SAMPLE: retrypolicy-samplepolicy
    // Implement IRetryPolicy interface
    public sealed class ExceptionFilteringRetryPolicy: IRetryPolicy
    {
        private readonly int maxTries;
        private readonly Func<Exception, bool> filter;

        private ExceptionFilteringRetryPolicy(int maxTries, Func<Exception, bool> filter)
        {
            this.maxTries = maxTries;
            this.filter = filter;
        }

        public static IRetryPolicy Once(Func<Exception, bool> filter = null)
        {
            return new ExceptionFilteringRetryPolicy(2, filter ?? (_ => true));
        }

        public static IRetryPolicy Twice(Func<Exception, bool> filter = null)
        {
            return new ExceptionFilteringRetryPolicy(3, filter ?? (_ => true));
        }

        public static IRetryPolicy NTimes(int times, Func<Exception, bool> filter = null)
        {
            return new ExceptionFilteringRetryPolicy(times + 1, filter ?? (_ => true));
        }

        public void Execute(Action operation)
        {
            Try(() => { operation(); return Task.CompletedTask; }, CancellationToken.None).GetAwaiter().GetResult();
        }

        public TResult Execute<TResult>(Func<TResult> operation)
        {
            return Try(() => Task.FromResult(operation()), CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken)
        {
            return Try(operation, cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken)
        {
            return Try(operation, cancellationToken);
        }

        private async Task Try(Func<Task> operation, CancellationToken token)
        {
            for (var tries = 0; ; token.ThrowIfCancellationRequested())
            {
                try
                {
                    await operation().ConfigureAwait(false);
                    return;
                }
                catch (Exception e) when (++tries < maxTries && filter(e))
                {
                }
            }
        }

        private async Task<T> Try<T>(Func<Task<T>> operation, CancellationToken token)
        {
            for (var tries = 0; ; token.ThrowIfCancellationRequested())
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception e) when (++tries < maxTries && filter(e))
                {
                }
            }
        }
    }

    // ENDSAMPLE

    public sealed class RetryPolicyTests: IntegratedFixture
    {
        [Fact]
        public void CanPlugInRetryPolicyThatRetriesOnException()
        {
            var m = new List<string>();
            StoreOptions(c =>
            {
                // SAMPLE: retrypolicy-samplepolicy-pluggingin
                // Plug in our custom retry policy via StoreOptions
                // We retry operations twice if they yield and NpgsqlException that is not transient (for the sake of easier demonstrability)
                c.RetryPolicy(ExceptionFilteringRetryPolicy.Twice(e => e is SqlException ne && !ne.IsTransient()));
                // ENDSAMPLE

                // For unit test, override the policy with one that captures messages
                c.RetryPolicy(ExceptionFilteringRetryPolicy.Twice(e =>
                {
                    if (e is SqlException ne && !ne.IsTransient())
                    {
                        m.Add(e.Message);
                        return true;
                    }

                    return false;
                }));
            });

            using (var s = theStore.QuerySession())
            {
                Assert.Throws<Marten.Exceptions.MartenCommandException>(() =>
                {
                    var _ = s.Query<object>("select null from mt_nonexistenttable").FirstOrDefault();
                });
            }

            // Our retry exception filter should have triggered twice
            Assert.True(m.Count(s => s.IndexOf("relation \"mt_nonexistenttable\" does not exist", StringComparison.OrdinalIgnoreCase) > -1) == 2);
        }
    }

    public static class SqlExceptionExtensions
    {
        public static bool IsTransient(this SqlException err)
        {
            switch (err.Number)
            {
                // SQL Error Code: 40501
                // The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
                case 40501:
                    return true;

                // SQL Error Code: 10928
                // Resource ID: %d. The %s limit for the database is %d and has been reached.
                case 10928:
                // SQL Error Code: 10929
                // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d. 
                // However, the server is currently too busy to support requests greater than %d for this database.
                case 10929:
                // SQL Error Code: 10053
                // A transport-level error has occurred when receiving results from the server.
                // An established connection was aborted by the software in your host machine.
                case 10053:
                // SQL Error Code: 10054
                // A transport-level error has occurred when sending the request to the server. 
                // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                case 10054:
                // SQL Error Code: 10060
                // A network-related or instance-specific error occurred while establishing a connection to SQL Server. 
                // The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server 
                // is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed 
                // because the connected party did not properly respond after a period of time, or established connection failed 
                // because connected host has failed to respond.)"}
                case 10060:
                // SQL Error Code: 40197
                // The service has encountered an error processing your request. Please try again.
                case 40197:
                // SQL Error Code: 40540
                // The service has encountered an error processing your request. Please try again.
                case 40540:
                // SQL Error Code: 40613
                // Database XXXX on server YYYY is not currently available. Please retry the connection later. If the problem persists, contact customer 
                // support, and provide them the session tracing ID of ZZZZZ.
                case 40613:
                // SQL Error Code: 40143
                // The service has encountered an error processing your request. Please try again.
                case 40143:
                // SQL Error Code: 233
                // The client was unable to establish a connection because of an error during connection initialization process before login. 
                // Possible causes include the following: the client tried to connect to an unsupported version of SQL Server; the server was too busy 
                // to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections) on the server. 
                // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                case 233:
                // SQL Error Code: 64
                // A connection was successfully established with the server, but then an error occurred during the login process. 
                // (provider: TCP Provider, error: 0 - The specified network name is no longer available.) 
                case 64:
                // DBNETLIB Error Code: 20
                // The instance of SQL Server you attempted to connect to does not support encryption.
                case (int)20:
                    return true;
                default:
                    return false;
            }
        }
    }
}
