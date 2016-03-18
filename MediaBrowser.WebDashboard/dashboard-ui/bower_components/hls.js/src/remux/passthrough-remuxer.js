/**
 * passthrough remuxer
*/
import Event from '../events';

class PassThroughRemuxer {
  constructor(observer) {
    this.observer = observer;
    this.ISGenerated = false;
  }

  get passthrough() {
    return true;
  }

  destroy() {
  }

  insertDiscontinuity() {
  }

  switchLevel() {
    this.ISGenerated = false;
  }

  remux(audioTrack,videoTrack,id3Track,textTrack,timeOffset,rawData) {
    var observer = this.observer;
    // generate Init Segment if needed
    if (!this.ISGenerated) {
      var tracks = {},
          data = { tracks : tracks, unique : true },
          track = videoTrack,
          codec = track.codec;

      if (codec) {
        data.tracks.video = {
          container : track.container,
          codec :  codec,
          metadata : {
            width : track.width,
            height : track.height
          }
        };
      }

      track = audioTrack;
      codec = track.codec;
      if (codec) {
        data.tracks.audio = {
          container : track.container,
          codec :  codec,
          metadata : {
            channelCount : track.channelCount
          }
        };
      }
      this.ISGenerated = true;
      observer.trigger(Event.FRAG_PARSING_INIT_SEGMENT,data);
    }
    observer.trigger(Event.FRAG_PARSING_DATA, {
      data1: rawData,
      startPTS: timeOffset,
      startDTS: timeOffset,
      type: 'audiovideo',
      nb: 1
    });
  }
}

export default PassThroughRemuxer;
