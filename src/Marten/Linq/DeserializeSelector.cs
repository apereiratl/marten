namespace Marten.Linq
{
    using System.Data.Common;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Baseline;

    using Marten.Services;

    using Microsoft.Data.SqlClient;

    public class DeserializeSelector<T>: BasicSelector, ISelector<T>
    {
        private readonly ISerializer _serializer;

        public DeserializeSelector(ISerializer serializer) : base("data")
        {
            _serializer = serializer;
        }

        public DeserializeSelector(ISerializer serializer, params string[] selectFields) : base(selectFields)
        {
            _serializer = serializer;
        }

        public T Resolve(DbDataReader reader, IIdentityMap map, QueryStatistics stats)
        {
            if (!typeof(T).IsSimple())
            {
                var json = reader.As<DbDataReader>().GetTextReader(0);
                return _serializer.FromJson<T>(json);
            }

            return reader.GetFieldValue<T>(0);
        }

        public async Task<T> ResolveAsync(DbDataReader reader, IIdentityMap map, QueryStatistics stats, CancellationToken token)
        {
            if (!typeof(T).IsSimple())
            {
                var json = await reader.As<SqlDataReader>().GetFieldValueAsync<TextReader>(0).ConfigureAwait(false);
                return _serializer.FromJson<T>(json);
            }

            return await reader.GetFieldValueAsync<T>(0, token).ConfigureAwait(false);
        }
    }
}
