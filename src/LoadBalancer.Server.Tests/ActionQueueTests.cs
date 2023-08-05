using LoadBalancer.Server.Common;
using static LoadBalancer.Extensions.Comparison;

namespace LoadBalancer.Server.Tests
{
    [TestClass]
    public class ActionQueueTests
    {
        private static readonly Random random = new Random((int)DateTime.UtcNow.Ticks);

        [TestMethod]
        public void Enqueue_5Actions_Fast()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    queue.Enqueue(() =>
                    {
                        list.Add($"{index}");
                        WriteLine($"{index}");
                    });
                }

                Thread.Sleep(1000);
            }

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4" }, list.ToArray()));
        }

        [TestMethod]
        public void Enqueue_5Actions_WithException()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    queue.Enqueue(() =>
                    {
                        list.Add($"{index}");
                        WriteLine($"{index}");
                        if ((index % 2) == 0)
                            throw new Exception($"{index} exception");
                    });
                }

                Thread.Sleep(1000);
            }

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4" }, list.ToArray()));
        }

        [TestMethod]
        public void Enqueue_5Actions_Delay()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    queue.Enqueue(() =>
                    {
                        Thread.Sleep(random.Next(100));
                        list.Add($"{index}");
                        WriteLine($"{index}");
                    });
                }

                Thread.Sleep(1000);
            }

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4" }, list.ToArray()));
        }

        [TestMethod]
        public void Enqueue_10Actions_DisposeAfter5th()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 10; i++)
                {
                    int index = i;
                    queue.Enqueue(() =>
                    {
                        Thread.Sleep(100);
                        list.Add($"{index}");
                        WriteLine($"{index}");
                    });
                }

                Thread.Sleep(500);
            }

            Thread.Sleep(500);
            Thread.Sleep(150);

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }, list.ToArray()));
        }

        [TestMethod]
        public void Enqueue_AsyncActions()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    queue.Enqueue(async () =>
                    {
                        await Task.Delay(100);
                        list.Add($"{index}");
                        WriteLine($"{index}");
                    });
                }

                Thread.Sleep(1000);
            }

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4" }, list.ToArray()));
        }

        [TestMethod]
        public void Enqueue_AsyncFunctions()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    queue.Enqueue(async () =>
                    {
                        await Task.Delay(100);
                        list.Add($"{index}");
                        WriteLine($"{index}");
                        return (index % 2) == 0;
                    });
                }

                Thread.Sleep(1000);
            }

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4" }, list.ToArray()));
        }

        [TestMethod]
        public void Enqueue_AsyncActions_WithException()
        {
            var list = new List<string>();
            using (var queue = new ActionQueue())
            {
                queue.Start();

                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    queue.Enqueue(async () =>
                    {
                        await Task.Delay(100);
                        list.Add($"{index}");
                        WriteLine($"{index}");
                        if ((index % 2) == 0)
                            throw new Exception($"{index} exception");
                    });
                }

                Thread.Sleep(1000);
            }

            Assert.IsTrue(EqualsSmart(new string[] { "0", "1", "2", "3", "4" }, list.ToArray()));
        }

        private static void WriteLine(string str)
        {
            Console.WriteLine($"{DateTime.UtcNow:mm:ss.fff}: {str}");
        }
    }
}
