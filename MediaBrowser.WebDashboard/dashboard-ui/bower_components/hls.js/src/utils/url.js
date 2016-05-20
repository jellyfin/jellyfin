var URLHelper = {
  // build an absolute URL from a relative one using the provided baseURL
  // if relativeURL is an absolute URL it will be returned as is.
  buildAbsoluteURL: function(baseURL, relativeURL) {
    // remove any remaining space and CRLF
    relativeURL = relativeURL.trim();
    if (/^[a-z]+:/i.test(relativeURL)) {
      // complete url, not relative
      return relativeURL;
    }

    var relativeURLQuery = null;
    var relativeURLHash = null;

    var relativeURLHashSplit = /^([^#]*)(.*)$/.exec(relativeURL);
    if (relativeURLHashSplit) {
      relativeURLHash = relativeURLHashSplit[2];
      relativeURL = relativeURLHashSplit[1];
    }
    var relativeURLQuerySplit = /^([^\?]*)(.*)$/.exec(relativeURL);
    if (relativeURLQuerySplit) {
      relativeURLQuery = relativeURLQuerySplit[2];
      relativeURL = relativeURLQuerySplit[1];
    }

    var baseURLHashSplit = /^([^#]*)(.*)$/.exec(baseURL);
    if (baseURLHashSplit) {
      baseURL = baseURLHashSplit[1];
    }
    var baseURLQuerySplit = /^([^\?]*)(.*)$/.exec(baseURL);
    if (baseURLQuerySplit) {
      baseURL = baseURLQuerySplit[1];
    }

    var baseURLDomainSplit = /^(([a-z]+:)?\/\/[a-z0-9\.\-_~]+(:[0-9]+)?)?(\/.*)$/i.exec(baseURL);
    if (!baseURLDomainSplit) {
      throw new Error('Error trying to parse base URL.');
    }
    
    // e.g. 'http:', 'https:', ''
    var baseURLProtocol = baseURLDomainSplit[2] || '';
    // e.g. 'http://example.com', '//example.com', ''
    var baseURLProtocolDomain = baseURLDomainSplit[1] || '';
    // e.g. '/a/b/c/playlist.m3u8'
    var baseURLPath = baseURLDomainSplit[4];

    var builtURL = null;
    if (/^\/\//.test(relativeURL)) {
      // relative url starts wth '//' so copy protocol (which may be '' if baseUrl didn't provide one)
      builtURL = baseURLProtocol+'//'+URLHelper.buildAbsolutePath('', relativeURL.substring(2));
    }
    else if (/^\//.test(relativeURL)) {
      // relative url starts with '/' so start from root of domain
      builtURL = baseURLProtocolDomain+'/'+URLHelper.buildAbsolutePath('', relativeURL.substring(1));
    }
    else {
      builtURL = URLHelper.buildAbsolutePath(baseURLProtocolDomain+baseURLPath, relativeURL);
    }

    // put the query and hash parts back
    if (relativeURLQuery) {
      builtURL += relativeURLQuery;
    }
    if (relativeURLHash) {
      builtURL += relativeURLHash;
    }
    return builtURL;
  },

  // build an absolute path using the provided basePath
  // adapted from https://developer.mozilla.org/en-US/docs/Web/API/document/cookie#Using_relative_URLs_in_the_path_parameter
  // this does not handle the case where relativePath is "/" or "//". These cases should be handled outside this.
  buildAbsolutePath: function(basePath, relativePath) {
    var sRelPath = relativePath;
    var nUpLn, sDir = '', sPath = basePath.replace(/[^\/]*$/, sRelPath.replace(/(\/|^)(?:\.?\/+)+/g, '$1'));
    for (var nEnd, nStart = 0; nEnd = sPath.indexOf('/../', nStart), nEnd > -1; nStart = nEnd + nUpLn) {
      nUpLn = /^\/(?:\.\.\/)*/.exec(sPath.slice(nEnd))[0].length;
      sDir = (sDir + sPath.substring(nStart, nEnd)).replace(new RegExp('(?:\\\/+[^\\\/]*){0,' + ((nUpLn - 1) / 3) + '}$'), '/');
    }
    return sDir + sPath.substr(nStart);
  }
};

module.exports = URLHelper;
