/*
 * CEA-708 interpreter
*/

class CEA708Interpreter {

  constructor() {
  }

  attach(media) {
    this.media = media;
    this.display = [];
    this.memory = [];
  }

  detach()
  {
    this.clear();
  }

  destroy() {
  }

  _createCue()
  {
    var VTTCue = window.VTTCue || window.TextTrackCue;

    var cue = this.cue = new VTTCue(-1, -1, '');
    cue.text = '';
    cue.pauseOnExit = false;

    // make sure it doesn't show up before it's ready
    cue.startTime = Number.MAX_VALUE;

    // show it 'forever' once we do show it
    // (we'll set the end time once we know it later)
    cue.endTime = Number.MAX_VALUE;

    this.memory.push(cue);
  }

  clear()
  {
    var textTrack = this._textTrack;
    if (textTrack && textTrack.cues)
    {
      while (textTrack.cues.length > 0)
      {
        textTrack.removeCue(textTrack.cues[0]);
      }
    }
  }

  push(timestamp, bytes)
  {
    if (!this.cue)
    {
      this._createCue();
    }

    var count = bytes[0] & 31;
    var position = 2;
    var tmpByte, ccbyte1, ccbyte2, ccValid, ccType;

    for (var j=0; j<count; j++)
    {
      tmpByte = bytes[position++];
      ccbyte1 = 0x7F & bytes[position++];
      ccbyte2 = 0x7F & bytes[position++];
      ccValid = ((4 & tmpByte) === 0 ? false : true);
      ccType = (3 & tmpByte);

      if (ccbyte1 === 0 && ccbyte2 === 0)
      {
        continue;
      }

      if (ccValid)
      {
        if (ccType === 0) // || ccType === 1
        {
          // Standard Characters
          if (0x20 & ccbyte1 || 0x40 & ccbyte1)
          {
            this.cue.text += this._fromCharCode(ccbyte1) + this._fromCharCode(ccbyte2);
          }
          // Special Characters
          else if ((ccbyte1 === 0x11 || ccbyte1 === 0x19) && ccbyte2 >= 0x30 && ccbyte2 <= 0x3F)
          {
            // extended chars, e.g. musical note, accents
            switch (ccbyte2)
            {
              case 48:
                this.cue.text += '®';
                break;
              case 49:
                this.cue.text += '°';
                break;
              case 50:
                this.cue.text += '½';
                break;
              case 51:
                this.cue.text += '¿';
                break;
              case 52:
                this.cue.text += '™';
                break;
              case 53:
                this.cue.text += '¢';
                break;
              case 54:
                this.cue.text += '';
                break;
              case 55:
                this.cue.text += '£';
                break;
              case 56:
                this.cue.text += '♪';
                break;
              case 57:
                this.cue.text += ' ';
                break;
              case 58:
                this.cue.text += 'è';
                break;
              case 59:
                this.cue.text += 'â';
                break;
              case 60:
                this.cue.text += 'ê';
                break;
              case 61:
                this.cue.text += 'î';
                break;
              case 62:
                this.cue.text += 'ô';
                break;
              case 63:
                this.cue.text += 'û';
                break;
            }
          }
          if ((ccbyte1 === 0x11 || ccbyte1 === 0x19) && ccbyte2 >= 0x20 && ccbyte2 <= 0x2F)
          {
            // Mid-row codes: color/underline
            switch (ccbyte2)
            {
              case 0x20:
                // White
                break;
              case 0x21:
                // White Underline
                break;
              case 0x22:
                // Green
                break;
              case 0x23:
                // Green Underline
                break;
              case 0x24:
                // Blue
                break;
              case 0x25:
                // Blue Underline
                break;
              case 0x26:
                // Cyan
                break;
              case 0x27:
                // Cyan Underline
                break;
              case 0x28:
                // Red
                break;
              case 0x29:
                // Red Underline
                break;
              case 0x2A:
                // Yellow
                break;
              case 0x2B:
                // Yellow Underline
                break;
              case 0x2C:
                // Magenta
                break;
              case 0x2D:
                // Magenta Underline
                break;
              case 0x2E:
                // Italics
                break;
              case 0x2F:
                // Italics Underline
                break;
            }
          }
          if ((ccbyte1 === 0x14 || ccbyte1 === 0x1C) && ccbyte2 >= 0x20 && ccbyte2 <= 0x2F)
          {
            // Mid-row codes: color/underline
            switch (ccbyte2)
            {
              case 0x20:
                // TODO: shouldn't affect roll-ups...
                this._clearActiveCues(timestamp);
                // RCL: Resume Caption Loading
                // begin pop on
                break;
              case 0x21:
                // BS: Backspace
                this.cue.text = this.cue.text.substr(0, this.cue.text.length-1);
                break;
              case 0x22:
                // AOF: reserved (formerly alarm off)
                break;
              case 0x23:
                // AON: reserved (formerly alarm on)
                break;
              case 0x24:
                // DER: Delete to end of row
                break;
              case 0x25:
                // RU2: roll-up 2 rows
                //this._rollup(2);
                break;
              case 0x26:
                // RU3: roll-up 3 rows
                //this._rollup(3);
                break;
              case 0x27:
                // RU4: roll-up 4 rows
                //this._rollup(4);
                break;
              case 0x28:
                // FON: Flash on
                break;
              case 0x29:
                // RDC: Resume direct captioning
                this._clearActiveCues(timestamp);
                break;
              case 0x2A:
                // TR: Text Restart
                break;
              case 0x2B:
                // RTD: Resume Text Display
                break;
              case 0x2C:
                // EDM: Erase Displayed Memory
                this._clearActiveCues(timestamp);
                break;
              case 0x2D:
                // CR: Carriage Return
                // only affects roll-up
                //this._rollup(1);
                break;
              case 0x2E:
                // ENM: Erase non-displayed memory
                this._text = '';
                break;
              case 0x2F:
                this._flipMemory(timestamp);
                // EOC: End of caption
                // hide any displayed captions and show any hidden one
                break;
            }
          }
          if ((ccbyte1 === 0x17 || ccbyte1 === 0x1F) && ccbyte2 >= 0x21 && ccbyte2 <= 0x23)
          {
            // Mid-row codes: color/underline
            switch (ccbyte2)
            {
              case 0x21:
                // TO1: tab offset 1 column
                break;
              case 0x22:
                // TO1: tab offset 2 column
                break;
              case 0x23:
                // TO1: tab offset 3 column
                break;
            }
          }
          else {
            // Probably a pre-amble address code
          }
        }
      }
    }
  }

