using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Events
{
    public interface IEventConsumer<in T>
        where T : EventArgs
    {
        Task OnEvent(T eventArgs);
    }
}
