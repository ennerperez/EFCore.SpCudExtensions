using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FastMember;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EFCore.SpCudExtensions
{
    public class TableInfo
    {
        public TableInfo()
        {
            SpCudConfig = new SpCudConfig();
        }

        public string Schema { get; set; }
        public string SchemaFormated => Schema != null ? $"[{Schema}]." : "";
        public string TableName { get; set; }
        public string FullTableName => $"{SchemaFormated}[{TableName}]";
        public List<string> PrimaryKeys { get; set; }
        public bool HasSinglePrimaryKey { get; set; }
        public bool UpdateByPropertiesAreNullable { get; set; }

        public bool InsertToTempTable { get; set; }
        public bool HasIdentity { get; set; }
        public bool HasOwnedTypes { get; set; }
        public bool ColumnNameContainsSquareBracket { get; set; }
        public bool LoadOnlyPKColumn { get; set; }

        //public int NumberOfEntities { get; set; }

        public SpCudConfig SpCudConfig { get; set; }
        public Dictionary<string, string> OutputPropertyColumnNamesDict { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PropertyColumnNamesDict { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, INavigation> OwnedTypesDict { get; set; } = new Dictionary<string, INavigation>();
        public HashSet<string> ShadowProperties { get; set; } = new HashSet<string>();
        public Dictionary<string, ValueConverter> ConvertibleProperties { get; set; } = new Dictionary<string, ValueConverter>();
        public string TimeStampOutColumnType => "varbinary(8)";
        public string TimeStampColumnName { get; set; }

        public static TableInfo CreateInstance<T>(DbContext context, IList<T> entities, OperationType operationType, SpCudConfig spCudConfig)
        {
            var tableInfo = new TableInfo
            {
                //NumberOfEntities = entities.Count,
                SpCudConfig = spCudConfig ?? new SpCudConfig()
            };

            bool isExplicitTransaction = context.Database.GetDbConnection().State == ConnectionState.Open;
            var isDeleteOperation = operationType == OperationType.Delete;
            tableInfo.LoadData<T>(context, isDeleteOperation);
            return tableInfo;
        }

        #region Main

        public void LoadData<T>(DbContext context, bool loadOnlyPKColumn)
        {
            LoadOnlyPKColumn = loadOnlyPKColumn;
            var entityType = context.Model.FindEntityType(typeof(T));
            if (entityType == null)
                throw new InvalidOperationException("DbContext does not contain EntitySet for Type: " + typeof(T).Name);

            var relationalData = entityType.Relational();
            Schema = relationalData.Schema ?? "dbo";
            TableName = relationalData.TableName;

            bool AreSpecifiedUpdateByProperties = SpCudConfig.UpdateByProperties?.Count() > 0; // TODO
            var primaryKeys = entityType.FindPrimaryKey().Properties.Select(a => a.Name).ToList();
            HasSinglePrimaryKey = primaryKeys.Count == 1;
            PrimaryKeys = AreSpecifiedUpdateByProperties ? SpCudConfig.UpdateByProperties : primaryKeys;

            var allProperties = entityType.GetProperties().AsEnumerable();

            var ownedTypes = entityType.GetNavigations().Where(a => a.GetTargetType().IsOwned());
            HasOwnedTypes = ownedTypes.Any();
            OwnedTypesDict = ownedTypes.ToDictionary(a => a.Name, a => a);

            // timestamp/row version properties are only set by the Db, the property has a [Timestamp] Attribute or is configured in in FluentAPI with .IsRowVersion()
            // They can be identified by the columne type "timestamp" or .IsConcurrencyToken in combination with .ValueGenerated == ValueGenerated.OnAddOrUpdate
            string timestampDbTypeName = nameof(TimestampAttribute).Replace("Attribute", "").ToLower(); // = "timestamp";
            var timeStampProperties = allProperties.Where(a => (a.IsConcurrencyToken && a.ValueGenerated == ValueGenerated.OnAddOrUpdate) || a.Relational().ColumnType == timestampDbTypeName);
            TimeStampColumnName = timeStampProperties.FirstOrDefault()?.Relational().ColumnName; // can be only One
            var allPropertiesExceptTimeStamp = allProperties.Except(timeStampProperties);
            var properties = allPropertiesExceptTimeStamp.Where(a => a.Relational().ComputedColumnSql == null);

            // TimeStamp prop. is last column in OutputTable since it is added later with varbinary(8) type in which Output can be inserted
            OutputPropertyColumnNamesDict = allPropertiesExceptTimeStamp.Concat(timeStampProperties).ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]")); // square brackets have to be escaped
            ColumnNameContainsSquareBracket = allPropertiesExceptTimeStamp.Concat(timeStampProperties).Any(a => a.Relational().ColumnName.Contains("]"));

            bool AreSpecifiedPropertiesToInclude = SpCudConfig.PropertiesToInclude?.Count() > 0;
            bool AreSpecifiedPropertiesToExclude = SpCudConfig.PropertiesToExclude?.Count() > 0;

            if (AreSpecifiedPropertiesToInclude)
            {
                if (AreSpecifiedUpdateByProperties) // Adds UpdateByProperties to PropertyToInclude if they are not already explicitly listed
                {
                    foreach (var updateByProperty in SpCudConfig.UpdateByProperties)
                    {
                        if (!SpCudConfig.PropertiesToInclude.Contains(updateByProperty))
                        {
                            SpCudConfig.PropertiesToInclude.Add(updateByProperty);
                        }
                    }
                }
                else
                {
                    foreach (var primaryKey in PrimaryKeys)
                    {
                        if (!SpCudConfig.PropertiesToInclude.Contains(primaryKey))
                        {
                            SpCudConfig.PropertiesToInclude.Add(primaryKey);
                        }
                    }
                }
            }

            UpdateByPropertiesAreNullable = properties.Any(a => PrimaryKeys.Contains(a.Name) && a.IsNullable);

            if (AreSpecifiedPropertiesToInclude || AreSpecifiedPropertiesToExclude)
            {
                if (AreSpecifiedPropertiesToInclude && AreSpecifiedPropertiesToExclude)
                    throw new InvalidOperationException("Only one group of properties, either PropertiesToInclude or PropertiesToExclude can be specifed, specifying both not allowed.");
                if (AreSpecifiedPropertiesToInclude)
                    properties = properties.Where(a => SpCudConfig.PropertiesToInclude.Contains(a.Name));
                if (AreSpecifiedPropertiesToExclude)
                    properties = properties.Where(a => !SpCudConfig.PropertiesToExclude.Contains(a.Name));
            }

            if (loadOnlyPKColumn)
            {
                PropertyColumnNamesDict = properties.Where(a => PrimaryKeys.Contains(a.Name)).ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]"));
            }
            else
            {
                PropertyColumnNamesDict = properties.ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]"));
                ShadowProperties = new HashSet<string>(properties.Where(p => p.IsShadowProperty).Select(p => p.Relational().ColumnName));
                foreach (var property in properties.Where(p => p.GetValueConverter() != null))
                {
                    string columnName = property.Relational().ColumnName;
                    ValueConverter converter = property.GetValueConverter();
                    ConvertibleProperties.Add(columnName, converter);
                }

                if (HasOwnedTypes)  // Support owned entity property update. TODO: Optimize
                {
                    var type = typeof(T);
                    foreach (var navgationProperty in ownedTypes)
                    {
                        var property = navgationProperty.PropertyInfo;
                        Type navOwnedType = type.Assembly.GetType(property.PropertyType.FullName);
                        var ownedEntityType = context.Model.FindEntityType(property.PropertyType);
                        if (ownedEntityType == null) // when entity has more then one ownedType (e.g. Address HomeAddress, Address WorkAddress) or one ownedType is in multiple Entities like Audit is usually.
                        {
                            ownedEntityType = context.Model.GetEntityTypes().SingleOrDefault(a => a.DefiningNavigationName == property.Name && a.DefiningEntityType.Name == entityType.Name);
                        }
                        var ownedEntityProperties = ownedEntityType.GetProperties().ToList();
                        var ownedEntityPropertyNameColumnNameDict = new Dictionary<string, string>();

                        foreach (var ownedEntityProperty in ownedEntityProperties)
                        {
                            if (!ownedEntityProperty.IsPrimaryKey())
                            {
                                string columnName = ownedEntityProperty.Relational().ColumnName;
                                ownedEntityPropertyNameColumnNameDict.Add(ownedEntityProperty.Name, columnName);
                            }
                        }
                        var ownedProperties = property.PropertyType.GetProperties();
                        foreach (var ownedProperty in ownedProperties)
                        {
                            if (ownedEntityPropertyNameColumnNameDict.ContainsKey(ownedProperty.Name))
                            {
                                string columnName = ownedEntityPropertyNameColumnNameDict[ownedProperty.Name];
                                var ownedPropertyType = Nullable.GetUnderlyingType(ownedProperty.PropertyType) ?? ownedProperty.PropertyType;
                                PropertyColumnNamesDict.Add(property.Name + "." + ownedProperty.Name, columnName);
                                OutputPropertyColumnNamesDict.Add(property.Name + "." + ownedProperty.Name, columnName);
                            }
                        }
                    }
                }
            }
        }

        public void SetSqlSpCudConfig<T>(SqlCommand sqlSpCud, IList<T> entities, bool setColumnMapping, Action<decimal> progress)
        {
            sqlSpCud.StatementCompleted += (sender, e) =>
            {
                progress?.Invoke((decimal)(e.RecordCount * 10000 / entities.Count) / 10000); // round to 4 decimal places
            };
            sqlSpCud.CommandTimeout = SpCudConfig.CommandTimeout ?? sqlSpCud.CommandTimeout;

            if (setColumnMapping)
            {
                foreach (var element in PropertyColumnNamesDict)
                {
                    //TODO: Dynamic param builder
                    sqlSpCud.Parameters.Add(new SqlParameter(element.Key, element.Value));
                }
            }
        }

        //public void LoadData(IMutableEntityType entityType, bool loadOnlyPKColumn)
        //{
        //    LoadOnlyPKColumn = loadOnlyPKColumn;
        //    if (entityType == null)
        //        throw new InvalidOperationException("DbContext does not contain EntitySet for Type: " + entityType.Name);

        //    var relationalData = entityType.Relational();
        //    Schema = relationalData.Schema ?? "dbo";
        //    TableName = relationalData.TableName;

        //    bool AreSpecifiedUpdateByProperties = SpCudConfig.UpdateByProperties?.Count() > 0;
        //    var primaryKeys = entityType.FindPrimaryKey().Properties.Select(a => a.Name).ToList();
        //    HasSinglePrimaryKey = primaryKeys.Count == 1;
        //    PrimaryKeys = AreSpecifiedUpdateByProperties ? SpCudConfig.UpdateByProperties : primaryKeys;

        //    var allProperties = entityType.GetProperties().AsEnumerable();

        //    var ownedTypes = allProperties; // entityType.GetNavigations().Where(a => a.GetTargetType().IsOwned());
        //    HasOwnedTypes = ownedTypes.Any();
        //    //OwnedTypesDict = ownedTypes.ToDictionary(a => a.Name, a => a);

        //    // timestamp/row version properties are only set by the Db, the property has a [Timestamp] Attribute or is configured in in FluentAPI with .IsRowVersion()
        //    // They can be identified by the columne type "timestamp" or .IsConcurrencyToken in combination with .ValueGenerated == ValueGenerated.OnAddOrUpdate
        //    string timestampDbTypeName = nameof(TimestampAttribute).Replace("Attribute", "").ToLower(); // = "timestamp";
        //    var timeStampProperties = allProperties.Where(a => (a.IsConcurrencyToken && a.ValueGenerated == ValueGenerated.OnAddOrUpdate) || a.Relational().ColumnType == timestampDbTypeName);
        //    TimeStampColumnName = timeStampProperties.FirstOrDefault()?.Relational().ColumnName; // can be only One
        //    var allPropertiesExceptTimeStamp = allProperties.Except(timeStampProperties);
        //    var properties = allPropertiesExceptTimeStamp.Where(a => a.Relational().ComputedColumnSql == null);

        //    // TimeStamp prop. is last column in OutputTable since it is added later with varbinary(8) type in which Output can be inserted
        //    OutputPropertyColumnNamesDict = allPropertiesExceptTimeStamp.Concat(timeStampProperties).ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]")); // square brackets have to be escaped
        //    ColumnNameContainsSquareBracket = allPropertiesExceptTimeStamp.Concat(timeStampProperties).Any(a => a.Relational().ColumnName.Contains("]"));

        //    bool AreSpecifiedPropertiesToInclude = SpCudConfig.PropertiesToInclude?.Count() > 0;
        //    bool AreSpecifiedPropertiesToExclude = SpCudConfig.PropertiesToExclude?.Count() > 0;

        //    if (AreSpecifiedPropertiesToInclude)
        //    {
        //        if (AreSpecifiedUpdateByProperties) // Adds UpdateByProperties to PropertyToInclude if they are not already explicitly listed
        //        {
        //            foreach (var updateByProperty in SpCudConfig.UpdateByProperties)
        //            {
        //                if (!SpCudConfig.PropertiesToInclude.Contains(updateByProperty))
        //                {
        //                    SpCudConfig.PropertiesToInclude.Add(updateByProperty);
        //                }
        //            }
        //        }
        //        else // Adds PrimaryKeys to PropertyToInclude if they are not already explicitly listed
        //        {
        //            foreach (var primaryKey in PrimaryKeys)
        //            {
        //                if (!SpCudConfig.PropertiesToInclude.Contains(primaryKey))
        //                {
        //                    SpCudConfig.PropertiesToInclude.Add(primaryKey);
        //                }
        //            }
        //        }
        //    }

        //    UpdateByPropertiesAreNullable = properties.Any(a => PrimaryKeys.Contains(a.Name) && a.IsNullable);

        //    if (AreSpecifiedPropertiesToInclude || AreSpecifiedPropertiesToExclude)
        //    {
        //        if (AreSpecifiedPropertiesToInclude && AreSpecifiedPropertiesToExclude)
        //            throw new InvalidOperationException("Only one group of properties, either PropertiesToInclude or PropertiesToExclude can be specifed, specifying both not allowed.");
        //        if (AreSpecifiedPropertiesToInclude)
        //            properties = properties.Where(a => SpCudConfig.PropertiesToInclude.Contains(a.Name));
        //        if (AreSpecifiedPropertiesToExclude)
        //            properties = properties.Where(a => !SpCudConfig.PropertiesToExclude.Contains(a.Name));
        //    }

        //    if (loadOnlyPKColumn)
        //    {
        //        PropertyColumnNamesDict = properties.Where(a => PrimaryKeys.Contains(a.Name)).ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]"));
        //    }
        //    else
        //    {
        //        PropertyColumnNamesDict = properties.ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]"));
        //        ShadowProperties = new HashSet<string>(properties.Where(p => p.IsShadowProperty).Select(p => p.Relational().ColumnName));
        //        foreach (var property in properties)//.Where(p => p.GetValueConverter() != null))
        //        {
        //            string columnName = property.Relational().ColumnName;
        //            string columType = property.Relational().ColumnType ?? property.FindRelationalMapping()?.StoreType;
        //            //string columnLenght = property.Relational().IsFixedLength ? property.Relational();
        //            ValueConverter converter = property.GetValueConverter();

        //            if (string.IsNullOrEmpty(columType))
        //            {
        //                if (property.ClrType == typeof(short) || property.ClrType == typeof(short?))
        //                    columType = "smallint";
        //                else if (property.ClrType == typeof(int) || property.ClrType == typeof(int?))
        //                    columType = "int";
        //                else if (property.ClrType == typeof(long) || property.ClrType == typeof(long?))
        //                    columType = "bigint";
        //                else if (property.ClrType == typeof(string))
        //                    columType = $"nvarchar(max)"; //TODO
        //                else if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
        //                    columType = "datetime"; //TODO: datetime2
        //                else if (property.ClrType == typeof(byte) || property.ClrType == typeof(byte?))
        //                    columType = $"varbinary(max)"; //TODO
        //            }

        //            ConvertibleProperties.Add(columnName, columType);
        //        }
        //    }
        //}

        //public void LoadData<T>(IEntityType entityType, bool loadOnlyPKColumn)
        //{
        //    LoadOnlyPKColumn = loadOnlyPKColumn;
        //    if (entityType == null)
        //        throw new InvalidOperationException("DbContext does not contain EntitySet for Type: " + typeof(T).Name);

        //    var relationalData = entityType.Relational();
        //    Schema = relationalData.Schema ?? "dbo";
        //    TableName = relationalData.TableName;

        //    bool AreSpecifiedUpdateByProperties = SpCudConfig.UpdateByProperties?.Count() > 0;
        //    var primaryKeys = entityType.FindPrimaryKey().Properties.Select(a => a.Name).ToList();
        //    HasSinglePrimaryKey = primaryKeys.Count == 1;
        //    PrimaryKeys = AreSpecifiedUpdateByProperties ? SpCudConfig.UpdateByProperties : primaryKeys;

        //    var allProperties = entityType.GetProperties().AsEnumerable();

        //    var ownedTypes = allProperties; // entityType.GetNavigations().Where(a => a.GetTargetType().IsOwned());
        //    HasOwnedTypes = ownedTypes.Any();
        //    //OwnedTypesDict = ownedTypes.ToDictionary(a => a.Name, a => a);

        //    // timestamp/row version properties are only set by the Db, the property has a [Timestamp] Attribute or is configured in in FluentAPI with .IsRowVersion()
        //    // They can be identified by the columne type "timestamp" or .IsConcurrencyToken in combination with .ValueGenerated == ValueGenerated.OnAddOrUpdate
        //    string timestampDbTypeName = nameof(TimestampAttribute).Replace("Attribute", "").ToLower(); // = "timestamp";
        //    var timeStampProperties = allProperties.Where(a => (a.IsConcurrencyToken && a.ValueGenerated == ValueGenerated.OnAddOrUpdate) || a.Relational().ColumnType == timestampDbTypeName);
        //    TimeStampColumnName = timeStampProperties.FirstOrDefault()?.Relational().ColumnName; // can be only One
        //    var allPropertiesExceptTimeStamp = allProperties.Except(timeStampProperties);
        //    var properties = allPropertiesExceptTimeStamp.Where(a => a.Relational().ComputedColumnSql == null);

        //    // TimeStamp prop. is last column in OutputTable since it is added later with varbinary(8) type in which Output can be inserted
        //    OutputPropertyColumnNamesDict = allPropertiesExceptTimeStamp.Concat(timeStampProperties).ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]")); // square brackets have to be escaped
        //    ColumnNameContainsSquareBracket = allPropertiesExceptTimeStamp.Concat(timeStampProperties).Any(a => a.Relational().ColumnName.Contains("]"));

        //    bool AreSpecifiedPropertiesToInclude = SpCudConfig.PropertiesToInclude?.Count() > 0;
        //    bool AreSpecifiedPropertiesToExclude = SpCudConfig.PropertiesToExclude?.Count() > 0;

        //    if (AreSpecifiedPropertiesToInclude)
        //    {
        //        if (AreSpecifiedUpdateByProperties) // Adds UpdateByProperties to PropertyToInclude if they are not already explicitly listed
        //        {
        //            foreach (var updateByProperty in SpCudConfig.UpdateByProperties)
        //            {
        //                if (!SpCudConfig.PropertiesToInclude.Contains(updateByProperty))
        //                {
        //                    SpCudConfig.PropertiesToInclude.Add(updateByProperty);
        //                }
        //            }
        //        }
        //        else // Adds PrimaryKeys to PropertyToInclude if they are not already explicitly listed
        //        {
        //            foreach (var primaryKey in PrimaryKeys)
        //            {
        //                if (!SpCudConfig.PropertiesToInclude.Contains(primaryKey))
        //                {
        //                    SpCudConfig.PropertiesToInclude.Add(primaryKey);
        //                }
        //            }
        //        }
        //    }

        //    UpdateByPropertiesAreNullable = properties.Any(a => PrimaryKeys.Contains(a.Name) && a.IsNullable);

        //    if (AreSpecifiedPropertiesToInclude || AreSpecifiedPropertiesToExclude)
        //    {
        //        if (AreSpecifiedPropertiesToInclude && AreSpecifiedPropertiesToExclude)
        //            throw new InvalidOperationException("Only one group of properties, either PropertiesToInclude or PropertiesToExclude can be specifed, specifying both not allowed.");
        //        if (AreSpecifiedPropertiesToInclude)
        //            properties = properties.Where(a => SpCudConfig.PropertiesToInclude.Contains(a.Name));
        //        if (AreSpecifiedPropertiesToExclude)
        //            properties = properties.Where(a => !SpCudConfig.PropertiesToExclude.Contains(a.Name));
        //    }

        //    if (loadOnlyPKColumn)
        //    {
        //        PropertyColumnNamesDict = properties.Where(a => PrimaryKeys.Contains(a.Name)).ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]"));
        //    }
        //    else
        //    {
        //        PropertyColumnNamesDict = properties.ToDictionary(a => a.Name, b => b.Relational().ColumnName.Replace("]", "]]"));
        //        ShadowProperties = new HashSet<string>(properties.Where(p => p.IsShadowProperty).Select(p => p.Relational().ColumnName));
        //        foreach (var property in properties)//.Where(p => p.GetValueConverter() != null))
        //        {
        //            string columnName = property.Relational().ColumnName;
        //            string columType = property.Relational().ColumnType ?? property.FindRelationalMapping()?.StoreType;
        //            //ValueConverter converter = property.GetValueConverter();
        //            ConvertibleProperties.Add(columnName, columType);
        //        }
        //    }
        //}

        #endregion Main

        public static string GetUniquePropertyValues<T>(T entity, List<string> propertiesNames, TypeAccessor accessor)
        {
            string result = String.Empty;
            foreach (var propertyName in propertiesNames)
            {
                result += accessor[entity, propertyName];
            }
            return result;
        }
    }
}