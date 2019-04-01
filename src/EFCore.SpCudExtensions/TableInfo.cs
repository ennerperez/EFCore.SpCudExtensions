using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.SpCudExtensions
{
    public class TableInfo
    {
        public string InsertSpName => $"{SchemaFormated}[{TableName}_Insert]";
        public string UpdateSpName => $"{SchemaFormated}[{TableName}_Update]";
        public string DeleteSpName => $"{SchemaFormated}[{TableName}_Delete]";

        public string Schema { get; set; }
        public string SchemaFormated => Schema != null ? $"[{Schema}]." : "";
        public string TableName { get; set; }
        public string FullTableName => $"{SchemaFormated}[{TableName}]";
    }
}