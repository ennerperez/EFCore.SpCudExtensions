using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.SpCudExtensions
{
    public enum OperationType
    {
        Insert,

        //InsertOrUpdate,
        //InsertOrUpdateDelete,
        Update,

        Delete,
        //Read
    }

    internal static class SqlSpCudOperation
    {
        internal static string ColumnMappingExceptionMessage => "The given ColumnMapping does not match up with any column in the source or destination";

        #region MainOps

        public static void Insert<T>(DbContext context, IList<T> entities, TableInfo tableInfo, Action<decimal> progress)
        {
            var sqlConnection = OpenAndGetSqlConnection(context, tableInfo.SpCudConfig);
            var transaction = context.Database.CurrentTransaction;
            try
            {
                using (var sqlSpCud = GetSqlCommand(sqlConnection, transaction, tableInfo.SpCudConfig))
                {
                    bool useFastMember = tableInfo.HasOwnedTypes == false                      // With OwnedTypes DataTable is used since library FastMember can not (https://github.com/mgravell/fast-member/issues/21)
                                         && tableInfo.ColumnNameContainsSquareBracket == false // FastMember does not support escaped columnNames  ] -> ]]
                                         && tableInfo.ShadowProperties.Count == 0              // With Shadow prop. Discriminator (TPH inheritance) also not used because FastMember is slow for Update
                                         && !tableInfo.ConvertibleProperties.Any();            // With ConvertibleProperties FastMember is slow as well
                    bool setColumnMapping = useFastMember;

                    tableInfo.SetSqlSpCudConfig(sqlSpCud, entities, setColumnMapping, progress);
                    try
                    {
                        if (useFastMember)
                        {
                            using (var reader = ObjectReaderEx.Create(entities, tableInfo.ShadowProperties, tableInfo.ConvertibleProperties, context, tableInfo.PropertyColumnNamesDict.Keys.ToArray()))
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message.Contains(ColumnMappingExceptionMessage))
                        {
                            //if (!tableInfo.CheckTableExist(context, tableInfo))
                            //{
                            //    context.Database.ExecuteSqlCommand(SqlQueryBuilder.CreateTableCopy(tableInfo.FullTableName, tableInfo.FullTempTableName, tableInfo)); // Will throw Exception specify missing db column: Invalid column name ''
                            //    context.Database.ExecuteSqlCommand(SqlQueryBuilder.DropTable(tableInfo.FullTempTableName));
                            //}
                        }
                        throw ex;
                    }
                }
            }
            finally
            {
                if (transaction == null)
                {
                    sqlConnection.Close();
                }
            }
        }

        #endregion MainOps

        #region Connection

        internal static SqlConnection OpenAndGetSqlConnection(DbContext context, SpCudConfig config)
        {
            var connection = context.GetUnderlyingConnection(config);
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            return (SqlConnection)connection;
        }

        internal static async Task<SqlConnection> OpenAndGetSqlConnectionAsync(DbContext context, SpCudConfig config)
        {
            var connection = context.GetUnderlyingConnection(config);
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            return (SqlConnection)connection;
        }

        private static SqlCommand GetSqlCommand(SqlConnection sqlConnection, IDbContextTransaction transaction, SpCudConfig config)
        {
            //var sqlSpCudOptions = config.SpCudOptions;
            if (transaction == null)
            {
                return new SqlCommand(null, sqlConnection, null);
            }
            else
            {
                var sqlTransaction = (SqlTransaction)transaction.GetUnderlyingTransaction(config);
                return new SqlCommand(null, sqlConnection, sqlTransaction);
            }
        }

        #endregion Connection
    }
}