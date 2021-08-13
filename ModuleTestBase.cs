using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture;
using AutoFixture.AutoMoq;

using Geex.Common.Abstraction;

using ImpromptuInterface;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mongo2Go;

using MongoDB.Bson;
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
            base.AfterAddApplication(services);
            DB.MigrateTargetAsync(this.GetType().Assembly.ExportedTypes.Where(x => x.IsAssignableTo<IMigration>()).ToArray()).Wait();
        }

        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
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
