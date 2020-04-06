namespace Marten.Testing.Session
{
    using System;
    using System.Data;

    using Marten.Services;
    using Marten.Testing.Documents;
    using Marten.Util;

    using Microsoft.Data.SqlClient;

    using Xunit;

    public class query_session_Tests : DocumentSessionFixture<NulloIdentityMap>
    {
        [Fact]
        public void should_respect_command_timeout_options()
        {
            using (var session = theStore.QuerySession(new SessionOptions() { Timeout = -1 }))
            {
                var e = Assert.Throws<ArgumentOutOfRangeException>(() => session.Query<int>("select 1"));
                Assert.StartsWith("CommandTimeout can't be less than zero", e.Message);
            }
        }

        [Fact]
        public void should_respect_isolationlevel_and_be_read_only_transaction_when_serializable_isolation()
        {
            var user = new User();

            theStore.BulkInsertDocuments(new [] { user });
            using (var session = theStore.QuerySession(new SessionOptions() { IsolationLevel = IsolationLevel.Serializable, Timeout = 1 }))
            {
                using (var cmd = session.Connection.CreateCommand("delete from mt_doc_user"))
                {
                    var e = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());

                    // ERROR: cannot execute DELETE in a read-only transaction
                    // read_only_sql_transaction
                    // todo: figure out what the value for this exception is later.
                    // currently, we will use the postgres version to fix the compile issue.
                    Assert.Equal(25006, e.Number);
                }
            }
        }
    }
}