  _fromCharCode(tmpByte)
  {
    switch (tmpByte)
    {
      case 42:
        return 'á';

      case 2:
        return 'á';

      case 2:
        return 'é';

      case 4:
        return 'í';

      case 5:
        return 'ó';

      case 6:
        return 'ú';

      case 3:
        return 'ç';

      case 4:
        return '÷';

      case 5:
        return 'Ñ';

      case 6:
        return 'ñ';

      case 7:
        return '█';

      default:
        return String.fromCharCode(tmpByte);
    }
  }

  _flipMemory(timestamp)
  {
    this._clearActiveCues(timestamp);
    this._flushCaptions(timestamp);
  }

  _flushCaptions(timestamp)
  {
    if (!this._has708)
    {
      this._textTrack = this.media.addTextTrack('captions', 'English', 'en');
      this._has708 = true;
    }

    for(let memoryItem of this.memory)
    {
      memoryItem.startTime = timestamp;
      this._textTrack.addCue(memoryItem);
      this.display.push(memoryItem);
    }

    this.memory = [];
    this.cue = null;
  }

  _clearActiveCues(timestamp)
  {
    for (let displayItem of this.display)
    {
      displayItem.endTime = timestamp;
    }

    this.display = [];
  }

/*  _rollUp(n)
  {
    // TODO: implement roll-up captions
  }
*/
  _clearBufferedCues()
  {
    //remove them all...
  }

}

export default CEA708Interpreter;

