using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Marten.Services;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Marten.Testing
{
    public class document_session_logs_SaveChanges_Tests : DocumentSessionFixture<NulloIdentityMap>
    {
        [Fact]
        public void records_on_SaveChanges()
        {
            var logger = new RecordingLogger();

            theSession.Logger = logger;

            theSession.Store(Target.Random());

            theSession.SaveChanges();

            logger.LastSession.ShouldBe(theSession);
        }

        [Fact]
        public async Task records_on_SaveChangesAsync()
        {
            var logger = new RecordingLogger();

            theSession.Logger = logger;

            theSession.Store(Target.Random());

            await theSession.SaveChangesAsync().ConfigureAwait(false);

            logger.LastSession.ShouldBe(theSession);
        }
    }

    public class RecordingLogger : IMartenSessionLogger
    {
        public readonly IList<IChangeSet> Commits = new List<IChangeSet>();

        public void LogSuccess(SqlCommand command)
        {
            
        }

        public void LogFailure(SqlCommand command, Exception ex)
        {
        }

        public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
        {
            LastSession = session;
            LastCommit = commit;

            Commits.Add(commit);
        }

        public IChangeSet LastCommit { get; set; }

        public IDocumentSession LastSession { get; set; }
    }
}
