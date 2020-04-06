using System;
using Microsoft.Data.SqlClient;

namespace Marten
{
    public class LambdaConnectionFactory: IConnectionFactory
    {
        private readonly Func<SqlConnection> _source;

        public LambdaConnectionFactory(Func<SqlConnection> source)
        {
            _source = source;
        }

        public SqlConnection Create()
        {
            return _source();
        }
    }
}
