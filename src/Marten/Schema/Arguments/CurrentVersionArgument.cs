using System;
using System.Linq.Expressions;
using System.Data;

namespace Marten.Schema.Arguments
{
    public class CurrentVersionArgument: UpsertArgument
    {
        public CurrentVersionArgument()
        {
            Arg = "current_version";
            PostgresType = "uuid";
            DbType = SqlDbType.UniqueIdentifier;
            Column = null;
        }

        public override Expression CompileBulkImporter(DocumentMapping mapping, EnumStorage enumStorage, Expression writer, ParameterExpression document, ParameterExpression alias, ParameterExpression serializer, ParameterExpression textWriter, ParameterExpression tenantId)
        {
            throw new NotSupportedException("This should not be used for CurrentVersionArgument");
        }

        public override Expression CompileUpdateExpression(EnumStorage enumStorage, ParameterExpression call, ParameterExpression doc, ParameterExpression updateBatch, ParameterExpression mapping, ParameterExpression currentVersion, ParameterExpression newVersion, ParameterExpression tenantId, bool useCharBufferPooling)
        {
            var argName = Expression.Constant(Arg);

            return Expression.Call(call, _paramMethod, argName, Expression.Convert(currentVersion, typeof(object)), Expression.Constant(DbType));
        }
    }
}
