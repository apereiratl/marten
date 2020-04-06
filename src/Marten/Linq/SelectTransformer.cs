namespace Marten.Linq
{
    using System.Data.Common;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Baseline;

    using Marten.Schema;
    using Marten.Services;

    using Microsoft.Data.SqlClient;

    public class SelectTransformer<T>: BasicSelector, ISelector<T>
    {
        public SelectTransformer(IQueryableDocument mapping, TargetObject target)
            : base(target.ToSelectField(mapping))
        {
        }

        public SelectTransformer(IQueryableDocument mapping, TargetObject target, bool distinct)
            : base(distinct, target.ToSelectField(mapping))
        {
        }

        public T Resolve(DbDataReader reader, IIdentityMap map, QueryStatistics stats)
        {
            var json = reader.GetTextReader(0);
            return map.Serializer.FromJson<T>(json);
        }

        public async Task<T> ResolveAsync(DbDataReader reader, IIdentityMap map, QueryStatistics stats, CancellationToken token)
        {
            var json = await reader.As<SqlDataReader>().GetFieldValueAsync<TextReader>(0).ConfigureAwait(false);
            return map.Serializer.FromJson<T>(json);
        }
    }
}
