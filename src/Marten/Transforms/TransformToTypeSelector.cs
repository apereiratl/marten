namespace Marten.Transforms
{
    using System.Data.Common;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Baseline;

    using Marten.Linq;
    using Marten.Schema;
    using Marten.Services;
    using Marten.Util;

    using Microsoft.Data.SqlClient;

    public class TransformToTypeSelector<T>: ISelector<T>
    {
        private readonly IQueryableDocument _document;
        private readonly string _fieldName;

        public TransformToTypeSelector(string dataLocator, TransformFunction transform, IQueryableDocument document)
        {
            _document = document;
            _fieldName = $"{transform.Identifier}({dataLocator}) as json";
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

        public string[] SelectFields()
        {
            return new[] { _fieldName };
        }

        public void WriteSelectClause(CommandBuilder sql, IQueryableDocument mapping)
        {
            sql.Append("select ");
            sql.Append(_fieldName);
            sql.Append(" from ");
            sql.Append(_document.Table.QualifiedName);
            sql.Append(" as d");
        }
    }
}
