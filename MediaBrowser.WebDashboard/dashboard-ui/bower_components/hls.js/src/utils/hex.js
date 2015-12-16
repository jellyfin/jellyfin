class Hex {

  static hexDump(array) {
    var i, str = '';
    for(i = 0; i < array.length; i++) {
      var h = array[i].toString(16);
      if (h.length < 2) {
        h = '0' + h;
      }
      str += h;
    }
    return str;
  }
}

export default Hex;
