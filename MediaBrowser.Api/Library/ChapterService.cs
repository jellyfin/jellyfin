using MediaBrowser.Controller.Chapters;
using ServiceStack;
using System.Linq;

namespace MediaBrowser.Api.Library
{
    [Route("/Providers/Chapters", "GET")]
    public class GetChapterProviders : IReturnVoid
    {
    }

    public class ChapterService : BaseApiService
    {
        private readonly IChapterManager _chapterManager;

        public ChapterService(IChapterManager chapterManager)
        {
            _chapterManager = chapterManager;
        }

        public object Get(GetChapterProviders request)
        {
            var result = _chapterManager.GetProviders().ToList();

            return ToOptimizedResult(result);
        }
    }
}
