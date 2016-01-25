
// adapted from https://github.com/kanongil/node-m3u8parse/blob/master/attrlist.js
class AttrList {

  constructor(attrs) {
    if (typeof attrs === 'string') {
      attrs = AttrList.parseAttrList(attrs);
    }
    for(var attr in attrs){
      if(attrs.hasOwnProperty(attr)) {
        this[attr] = attrs[attr];
      }
    }
  }

  decimalInteger(attrName) {
    const intValue = parseInt(this[attrName], 10);
    if (intValue > Number.MAX_SAFE_INTEGER) {
      return Infinity;
    }
    return intValue;
  }

  hexadecimalInteger(attrName) {
    if(this[attrName]) {
      let stringValue = (this[attrName] || '0x').slice(2);
      stringValue = ((stringValue.length & 1) ? '0' : '') + stringValue;

      const value = new Uint8Array(stringValue.length / 2);
      for (let i = 0; i < stringValue.length / 2; i++) {
        value[i] = parseInt(stringValue.slice(i * 2, i * 2 + 2), 16);
      }
      return value;
    } else {
      return null;
    }
  }

  hexadecimalIntegerAsNumber(attrName) {
    const intValue = parseInt(this[attrName], 16);
    if (intValue > Number.MAX_SAFE_INTEGER) {
      return Infinity;
    }
    return intValue;
  }

  decimalFloatingPoint(attrName) {
    return parseFloat(this[attrName]);
  }

  enumeratedString(attrName) {
    return this[attrName];
  }

  decimalResolution(attrName) {
    const res = /^(\d+)x(\d+)$/.exec(this[attrName]);
    if (res === null) {
      return undefined;
    }
    return {
      width: parseInt(res[1], 10),
      height: parseInt(res[2], 10)
    };
  }

  static parseAttrList(input) {
    const re = /\s*(.+?)\s*=((?:\".*?\")|.*?)(?:,|$)/g;
    var match, attrs = {};
    while ((match = re.exec(input)) !== null) {
      var value = match[2], quote = '"';

      if (value.indexOf(quote) === 0 &&
          value.lastIndexOf(quote) === (value.length-1)) {
        value = value.slice(1, -1);
      }
      attrs[match[1]] = value;
    }
    return attrs;
  }

}

export default AttrList;
