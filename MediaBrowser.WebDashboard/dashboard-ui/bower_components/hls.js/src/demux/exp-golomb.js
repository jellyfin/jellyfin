/**
 * Parser for exponential Golomb codes, a variable-bitwidth number encoding scheme used by h264.
*/

import {logger} from '../utils/logger';

class ExpGolomb {

  constructor(data) {
    this.data = data;
    // the number of bytes left to examine in this.data
    this.bytesAvailable = this.data.byteLength;
    // the current word being examined
    this.word = 0; // :uint
    // the number of bits left to examine in the current word
    this.bitsAvailable = 0; // :uint
  }

  // ():void
  loadWord() {
    var
      position = this.data.byteLength - this.bytesAvailable,
      workingBytes = new Uint8Array(4),
      availableBytes = Math.min(4, this.bytesAvailable);
    if (availableBytes === 0) {
      throw new Error('no bytes available');
    }
    workingBytes.set(this.data.subarray(position, position + availableBytes));
    this.word = new DataView(workingBytes.buffer).getUint32(0);
    // track the amount of this.data that has been processed
    this.bitsAvailable = availableBytes * 8;
    this.bytesAvailable -= availableBytes;
  }

  // (count:int):void
  skipBits(count) {
    var skipBytes; // :int
    if (this.bitsAvailable > count) {
      this.word <<= count;
      this.bitsAvailable -= count;
    } else {
      count -= this.bitsAvailable;
      skipBytes = count >> 3;
      count -= (skipBytes >> 3);
      this.bytesAvailable -= skipBytes;
      this.loadWord();
      this.word <<= count;
      this.bitsAvailable -= count;
    }
  }

  // (size:int):uint
  readBits(size) {
    var
      bits = Math.min(this.bitsAvailable, size), // :uint
      valu = this.word >>> (32 - bits); // :uint
    if (size > 32) {
      logger.error('Cannot read more than 32 bits at a time');
    }
    this.bitsAvailable -= bits;
    if (this.bitsAvailable > 0) {
      this.word <<= bits;
    } else if (this.bytesAvailable > 0) {
      this.loadWord();
    }
    bits = size - bits;
    if (bits > 0) {
      return valu << bits | this.readBits(bits);
    } else {
      return valu;
    }
  }

  // ():uint
  skipLZ() {
    var leadingZeroCount; // :uint
    for (leadingZeroCount = 0; leadingZeroCount < this.bitsAvailable; ++leadingZeroCount) {
      if (0 !== (this.word & (0x80000000 >>> leadingZeroCount))) {
        // the first bit of working word is 1
        this.word <<= leadingZeroCount;
        this.bitsAvailable -= leadingZeroCount;
        return leadingZeroCount;
      }
    }
    // we exhausted word and still have not found a 1
    this.loadWord();
    return leadingZeroCount + this.skipLZ();
  }

  // ():void
  skipUEG() {
    this.skipBits(1 + this.skipLZ());
  }

  // ():void
  skipEG() {
    this.skipBits(1 + this.skipLZ());
  }

  // ():uint
  readUEG() {
    var clz = this.skipLZ(); // :uint
    return this.readBits(clz + 1) - 1;
  }

  // ():int
  readEG() {
    var valu = this.readUEG(); // :int
    if (0x01 & valu) {
      // the number is odd if the low order bit is set
      return (1 + valu) >>> 1; // add 1 to make it even, and divide by 2
    } else {
      return -1 * (valu >>> 1); // divide by two then make it negative
    }
  }

  // Some convenience functions
  // :Boolean
  readBoolean() {
    return 1 === this.readBits(1);
  }

  // ():int
  readUByte() {
    return this.readBits(8);
  }

  // ():int
  readUShort() {
    return this.readBits(16);
  }
    // ():int
  readUInt() {
    return this.readBits(32);
  }

  /**
   * Advance the ExpGolomb decoder past a scaling list. The scaling
   * list is optionally transmitted as part of a sequence parameter
   * set and is not relevant to transmuxing.
   * @param count {number} the number of entries in this scaling list
   * @see Recommendation ITU-T H.264, Section 7.3.2.1.1.1
   */
  skipScalingList(count) {
    var
      lastScale = 8,
      nextScale = 8,
      j,
      deltaScale;
    for (j = 0; j < count; j++) {
      if (nextScale !== 0) {
        deltaScale = this.readEG();
        nextScale = (lastScale + deltaScale + 256) % 256;
      }
      lastScale = (nextScale === 0) ? lastScale : nextScale;
    }
  }

