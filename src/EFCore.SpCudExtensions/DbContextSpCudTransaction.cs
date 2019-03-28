using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.SpCudExtensions
{
    internal static class DbContextSpCudTransaction
    {
        public static void Execute<T>(DbContext context, IList<T> entities, OperationType operationType, SpCudConfig spCudConfig, Action<decimal> progress) where T : class
        {
            if (entities.Count == 0)
            {
                return;
            }

            if (operationType == OperationType.Insert)
            {
            }
            else if (operationType == OperationType.Update)
            {
            }
            else if (operationType == OperationType.Delete)
            {
            }

        }

        public static Task ExecuteAsync<T>(DbContext context, IList<T> entities, OperationType operationType, SpCudConfig spCudConfig, Action<decimal> progress) where T : class
        {
            if (entities.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (operationType == OperationType.Insert)
            {
            }
            else if (operationType == OperationType.Update)
            {
            }
            else if (operationType == OperationType.Delete)
            {
            }

            return null;

        }
    }
}
