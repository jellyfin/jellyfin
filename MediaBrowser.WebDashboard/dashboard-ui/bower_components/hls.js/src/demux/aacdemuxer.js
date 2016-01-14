/**
 * AAC demuxer
 */
import ADTS from './adts';
import {logger} from '../utils/logger';
import ID3 from '../demux/id3';

 class AACDemuxer {

  constructor(observer,remuxerClass) {
    this.observer = observer;
    this.remuxerClass = remuxerClass;
    this.remuxer = new this.remuxerClass(observer);
    this._aacTrack = {type: 'audio', id :-1, sequenceNumber: 0, samples : [], len : 0};
  }

  static probe(data) {
    // check if data contains ID3 timestamp and ADTS sync worc
    var id3 = new ID3(data), adtsStartOffset,len;
    if(id3.hasTimeStamp) {
      // look for ADTS header (0xFFFx)
      for (adtsStartOffset = id3.length, len = data.length; adtsStartOffset < len - 1; adtsStartOffset++) {
        if ((data[adtsStartOffset] === 0xff) && (data[adtsStartOffset+1] & 0xf0) === 0xf0) {
          //logger.log('ADTS sync word found !');
          return true;
        }
      }
    }
    return false;
  }


  // feed incoming data to the front of the parsing pipeline
  push(data, audioCodec, videoCodec, timeOffset, cc, level, sn, duration) {
    var track = this._aacTrack,
        id3 = new ID3(data),
        pts = 90*id3.timeStamp,
        config, adtsFrameSize, adtsStartOffset, adtsHeaderLen, stamp, nbSamples, len, aacSample;
    // look for ADTS header (0xFFFx)
    for (adtsStartOffset = id3.length, len = data.length; adtsStartOffset < len - 1; adtsStartOffset++) {
      if ((data[adtsStartOffset] === 0xff) && (data[adtsStartOffset+1] & 0xf0) === 0xf0) {
        break;
      }
    }

    if (!track.audiosamplerate) {
      config = ADTS.getAudioConfig(this.observer,data, adtsStartOffset, audioCodec);
      track.config = config.config;
      track.audiosamplerate = config.samplerate;
      track.channelCount = config.channelCount;
      track.codec = config.codec;
      track.timescale = this.remuxer.timescale;
      track.duration = this.remuxer.timescale * duration;
      logger.log(`parsed codec:${track.codec},rate:${config.samplerate},nb channel:${config.channelCount}`);
    }
    nbSamples = 0;
    while ((adtsStartOffset + 5) < len) {
      // retrieve frame size
      adtsFrameSize = ((data[adtsStartOffset + 3] & 0x03) << 11);
      // byte 4
      adtsFrameSize |= (data[adtsStartOffset + 4] << 3);
      // byte 5
      adtsFrameSize |= ((data[adtsStartOffset + 5] & 0xE0) >>> 5);
      adtsHeaderLen = (!!(data[adtsStartOffset + 1] & 0x01) ? 7 : 9);
      adtsFrameSize -= adtsHeaderLen;
      stamp = Math.round(pts + nbSamples * 1024 * 90000 / track.audiosamplerate);
      //stamp = pes.pts;
      //console.log('AAC frame, offset/length/pts:' + (adtsStartOffset+7) + '/' + adtsFrameSize + '/' + stamp.toFixed(0));
      if ((adtsFrameSize > 0) && ((adtsStartOffset + adtsHeaderLen + adtsFrameSize) <= len)) {
        aacSample = {unit: data.subarray(adtsStartOffset + adtsHeaderLen, adtsStartOffset + adtsHeaderLen + adtsFrameSize), pts: stamp, dts: stamp};
        track.samples.push(aacSample);
        track.len += adtsFrameSize;
        adtsStartOffset += adtsFrameSize + adtsHeaderLen;
        nbSamples++;
        // look for ADTS header (0xFFFx)
        for ( ; adtsStartOffset < (len - 1); adtsStartOffset++) {
          if ((data[adtsStartOffset] === 0xff) && ((data[adtsStartOffset + 1] & 0xf0) === 0xf0)) {
            break;
          }
        }
      } else {
        break;
      }
    }
    this.remuxer.remux(this._aacTrack,{samples : []}, {samples : [ { pts: pts, dts : pts, unit : id3.payload} ]}, timeOffset);
  }

  destroy() {
  }

}

export default AACDemuxer;
