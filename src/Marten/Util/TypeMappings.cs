namespace Marten.Util
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Baseline;

    using Microsoft.Data.SqlClient;

    public static class TypeMappings
    {
        private static readonly Ref<ImHashMap<Type, string>> PgTypeMemo;
        private static readonly Ref<ImHashMap<Type, SqlDbType?>> SqlDbTypeMemo;
        private static readonly Ref<ImHashMap<SqlDbType, Type[]>> TypeMemo;

        public static Func<object, DateTime> CustomMappingToDateTime = null;
        public static Func<object, DateTimeOffset> CustomMappingToDateTimeOffset = null;
        public static Func<DateTime, object> CustomMappingFromDateTime = null;
        public static Func<DateTimeOffset, object> CustomMappingFromDateTimeOffset = null;

        public static List<Type> ContainmentOperatorTypes { get; } = new List<Type>();
        public static List<Type> TimespanTypes { get; } = new List<Type>();
        public static List<Type> TimespanZTypes { get; } = new List<Type>();

        static TypeMappings()
        {
            // Initialize PgTypeMemo with Types which are not available in Npgsql mappings
            PgTypeMemo = Ref.Of(ImHashMap<Type, string>.Empty);

            PgTypeMemo.Swap(d => d.AddOrUpdate(typeof(long), "bigint"));
            PgTypeMemo.Swap(d => d.AddOrUpdate(typeof(string), "varchar"));
            PgTypeMemo.Swap(d => d.AddOrUpdate(typeof(float), "decimal"));

            // Default Npgsql mapping is 'numeric' but we are using 'decimal'
            PgTypeMemo.Swap(d => d.AddOrUpdate(typeof(decimal), "decimal"));

            // Default Npgsql mappings is 'timestamp' but we are using 'timestamp without time zone'
            PgTypeMemo.Swap(d => d.AddOrUpdate(typeof(DateTime), "timestamp without time zone"));

            SqlDbTypeMemo = Ref.Of(ImHashMap<Type, SqlDbType?>.Empty);

            TypeMemo = Ref.Of(ImHashMap<SqlDbType, Type[]>.Empty);

            // AddTimespanTypes(SqlDbType.Timestamp, ResolveTypes(SqlDbType.Timestamp));
        }

        public static void RegisterMapping(Type type, string pgType, SqlDbType? SqlDbType)
        {
            PgTypeMemo.Swap(d => d.AddOrUpdate(type, pgType));
            SqlDbTypeMemo.Swap(d => d.AddOrUpdate(type, SqlDbType));
        }

        // Lazily retrieve the CLR type to SqlDbType and PgTypeName mapping from exposed INpgsqlTypeMapper.Mappings.
        // This is lazily calculated instead of precached because it allows consuming code to register
        // custom npgsql mappings prior to execution.
        private static string ResolvePgType(Type type)
        {
            if (PgTypeMemo.Value.TryFind(type, out var value))
                return value;

            //value = GetTypeMapping(type)?.PgTypeName;

            PgTypeMemo.Swap(d => d.AddOrUpdate(type, value));

            return value;
        }

        private static SqlDbType? ResolveSqlDbType(Type type)
        {
            if (SqlDbTypeMemo.Value.TryFind(type, out var value))
                return value;

            //value = GetTypeMapping(type)?.SqlDbType;

            SqlDbTypeMemo.Swap(d => d.AddOrUpdate(type, value));

            return value;
        }

        internal static Type[] ResolveTypes(SqlDbType SqlDbType)
        {
            if (TypeMemo.Value.TryFind(SqlDbType, out var values))
                return values;

            //values = GetTypeMapping(SqlDbType)?.ClrTypes;

            TypeMemo.Swap(d => d.AddOrUpdate(SqlDbType, values));

            return values;
        }

        //private static NpgsqlTypeMapping GetTypeMapping(Type type)
        //    => SqlConnection
        //        .GlobalTypeMapper
        //        .Mappings
        //        .FirstOrDefault(mapping => mapping.ClrTypes.Contains(type));

        //private static NpgsqlTypeMapping GetTypeMapping(SqlDbType type)
        //    => SqlConnection
        //        .GlobalTypeMapper
        //        .Mappings
        //        .FirstOrDefault(mapping => mapping.SqlDbType == type);

        public static string ConvertSynonyms(string type)
        {
            switch (type.ToLower())
            {
                case "varchar":
                    return "varchar";

                case "boolean":
                case "bool":
                    return "boolean";

                case "integer":
                    return "int";

                case "decimal":
                case "numeric":
                    return "decimal";

                case "timestamp without time zone":
                    return "timestamp";

                case "timestamp with time zone":
                    return "Timestamp";
            }

            return type;
        }

        public static string ReplaceMultiSpace(this string str, string newStr)
        {
            var regex = new Regex("\\s+");
            return regex.Replace(str, newStr);
        }

        public static string CanonicizeSql(this string sql)
        {
            var replaced = sql
                .Trim()
                .Replace('\n', ' ')
                .Replace('\r', ' ')
                .Replace('\t', ' ')
                .ReplaceMultiSpace(" ")
                .Replace(" ;", ";")
                .Replace("SECURITY INVOKER", "")
                .Replace("  ", " ")
                .Replace("LANGUAGE plpgsql AS $function$", "")
                .Replace("$$ LANGUAGE plpgsql", "$function$")
                .Replace("AS $$ DECLARE", "DECLARE")
                .Replace("character varying", "varchar")
                .Replace("Boolean", "boolean")
                .Replace("bool,", "boolean,")
                .Replace("int[]", "integer[]")
                .Replace("numeric", "decimal").TrimEnd(';').TrimEnd();

            if (replaced.Contains("PLV8", StringComparison.OrdinalIgnoreCase))
            {
                replaced = replaced
                    .Replace("LANGUAGE plv8 IMMUTABLE STRICT AS $function$", "AS $$");

                const string languagePlv8ImmutableStrict = "$$ LANGUAGE plv8 IMMUTABLE STRICT";
                const string functionMarker = "$function$";
                if (replaced.EndsWith(functionMarker))
                {
                    replaced = replaced.Substring(0, replaced.LastIndexOf(functionMarker)) + languagePlv8ImmutableStrict;
                }
            }

            return replaced
                .Replace("  ", " ").TrimEnd().TrimEnd(';');
        }

        /// <summary>
        /// Some portion of implementation adapted from Npgsql GlobalTypeMapper.ToSqlDbType(Type type)
        /// https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/TypeMapping/GlobalTypeMapper.cs
        /// Possibly this method can be trimmed down when Npgsql eventually exposes ToSqlDbType
        /// </summary>
        public static SqlDbType ToDbType(Type type)
        {
            if (determineSqlDbType(type, out var dbType))
                return dbType;

            throw new NotSupportedException("Can't infer SqlDbType for type " + type);
        }

        public static SqlDbType? TryGetDbType(Type type)
        {
            if (type == null || !determineSqlDbType(type, out var dbType))
                return null;

            return dbType;
        }

        private static bool determineSqlDbType(Type type, out SqlDbType dbType)
        {
            var sqlDbType = ResolveSqlDbType(type);
            if (sqlDbType != null)
            {
                {
                    dbType = sqlDbType.Value;
                    return true;
                }
            }

            if (type.IsNullable())
            {
                dbType = ToDbType(type.GetInnerTypeFromNullable());
                return true;
            }

            if (type.IsEnum)
            {
                dbType = SqlDbType.Int;
                return true;
            }

            if (type.IsString())
            {
                dbType = SqlDbType.NVarChar;
                return true;
            }

            if (type.IsIntegerBased())
            {
                dbType = SqlDbType.BigInt;
                return true;
            }

            if (type == typeof(Guid))
            {
                dbType = SqlDbType.UniqueIdentifier;
                return true;
            }

            // no array support in sql server
            //if (type.IsArray)
            //{
            //    if (type == typeof(byte[]))
            //    {
            //        dbType = sqlDbType.Bytea;
            //        return true;
            //    }

            //    {
            //        dbType = SqlDbType.Array | ToDbType(type.GetElementType());
            //        return true;
            //    }
            //}

            //var typeInfo = type.GetTypeInfo();

            //var ilist = typeInfo.ImplementedInterfaces.FirstOrDefault(x =>
            //    x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
            //if (ilist != null)
            //{
            //    dbType = SqlDbType.Array | ToDbType(ilist.GetGenericArguments()[0]);
            //    return true;
            //}

            if (type == typeof(DBNull))
            {
                dbType = default;
                return true;
            }

            dbType = default;
            return false;
        }

        public static string GetPgType(Type memberType, EnumStorage enumStyle)
        {
            if (memberType.IsEnum)
            {
                return enumStyle == EnumStorage.AsInteger ? "integer" : "varchar";
            }

            if (memberType.IsArray)
            {
                return GetPgType(memberType.GetElementType(), enumStyle) + "[]";
            }

            if (memberType.IsNullable())
            {
                return GetPgType(memberType.GetInnerTypeFromNullable(), enumStyle);
            }

            if (memberType.IsConstructedGenericType)
            {
                var templateType = memberType.GetGenericTypeDefinition();
                return ResolvePgType(templateType) ?? "jsonb";
            }

            return ResolvePgType(memberType) ?? "jsonb";
        }

        public static bool HasTypeMapping(Type memberType)
        {
            if (memberType.IsNullable())
            {
                return HasTypeMapping(memberType.GetInnerTypeFromNullable());
            }

            // more complicated later
            return ResolvePgType(memberType) != null || memberType.IsEnum;
        }

        [Obsolete("Use JsonLocatorField to build locators with appropriate casting.  This might be removed in v4.0.")]
        public static string ApplyCastToLocator(this string locator, EnumStorage enumStyle, Type memberType)
        {
            if (memberType.IsEnum)
            {
                return enumStyle == EnumStorage.AsInteger ? "({0})::int".ToFormat(locator) : locator;
            }

            // Treat "unknown" PgTypes as jsonb (this way null checks of arbitary depth won't fail on cast).
            return "CAST({0} as {1})".ToFormat(locator, GetPgType(memberType, enumStyle));
        }

        private static Type GetNullableType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsValueType)
                return typeof(Nullable<>).MakeGenericType(type);
            else
                return type;
        }

        public static void AddTimespanTypes(SqlDbType SqlDbType, params Type[] types)
        {
            var timespanTypesList = (SqlDbType == SqlDbType.Timestamp) ? TimespanTypes : TimespanZTypes;
            var typesWithNullables = types.Union(types.Select(t => GetNullableType(t))).Where(t => !timespanTypesList.Contains(t)).ToList();

            timespanTypesList.AddRange(typesWithNullables);

            ContainmentOperatorTypes.AddRange(typesWithNullables);
        }

        internal static DateTime MapToDateTime(this object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (CustomMappingToDateTime != null)
                return CustomMappingToDateTime(value);

            if (value is DateTimeOffset offset)
                return offset.DateTime;

            if (value is DateTime dateTime)
                return dateTime;

            throw new ArgumentException($"Cannot convert type {value.GetType()} to DateTime", nameof(value));
        }

        internal static DateTimeOffset MapToDateTimeOffset(this object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (CustomMappingToDateTimeOffset != null)
                return CustomMappingToDateTimeOffset(value);

            if (value is DateTimeOffset offset)
                return offset;

            if (value is DateTime dateTime)
                return dateTime;

            throw new ArgumentException($"Cannot convert type {value.GetType()} to DateTimeOffset", nameof(value));
        }

        internal static object MapFromDateTime(this DateTime value)
        {
            if (CustomMappingFromDateTime != null)
                return CustomMappingFromDateTime(value);

            return value;
        }

        internal static object MapFromDateTimeOffset(this DateTimeOffset value)
        {
            if (CustomMappingFromDateTimeOffset != null)
                return CustomMappingFromDateTimeOffset(value);

            return value;
        }
    }
}