  /**
   * Read a sequence parameter set and return some interesting video
   * properties. A sequence parameter set is the H264 metadata that
   * describes the properties of upcoming video frames.
   * @param data {Uint8Array} the bytes of a sequence parameter set
   * @return {object} an object with configuration parsed from the
   * sequence parameter set, including the dimensions of the
   * associated video frames.
   */
  readSPS() {
    var
      frameCropLeftOffset = 0,
      frameCropRightOffset = 0,
      frameCropTopOffset = 0,
      frameCropBottomOffset = 0,
      sarScale = 1,
      profileIdc,profileCompat,levelIdc,
      numRefFramesInPicOrderCntCycle, picWidthInMbsMinus1,
      picHeightInMapUnitsMinus1,
      frameMbsOnlyFlag,
      scalingListCount,
      i;
    this.readUByte();
    profileIdc = this.readUByte(); // profile_idc
    profileCompat = this.readBits(5); // constraint_set[0-4]_flag, u(5)
    this.skipBits(3); // reserved_zero_3bits u(3),
    levelIdc = this.readUByte(); //level_idc u(8)
    this.skipUEG(); // seq_parameter_set_id
    // some profiles have more optional data we don't need
    if (profileIdc === 100 ||
        profileIdc === 110 ||
        profileIdc === 122 ||
        profileIdc === 244 ||
        profileIdc === 44  ||
        profileIdc === 83  ||
        profileIdc === 86  ||
        profileIdc === 118 ||
        profileIdc === 128) {
      var chromaFormatIdc = this.readUEG();
      if (chromaFormatIdc === 3) {
        this.skipBits(1); // separate_colour_plane_flag
      }
      this.skipUEG(); // bit_depth_luma_minus8
      this.skipUEG(); // bit_depth_chroma_minus8
      this.skipBits(1); // qpprime_y_zero_transform_bypass_flag
      if (this.readBoolean()) { // seq_scaling_matrix_present_flag
        scalingListCount = (chromaFormatIdc !== 3) ? 8 : 12;
        for (i = 0; i < scalingListCount; i++) {
          if (this.readBoolean()) { // seq_scaling_list_present_flag[ i ]
            if (i < 6) {
              this.skipScalingList(16);
            } else {
              this.skipScalingList(64);
            }
          }
        }
      }
    }
    this.skipUEG(); // log2_max_frame_num_minus4
    var picOrderCntType = this.readUEG();
    if (picOrderCntType === 0) {
      this.readUEG(); //log2_max_pic_order_cnt_lsb_minus4
    } else if (picOrderCntType === 1) {
      this.skipBits(1); // delta_pic_order_always_zero_flag
      this.skipEG(); // offset_for_non_ref_pic
      this.skipEG(); // offset_for_top_to_bottom_field
      numRefFramesInPicOrderCntCycle = this.readUEG();
      for(i = 0; i < numRefFramesInPicOrderCntCycle; i++) {
        this.skipEG(); // offset_for_ref_frame[ i ]
      }
    }
    this.skipUEG(); // max_num_ref_frames
    this.skipBits(1); // gaps_in_frame_num_value_allowed_flag
    picWidthInMbsMinus1 = this.readUEG();
    picHeightInMapUnitsMinus1 = this.readUEG();
    frameMbsOnlyFlag = this.readBits(1);
    if (frameMbsOnlyFlag === 0) {
      this.skipBits(1); // mb_adaptive_frame_field_flag
    }
    this.skipBits(1); // direct_8x8_inference_flag
    if (this.readBoolean()) { // frame_cropping_flag
      frameCropLeftOffset = this.readUEG();
      frameCropRightOffset = this.readUEG();
      frameCropTopOffset = this.readUEG();
      frameCropBottomOffset = this.readUEG();
    }
    if (this.readBoolean()) {
      // vui_parameters_present_flag
      if (this.readBoolean()) {
        // aspect_ratio_info_present_flag
        let sarRatio;
        const aspectRatioIdc = this.readUByte();
        switch (aspectRatioIdc) {
          case 1: sarRatio = [1,1]; break;
          case 2: sarRatio = [12,11]; break;
          case 3: sarRatio = [10,11]; break;
          case 4: sarRatio = [16,11]; break;
          case 5: sarRatio = [40,33]; break;
          case 6: sarRatio = [24,11]; break;
          case 7: sarRatio = [20,11]; break;
          case 8: sarRatio = [32,11]; break;
          case 9: sarRatio = [80,33]; break;
          case 10: sarRatio = [18,11]; break;
          case 11: sarRatio = [15,11]; break;
          case 12: sarRatio = [64,33]; break;
          case 13: sarRatio = [160,99]; break;
          case 14: sarRatio = [4,3]; break;
          case 15: sarRatio = [3,2]; break;
          case 16: sarRatio = [2,1]; break;
          case 255: {
            sarRatio = [this.readUByte() << 8 | this.readUByte(), this.readUByte() << 8 | this.readUByte()];
            break;
          }
        }
        if (sarRatio) {
          sarScale = sarRatio[0] / sarRatio[1];
        }
      }
    }
    return {
      width: Math.ceil((((picWidthInMbsMinus1 + 1) * 16) - frameCropLeftOffset * 2 - frameCropRightOffset * 2) * sarScale),
      height: ((2 - frameMbsOnlyFlag) * (picHeightInMapUnitsMinus1 + 1) * 16) - ((frameMbsOnlyFlag? 2 : 4) * (frameCropTopOffset + frameCropBottomOffset))
    };
  }

  readSliceType() {
    // skip NALu type
    this.readUByte();
    // discard first_mb_in_slice
    this.readUEG();
    // return slice_type
    return this.readUEG();
  }
}

export default ExpGolomb;
