using System;
using System.Threading.Tasks;
using Marten.Services;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Marten.Testing.Services
{
    public class ManagedConnectionTests
    {
        [Fact]
        public async Task increments_the_request_count()
        {
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()))
            {
                connection.RequestCount.ShouldBe(0);

                connection.Execute(cmd => { });
                connection.RequestCount.ShouldBe(1);

                connection.Execute(new SqlCommand(), c => { });
                connection.RequestCount.ShouldBe(2);

                connection.Execute(c => "");
                connection.RequestCount.ShouldBe(3);

                connection.Execute(new SqlCommand(), c => "");
                connection.RequestCount.ShouldBe(4);


                await connection.ExecuteAsync(async (c, t) => { await Task.CompletedTask; });
                connection.RequestCount.ShouldBe(5);

                await connection.ExecuteAsync(new SqlCommand(), async (c, t) => { await Task.CompletedTask; });
                connection.RequestCount.ShouldBe(6);

                await connection.ExecuteAsync(async (c, t) =>
                {
                    await Task.CompletedTask;
                    return "";
                });
                connection.RequestCount.ShouldBe(7);

                await connection.ExecuteAsync(new SqlCommand(), async (c, t) =>
                {
                    await Task.CompletedTask;
                    return "";
                });
                connection.RequestCount.ShouldBe(8);
            }
        }

        [Fact]
        public void log_execute_failure_1()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                Exception<DivideByZeroException>.ShouldBeThrownBy(() =>
                {
                    connection.Execute(c =>
                    {
                        c.CommandText = "do something";
                        throw ex;
                    });
                });


                logger.LastCommand.CommandText.ShouldBe("do something");
                logger.LastException.ShouldBe(ex);
            }
        }

        [Fact]
        public async Task log_execute_failure_1_async()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                await Exception<DivideByZeroException>.ShouldBeThrownByAsync(async () =>
                {
                    await connection.ExecuteAsync(async (c, tkn) =>
                    {
                        await Task.CompletedTask;
                        c.CommandText = "do something";
                        throw ex;
                    });
                });


                logger.LastCommand.CommandText.ShouldBe("do something");
                logger.LastException.ShouldBe(ex);
            }
        }


        [Fact]
        public void log_execute_failure_2()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();

                Exception<DivideByZeroException>.ShouldBeThrownBy(
                    () => { connection.Execute(cmd, c => { throw ex; }); });


                logger.LastCommand.ShouldBe(cmd);
                logger.LastException.ShouldBe(ex);
            }
        }


        [Fact]
        public async Task log_execute_failure_2_async()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();

                await Exception<DivideByZeroException>.ShouldBeThrownByAsync(async () =>
                {
                    await connection.ExecuteAsync(cmd, async (c, tkn) =>
                    {
                        await Task.CompletedTask;
                        throw ex;
                    });
                });


                logger.LastCommand.ShouldBe(cmd);
                logger.LastException.ShouldBe(ex);
            }
        }

        [Fact]
        public void log_execute_success_1()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                connection.Execute(c => c.CommandText = "do something");

                logger.LastCommand.CommandText.ShouldBe("do something");
            }
        }


        [Fact]
        public async Task log_execute_success_1_async()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                await connection.ExecuteAsync(async (c, tkn) =>
                {
                    await Task.CompletedTask;
                    c.CommandText = "do something";
                });

                logger.LastCommand.CommandText.ShouldBe("do something");
            }
        }


        [Fact]
        public void log_execute_success_2()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();
                connection.Execute(cmd, c => c.CommandText = "do something");

                logger.LastCommand.ShouldBeSameAs(cmd);
            }
        }


        [Fact]
        public async Task log_execute_success_2_async()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();
                await connection.ExecuteAsync(cmd, async (c, tkn) =>
                {
                    await Task.CompletedTask;
                    c.CommandText = "do something";
                });

                logger.LastCommand.ShouldBeSameAs(cmd);
            }
        }


        [Fact]
        public void log_execute_success_with_answer_1()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                connection.Execute(c =>
                {
                    c.CommandText = "do something";
                    return "something";
                });

                logger.LastCommand.CommandText.ShouldBe("do something");
            }
        }


        [Fact]
        public async Task log_execute_success_with_answer_1_async()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                await connection.Execute(async c =>
                {
                    await Task.CompletedTask;
                    c.CommandText = "do something";
                    return "something";
                });

                logger.LastCommand.CommandText.ShouldBe("do something");
            }
        }


        [Fact]
        public void log_execute_success_with_answer_2()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();
                connection.Execute(cmd, c => "something");

                logger.LastCommand.ShouldBeSameAs(cmd);
            }
        }


        [Fact]
        public async Task log_execute_success_with_answer_2_async()
        {
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();
                await connection.ExecuteAsync(cmd, async (c, tkn) =>
                {
                    await Task.CompletedTask;
                    return "something";
                });

                logger.LastCommand.ShouldBeSameAs(cmd);
            }
        }

        [Fact]
        public void log_execute_with_answer_failure_1()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                Exception<DivideByZeroException>.ShouldBeThrownBy(() =>
                {
                    connection.Execute<string>(c =>
                    {
                        c.CommandText = "do something";
                        throw ex;
                    });
                });


                logger.LastCommand.CommandText.ShouldBe("do something");
                logger.LastException.ShouldBe(ex);
            }
        }

        [Fact]
        public async Task log_execute_with_answer_failure_1_async()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                await Exception<DivideByZeroException>.ShouldBeThrownByAsync(async () =>
                {
                    await connection.ExecuteAsync<string>(async (c, tkn) =>
                    {
                        await Task.CompletedTask;
                        c.CommandText = "do something";
                        throw ex;
                    });
                });


                logger.LastCommand.CommandText.ShouldBe("do something");
                logger.LastException.ShouldBe(ex);
            }
        }


        [Fact]
        public void log_execute_with_answer_failure_2()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();

                Exception<DivideByZeroException>.ShouldBeThrownBy(() =>
                {
                    connection.Execute<string>(cmd, c => { throw ex; });
                });


                logger.LastCommand.ShouldBe(cmd);
                logger.LastException.ShouldBe(ex);
            }
        }


        [Fact]
        public async Task log_execute_with_answer_failure_2_asycn()
        {
            var ex = new DivideByZeroException();
            var logger = new RecordingLogger();
            using (var connection = new ManagedConnection(new ConnectionSource(), new NulloRetryPolicy()) {Logger = logger})
            {
                var cmd = new SqlCommand();

                await Exception<DivideByZeroException>.ShouldBeThrownByAsync(async () =>
                {
                    await connection.ExecuteAsync<string>(cmd, async (c, tkn) =>
                            {
                                await Task.CompletedTask;
                                throw ex;
                    });
                });


                logger.LastCommand.ShouldBe(cmd);
                logger.LastException.ShouldBe(ex);
            }
        }
    }


    public class RecordingLogger : IMartenSessionLogger
    {
        public SqlCommand LastCommand;
        public Exception LastException;

        public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
        {
            throw new NotImplementedException();
        }

        public void LogSuccess(SqlCommand command)
        {
            LastCommand = command;
        }

        public void LogFailure(SqlCommand command, Exception ex)
        {
            LastCommand = command;
            LastException = ex;
        }
    }
}
