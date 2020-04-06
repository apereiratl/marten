using System;
using System.Collections.Generic;
using System.Linq;
using Marten.Schema;
using Marten.Storage;
using Marten.Util;
using Microsoft.Data.SqlClient;

using System.Data;

namespace Marten.Services
{
    public interface IBatchCommand
    {
        SqlParameter AddParameter(object value, SqlDbType dbType);

        ISerializer Serializer { get; }
        SqlCommand Command { get; }
    }

    public class BatchCommand: IBatchCommand
    {
        private readonly ITenant _tenant;
        public ISerializer Serializer { get; }
        private int _counter = 0;

        public BatchCommand(ISerializer serializer, ITenant tenant)
        {
            _tenant = tenant;
            Serializer = serializer;
        }

        public SqlCommand Command { get; } = new SqlCommand();

        public int Count => Calls.Count;

        public SqlParameter AddParameter(object value, SqlDbType dbType)
        {
            var name = "p" + _counter++;
            var param = Command.AddNamedParameter(name, value);
            param.SqlDbType = dbType;

            return param;
        }

        public IList<ICallback> Callbacks { get; } = new List<ICallback>();
        public IList<IExceptionTransform> ExceptionTransforms { get; } = new List<IExceptionTransform>();

        public IList<IStorageOperation> Calls { get; } = new List<IStorageOperation>();

        public SqlCommand BuildCommand()
        {
            return CommandBuilder.ToBatchCommand(_tenant, Calls);
        }

        public void AddCall(IStorageOperation call, ICallback callback = null, IExceptionTransform exceptionTransform = null)
        {
            Calls.Add(call);
            Callbacks.Add(callback);
            ExceptionTransforms.Add(exceptionTransform);
        }

        public SprocCall Sproc(DbObjectName function, ICallback callback = null, IExceptionTransform exceptionTransform = null)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var call = new SprocCall(this, function);
            AddCall(call, callback, exceptionTransform);

            return call;
        }

        public bool HasCallbacks()
        {
            return Callbacks.Any(x => x != null);
        }

        public bool HasExceptionTransforms()
        {
            return ExceptionTransforms.Any(x => x != null);
        }
    }
}
