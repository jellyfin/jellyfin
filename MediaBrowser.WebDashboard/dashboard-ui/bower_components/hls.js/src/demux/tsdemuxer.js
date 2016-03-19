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

 import ADTS from './adts';
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
    this.lastAacPTS = null;
    this.aacOverFlow = null;
    this._avcTrack = {container : 'video/mp2t', type: 'video', id :-1, sequenceNumber: 0, samples : [], len : 0, nbNalu : 0};
    this._aacTrack = {container : 'video/mp2t', type: 'audio', id :-1, sequenceNumber: 0, samples : [], len : 0};
    this._id3Track = {type: 'id3', id :-1, sequenceNumber: 0, samples : [], len : 0};
    this._txtTrack = {type: 'text', id: -1, sequenceNumber: 0, samples: [], len: 0};
    this.remuxer.switchLevel();
  }

  insertDiscontinuity() {
    this.switchLevel();
    this.remuxer.insertDiscontinuity();
  }

  // feed incoming data to the front of the parsing pipeline
  push(data, audioCodec, videoCodec, timeOffset, cc, level, sn, duration) {
    var avcData, aacData, id3Data,
        start, len = data.length, stt, pid, atf, offset,
        codecsOnly = this.remuxer.passthrough;

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

    // don't parse last TS packet if incomplete
    len -= len % 188;
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
                if (codecsOnly) {
                  // if we have video codec info AND
                  // if audio PID is undefined OR if we have audio codec info,
                  // we have all codec info !
                  if (this._avcTrack.codec && (aacId === -1 || this._aacTrack.codec)) {
                    this.remux(data);
                    return;
                  }
                }
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
                if (codecsOnly) {
                  // here we now that we have audio codec info
                  // if video PID is undefined OR if we have video codec info,
                  // we have all codec infos !
                  if (this._aacTrack.codec && (avcId === -1 || this._avcTrack.codec)) {
                    this.remux(data);
                    return;
                  }
                }
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
    this.remux(null);
  }

  remux(data) {
    this.remuxer.remux(this._aacTrack, this._avcTrack, this._id3Track, this._txtTrack, this.timeOffset, this.contiguous, data);
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
    var i = 0, frag, pesFlags, pesPrefix, pesLen, pesHdrLen, pesData, pesPts, pesDts, payloadStartOffset, data = stream.data;
    //retrieve PTS/DTS from first fragment
    frag = data[0];
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

      stream.size -= payloadStartOffset;
      //reassemble PES packet
      pesData = new Uint8Array(stream.size);
      while (data.length) {
        frag = data.shift();
        var len = frag.byteLength;
        if (payloadStartOffset) {
          if (payloadStartOffset > len) {
            // trim full frag if PES header bigger than frag
            payloadStartOffset-=len;
            continue;
          } else {
            // trim partial frag if PES header smaller than frag
            frag = frag.subarray(payloadStartOffset);
            len-=payloadStartOffset;
            payloadStartOffset = 0;
          }
        }
        pesData.set(frag, i);
        i+=len;
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
        expGolombDecoder,
        avcSample,
        push,
        i;
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
        //SEI
        case 6:
          push = true;
          if(debug) {
            debugString += 'SEI ';
          }
          expGolombDecoder = new ExpGolomb(unit.data);

          // skip frameType
          expGolombDecoder.readUByte();

          var payloadType = expGolombDecoder.readUByte();

          // TODO: there can be more than one payload in an SEI packet...
          // TODO: need to read type and size in a while loop to get them all
          if (payloadType === 4)
          {
            var payloadSize = 0;

            do {
              payloadSize = expGolombDecoder.readUByte();
            }
            while (payloadSize === 255);

            var countryCode = expGolombDecoder.readUByte();

            if (countryCode === 181)
            {
              var providerCode = expGolombDecoder.readUShort();

              if (providerCode === 49)
              {
                var userStructure = expGolombDecoder.readUInt();

                if (userStructure === 0x47413934)
                {
                  var userDataType = expGolombDecoder.readUByte();

                  // Raw CEA-608 bytes wrapped in CEA-708 packet
                  if (userDataType === 3)
                  {
                    var firstByte = expGolombDecoder.readUByte();
                    var secondByte = expGolombDecoder.readUByte();

                    var totalCCs = 31 & firstByte;
                    var byteArray = [firstByte, secondByte];

                    for (i=0; i<totalCCs; i++)
                    {
                      // 3 bytes per CC
                      byteArray.push(expGolombDecoder.readUByte());
                      byteArray.push(expGolombDecoder.readUByte());
                      byteArray.push(expGolombDecoder.readUByte());
                    }

                    this._txtTrack.samples.push({type: 3, pts: pes.pts, bytes: byteArray});
                  }
                }
              }
            }
          }
          break;
        //SPS
        case 7:
          push = true;
          if(debug) {
            debugString += 'SPS ';
          }
          if(!track.sps) {
            expGolombDecoder = new ExpGolomb(unit.data);
            var config = expGolombDecoder.readSPS();
            track.width = config.width;
            track.height = config.height;
            track.sps = [unit.data];
            track.duration = this._duration;
            var codecarray = unit.data.subarray(1, 4);
            var codecstring = 'avc1.';
            for (i = 0; i < 3; i++) {
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
          push = false;
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
          } else if (value === 1 && i < len) {
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
                var track = this._avcTrack,
                    samples = track.samples;
                //logger.log('first NALU found with overflow:' + overflow);
                if (samples.length) {
                  var lastavcSample = samples[samples.length - 1],
                      lastUnits = lastavcSample.units.units,
                      lastUnit = lastUnits[lastUnits.length - 1],
                      tmp = new Uint8Array(lastUnit.data.byteLength + overflow);
                  tmp.set(lastUnit.data, 0);
                  tmp.set(array.subarray(0, overflow), lastUnit.data.byteLength);
                  lastUnit.data = tmp;
                  lastavcSample.units.length += overflow;
                  track.len += overflow;
                }
              }
            }
            lastUnitStart = i;
            lastUnitType = unitType;
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
    var track = this._aacTrack,
        data = pes.data,
        pts = pes.pts,
        startOffset = 0,
        duration = this._duration,
        audioCodec = this.audioCodec,
        aacOverFlow = this.aacOverFlow,
        lastAacPTS = this.lastAacPTS,
        config, frameLength, frameDuration, frameIndex, offset, headerLength, stamp, len, aacSample;
    if (aacOverFlow) {
      var tmp = new Uint8Array(aacOverFlow.byteLength + data.byteLength);
      tmp.set(aacOverFlow, 0);
      tmp.set(data, aacOverFlow.byteLength);
      //logger.log(`AAC: append overflowing ${aacOverFlow.byteLength} bytes to beginning of new PES`);
      data = tmp;
    }
    // look for ADTS header (0xFFFx)
    for (offset = startOffset, len = data.length; offset < len - 1; offset++) {
      if ((data[offset] === 0xff) && (data[offset+1] & 0xf0) === 0xf0) {
        break;
      }
    }
    // if ADTS header does not start straight from the beginning of the PES payload, raise an error
    if (offset) {
      var reason, fatal;
      if (offset < len - 1) {
        reason = `AAC PES did not start with ADTS header,offset:${offset}`;
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
      config = ADTS.getAudioConfig(this.observer,data, offset, audioCodec);
      track.config = config.config;
      track.audiosamplerate = config.samplerate;
      track.channelCount = config.channelCount;
      track.codec = config.codec;
      track.duration = duration;
      logger.log(`parsed codec:${track.codec},rate:${config.samplerate},nb channel:${config.channelCount}`);
    }
    frameIndex = 0;
    frameDuration = 1024 * 90000 / track.audiosamplerate;

    // if last AAC frame is overflowing, we should ensure timestamps are contiguous:
    // first sample PTS should be equal to last sample PTS + frameDuration
    if(aacOverFlow && lastAacPTS) {
      var newPTS = lastAacPTS+frameDuration;
      if(Math.abs(newPTS-pts) > 1) {
        logger.log(`AAC: align PTS for overlapping frames by ${Math.round((newPTS-pts)/90)}`);
        pts=newPTS;
      }
    }

    while ((offset + 5) < len) {
      // The protection skip bit tells us if we have 2 bytes of CRC data at the end of the ADTS header
      headerLength = (!!(data[offset + 1] & 0x01) ? 7 : 9);
      // retrieve frame size
      frameLength = ((data[offset + 3] & 0x03) << 11) |
                     (data[offset + 4] << 3) |
                    ((data[offset + 5] & 0xE0) >>> 5);
      frameLength  -= headerLength;
      //stamp = pes.pts;

      if ((frameLength > 0) && ((offset + headerLength + frameLength) <= len)) {
        stamp = pts + frameIndex * frameDuration;
        //logger.log(`AAC frame, offset/length/total/pts:${offset+headerLength}/${frameLength}/${data.byteLength}/${(stamp/90).toFixed(0)}`);
        aacSample = {unit: data.subarray(offset + headerLength, offset + headerLength + frameLength), pts: stamp, dts: stamp};
        track.samples.push(aacSample);
        track.len += frameLength;
        offset += frameLength + headerLength;
        frameIndex++;
        // look for ADTS header (0xFFFx)
        for ( ; offset < (len - 1); offset++) {
          if ((data[offset] === 0xff) && ((data[offset + 1] & 0xf0) === 0xf0)) {
            break;
          }
        }
      } else {
        break;
      }
    }
    if (offset < len) {
      aacOverFlow = data.subarray(offset, len);
      //logger.log(`AAC: overflow detected:${len-offset}`);
    } else {
      aacOverFlow = null;
    }
    this.aacOverFlow = aacOverFlow;
    this.lastAacPTS = stamp;
  }

  _parseID3PES(pes) {
    this._id3Track.samples.push(pes);
  }
}

export default TSDemuxer;

