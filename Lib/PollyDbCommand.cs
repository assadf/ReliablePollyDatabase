using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Wrap;

namespace Lib
{
    public class PollyDbCommand : DbCommand
    {
        private readonly SqlCommand _sqlCommand;

        private readonly ISyncPolicy _syncSinglePolicy;
        private readonly PolicyWrap _syncPolicyWrapper;

        private readonly IAsyncPolicy _asyncSinglePolicy;
        private readonly AsyncPolicyWrap _asyncPolicyWrapper;

        public PollyDbCommand(SqlCommand command, IAsyncPolicy[] asyncPolicies, ISyncPolicy[] syncPolicies)
        {
            _sqlCommand = command;
            _sqlCommand.CommandTimeout = 5;

            if (asyncPolicies == null)
            {
                throw new ArgumentNullException($"{nameof(asyncPolicies)} must have at least one async policy provided");
            }

            if (asyncPolicies.Length == 1)
            {
                _asyncSinglePolicy = asyncPolicies[0];
            }
            else
            {
                _asyncPolicyWrapper = Policy.WrapAsync(asyncPolicies);
            }

            if (syncPolicies == null)
            {
                throw new ArgumentNullException($"{nameof(syncPolicies)} must have at least one sync policy provided");
            }

            if (syncPolicies.Length == 1)
            {
                _syncSinglePolicy = syncPolicies[0];
            }
            else
            {
                _syncPolicyWrapper = Policy.Wrap(syncPolicies);
            }
        }

        public override string CommandText
        {
            get => _sqlCommand.CommandText;
            set => _sqlCommand.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _sqlCommand.CommandTimeout;
            set => _sqlCommand.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _sqlCommand.CommandType;
            set => _sqlCommand.CommandType = value;
        }

        public override bool DesignTimeVisible
        {
            get => _sqlCommand.DesignTimeVisible;
            set => _sqlCommand.DesignTimeVisible = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _sqlCommand.UpdatedRowSource;
            set => _sqlCommand.UpdatedRowSource = value;
        }

        protected override DbConnection DbConnection
        {
            get => _sqlCommand.Connection;
            set => _sqlCommand.Connection = (SqlConnection)value;
        }

        protected override DbParameterCollection DbParameterCollection => _sqlCommand.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => _sqlCommand.Transaction;
            set => _sqlCommand.Transaction = (SqlTransaction)value;
        }

        public override void Cancel()
        {
            _sqlCommand.Cancel();
        }

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var context = GetContext();

            if (_asyncSinglePolicy != null)
            {
                return await _asyncSinglePolicy.ExecuteAsync(async (ctx) =>
                        await _sqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false)
                    , context).ConfigureAwait(false);
            }

            return await _asyncPolicyWrapper.ExecuteAsync(async (ctx) =>
                    await _sqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false)
                , context).ConfigureAwait(false);
        }

        public override int ExecuteNonQuery()
        {
            return CommandInvoker((ctx) => _sqlCommand.ExecuteNonQuery());
        }

        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return await CommandInvokerAsync(async (ctx) =>
                    await _sqlCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                ).ConfigureAwait(false);
        }

        public override object ExecuteScalar()
        {
            return CommandInvoker((ctx) => _sqlCommand.ExecuteScalar());
        }

        public override void Prepare()
        {
            CommandInvoker((ctx) => _sqlCommand.Prepare());
        }

        protected override DbParameter CreateDbParameter()
        {
            return _sqlCommand.CreateParameter();
        }

        
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return await CommandInvokerAsync(async (ctx) =>
            {
                Console.WriteLine("Trying Command Call");
                return await _sqlCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return CommandInvoker((ctx) => _sqlCommand.ExecuteReader(behavior));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sqlCommand.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        private void CommandInvoker(Action<Context> action)
        {
            var context = GetContext();

            if (_syncSinglePolicy == null)
            {
                _syncPolicyWrapper.Execute(action, context);
                return;
            }

            _syncSinglePolicy.Execute(action, context);
        }

        private T CommandInvoker<T>(Func<Context, T> action)
        {
            var context = GetContext();

            if (_syncSinglePolicy == null)
            {
                return _syncPolicyWrapper.Execute(action, context);
            }

            return _syncSinglePolicy.Execute(action, context);
        }

        private async Task<T> CommandInvokerAsync<T>(Func<Context, Task<T>> action)
        {
            var context = GetContext();

            if (_asyncSinglePolicy == null)
            {
                return await _asyncPolicyWrapper.ExecuteAsync(action, context).ConfigureAwait(false);
            }

            return await _asyncSinglePolicy.ExecuteAsync(action, context).ConfigureAwait(false);
        }

        private Context GetContext()
        {
            var dictionary = new Dictionary<string, object>
            {
                {"CommandText", _sqlCommand.CommandText },
                {"CommandType", _sqlCommand.CommandType },
                {"CommandTimeout", _sqlCommand.CommandTimeout}
            };

            return new Context("PollyDbCommand", dictionary);
        }
    }
}
