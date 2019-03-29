//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace EFCore.SpCudExtensions
//{
//    public interface ISpCudProvider
//    {
//    }

//    public class SpCudExtension : IDbContextOptionsExtension
//    {
//        private ISpCudProvider _spCudProvider;

//        internal SpCudExtension(ISpCudProvider spCudProvider)
//        {
//            _spCudProvider = spCudProvider;
//        }

//        public string LogFragment => $"Using {_spCudProvider.GetType().Name}";

//        public bool ApplyServices(IServiceCollection services)
//        {
//            services.AddSingleton<ISpCudProvider>(_spCudProvider);

//            return false;
//        }

//        public long GetServiceProviderHashCode() => 0L;

//        public void Validate(IDbContextOptions options)
//        {
//        }

//        public virtual ISpCudProvider SpCudProvider => _spCudProvider;
//    }
//}