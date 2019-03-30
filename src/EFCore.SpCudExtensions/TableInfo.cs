using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.SpCudExtensions
{
    internal class TableInfo
    {
        public string CreateSpName { get; set; }
        public string UpdateSpName { get; set; }
        public string DeleteSpName { get; set; }
    }
}