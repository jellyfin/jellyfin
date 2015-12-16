/**
 * highly optimized TS demuxer:
 * parse PAT, PMT
 * extract PES packet from audio and video PIDs
 * extract AVC/H264 NAL units and AAC/ADTS samples from PES packet
 * trigger the remuxer upon parsing completion
 * it also tries to workaround as best as it can audio codec switch (HE-AAC to AAC and vice versa), without having to restart the MediaSource.
 * it also controls the remuxing process :
 * upon discontinuity or level switch detection, it will also notifies the remuxer so that it can reset its state.
*/

 import Event from '../events';
 import ExpGolomb from './exp-golomb';
// import Hex from '../utils/hex';
 import {logger} from '../utils/logger';
 import {ErrorTypes, ErrorDetails} from '../errors';

 class TSDemuxer {

  constructor(observer,remuxerClass) {
    this.observer = observer;
    this.remuxerClass = remuxerClass;
    this.lastCC = 0;
    this.PES_TIMESCALE = 90000;
    this.remuxer = new this.remuxerClass(observer);
  }

  static probe(data) {
    // a TS fragment should contain at least 3 TS packets, a PAT, a PMT, and one PID, each starting with 0x47
    if (data.length >= 3*188 && data[0] === 0x47 && data[188] === 0x47 && data[2*188] === 0x47) {
      return true;
    } else {
      return false;
    }
  }

  switchLevel() {
    this.pmtParsed = false;
    this._pmtId = -1;
    this._avcTrack = {type: 'video', id :-1, sequenceNumber: 0, samples : [], len : 0, nbNalu : 0};
    this._aacTrack = {type: 'audio', id :-1, sequenceNumber: 0, samples : [], len : 0};
    this._id3Track = {type: 'id3', id :-1, sequenceNumber: 0, samples : [], len : 0};
    this.remuxer.switchLevel();
  }

  insertDiscontinuity() {
    this.switchLevel();
    this.remuxer.insertDiscontinuity();
  }

  // feed incoming data to the front of the parsing pipeline
  push(data, audioCodec, videoCodec, timeOffset, cc, level, sn, duration) {
    var avcData, aacData, id3Data,
        start, len = data.length, stt, pid, atf, offset;
    this.audioCodec = audioCodec;
    this.videoCodec = videoCodec;
    this.timeOffset = timeOffset;
    this._duration = duration;
    this.contiguous = false;
    if (cc !== this.lastCC) {
      logger.log('discontinuity detected');
      this.insertDiscontinuity();
      this.lastCC = cc;
    } else if (level !== this.lastLevel) {
      logger.log('level switch detected');
      this.switchLevel();
      this.lastLevel = level;
    } else if (sn === (this.lastSN+1)) {
      this.contiguous = true;
    }
    this.lastSN = sn;

    if(!this.contiguous) {
      // flush any partial content
      this.aacOverFlow = null;
    }

    var pmtParsed = this.pmtParsed,
        avcId = this._avcTrack.id,
        aacId = this._aacTrack.id,
        id3Id = this._id3Track.id;
    // loop through TS packets
    for (start = 0; start < len; start += 188) {
      if (data[start] === 0x47) {
        stt = !!(data[start + 1] & 0x40);
        // pid is a 13-bit field starting at the last bit of TS[1]
        pid = ((data[start + 1] & 0x1f) << 8) + data[start + 2];
        atf = (data[start + 3] & 0x30) >> 4;
        // if an adaption field is present, its length is specified by the fifth byte of the TS packet header.
        if (atf > 1) {
          offset = start + 5 + data[start + 4];
          // continue if there is only adaptation field
          if (offset === (start + 188)) {
            continue;
          }
        } else {
          offset = start + 4;
        }
        if (pmtParsed) {
          if (pid === avcId) {
            if (stt) {
              if (avcData) {
                this._parseAVCPES(this._parsePES(avcData));
              }
              avcData = {data: [], size: 0};
            }
            if (avcData) {
              avcData.data.push(data.subarray(offset, start + 188));
              avcData.size += start + 188 - offset;
            }
          } else if (pid === aacId) {
            if (stt) {
              if (aacData) {
                this._parseAACPES(this._parsePES(aacData));
              }
              aacData = {data: [], size: 0};
            }
            if (aacData) {
              aacData.data.push(data.subarray(offset, start + 188));
              aacData.size += start + 188 - offset;
            }
          } else if (pid === id3Id) {
            if (stt) {
              if (id3Data) {
                this._parseID3PES(this._parsePES(id3Data));
              }
              id3Data = {data: [], size: 0};
            }
            if (id3Data) {
              id3Data.data.push(data.subarray(offset, start + 188));
              id3Data.size += start + 188 - offset;
            }
          }
        } else {
          if (stt) {
            offset += data[offset] + 1;
          }
          if (pid === 0) {
            this._parsePAT(data, offset);
          } else if (pid === this._pmtId) {
            this._parsePMT(data, offset);
            pmtParsed = this.pmtParsed = true;
            avcId = this._avcTrack.id;
            aacId = this._aacTrack.id;
            id3Id = this._id3Track.id;
          }
        }
      } else {
        this.observer.trigger(Event.ERROR, {type : ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: false, reason: 'TS packet did not start with 0x47'});
      }
    }
    // parse last PES packet
    if (avcData) {
      this._parseAVCPES(this._parsePES(avcData));
    }
    if (aacData) {
      this._parseAACPES(this._parsePES(aacData));
    }
    if (id3Data) {
      this._parseID3PES(this._parsePES(id3Data));
    }
    this.remux();
  }

  remux() {
    this.remuxer.remux(this._aacTrack,this._avcTrack, this._id3Track, this.timeOffset, this.contiguous);
  }

  destroy() {
    this.switchLevel();
    this._initPTS = this._initDTS = undefined;
    this._duration = 0;
  }

  _parsePAT(data, offset) {
    // skip the PSI header and parse the first PMT entry
    this._pmtId  = (data[offset + 10] & 0x1F) << 8 | data[offset + 11];
    //logger.log('PMT PID:'  + this._pmtId);
  }

  _parsePMT(data, offset) {
    var sectionLength, tableEnd, programInfoLength, pid;
    sectionLength = (data[offset + 1] & 0x0f) << 8 | data[offset + 2];
    tableEnd = offset + 3 + sectionLength - 4;
    // to determine where the table is, we have to figure out how
    // long the program info descriptors are
    programInfoLength = (data[offset + 10] & 0x0f) << 8 | data[offset + 11];
    // advance the offset to the first entry in the mapping table
    offset += 12 + programInfoLength;
    while (offset < tableEnd) {
      pid = (data[offset + 1] & 0x1F) << 8 | data[offset + 2];
      switch(data[offset]) {
        // ISO/IEC 13818-7 ADTS AAC (MPEG-2 lower bit-rate audio)
        case 0x0f:
          //logger.log('AAC PID:'  + pid);
          this._aacTrack.id = pid;
          break;
        // Packetized metadata (ID3)
        case 0x15:
          //logger.log('ID3 PID:'  + pid);
          this._id3Track.id = pid;
          break;
        // ITU-T Rec. H.264 and ISO/IEC 14496-10 (lower bit-rate video)
        case 0x1b:
          //logger.log('AVC PID:'  + pid);
          this._avcTrack.id = pid;
          break;
        default:
        logger.log('unkown stream type:'  + data[offset]);
        break;
      }
      // move to the next table entry
      // skip past the elementary stream descriptors, if present
      offset += ((data[offset + 3] & 0x0F) << 8 | data[offset + 4]) + 5;
    }
  }

  _parsePES(stream) {
    var i = 0, frag, pesFlags, pesPrefix, pesLen, pesHdrLen, pesData, pesPts, pesDts, payloadStartOffset;
    //retrieve PTS/DTS from first fragment
    frag = stream.data[0];
    pesPrefix = (frag[0] << 16) + (frag[1] << 8) + frag[2];
    if (pesPrefix === 1) {
      pesLen = (frag[4] << 8) + frag[5];
      pesFlags = frag[7];
      if (pesFlags & 0xC0) {
        /* PES header described here : http://dvd.sourceforge.net/dvdinfo/pes-hdr.html
            as PTS / DTS is 33 bit we cannot use bitwise operator in JS,
            as Bitwise operators treat their operands as a sequence of 32 bits */
        pesPts = (frag[9] & 0x0E) * 536870912 +// 1 << 29
          (frag[10] & 0xFF) * 4194304 +// 1 << 22
          (frag[11] & 0xFE) * 16384 +// 1 << 14
          (frag[12] & 0xFF) * 128 +// 1 << 7
          (frag[13] & 0xFE) / 2;
          // check if greater than 2^32 -1
          if (pesPts > 4294967295) {
            // decrement 2^33
            pesPts -= 8589934592;
          }
        if (pesFlags & 0x40) {
          pesDts = (frag[14] & 0x0E ) * 536870912 +// 1 << 29
            (frag[15] & 0xFF ) * 4194304 +// 1 << 22
            (frag[16] & 0xFE ) * 16384 +// 1 << 14
            (frag[17] & 0xFF ) * 128 +// 1 << 7
            (frag[18] & 0xFE ) / 2;
          // check if greater than 2^32 -1
          if (pesDts > 4294967295) {
            // decrement 2^33
            pesDts -= 8589934592;
          }
        } else {
          pesDts = pesPts;
        }
      }
      pesHdrLen = frag[8];
      payloadStartOffset = pesHdrLen + 9;
      // trim PES header
      stream.data[0] = stream.data[0].subarray(payloadStartOffset);
      stream.size -= payloadStartOffset;
      //reassemble PES packet
      pesData = new Uint8Array(stream.size);
      // reassemble the packet
      while (stream.data.length) {
        frag = stream.data.shift();
        pesData.set(frag, i);
        i += frag.byteLength;
      }
      return {data: pesData, pts: pesPts, dts: pesDts, len: pesLen};
    } else {
      return null;
    }
  }

  _parseAVCPES(pes) {
    var track = this._avcTrack,
        samples = track.samples,
        units = this._parseAVCNALu(pes.data),
        units2 = [],
        debug = false,
        key = false,
        length = 0,
        avcSample,
        push;
    // no NALu found
    if (units.length === 0 && samples.length > 0) {
      // append pes.data to previous NAL unit
      var lastavcSample = samples[samples.length - 1];
      var lastUnit = lastavcSample.units.units[lastavcSample.units.units.length - 1];
      var tmp = new Uint8Array(lastUnit.data.byteLength + pes.data.byteLength);
      tmp.set(lastUnit.data, 0);
      tmp.set(pes.data, lastUnit.data.byteLength);
      lastUnit.data = tmp;
      lastavcSample.units.length += pes.data.byteLength;
      track.len += pes.data.byteLength;
    }
    //free pes.data to save up some memory
    pes.data = null;
    var debugString = '';
    units.forEach(unit => {
      switch(unit.type) {
        //NDR
         case 1:
           push = true;
           if(debug) {
            debugString += 'NDR ';
           }
           break;
        //IDR
        case 5:
          push = true;
          if(debug) {
            debugString += 'IDR ';
          }
          key = true;
          break;
        case 6:
          push = true;
          if(debug) {
            debugString += 'SEI ';
          }
          break;
        //SPS
        case 7:
          push = true;
          if(debug) {
            debugString += 'SPS ';
          }
          if(!track.sps) {
            var expGolombDecoder = new ExpGolomb(unit.data);
            var config = expGolombDecoder.readSPS();
            track.width = config.width;
            track.height = config.height;
            track.sps = [unit.data];
            track.timescale = this.remuxer.timescale;
            track.duration = this.remuxer.timescale * this._duration;
            var codecarray = unit.data.subarray(1, 4);
            var codecstring = 'avc1.';
            for (var i = 0; i < 3; i++) {
              var h = codecarray[i].toString(16);
              if (h.length < 2) {
                h = '0' + h;
              }
              codecstring += h;
            }
            track.codec = codecstring;
          }
          break;
        //PPS
        case 8:
          push = true;
          if(debug) {
            debugString += 'PPS ';
          }
          if (!track.pps) {
            track.pps = [unit.data];
          }
          break;
        case 9:
          push = true;
          if(debug) {
            debugString += 'AUD ';
          }
          break;
        default:
          push = false;
          debugString += 'unknown NAL ' + unit.type + ' ';
          break;
      }
      if(push) {
        units2.push(unit);
        length+=unit.data.byteLength;
      }
    });
    if(debug || debugString.length) {
      logger.log(debugString);
    }
    //build sample from PES
    // Annex B to MP4 conversion to be done
    if (units2.length) {
      // only push AVC sample if keyframe already found. browsers expect a keyframe at first to start decoding
      if (key === true || track.sps ) {
        avcSample = {units: { units : units2, length : length}, pts: pes.pts, dts: pes.dts, key: key};
        samples.push(avcSample);
        track.len += length;
        track.nbNalu += units2.length;
      }
    }
  }


  _parseAVCNALu(array) {
    var i = 0, len = array.byteLength, value, overflow, state = 0;
    var units = [], unit, unitType, lastUnitStart, lastUnitType;
    //logger.log('PES:' + Hex.hexDump(array));
    while (i < len) {
      value = array[i++];
      // finding 3 or 4-byte start codes (00 00 01 OR 00 00 00 01)
      switch (state) {
        case 0:
          if (value === 0) {
            state = 1;
          }
          break;
        case 1:
          if( value === 0) {
            state = 2;
          } else {
            state = 0;
          }
          break;
        case 2:
        case 3:
          if( value === 0) {
            state = 3;
          } else if (value === 1) {
            unitType = array[i] & 0x1f;
            //logger.log('find NALU @ offset:' + i + ',type:' + unitType);
            if (lastUnitStart) {
              unit = {data: array.subarray(lastUnitStart, i - state - 1), type: lastUnitType};
              //logger.log('pushing NALU, type/size:' + unit.type + '/' + unit.data.byteLength);
              units.push(unit);
            } else {
              // If NAL units are not starting right at the beginning of the PES packet, push preceding data into previous NAL unit.
              overflow  = i - state - 1;
              if (overflow) {
                //logger.log('first NALU found with overflow:' + overflow);
                if (this._avcTrack.samples.length) {
                  var lastavcSample = this._avcTrack.samples[this._avcTrack.samples.length - 1];
                  var lastUnit = lastavcSample.units.units[lastavcSample.units.units.length - 1];
                  var tmp = new Uint8Array(lastUnit.data.byteLength + overflow);
                  tmp.set(lastUnit.data, 0);
                  tmp.set(array.subarray(0, overflow), lastUnit.data.byteLength);
                  lastUnit.data = tmp;
                  lastavcSample.units.length += overflow;
                  this._avcTrack.len += overflow;
                }
              }
            }
            lastUnitStart = i;
            lastUnitType = unitType;
            if (unitType === 1 || unitType === 5) {
              // OPTI !!! if IDR/NDR unit, consider it is last NALu
              i = len;
            }
            state = 0;
          } else {
            state = 0;
          }
          break;
        default:
          break;
      }
    }
    if (lastUnitStart) {
      unit = {data: array.subarray(lastUnitStart, len), type: lastUnitType};
      units.push(unit);
      //logger.log('pushing NALU, type/size:' + unit.type + '/' + unit.data.byteLength);
    }
    return units;
  }

  _parseAACPES(pes) {
    var track = this._aacTrack, aacSample, data = pes.data, config, adtsFrameSize, adtsStartOffset, adtsHeaderLen, stamp, nbSamples, len;
    if (this.aacOverFlow) {
      var tmp = new Uint8Array(this.aacOverFlow.byteLength + data.byteLength);
      tmp.set(this.aacOverFlow, 0);
      tmp.set(data, this.aacOverFlow.byteLength);
      data = tmp;
    }
    // look for ADTS header (0xFFFx)
    for (adtsStartOffset = 0, len = data.length; adtsStartOffset < len - 1; adtsStartOffset++) {
      if ((data[adtsStartOffset] === 0xff) && (data[adtsStartOffset+1] & 0xf0) === 0xf0) {
        break;
      }
    }
    // if ADTS header does not start straight from the beginning of the PES payload, raise an error
    if (adtsStartOffset) {
      var reason, fatal;
      if (adtsStartOffset < len - 1) {
        reason = `AAC PES did not start with ADTS header,offset:${adtsStartOffset}`;
        fatal = false;
      } else {
        reason = 'no ADTS header found in AAC PES';
        fatal = true;
      }
      this.observer.trigger(Event.ERROR, {type: ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: fatal, reason: reason});
      if (fatal) {
        return;
      }
    }
    if (!track.audiosamplerate) {
      config = this._ADTStoAudioConfig(data, adtsStartOffset, this.audioCodec);
      track.config = config.config;
      track.audiosamplerate = config.samplerate;
      track.channelCount = config.channelCount;
      track.codec = config.codec;
      track.timescale = this.remuxer.timescale;
      track.duration = this.remuxer.timescale * this._duration;
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
      stamp = Math.round(pes.pts + nbSamples * 1024 * this.PES_TIMESCALE / track.audiosamplerate);
      //stamp = pes.pts;
      //console.log('AAC frame, offset/length/pts:' + (adtsStartOffset+7) + '/' + adtsFrameSize + '/' + stamp.toFixed(0));
      if ((adtsFrameSize > 0) && ((adtsStartOffset + adtsHeaderLen + adtsFrameSize) <= len)) {
        aacSample = {unit: data.subarray(adtsStartOffset + adtsHeaderLen, adtsStartOffset + adtsHeaderLen + adtsFrameSize), pts: stamp, dts: stamp};
        this._aacTrack.samples.push(aacSample);
        this._aacTrack.len += adtsFrameSize;
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
    if (adtsStartOffset < len) {
      this.aacOverFlow = data.subarray(adtsStartOffset, len);
    } else {
      this.aacOverFlow = null;
    }
  }

  _ADTStoAudioConfig(data, offset, audioCodec) {
    var adtsObjectType, // :int
        adtsSampleingIndex, // :int
        adtsExtensionSampleingIndex, // :int
        adtsChanelConfig, // :int
        config,
        userAgent = navigator.userAgent.toLowerCase(),
        adtsSampleingRates = [
            96000, 88200,
            64000, 48000,
            44100, 32000,
            24000, 22050,
            16000, 12000,
            11025, 8000,
            7350];
    // byte 2
    adtsObjectType = ((data[offset + 2] & 0xC0) >>> 6) + 1;
    adtsSampleingIndex = ((data[offset + 2] & 0x3C) >>> 2);
    if(adtsSampleingIndex > adtsSampleingRates.length-1) {
      this.observer.trigger(Event.ERROR, {type: ErrorTypes.MEDIA_ERROR, details: ErrorDetails.FRAG_PARSING_ERROR, fatal: true, reason: `invalid ADTS sampling index:${adtsSampleingIndex}`});
      return;
    }
    adtsChanelConfig = ((data[offset + 2] & 0x01) << 2);
    // byte 3
    adtsChanelConfig |= ((data[offset + 3] & 0xC0) >>> 6);
    logger.log(`manifest codec:${audioCodec},ADTS data:type:${adtsObjectType},sampleingIndex:${adtsSampleingIndex}[${adtsSampleingRates[adtsSampleingIndex]}Hz],channelConfig:${adtsChanelConfig}`);
    // firefox: freq less than 24kHz = AAC SBR (HE-AAC)
    if (userAgent.indexOf('firefox') !== -1) {
      if (adtsSampleingIndex >= 6) {
        adtsObjectType = 5;
        config = new Array(4);
        // HE-AAC uses SBR (Spectral Band Replication) , high frequencies are constructed from low frequencies
        // there is a factor 2 between frame sample rate and output sample rate
        // multiply frequency by 2 (see table below, equivalent to substract 3)
        adtsExtensionSampleingIndex = adtsSampleingIndex - 3;
      } else {
        adtsObjectType = 2;
        config = new Array(2);
        adtsExtensionSampleingIndex = adtsSampleingIndex;
      }
      // Android : always use AAC
    } else if (userAgent.indexOf('android') !== -1) {
      adtsObjectType = 2;
      config = new Array(2);
      adtsExtensionSampleingIndex = adtsSampleingIndex;
    } else {
      /*  for other browsers (chrome ...)
          always force audio type to be HE-AAC SBR, as some browsers do not support audio codec switch properly (like Chrome ...)
      */
      adtsObjectType = 5;
      config = new Array(4);
      // if (manifest codec is HE-AAC or HE-AACv2) OR (manifest codec not specified AND frequency less than 24kHz)
      if ((audioCodec && ((audioCodec.indexOf('mp4a.40.29') !== -1) ||
                          (audioCodec.indexOf('mp4a.40.5') !== -1))) ||
          (!audioCodec && adtsSampleingIndex >= 6)) {
        // HE-AAC uses SBR (Spectral Band Replication) , high frequencies are constructed from low frequencies
        // there is a factor 2 between frame sample rate and output sample rate
        // multiply frequency by 2 (see table below, equivalent to substract 3)
        adtsExtensionSampleingIndex = adtsSampleingIndex - 3;
      } else {
        // if (manifest codec is AAC) AND (frequency less than 24kHz OR nb channel is 1) OR (manifest codec not specified and mono audio)
        // Chrome fails to play back with AAC LC mono when initialized with HE-AAC.  This is not a problem with stereo.
        if (audioCodec && audioCodec.indexOf('mp4a.40.2') !== -1 && (adtsSampleingIndex >= 6 || adtsChanelConfig === 1) ||
            (!audioCodec && adtsChanelConfig === 1)) {
          adtsObjectType = 2;
          config = new Array(2);
        }
        adtsExtensionSampleingIndex = adtsSampleingIndex;
      }
    }
    /* refer to http://wiki.multimedia.cx/index.php?title=MPEG-4_Audio#Audio_Specific_Config
        ISO 14496-3 (AAC).pdf - Table 1.13 — Syntax of AudioSpecificConfig()
      Audio Profile / Audio Object Type
      0: Null
      1: AAC Main
      2: AAC LC (Low Complexity)
      3: AAC SSR (Scalable Sample Rate)
      4: AAC LTP (Long Term Prediction)
      5: SBR (Spectral Band Replication)
      6: AAC Scalable
     sampling freq
      0: 96000 Hz
      1: 88200 Hz
      2: 64000 Hz
      3: 48000 Hz
      4: 44100 Hz
      5: 32000 Hz
      6: 24000 Hz
      7: 22050 Hz
      8: 16000 Hz
      9: 12000 Hz
      10: 11025 Hz
      11: 8000 Hz
      12: 7350 Hz
      13: Reserved
      14: Reserved
      15: frequency is written explictly
      Channel Configurations
      These are the channel configurations:
      0: Defined in AOT Specifc Config
      1: 1 channel: front-center
      2: 2 channels: front-left, front-right
    */
    // audioObjectType = profile => profile, the MPEG-4 Audio Object Type minus 1
    config[0] = adtsObjectType << 3;
    // samplingFrequencyIndex
    config[0] |= (adtsSampleingIndex & 0x0E) >> 1;
    config[1] |= (adtsSampleingIndex & 0x01) << 7;
    // channelConfiguration
    config[1] |= adtsChanelConfig << 3;
    if (adtsObjectType === 5) {
      // adtsExtensionSampleingIndex
      config[1] |= (adtsExtensionSampleingIndex & 0x0E) >> 1;
      config[2] = (adtsExtensionSampleingIndex & 0x01) << 7;
      // adtsObjectType (force to 2, chrome is checking that object type is less than 5 ???
      //    https://chromium.googlesource.com/chromium/src.git/+/master/media/formats/mp4/aac.cc
      config[2] |= 2 << 2;
      config[3] = 0;
    }
    return {config: config, samplerate: adtsSampleingRates[adtsSampleingIndex], channelCount: adtsChanelConfig, codec: ('mp4a.40.' + adtsObjectType)};
  }

  _parseID3PES(pes) {
    this._id3Track.samples.push(pes);
  }
}

export default TSDemuxer;

