using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace EFCore.SpCudExtensions
{
    public static class SqlQueryBuilder
    {

        public static string CreateInsertSp(TableInfo tableInfo)
        {
            var columnsDefs = tableInfo.ConvertibleProperties;
            if (tableInfo.TimeStampColumnName != null)
                columnsDefs.Remove(tableInfo.TimeStampColumnName);

            var columnsNames = columnsDefs.Keys.ToList();
            var q = $"CREATE PROCEDURE {tableInfo.SchemaFormated}[{tableInfo.TableName}_Insert] " + Environment.NewLine +
                    $"  {GetCommaSeparatedParamsWithTypes(columnsDefs)} " + Environment.NewLine +
                    $"AS " + Environment.NewLine +
                    $"  INSERT INTO {tableInfo.FullTableName} ({GetCommaSeparatedColumns(columnsNames)}) VALUES ({GetCommaSeparatedParams(columnsNames)}) " + Environment.NewLine +
                    $"RETURN";
            return q;
        }

        public static string GetCommaSeparatedColumns(List<string> columnsNames, string prefixTable = null, string equalsTable = null)
        {
            string commaSeparatedColumns = "";
            foreach (var columnName in columnsNames)
            {
                commaSeparatedColumns += prefixTable != null ? $"{prefixTable}.[{columnName}]" : $"[{columnName}]";
                commaSeparatedColumns += equalsTable != null ? $" = {equalsTable}.[{columnName}]" : "";
                commaSeparatedColumns += ", ";
            }
            if (commaSeparatedColumns != "")
            {
                commaSeparatedColumns = commaSeparatedColumns.Remove(commaSeparatedColumns.Length - 2, 2); // removes last excess comma and space: ", "
            }
            return commaSeparatedColumns;
        }

        public static string GetCommaSeparatedParams(List<string> columnsNames)
        {
            string commaSeparatedParams = "";
            foreach (var columnName in columnsNames)
            {
                commaSeparatedParams += $"@{columnName}";
                commaSeparatedParams += ", ";
            }
            if (commaSeparatedParams != "")
            {
                commaSeparatedParams = commaSeparatedParams.Remove(commaSeparatedParams.Length - 2, 2); // removes last excess comma and space: ", "
            }
            return commaSeparatedParams;
        }

        public static string GetCommaSeparatedParamsWithTypes(Dictionary<string, string> columnsDefs)
        {
            string commaSeparatedParamsWithTypes = "";
            foreach (var columDef in columnsDefs)
            {
                commaSeparatedParamsWithTypes += $"@{columDef.Key} {columDef.Value}";
                commaSeparatedParamsWithTypes += ", ";
            }
            if (commaSeparatedParamsWithTypes != "")
            {
                commaSeparatedParamsWithTypes = commaSeparatedParamsWithTypes.Remove(commaSeparatedParamsWithTypes.Length - 2, 2); // removes last excess comma and space: ", "
            }
            return commaSeparatedParamsWithTypes;
        }
    }
}