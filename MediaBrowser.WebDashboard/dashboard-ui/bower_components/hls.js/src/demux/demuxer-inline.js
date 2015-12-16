/*  inline demuxer.
 *   probe fragments and instantiate appropriate demuxer depending on content type (TSDemuxer, AACDemuxer, ...)
 */

import Event from '../events';
import {ErrorTypes, ErrorDetails} from '../errors';
import AACDemuxer from '../demux/aacdemuxer';
import TSDemuxer from '../demux/tsdemuxer';

class DemuxerInline {

  constructor(hls,remuxer) {
    this.hls = hls;
    this.remuxer = remuxer;
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
      // probe for content type
      if (TSDemuxer.probe(data)) {
        demuxer = this.demuxer = new TSDemuxer(this.hls,this.remuxer);
      } else if(AACDemuxer.probe(data)) {
        demuxer = this.demuxer = new AACDemuxer(this.hls,this.remuxer);
      } else {
        this.hls.trigger(Event.ERROR, {type : ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: true, reason: 'no demux matching with content found'});
        return;
      }
    }
    demuxer.push(data,audioCodec,videoCodec,timeOffset,cc,level,sn,duration);
  }
}

export default DemuxerInline;
