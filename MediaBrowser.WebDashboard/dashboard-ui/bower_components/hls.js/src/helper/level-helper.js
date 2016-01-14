/**
 * Level Helper class, providing methods dealing with playlist sliding and drift
*/

import {logger} from '../utils/logger';

class LevelHelper {

  static mergeDetails(oldDetails,newDetails) {
    var start = Math.max(oldDetails.startSN,newDetails.startSN)-newDetails.startSN,
        end = Math.min(oldDetails.endSN,newDetails.endSN)-newDetails.startSN,
        delta = newDetails.startSN - oldDetails.startSN,
        oldfragments = oldDetails.fragments,
        newfragments = newDetails.fragments,
        ccOffset =0,
        PTSFrag;

    // check if old/new playlists have fragments in common
    if ( end < start) {
      newDetails.PTSKnown = false;
      return;
    }
    // loop through overlapping SN and update startPTS , cc, and duration if any found
    for(var i = start ; i <= end ; i++) {
      var oldFrag = oldfragments[delta+i],
          newFrag = newfragments[i];
      ccOffset = oldFrag.cc - newFrag.cc;
      if (!isNaN(oldFrag.startPTS)) {
        newFrag.start = newFrag.startPTS = oldFrag.startPTS;
        newFrag.endPTS = oldFrag.endPTS;
        newFrag.duration = oldFrag.duration;
        PTSFrag = newFrag;
      }
    }

    if(ccOffset) {
      logger.log(`discontinuity sliding from playlist, take drift into account`);
      for(i = 0 ; i < newfragments.length ; i++) {
        newfragments[i].cc += ccOffset;
      }
    }

    // if at least one fragment contains PTS info, recompute PTS information for all fragments
    if(PTSFrag) {
      LevelHelper.updateFragPTS(newDetails,PTSFrag.sn,PTSFrag.startPTS,PTSFrag.endPTS);
    } else {
      // adjust start by sliding offset
      var sliding = oldfragments[delta].start;
      for(i = 0 ; i < newfragments.length ; i++) {
        newfragments[i].start += sliding;
      }
    }
    // if we are here, it means we have fragments overlapping between
    // old and new level. reliable PTS info is thus relying on old level
    newDetails.PTSKnown = oldDetails.PTSKnown;
    return;
  }

  static updateFragPTS(details,sn,startPTS,endPTS) {
    var fragIdx, fragments, frag, i;
    // exit if sn out of range
    if (sn < details.startSN || sn > details.endSN) {
      return 0;
    }
    fragIdx = sn - details.startSN;
    fragments = details.fragments;
    frag = fragments[fragIdx];
    if(!isNaN(frag.startPTS)) {
      startPTS = Math.min(startPTS,frag.startPTS);
      endPTS = Math.max(endPTS, frag.endPTS);
    }

    var drift = startPTS - frag.start;

    frag.start = frag.startPTS = startPTS;
    frag.endPTS = endPTS;
    frag.duration = endPTS - startPTS;
    // adjust fragment PTS/duration from seqnum-1 to frag 0
    for(i = fragIdx ; i > 0 ; i--) {
      LevelHelper.updatePTS(fragments,i,i-1);
    }

    // adjust fragment PTS/duration from seqnum to last frag
    for(i = fragIdx ; i < fragments.length - 1 ; i++) {
      LevelHelper.updatePTS(fragments,i,i+1);
    }
    details.PTSKnown = true;
    //logger.log(`                                            frag start/end:${startPTS.toFixed(3)}/${endPTS.toFixed(3)}`);

    return drift;
  }

  static updatePTS(fragments,fromIdx, toIdx) {
    var fragFrom = fragments[fromIdx],fragTo = fragments[toIdx], fragToPTS = fragTo.startPTS;
    // if we know startPTS[toIdx]
    if(!isNaN(fragToPTS)) {
      // update fragment duration.
      // it helps to fix drifts between playlist reported duration and fragment real duration
      if (toIdx > fromIdx) {
        fragFrom.duration = fragToPTS-fragFrom.start;
        if(fragFrom.duration < 0) {
          logger.error(`negative duration computed for frag ${fragFrom.sn},level ${fragFrom.level}, there should be some duration drift between playlist and fragment!`);
        }
      } else {
        fragTo.duration = fragFrom.start - fragToPTS;
        if(fragTo.duration < 0) {
          logger.error(`negative duration computed for frag ${fragTo.sn},level ${fragTo.level}, there should be some duration drift between playlist and fragment!`);
        }
      }
    } else {
      // we dont know startPTS[toIdx]
      if (toIdx > fromIdx) {
        fragTo.start = fragFrom.start + fragFrom.duration;
      } else {
        fragTo.start = fragFrom.start - fragTo.duration;
      }
    }
  }
}

export default LevelHelper;
