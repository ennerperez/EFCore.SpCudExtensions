using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EFCore.SpCudExtensions
{
    public class SpCudOptionsExtension : IDbContextOptionsExtension
    {
        private ISpCudProvider _spCudProvider;

        internal SpCudOptionsExtension(ISpCudProvider cacheProvider)
        {
            _spCudProvider = cacheProvider;
        }

        public string LogFragment => $"Using {_spCudProvider.GetType().Name}";

        public bool ApplyServices(IServiceCollection services)
        {
            services.AddSingleton<ISpCudProvider>(_spCudProvider);
            ////{Microsoft.EntityFrameworkCore.Storage.CoreTypeMapperDependencies}
            //var sql = services.Where(m => m.ServiceType.Name.Contains("SqlServer"));
            //var mapp = services.Where(m => m.ServiceType.Name.Contains("Mapp"));
            return false;
        }

        public long GetServiceProviderHashCode() => 0L;

        public void Validate(IDbContextOptions options)
        {
        }

        public virtual ISpCudProvider SpCudProvider => _spCudProvider;
    }
}