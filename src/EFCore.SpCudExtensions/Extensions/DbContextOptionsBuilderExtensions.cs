using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.SpCudExtensions.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseSpCudGenetarion(this DbContextOptionsBuilder optionsBuilder)
        {
            return optionsBuilder.UseSpCudGenetarion(new SpCudProvider());
        }

        public static DbContextOptionsBuilder UseSpCudGenetarion(this DbContextOptionsBuilder optionsBuilder, ISpCudProvider spCudProvider)
        {
            //optionsBuilder.ReplaceService<IQueryCompiler, CustomQueryCompiler>();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new SpCudOptionsExtension(spCudProvider));

            return optionsBuilder;
        }
    }
}