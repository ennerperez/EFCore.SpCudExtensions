using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFCore.SpCudExtensions
{
    public static class DbContextSpCudExtensions
    {
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

        /* ModelBuilder */

        //public static void MapToStoredProcedures(this ModelBuilder modelBuilder)
        //{
        //    foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        //    {
        //        if (!entity.IsOwned()) // without this exclusion OwnedType would not be by default in Owner Table
        //        {
        //            var tableInfo = new TableInfo();
        //            tableInfo.LoadData(entity, false);
        //            var qInsert = SqlQueryBuilder.CreateInsertSp(tableInfo);
        //        }
        //    }
        //}

        //public static EntityTypeBuilder<TEntityType> MapToStoredProcedures<TEntityType>(this EntityTypeBuilder<TEntityType> entity, DbContext context) where TEntityType : class
        //{
        //    var type = typeof(TEntityType);
        //    var entityType = entity.Metadata.Model.FindEntityType(type);
        //    var tableInfo = new TableInfo();
        //    tableInfo.LoadData<TEntityType>(entityType, false);

        //    var qInsert = SqlQueryBuilder.CreateInsertSp(tableInfo);

        //    var services = entity.Metadata.GetServiceProperties();

        //    return entity;
        //}
    }
}