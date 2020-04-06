using System;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Marten.Exceptions
{
    /// <summary>
    /// Class responsible for creating MartenCommandException exception or exceptions derived from it based on exact command code.
    /// </summary>
    internal static class MartenCommandExceptionFactory
    {
        internal static MartenCommandException Create(SqlCommand command, Exception innerException)
        {
            if (TryToMapToMartenCommandNotSupportedException(command, innerException, out var notSupportedException))
            {
                return notSupportedException;
            }

            return new MartenCommandException(command, innerException);
        }

        internal static bool TryToMapToMartenCommandNotSupportedException(SqlCommand command, Exception innerException, out MartenCommandNotSupportedException notSupportedException)
        {
            var knownCause = KnownNotSupportedExceptionCause.KnownCauses.FirstOrDefault(x => x.Matches(innerException));

            if (knownCause != null)
            {
                notSupportedException = new MartenCommandNotSupportedException(knownCause.Reason, command, innerException, knownCause.Description);

                return true;
            }

            notSupportedException = null;
            return false;
        }
    }
}
