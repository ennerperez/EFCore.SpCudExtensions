using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace EFCore.SpCudExtensions.Tests
{
    public class EFCoreSpCudTest
    {
        protected int EntitiesNumber => 100000;

        private static Func<TestContext, int> ItemsCountQuery = EF.CompileQuery<TestContext, int>(ctx => ctx.Items.Count());
        private static Func<TestContext, Item> LastItemQuery = EF.CompileQuery<TestContext, Item>(ctx => ctx.Items.LastOrDefault());
        private static Func<TestContext, IEnumerable<Item>> AllItemsQuery = EF.CompileQuery<TestContext, IEnumerable<Item>>(ctx => ctx.Items.AsNoTracking());

        [Theory]
        [InlineData(true)]
        //[InlineData(false)] // for speed comparison with Regular EF CUD operations
        public void OperationsTest(bool runTest)
        {
            //DeletePreviousDatabase();

            //RunInsert();
            //RunInsertOrUpdate();
            //RunUpdate();
            //RunRead();
            //RunDelete();

            CheckQueryCache();
        }

        private void DeletePreviousDatabase()
        {
            using (var context = new TestContext(ContextUtil.GetOptions()))
            {
                context.Database.EnsureDeleted();
            }
        }

        private void CheckQueryCache()
        {
            using (var context = new TestContext(ContextUtil.GetOptions()))
            {
                var compiledQueryCache = ((MemoryCache)context.GetService<IMemoryCache>());

                Assert.Equal(0, compiledQueryCache.Count);
            }
        }

        private void WriteProgress(decimal percentage)
        {
            Debug.WriteLine(percentage);
        }
    }
}
