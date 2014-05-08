using System;

namespace MediaBrowser.Common
{
    public interface IDependencyContainer
    {
        void RegisterSingleInstance<T>(T obj, bool manageLifetime = true)
            where T : class;

        void RegisterSingleInstance<T>(Func<T> func)
            where T : class;

        void Register(Type typeInterface, Type typeImplementation);
    }
}