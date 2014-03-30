using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Api.Playback.Progressive;

namespace MediaBrowser.Api.Playback
{
    //public class GetProgressiveAudioStream : StreamRequest
    //{

    //}
    
    //public class ProgressiveStreamService : BaseApiService
    //{
    //    public object Get(GetProgressiveAudioStream request)
    //    {
    //        return ProcessRequest(request, false);
    //    }

    //    /// <summary>
    //    /// Gets the specified request.
    //    /// </summary>
    //    /// <param name="request">The request.</param>
    //    /// <returns>System.Object.</returns>
    //    public object Head(GetProgressiveAudioStream request)
    //    {
    //        return ProcessRequest(request, true);
    //    }

    //    protected object ProcessRequest(StreamRequest request, bool isHeadRequest)
    //    {
    //        var state = GetState(request, CancellationToken.None).Result;

    //        var responseHeaders = new Dictionary<string, string>();

    //        if (request.Static && state.IsRemote)
    //        {
    //            AddDlnaHeaders(state, responseHeaders, true);

    //            return GetStaticRemoteStreamResult(state.MediaPath, responseHeaders, isHeadRequest).Result;
    //        }

    //        var outputPath = GetOutputFilePath(state);
    //        var outputPathExists = File.Exists(outputPath);

    //        var isStatic = request.Static ||
    //                       (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive));

    //        AddDlnaHeaders(state, responseHeaders, isStatic);

    //        if (request.Static)
    //        {
    //            var contentType = state.GetMimeType(state.MediaPath);

    //            return ResultFactory.GetStaticFileResult(Request, state.MediaPath, contentType, FileShare.Read, responseHeaders, isHeadRequest);
    //        }

    //        if (outputPathExists && !ApiEntryPoint.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
    //        {
    //            var contentType = state.GetMimeType(outputPath);

    //            return ResultFactory.GetStaticFileResult(Request, outputPath, contentType, FileShare.Read, responseHeaders, isHeadRequest);
    //        }

    //        return GetStreamResult(state, responseHeaders, isHeadRequest).Result;
    //    }

    //}
}
