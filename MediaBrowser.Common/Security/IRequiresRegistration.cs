using System.Threading.Tasks;

namespace MediaBrowser.Common.Security
{
    public interface IRequiresRegistration
    {
        Task LoadRegistrationInfoAsync();
    }
}
