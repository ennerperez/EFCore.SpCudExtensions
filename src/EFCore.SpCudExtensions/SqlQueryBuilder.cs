using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFCore.SpCudExtensions
{
    internal static class SqlQueryBuilder
    {
        public static string CheckSpExist(string fullSpName)
        {
            string q = null;
            q = $"IF OBJECT_ID ('{fullSpName}', 'P') IS NOT NULL SELECT 1 AS res ELSE SELECT 0 AS res;";
            return q;
        }

        public static string CreateInsertSp(TableInfo tableInfo)
        {
            var columnsDefs = new Dictionary<string, string>();
            //var columnsDefs = tableInfo.ConvertibleProperties;
            //if (tableInfo.TimeStampColumnName != null)
            //    columnsDefs.Remove(tableInfo.TimeStampColumnName);

            var columnsNames = columnsDefs.Keys.ToList();
            var q = $"CREATE PROCEDURE {tableInfo.SchemaFormated}[{tableInfo.TableName}_Insert] " + Environment.NewLine +
                    $"  {GetCommaSeparatedParamsWithTypes(columnsDefs)} " + Environment.NewLine +
                    $"AS " + Environment.NewLine +
                    $"  INSERT INTO {tableInfo.FullTableName} ({GetCommaSeparatedColumns(columnsNames)}) VALUES ({GetCommaSeparatedParams(columnsNames)}) " + Environment.NewLine +
                    $"RETURN";
            return q;
        }

        public static string CreateUpdateSp(string fullSpName)
        {
            string q = null;
            q = $"";
            return q;
        }

        public static string CreateDeleteSp(string fullSpName)
        {
            string q = null;
            q = $"";
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
            commaSeparatedColumns = commaSeparatedColumns.Remove(commaSeparatedColumns.Length - 2, 2); // removes last excess comma and space: ", "
            return commaSeparatedColumns;
        }

        public static string GetAndSeparatedColumns(List<string> columnsNames, string prefixTable = null, string equalsTable = null)
        {
            string commaSeparatedColumns = GetCommaSeparatedColumns(columnsNames, prefixTable, equalsTable);
            string andSeparatedColumns = commaSeparatedColumns.Replace(",", " AND");
            return andSeparatedColumns;
        }

        public static string GetCommaSeparatedParams(List<string> columnsNames)
        {
            string commaSeparatedParams = "";
            foreach (var columnName in columnsNames)
            {
                commaSeparatedParams += $"@{columnName}";
                commaSeparatedParams += ", ";
            }
            commaSeparatedParams = commaSeparatedParams.Remove(commaSeparatedParams.Length - 2, 2); // removes last excess comma and space: ", "
            return commaSeparatedParams;
        }

        public static string GetCommaSeparatedParamsWithTypes(Dictionary<string, string> columnsNames, string prefixTable = null, string equalsTable = null)
        {
            throw new NotImplementedException();
        }
    }
}