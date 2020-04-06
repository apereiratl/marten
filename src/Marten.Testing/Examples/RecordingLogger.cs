using System;
using System.Collections.Generic;
using Marten.Services;
using Microsoft.Data.SqlClient;

namespace Marten.Testing.Examples
{
    public class RecordingLogger: IMartenSessionLogger
    {
        public readonly IList<SqlCommand> Commands = new List<SqlCommand>();

        public void LogSuccess(SqlCommand command)
        {
            Commands.Add(command);
        }

        public void LogFailure(SqlCommand command, Exception ex)
        {
            Commands.Add(command);
        }

        public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
        {
            // do nothing
        }
    }
}
