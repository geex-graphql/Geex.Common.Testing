using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Mongo2Go;
using Mongo2Go.Helper;

using Redis2Go;

namespace Geex.Common.Testing
{
    public class TestEnvironment : IDisposable
    {
        public Task<MongoDbRunner> Db;
        public Task<RedisRunner> Redis;
        public TestEnvironment(CancellationToken? token = default)
        {
            Db = Task.Run(() => MongoDbRunner.StartForDebugging(singleNodeReplSet: true), token.GetValueOrDefault());
            this.Db.ContinueWith(_ =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Mongodb启动完成, 连接字符串 " + _.Result.ConnectionString);
                    Console.ResetColor();
                }, TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(_ => Interlocked.Or(ref this.State, 0b01), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(_ => CheckFinish(), TaskContinuationOptions.NotOnFaulted);
            Redis = Task.Run(RedisRunner.StartForDebugging, token.GetValueOrDefault());
            Redis.ContinueWith(_ =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Redis(Memurai)启动完成, 端口 " + _.Result.Port);
                    Console.ResetColor();
                }, TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(_ => Interlocked.Or(ref this.State, 0b10), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(_ => CheckFinish(), TaskContinuationOptions.NotOnFaulted);
            token?.Register(this.Dispose, true);
            ;
            void CheckFinish()
            {
                if (Interlocked.Read(ref this.State) == 0b11)
                {
                    if (Db.Exception != default || Redis.Exception != default)
                    {
                        Console.WriteLine(Db.Exception);
                        Console.WriteLine(Redis.Exception);
                        this.Dispose();
                    }
                    this.Initialized?.Invoke();
                }
            }
        }
        public long State = 0;

        public event Action Initialized;

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (this.Db.Result.Disposed && this.Redis.Result.Disposed)
            {
                return;
            }

            if (Db?.Result != default)
            {
                try
                {
                    var mongoDbProcess = (typeof(MongoDbRunner).GetField("_mongoDbProcess", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Db?.Result) as MongoDbProcess);
                    var wrappedProcess = (typeof(MongoDbProcess).GetField("_process", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mongoDbProcess) as WrappedProcess);
                    wrappedProcess.DoNotKill = false;
                    Db?.Result?.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Redis?.Result?.Dispose();
        }
    }
}
