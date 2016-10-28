
namespace MediaBrowser.Model.Services
{
    public interface IRequestFilter
    {
        void Filter(IRequest request, IResponse response, object requestDto);
    }
}
