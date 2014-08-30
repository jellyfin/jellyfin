using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Net;
using ServiceStack;
using System.Linq;

namespace MediaBrowser.Api.Library
{
    [Route("/Providers/Chapters", "GET")]
    public class GetChapterProviders : IReturnVoid
    {
    }

    [Authenticated]
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
