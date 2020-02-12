#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Services
{
    // marker interface
    public interface IService
    {
    }

    public interface IReturn { }
    public interface IReturn<T> : IReturn { }
    public interface IReturnVoid : IReturn { }
}
