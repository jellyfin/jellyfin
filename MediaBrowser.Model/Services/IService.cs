
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
