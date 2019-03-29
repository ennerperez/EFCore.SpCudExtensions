using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.SpCudExtensions
{
    public static class DbContextUnderlyingExtensions
    {
        public static DbConnection GetUnderlyingConnection(this DbContext context, SpCudConfig config)
        {
            var connection = context.Database.GetDbConnection();
            if (config?.UnderlyingConnection != null)
            {
                connection = config.UnderlyingConnection(connection);
            }
            return connection;
        }

        public static DbTransaction GetUnderlyingTransaction(this IDbContextTransaction ctxTransaction, SpCudConfig config)
        {
            var dbTransaction = ctxTransaction.GetDbTransaction();
            if (config?.UnderlyingTransaction != null)
            {
                dbTransaction = config.UnderlyingTransaction(dbTransaction);
            }
            return dbTransaction;
        }
    }
}