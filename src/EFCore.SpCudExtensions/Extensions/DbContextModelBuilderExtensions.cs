using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.SpCudExtensions.Extensions
{
    public static class DbContextModelBuilderExtensions
    {
        public static void MapToStoredProcedures(this ModelBuilder modelBuilder)
        {
            foreach (var item in modelBuilder.Model.GetEntityTypes())
            {
            }
        }

        public static void MapToStoredProcedures<T>(this EntityTypeBuilder<T> model) where T : class
        {
            var type = typeof(T);
        }
    }
}