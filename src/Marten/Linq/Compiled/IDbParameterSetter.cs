using Marten.Util;
using Microsoft.Data.SqlClient;

namespace Marten.Linq.Compiled
{
    public interface IDbParameterSetter
    {
        SqlParameter AddParameter(object query, CommandBuilder command);

        void ReplaceValue(SqlParameter cmdParameter);
    }
}
