using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.SpCudExtensions
{
    public interface ISpCudProvider
    {
        bool CheckSpExist(DbContext context, TableInfo tableInfo);

        bool CreateInsertSp(DbContext context, TableInfo tableInfo)
    }
}