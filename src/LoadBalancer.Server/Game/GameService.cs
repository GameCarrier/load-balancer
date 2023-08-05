using GameCarrier.Adapter;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Game
{
    public partial class GameService : ServiceBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<GameService>();

        public GameServiceSettings Settings { get; private set; }

        private IScheduledItem SchedulerReadCounters;
        private GCCounter CounterCPUSystem;
        private GCCounter CounterCPUUser;
        private GCCounter CounterMemoryFree;
        private readonly AverageCounter AverageCounterCPU = new AverageCounter();
        private readonly AverageCounter AverageCounterMemory = new AverageCounter();
        private const int Interval_ReadCounters = 1000;

        private SystemLoadLevel LoadLevel => new[] { CPULoadLevel, MemoryLoadLevel }.Max();
        private SystemLoadLevel CPULoadLevel => AverageCounterCPU.Average switch
        {
            > 90 => SystemLoadLevel.Highest,
            > 70 => SystemLoadLevel.High,
            > 50 => SystemLoadLevel.Normal,
            > 35 => SystemLoadLevel.Low,
            _ => SystemLoadLevel.Lowest,
        };

        private SystemLoadLevel MemoryLoadLevel => AverageCounterCPU.Average switch
        {
            > 95 => SystemLoadLevel.Highest,
            > 80 => SystemLoadLevel.High,
            > 60 => SystemLoadLevel.Normal,
            > 45 => SystemLoadLevel.Low,
            _ => SystemLoadLevel.Lowest,
        };

        public override void OnStart()
        {
            base.OnStart();
            Settings = ReadConfigurationSection<GameServiceSettings>();
            AuthTokenUtils.CryptographySettings = ReadConfigurationSection<CryptographySettings>();

            SetupLoadLevelMonitoring();

            SetupJumpServiceConnect();

            InitializeRoomLogic();
        }

        protected override HandlerBase CreateHandler() => new GameServiceHandler();

        public override void OnStop()
        {
            base.OnStop();
            JumpServiceConnect.Dispose();
        }

        private void SetupLoadLevelMonitoring()
        {
            CounterCPUSystem = GCCounter.Open("System.CPU.System.Percent.Current");
            CounterCPUUser = GCCounter.Open("System.CPU.User.Percent.Current");
            CounterMemoryFree = GCCounter.Open("System.Memory.Free.Percent.Current");

            SchedulerReadCounters = Thread.Schedule(() =>
            {
                var system = (int)CounterCPUSystem.Value;
                var user = (int)CounterCPUUser.Value;
                var free = (int)CounterMemoryFree.Value;
                Logger.LogDebug($"CPU: {system + user}, Memory: {100 - free}");

                AverageCounterCPU.Add(system + user);
                AverageCounterMemory.Add(100 - free);
            }, Interval_ReadCounters, Interval_ReadCounters).UseSynchronousExecutor();
        }
    }
}
