/*  inline demuxer.
 *   probe fragments and instantiate appropriate demuxer depending on content type (TSDemuxer, AACDemuxer, ...)
 */

import Event from '../events';
import {ErrorTypes, ErrorDetails} from '../errors';
import AACDemuxer from '../demux/aacdemuxer';
import TSDemuxer from '../demux/tsdemuxer';
import MP4Remuxer from '../remux/mp4-remuxer';
import PassThroughRemuxer from '../remux/passthrough-remuxer';

class DemuxerInline {

  constructor(hls,typeSupported) {
    this.hls = hls;
    this.typeSupported = typeSupported;
  }

  destroy() {
    var demuxer = this.demuxer;
    if (demuxer) {
      demuxer.destroy();
    }
  }

  push(data, audioCodec, videoCodec, timeOffset, cc, level, sn, duration) {
    var demuxer = this.demuxer;
    if (!demuxer) {
      var hls = this.hls;
      // probe for content type
      if (TSDemuxer.probe(data)) {
        if (this.typeSupported.mp2t === true) {
          demuxer = new TSDemuxer(hls,PassThroughRemuxer);
        } else {
          demuxer = new TSDemuxer(hls,MP4Remuxer);
        }
      } else if(AACDemuxer.probe(data)) {
        demuxer = new AACDemuxer(hls,MP4Remuxer);
      } else {
        hls.trigger(Event.ERROR, {type : ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: true, reason: 'no demux matching with content found'});
        return;
      }
      this.demuxer = demuxer;
    }
    demuxer.push(data,audioCodec,videoCodec,timeOffset,cc,level,sn,duration);
  }
}

export default DemuxerInline;
