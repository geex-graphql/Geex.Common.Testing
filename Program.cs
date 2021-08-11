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
            var portWatcher = new PortWatcher();
            if (!portWatcher.IsPortAvailable(6379) || !portWatcher.IsPortAvailable(27017))
            {
                Console.WriteLine("默认端口6379/27017被占用, 按 Enter 继续启动...");
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Enter)
                {
                    return;
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
