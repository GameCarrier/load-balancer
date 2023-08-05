using GameCarrier.Async;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Tests
{
    public static class ClientExtensions
    {
        public static async Task<bool> ExpectRealtimeOperation(this IServiceConnect connect, int timeout = 5000)
        {
            await new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<ClientCallContext>>(
                    h => connect.OnRealtimeOperation += h, h => connect.OnRealtimeOperation -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectEventReceived(this IServiceConnect connect, int timeout = 5000)
        {
            await new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<ClientCallContext>>(
                    h => connect.OnEventReceived += h, h => connect.OnEventReceived -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectPropertiesChanged(this IClientPlayer player, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<KeyValueCollection>>(
                    h => player.OnPropertiesChanged += h, h => player.OnPropertiesChanged -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectPropertiesChanged(this IClientRoomObject obj, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<KeyValueCollection>>(
                    h => obj.OnPropertiesChanged += h, h => obj.OnPropertiesChanged -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectPropertiesChanged(this IClientRoom room, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<KeyValueCollection>>(
                    h => room.OnPropertiesChanged += h, h => room.OnPropertiesChanged -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectEventReceived(this IClientRoom room, int timeout = 5000)
        {
            await new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<RoomEvent>>(
                    h => room.OnEventReceived += h, h => room.OnEventReceived -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectPlayerJoined(this IClientRoom room, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<IClientPlayer>>(
                    h => room.Players.OnJoin += h, h => room.Players.OnJoin -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectPlayerLeaved(this IClientRoom room, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<IClientPlayer>>(
                    h => room.Players.OnLeave += h, h => room.Players.OnLeave -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectObjectSpawned(this IClientRoom room, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<IClientRoomObject>>(
                    h => room.Objects.OnSpawned += h, h => room.Objects.OnSpawned -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }

        public static async Task<bool> ExpectObjectDestroyed(this IClientRoom room, int timeout = 5000)
        {
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout)
                .AddSubscription(new AsyncEventSubscription<Action<IClientRoomObject>>(
                    h => room.Objects.OnDestroyed += h, h => room.Objects.OnDestroyed -= h)
                    .Handler(_ => operation.Complete()))
                .ExecuteAsync();
            await task;

            return operation.rt.WasCompleted;
        }
    }
}
