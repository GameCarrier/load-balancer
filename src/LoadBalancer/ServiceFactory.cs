using System;
using System.Collections.Concurrent;

namespace LoadBalancer
{
    public sealed class ServiceFactory
    {
        static ServiceFactory()
        {
            Instance.Register<ILogger>(name => new ConsoleLogger(name));
        }

        public static readonly ServiceFactory Instance = new ServiceFactory();

        private readonly ConcurrentDictionary<Type, Func<string, object>> repository = new ConcurrentDictionary<Type, Func<string, object>>();

        public void Register<T>(Func<T> factory) => repository[typeof(T)] = _ => factory();
        public void Register<T>(Func<string, T> factory) => repository[typeof(T)] = name => factory(name);
        public void Register<I, T>() where T : I, new() => repository[typeof(I)] = _ => new T();

        public I Get<I>(string name = null)
        {
            if (repository.TryGetValue(typeof(I), out var factory))
                return (I)factory(name);

            throw new NotImplementedException($"No factory provided for {typeof(I)}");
        }

        public ILogger GetLogger(string name) => Get<ILogger>(name);
        public ILogger GetLogger<T>() => Get<ILogger>(typeof(T).Name);
    }
}
