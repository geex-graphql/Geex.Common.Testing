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
                Console.WriteLine("Ĭ�϶˿�6379/27017��ռ��, �� Enter ��������...");
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Enter)
                {
                    return;
                }
            }
            Console.WriteLine("��������������������...");
            var cancel = new CancellationTokenSource();
            var testEnvironment = new TestEnvironment(cancel.Token);
            var task = Task.Delay(-1, cancel.Token);
            Task.Run(() => task);
            testEnvironment.Initialized += () =>
              {
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.WriteLine("�� Ees �رտ�����������...ֱ�ӹرձ����ڿ��ܻᵼ�»��������ظ�����(��Ҫ������������н�����Ӧ����).");
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
