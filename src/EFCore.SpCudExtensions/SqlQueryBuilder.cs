using System;
using System.Collections.Generic;
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
    }
}