using LoadBalancer.Server.Common;
using static LoadBalancer.Extensions.Comparison;

namespace LoadBalancer.Server.Tests
{
    [TestClass]
    public class ActionSchedulerTests
    {
        [TestMethod]
        public void Schedule_Timeout()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                scheduler.Schedule(() => workItemA.Execute(), 500);
                workItemA.Register();

                Thread.Sleep(1500);
            }

            history.Print();

            Assert.IsTrue(history.Distance("a", "a1", 500));
        }

        [TestMethod]
        public void Schedule_Timeout_Dispose()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                using var itemA = scheduler.Schedule(() => workItemA.Execute(), 500);
                workItemA.Register();
                Thread.Sleep(200);
                itemA.Dispose();

                var workItemB = new WorkItem(history, "b");
                using var itemB = scheduler.Schedule(() => workItemB.Execute(), 500);
                workItemB.Register();
                Thread.Sleep(700);
                itemB.Dispose();

                Thread.Sleep(1500);
            }

            history.Print();

            Assert.IsFalse(history.HasEvent("a1"));
            Assert.IsTrue(history.HasEvent("b1"));
            Assert.IsTrue(history.Distance("b", "b1", 500));
        }

        [TestMethod]
        public void Schedule_Timeout_Multiple_Sequence()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                scheduler.Schedule(() => workItemA.Execute(), 200);
                workItemA.Register();

                var workItemB = new WorkItem(history, "b");
                scheduler.Schedule(() => workItemB.Execute(), 500);
                workItemB.Register();

                Thread.Sleep(1500);
            }

            history.Print();
            Assert.IsTrue(history.InSequence("a", "b", "a1", "b1"));
            Assert.IsTrue(history.Distance("a", "a1", 200));
            Assert.IsTrue(history.Distance("b", "b1", 500));
        }

        [TestMethod]
        public void Schedule_Timeout_Multiple_Reverse()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                scheduler.Schedule(() => workItemA.Execute(), 500);
                workItemA.Register();

                var workItemB = new WorkItem(history, "b");
                scheduler.Schedule(() => workItemB.Execute(), 200);
                workItemB.Register();

                Thread.Sleep(1500);
            }

            history.Print();
            Assert.IsTrue(history.InSequence("a", "b", "b1", "a1"));
            Assert.IsTrue(history.Distance("a", "a1", 500));
            Assert.IsTrue(history.Distance("b", "b1", 200));
        }

        [TestMethod]
        public void Schedule_Interval()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItem = new WorkItem(history, "a");
                scheduler.Schedule(() => workItem.Execute(), 500, 500);
                workItem.Register();

                Thread.Sleep(2550);
            }

            history.Print();

            Assert.IsTrue(history.Distance("a", "a1", 500));
            Assert.IsTrue(history.Distance("a", "a2", 1000));
            Assert.IsTrue(history.Distance("a", "a3", 1500));
            Assert.IsTrue(history.Distance("a", "a4", 2000));
            Assert.IsTrue(history.Distance("a", "a5", 2500));
            Assert.IsFalse(history.HasEvent("a6"));
        }

        [TestMethod]
        public void Schedule_Interval_SuspendResume()
        {
            var history = new History();

            DateTime resumed;
            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItem = new WorkItem(history, "a");
                var item = scheduler.Schedule(() => workItem.Execute(), 500, 500);
                workItem.Register();

                Thread.Sleep(1050);
                item.Suspend();
                Thread.Sleep(1000);
                item.Resume();
                resumed = DateTime.UtcNow;
                Thread.Sleep(1550);
            }

            history.Print();

            Assert.IsTrue(history.Distance("a", "a1", 500));
            Assert.IsTrue(history.Distance("a", "a2", 1000));
            Assert.IsTrue(history.Distance(resumed, "a3", 500));
            Assert.IsTrue(history.Distance(resumed, "a4", 1000));
            Assert.IsTrue(history.Distance(resumed, "a5", 1500));
            Assert.IsFalse(history.HasEvent("a6"));
        }

        [TestMethod]
        public void Schedule_Interval_Dispose()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                using var itemA = scheduler.Schedule(() => workItemA.Execute(), 500, 200);
                workItemA.Register();
                Thread.Sleep(200);
                itemA.Dispose();

                var workItemB = new WorkItem(history, "b");
                using var itemB = scheduler.Schedule(() => workItemB.Execute(), 500, 200);
                workItemB.Register();
                Thread.Sleep(800);
                itemB.Dispose();

                Thread.Sleep(1500);
            }

            history.Print();

            Assert.IsTrue(history.InSequence("a", "b", "b1", "b2"));
        }

        [TestMethod]
        public void Schedule_Interval_Multiple_Sequence()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                scheduler.Schedule(() => workItemA.Execute(), 250, 500);
                workItemA.Register();

                var workItemB = new WorkItem(history, "b");
                scheduler.Schedule(() => workItemB.Execute(), 500, 300);
                workItemB.Register();

                Thread.Sleep(1550);
            }

            history.Print();

            Assert.IsTrue(history.Distance("a", "a1", 250));
            Assert.IsTrue(history.Distance("a", "a2", 750));
            Assert.IsTrue(history.Distance("a", "a3", 1250));

            Assert.IsTrue(history.Distance("b", "b1", 500));
            Assert.IsTrue(history.Distance("b", "b2", 800));
            Assert.IsTrue(history.Distance("b", "b3", 1100));
            Assert.IsTrue(history.Distance("b", "b4", 1400));

            Assert.IsTrue(history.InSequence("a", "b", "a1", "b1", "a2", "b2", "b3", "a3", "b4"));
        }

        [TestMethod]
        public void Schedule_Interval_Multiple_Reverse()
        {
            var history = new History();

            using (var scheduler = new ActionScheduler())
            {
                scheduler.Start();

                var workItemA = new WorkItem(history, "a");
                scheduler.Schedule(() => workItemA.Execute(), 550, 500);
                workItemA.Register();

                var workItemB = new WorkItem(history, "b");
                scheduler.Schedule(() => workItemB.Execute(), 200, 300);
                workItemB.Register();

                Thread.Sleep(1650);
            }

            history.Print();

            Assert.IsTrue(history.Distance("a", "a1", 550));
            Assert.IsTrue(history.Distance("a", "a2", 1050));
            Assert.IsTrue(history.Distance("a", "a3", 1550));

            Assert.IsTrue(history.Distance("b", "b1", 200));
            Assert.IsTrue(history.Distance("b", "b2", 500));
            Assert.IsTrue(history.Distance("b", "b3", 800));
            Assert.IsTrue(history.Distance("b", "b4", 1100));
            Assert.IsTrue(history.Distance("b", "b5", 1400));

            Assert.IsTrue(history.InSequence("a", "b", "b1", "b2", "a1", "b3", "a2", "b4", "b5", "a3"));
        }

        private class History
        {
            private readonly List<(DateTime, string)> list = new List<(DateTime, string)>();

            public void Add(DateTime time, string evt) => list.Add((time, evt));

            public DateTime this[string evt] => list.Where(i => i.Item2 == evt).Select(i => i.Item1).FirstOrDefault();

            public bool HasEvent(string evt) => list.Any(i => i.Item2 == evt);

            private const int tolerance = 25;

            public bool Distance(string evt1, string evt2, int milliseconds)
            {
                DateTime time1 = this[evt1];
                DateTime time2 = this[evt2];

                int distance = (int)time2.Subtract(time1).TotalMilliseconds;
                return Math.Abs(distance - milliseconds) < tolerance;
            }

            public bool Distance(DateTime time1, string evt2, int milliseconds)
            {
                DateTime time2 = this[evt2];

                int distance = (int)time2.Subtract(time1).TotalMilliseconds;
                return Math.Abs(distance - milliseconds) < tolerance;
            }

            public bool InSequence(params string[] evts)
            {
                return EqualsSmart(evts, list.Select(i => i.Item2).ToArray());
            }

            public void Print()
            {
                var first = list.Select(h => h.Item1).FirstOrDefault();
                var prev = first;
                foreach (var item in list)
                {
                    Console.WriteLine($"{item.Item1:mm:ss.fff} - {item.Item2} [{item.Item1.Subtract(first):s\\.fff}] [{item.Item1.Subtract(prev):s\\.fff}]");
                    prev = item.Item1;
                }
            }
        }

        private class WorkItem
        {
            private readonly History history;

            public WorkItem(History history, string name, int delay = 50)
            {
                this.history = history;
                Name = name;
                Delay = delay;
            }

            public string Name { get; private set; }
            public int Delay { get; private set; }
            public DateTime Created { get; private set; }
            public DateTime Executed { get; private set; }
            public string Value { get; private set; }
            private int Index { get; set; }

            public void Register()
            {
                Created = DateTime.UtcNow;
                history.Add(Created, Name);
            }

            public void Execute()
            {
                Executed = DateTime.UtcNow;
                Index++;
                Value = $"{Name}{Index}";

                history.Add(Executed, Value);
            }
        }
    }
}
