using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture;
using AutoFixture.AutoMoq;

using Geex.Common.Abstraction;
using Geex.Common.Abstractions;

using ImpromptuInterface;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

using Moq;

using Redis2Go;

using StackExchange.Redis.Extensions.Core.Configuration;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;

namespace Geex.Common.Testing
{
    public abstract class ModuleTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
        where TStartupModule : IAbpModule
    {
        public string TestName = typeof(TStartupModule).Name + "Testing";

        protected ModuleTestBase()
        {
            DB.Flush().Wait();
            DB.MigrateTargetAsync(this.GetType().Assembly.ExportedTypes.Where(x => x.IsAssignableTo<IMigration>()).ToArray()).Wait();
        }

        protected virtual async Task WithUow(Action action)
        {
            var func = new Func<Task>(() =>
            {
                action.Invoke();
                return Task.CompletedTask;
            });
            await this.WithUow(func);
        }
        protected virtual async Task WithUow(Func<Task> action)
        {
            var uow = GetRequiredService<IUnitOfWork>();
            await action.Invoke();
            await uow.CommitAsync();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { GeexClaim.AdminClaim }));
            //var a = options.Services.Where((Func<ServiceDescriptor, bool>)(s => s.ServiceType == typeof(ClaimsPrincipal))).ToList();
            //options.Services.RemoveAll(a);
            services.ReplaceAll(ServiceDescriptor.Singleton(claimsPrincipal));
            base.AfterAddApplication(services);
        }

        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
            var mockEvn = new Mock<IWebHostEnvironment>();
            mockEvn.Setup(x => x.EnvironmentName).Returns("UnitTest");
            options.Services.TryAdd(ServiceDescriptor.Singleton(mockEvn.Object));
            options.Services.Replace(ServiceDescriptor.Singleton(new GeexCoreModuleOptions()
            {
                AppName = TestName,
                ConnectionString = "mongodb://localhost:27017/",
                Redis = new RedisConfiguration()
                {
                    Database = 0,
                    ServiceName = TestName,
                    Hosts = new[]
                    {
                        new RedisHost()
                        {
                            Host = "localhost",
                            Port = 6379
                        }
                    }
                }
            }));
        }

    }
}
