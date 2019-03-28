using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFCore.SpCudExtensions
{
    public static class DbContextSpCudExtensions
    {
        /* ModelBuilder */
        public static EntityTypeBuilder<TEntityType> MapToStoredProcedures<TEntityType>(this EntityTypeBuilder<TEntityType> entity, DbContext context) where TEntityType : class
        {

            var type = typeof(TEntityType);
            var entityType = entity.Metadata.Model.FindEntityType(type);
            var tableInfo = new TableInfo();
            tableInfo.LoadData<TEntityType>(entityType, false);
            var qInsert = SqlQueryBuilder.CreateInsertSp(tableInfo);

            return entity;
        }

        /* DbContext */
        public static void SpCudInsert<T>(this DbContext context, IList<T> entities, SpCudConfig spCudConfig = null, Action<decimal> progress = null) where T : class
        {
            DbContextSpCudTransaction.Execute(context, entities, OperationType.Insert, spCudConfig, progress);
        }

        public static void SpCudInsert<T>(this DbContext context, IList<T> entities, Action<SpCudConfig> spCudAction, Action<decimal> progress = null) where T : class
        {
            SpCudConfig spCudConfig = new SpCudConfig();
            spCudAction?.Invoke(spCudConfig);
            DbContextSpCudTransaction.Execute(context, entities, OperationType.Insert, spCudConfig, progress);
        }
    }
}
