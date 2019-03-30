using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace EFCore.SpCudExtensions
{
    public class SpCudProvider : ISpCudProvider
    {
        internal bool CheckSpExist(DbContext context, TableInfo tableInfo)
        {
            bool tableExist = false;
            var sqlConnection = context.Database.GetDbConnection();
            var currentTransaction = context.Database.CurrentTransaction;
            try
            {
                if (currentTransaction == null)
                {
                    if (sqlConnection.State != ConnectionState.Open)
                        sqlConnection.Open();
                }
                using (var command = sqlConnection.CreateCommand())
                {
                    if (currentTransaction != null)
                        command.Transaction = currentTransaction.GetDbTransaction();
                    command.CommandText = SqlQueryBuilder.CheckSpExist(tableInfo.CreateSpName);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                tableExist = (int)reader[0] == 1;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (currentTransaction == null)
                    sqlConnection.Close();
            }
            return tableExist;
        }
    }
}