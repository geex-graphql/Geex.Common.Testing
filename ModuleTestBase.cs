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
        public static TestEnvironment TestEnvironment { get; } = new TestEnvironment();
        public string TestName = typeof(TStartupModule).Name + "Testing";
        public static IFixture Fixture { get; protected set; } = new Fixture();

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

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose()
        {
            TestEnvironment.Dispose();
            this.Application?.Shutdown();
            this.TestServiceScope?.Dispose();
            this.Application?.Dispose();
        }

        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            Fixture.Register<string>(() => ObjectId.GenerateNewId().ToString());
            Fixture.Customize(new AutoMoqCustomization()
            {
                ConfigureMembers = true,
                GenerateDelegates = true
            });
            options.UseAutofac();
            //        Fixture.Register<IWebHostEnvironment>(() =>
            //new
            //{
            //    ApplicationName = TestName,
            //    EnvironmentName = "Testing"
            //}.ActLike<IWebHostEnvironment>());
            //        options.Services.AddSingleton<IWebHostEnvironment>(Fixture.Create<IWebHostEnvironment>());
            options.Services.Replace(ServiceDescriptor.Singleton(new GeexCoreModuleOptions()
            {
                AppName = TestName,
                ConnectionString = TestEnvironment.Db.Result.ConnectionString,
                Redis = new RedisConfiguration()
                {
                    Database = 0,
                    ServiceName = TestName,
                    Hosts = new[]
                    {
                        new RedisHost()
                        {
                            Host = "localhost",
                            Port = TestEnvironment.Redis.Result.Port
                        }
                    }
                }
            }));
        }

    }
}
