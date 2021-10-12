using System;
using System.Threading;
using System.Threading.Tasks;

using Mongo2Go.Helper;

namespace Geex.Common.Testing
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // 通过args 来开启MongoDb或者Redis
            string startProgram = args.Length > 0 ? args[0] : "mongoredis";

            Console.WriteLine("-------------------------------argsStart---------------------------------------");
            Console.WriteLine(startProgram);
            Console.WriteLine("-------------------------------argsEnd---------------------------------------");

            TestEnvironment.startProgram = startProgram;

            var portWatcher = new PortWatcher();

            if (startProgram.Contains("redis"))
            {
                if (!portWatcher.IsPortAvailable(6379))
                {
                    Console.WriteLine("默认端口6379被占用, 按 Enter 继续启动...");
                    var key = Console.ReadKey();
                    if (key.Key != ConsoleKey.Enter)
                    {
                        return;
                    }
                }
            }


            if (startProgram.Contains("mongo"))
            {
                if (!portWatcher.IsPortAvailable(27017))
                {
                    Console.WriteLine("默认端口27017被占用, 按 Enter 继续启动...");
                    var key = Console.ReadKey();
                    if (key.Key != ConsoleKey.Enter)
                    {
                        return;
                    }
                }
            }


            Console.WriteLine("正在启动开发依赖环境...");
            var cancel = new CancellationTokenSource();
            var testEnvironment = new TestEnvironment(cancel.Token);
            var task = Task.Delay(-1, cancel.Token);
            Task.Run(() => task);
            testEnvironment.Initialized += () =>
              {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine("按 Ees 关闭开发依赖环境...直接关闭本窗口可能会导致环境依赖重复启动(需要在任务管理器中结束对应进程).");
                  Console.ResetColor();
              };
            while (!cancel.IsCancellationRequested)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    cancel.Cancel();
                }
            }
        }
    }
}
