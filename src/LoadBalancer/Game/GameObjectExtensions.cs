namespace LoadBalancer.Game
{
    public static class GameObjectExtensions
    {
        public static void EnableChangeTracking(this IGameObject obj) =>
            obj.Properties.EnableChangeTracking();

        public static void DisableChangeTracking(this IGameObject obj) =>
            obj.Properties.DisableChangeTracking();

        public static bool CommitChanges(this IGameObject obj) =>
            obj.Properties.CommitChanges(obj.UpdateProperties);
    }
}
