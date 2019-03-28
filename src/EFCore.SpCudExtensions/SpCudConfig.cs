using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace EFCore.SpCudExtensions
{
    public class SpCudConfig
    {

        public bool SetOutputIdentity { get; set; }
        public int? NotifyAfter { get; set; }

        public bool EnableStreaming { get; set; }

        public bool TrackingEntities { get; set; }

        public List<string> PropertiesToInclude { get; set; }

        public List<string> PropertiesToExclude { get; set; }

        public List<string> UpdateByProperties { get; set; }

        public Func<DbConnection, DbConnection> UnderlyingConnection { get; set; }
        public Func<DbTransaction, DbTransaction> UnderlyingTransaction { get; set; }

        protected bool HasOutput { get; set; }
    }
}
