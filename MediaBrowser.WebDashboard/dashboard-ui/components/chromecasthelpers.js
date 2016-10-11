define([], function () {

    // LinkParser
    //
    // https://github.com/ravisorg/LinkParser
    //
    // Locate and extract almost any URL within a string. Handles protocol-less domains, IPv4 and 
    // IPv6, unrecognised TLDs, and more.
    //
    // This work is licensed under a Creative Commons Attribution-ShareAlike 4.0 International License.
    // http://creativecommons.org/licenses/by-sa/4.0/
    (function () {

        // Original URL regex from the Android android.text.util.Linkify function, found here:
        // http://stackoverflow.com/a/19696443
        // 
        // However there were problems with it, most probably related to the fact it was 
        // written in 2007, and it's been highly modified.
        // 
        // 1) I didn't like the fact that it was tied to specific TLDs, since new ones 
        // are being added all the time it wouldn't be reasonable to expect developer to
        // be continually updating their regular expressions.
        // 
        // 2) It didn't allow unicode characters in the domains which are now allowed in 
        // many languages, (including some IDN TLDs). Again these are constantly being
        // added to and it doesn't seem reasonable to hard-code them. Note this ended up
        // not being possible in standard JS due to the way it handles multibyte strings.
        // It is possible using XRegExp, however a big performance hit results. Disabled
        // for now.
        // 
        // 3) It didn't allow for IPv6 hostnames
        // IPv6 regex from http://stackoverflow.com/a/17871737
        //
        // 4) It was very poorly commented
        // 
        // 5) It wasn't as smart as it could have been about what should be part of a
        // URL and what should be part of human language.

        var protocols = "(?:(?:http|https|rtsp|ftp):\\/\\/)";
        var credentials = "(?:(?:[a-z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-f0-9]{2})){1,64}" // username (1-64 normal or url escaped characters)
            + "(?:\\:(?:[a-z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-f0-9]{2})){1,25})?" // followed by optional password (: + 1-25 normal or url escaped characters)
            + "\\@)";

        var hostnames = "(?:[a-z0-9][a-z0-9\\-]{0,64}\\.)+";   // named host
        var unknownTLDs = "(?:[a-z]{2,})";

        // unicode regexp isn't really supported in JS. It can be emulated with XRegExp 
        // <http://xregexp.com/> but that creates a pretty big performance hit that 
        // is very noticable on mobile devices. We'll add an option in the future to
        // support them.
        var XRegExp_hostnames = "(?:[\\p{L}0-9][\\p{L}0-9\\-]{0,64}\\.)+";   // named host
        var XRegExp_unknownTLDs = "(?:[\\p{L}]{2,})"; // top level domain

        // strings that start with one of these are more likely to be URLs
        var knownSubdomains = "(?:(?:www|ftp)\\.)";

        // update from here when needed: https://data.iana.org/TLD/tlds-alpha-by-domain.txt
        var knownTLDs = "(?:"
            + "A[CDEFGILMNOQRSTUWXZ]|ACADEMY|ACCOUNTANTS|ACTOR|AERO|AGENCY|AIRFORCE|ARCHI|ARPA|ASIA|ASSOCIATES|AUDIO|AUTOS|AXA"
            + "|B[ABDEFGHIJMNORSTVWYZ]|BAR|BARGAINS|BAYERN|BEER|BERLIN|BEST|BID|BIKE|BIZ|BLACK|BLACKFRIDAY|BLUE|BOUTIQUE|BUILD|BUILDERS|BUZZ"
            + "|C[ACDFGHIKLMNORUVWXYZ]|CAB|CAMERA|CAMP|CAPITAL|CARDS|CARE|CAREER|CAREERS|CASH|CAT|CATERING|CENTER|CEO|CHEAP|CHRISTMAS|CHURCH|CITIC|CLAIMS|CLEANING|CLINIC|CLOTHING|CLUB|CODES|COFFEE|COLLEGE|COLOGNE|COM|COMMUNITY|COMPANYCOMPUTER|CONDOS|CONSTRUCTION|CONSULTING|CONTRACTORS|COOKING|COOL|COOP|COUNTRY|CREDIT|CREDITCARD|CRUISES"
            + "|D[EJKMOZ]|DANCE|DATING|DEMOCRAT|DENTAL|DESI|DIAMONDS|DIGITAL|DIRECTORY|DISCOUNT|DNP|DOMAINS"
            + "|E[CEGRSTU]|EDU|EDUCATION|EMAIL|ENGINEERING|ENTERPRISES|EQUIPMENT|ESTATE|EUS|EVENTS|EXCHANGE|EXPERT|EXPOSED"
            + "|F[IJKMOR]|FAIL|FARM|FEEDBACK|FINANCE|FINANCIAL|FISH|FISHING|FITNESS|FLIGHTS|FLORIST|FOO|FOUNDATION|FROGANS|FUND|FURNITURE|FUTBOL"
            + "|G[ABDEFGHILMNPQRSTUWY]|GAL|GALLERY|GIFT|GLASS|GLOBO|GMO|GOP|GOV|GRAPHICS|GRATIS|GRIPE|GUIDE|GUITARS|GURU"
            + "|H[KMNRTU]|HAUS|HIPHOP|HOLDINGS|HOLIDAY|HOMES|HORSE|HOUSE"
            + "|I[DELMNOQRST]|IMMOBILIEN|INDUSTRIES|INFO|INK|INSTITUTE|INSURE|INT|INTERNATIONAL|INVESTMENTS"
            + "|J[EMOP]|JETZT|JOBS|JUEGOS"
            + "|K[EGHIMNPRWYZ]|KAUFEN|KIM|KITCHEN|KIWI|KOELN|KRED"
            + "|L[ABCIKRSTUVY]|LAND|LEASE|LIFE|LIGHTING|LIMITED|LIMO|LINK|LOANS|LONDON|LUXE|LUXURY"
            + "|M[ACDEGHKLMNOPQRSTUVWXYZ]|MAISON|MANAGEMENT|MANGO|MARKETING|MEDIA|MEET|MENU|MIAMI|MIL|MOBI|MODA|MOE|MONASH|MOSCOW|MOTORCYCLES|MUSEUM"
            + "|N[ACEFGILOPRUZ]|NAGOYA|NAME|NET|NEUSTAR|NINJA|NYC"
            + "|O[M]|OKINAWA|ONL|ORG"
            + "|P[AEFGHKLMNRSTWY]|PARIS|PARTNERS|PARTS|PHOTO|PHOTOGRAPHY|PHOTOS|PICS|PICTURES|PINK|PLUMBING|POST|PRO|PRODUCTIONS|PROPERTIES|PUB"
            + "|Q[A]|QPON|QUEBEC"
            + "|R[EOSUW]|RECIPES|RED|REISE|REISEN|REN|RENTALS|REPAIR|REPORT|REST|REVIEWS|RICH|RIO|ROCKS|RODEO|RUHR|RYUKYU"
            + "|S[ABCDEGHIJKLMNORTUVXYZ]|SAARLAND|SCHULE|SERVICES|SEXY|SHIKSHA|SHOES|SINGLES|SOCIAL|SOHU|SOLAR|SOLUTIONS|SOY|SUPPLIES|SUPPLY|SUPPORT|SURGERY|SYSTEMS"
            + "|T[CDFGHJKLMNOPRTVWZ]|TATTOO|TAX|TECHNOLOGY|TEL|TIENDA|TIPS|TODAY|TOKYO|TOOLS|TOWN|TOYS|TRADE|TRAINING|TRAVEL"
            + "|U[AGKSYZ]|UNIVERSITY|UNO"
            + "|V[ACEGINU]|VACATIONS|VEGAS|VENTURES|VERSICHERUNG|VIAJES|VILLAS|VISION|VODKA|VOTE|VOTING|VOTO|VOYAGE"
            + "|W[FS]|WANG|WATCH|WEBCAM|WED|WIEN|WIKI|WORKS|WTC|WTF"
            + "|XXX|XYZ"
            + "|Y[ET]|YACHTS|YOKOHAMA"
            + "|Z[AMW]|ZONE"
            // IDN TLDs in both punycode and unicode format
            + "|XN--(?:3BST00M|3DS443G|3E0B707E|45BRJ9C|55QW42G|55QX5D|6FRZ82G|6QQ986B3XL|80ADXHKS|80AO21A|80ASEHDB|80ASWG|90A3AC|C1AVG|CG4BKI|CLCHC0EA0B2G2A9GCD|CZR694B|CZRU2D|D1ACJ3B|FIQ228C5HS|FIQ64B|FIQS8S|FIQZ9S|FPCRJ9C3D|FZC2C9E2C|GECRJ9C|H2BRJ9C|I1B6B1A6A2E|IO0A7I|J1AMH|J6W193G|KPRW13D|KPRY57D|L1ACC|LGBBAT1AD8J|MGB9AWBF|MGBA3A4F16A|MGBAAM7A8H|MGBAB2BD|MGBAYH7GPA|MGBBH1A71E|MGBC0A9AZCG|MGBERP4A5D4AR|MGBX4CD0AB|NGBC5AZD|NQV7F|NQV7FS00EMA|O3CW4H|OGBPF8FL|P1AI|PGBS0DH|Q9JYB4C|RHQV96G|S9BRJ9C|SES554G|UNUP4Y|WGBH1C|WGBL6A|XKC2AL3HYE2A|XKC2DL3A5EE0H|YFRO4I67O|YGBI2AMMX|ZFR164B)"
            + "|\u0627\u0644\u0627\u0631\u062F\u0646|\u4E2D\u570B|\u4E2D\u56FD|\u0627\u0645\u0627\u0631\u0627\u062A|\u9999\u6E2F|\u0627\u06CC\u0631\u0627\u0646|\u0DBD\u0D82\u0D9A\u0DCF|\u0B87\u0BB2\u0B99\u0BCD\u0B95\u0BC8|\u0645\u0635\u0631|\u0642\u0637\u0631|\u0420\u0424|\u0633\u0648\u0631\u064A\u0629|\u53F0\u7063|\u53F0\u6E7E"
            + ")";

        // IPv6 Regex http://forums.intermapper.com/viewtopic.php?t=452
        // by Dartware, LLC is licensed under a Creative Commons Attribution-ShareAlike 3.0 Unported License
        // http://intermapper.com/
        var ipv6 = "("
            + "(([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))"
            + "|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))"
            + "|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))"
            + "|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + ")(%.+)?";

        var ipv4 = "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9])\\."
            + "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\."
            + "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\."
            + "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[0-9])";

        // This would have been a lot cleaner if JS RegExp supported conditionals...
        var linkRegExpString =

            // begin match for protocol / username / password / host
            "(?:"

                // ============================
                // If we have a recognized protocol at the beginning of the URL, we're
                // more relaxed about what we accept, because we assume the user wants
                // this to be a URL, and we're not accidentally matching human language
                + protocols

                // optional username:password@
                + credentials + "?"

                // loose definition of a host name
                // any valid unicode sequence is ok for domain
                // this is our failsafe. If we don't recognize a TLD (because new TLDs are
                // constantly being added) and the host name doesn't start with a common
                // prefix like www, then this should catch anything that begins with a 
                // valid protocol (above)
                + hostnames + unknownTLDs

                // ============================
                // OR no protocol and a stricter form of a host name, with something that
                // we recognize as a likely URL. This will hopefully keep us from matching
                // typos like.this as a URL.
                + "|(?!" + protocols + ")"

                // optional username:password@
                + credentials + "?"

                + "(?:"

                    // ends with known TLD
                    + hostnames + knownTLDs

                    // OR begins with a known common subdomain
                    + "|" + knownSubdomains + hostnames + unknownTLDs

                    // OR contains a slash after the TLD (very likely a URL, unlikely to exist in common language)
                    + "|" + hostnames + unknownTLDs + "(?=\\/)" // use a lookahead so we don't pre-match the path below

                + ")"

                // ============================
                // OR we'll also accept IPv6 and IPv4 addresses, with or without a protocol, since it's highly 
                // unlikely that these would appear in normal language without being intended as a URL
                + "|" + protocols + "?"

                // optional username:password@
                + credentials + "?"

                // IP address (both v4 and v6)
                + "(?:"

                    // IPv6
                    + ipv6

                    // IPv4
                    + "|" + ipv4

                + ")"

            // end match for protocol / username / password / host
            + ")"

            // optional port number
            + "(?:\\:\\d{1,5})?"

            // plus optional path and query params (no unicode allowed here?)
            + "(?:"
                + "\\/(?:"
                    // some characters we'll accept because it's unlikely human language
                    // would use them after a URL unless they were part of the url
                    + "(?:[a-z0-9\\/\\@\\&\\#\\~\\*\\_\\-\\+])"
                    + "|(?:\\%[a-f0-9]{2})"
                    // some characters are much more likely to be used AFTER a url and
                    // were not intended to be included in the url itself. Mostly end
                    // of sentence type things. It's also likely that the URL would 
                    // still work if any of these characters were missing from the end 
                    // because we parsed it incorrectly. For these characters to be accepted
                    // they must be followed by another character that we're reasonably
                    // sure is part of the url
                    + "|(?:[\\;\\?\\:\\.\\!\\'\\(\\)\\,\\=]+(?=(?:[a-z0-9\\/\\@\\&\\#\\~\\*\\_\\-\\+])|(?:\\%[a-f0-9]{2})))"
                + ")*"
                + "|\\b|\$"
            + ")";

        // regex = XRegExp(regex,'gi');
        var linkRegExp = RegExp(linkRegExpString, 'gi');

        var protocolRegExp = RegExp('^' + protocols, 'i');

        // if url doesn't begin with a known protocol, add http by default
        function ensureProtocol(url) {
            if (!url.match(protocolRegExp)) {
                url = "http://" + url;
            }
            return url;
        }

        // look for links in the text
        var LinkParser = {

            parse: function (text) {
                var links = [];

                while (match = linkRegExp.exec(text)) {
                    // console.log(matches);
                    var txt = match[0];
                    var pos = match['index'];
                    var len = txt.length;
                    var url = ensureProtocol(text);
                    links.push({ 'pos': pos, 'text': txt, 'len': len, 'url': url });
                }

                return links;
            }

        }

        window.LinkParser = LinkParser;
    })();

    var cache = {};

    function getEndpointInfo(apiClient) {

        return apiClient.getJSON(apiClient.getUrl('System/Endpoint'));
    }

    function isValidIpAddress(address) {

        var links = LinkParser.parse(address);

        return links.length == 1;
    }

    function isLocalIpAddress(address) {

        address = address.toLowerCase();

        if (address.indexOf('127.0.0.1') != -1) {
            return true;
        }
        if (address.indexOf('localhost') != -1) {
            return true;
        }

        return false;
    }

    function getServerAddress(apiClient) {

        var serverAddress = apiClient.serverAddress();

        if (isValidIpAddress(serverAddress) && !isLocalIpAddress(serverAddress)) {
            return Promise.resolve(serverAddress);
        }

        var cachedValue = getCachedValue(serverAddress);
        if (cachedValue) {
            return Promise.resolve(cachedValue);
        }

        return apiClient.getJSON(apiClient.getUrl('System/Endpoint')).then(function (endpoint) {
            if (endpoint.IsInNetwork) {
                return apiClient.getPublicSystemInfo().then(function (info) {
                    addToCache(serverAddress, info.LocalAddress);
                    return info.LocalAddress;
                });
            } else {
                addToCache(serverAddress, serverAddress);
                return serverAddress;
            }
        });
    }

    function clearCache() {
        cache = {};
    }

    function addToCache(key, value) {
        cache[key] = {
            value: value,
            time: new Date().getTime()
        };
    }

    function getCachedValue(key) {

        var obj = cache[key];

        if (obj && (new Date().getTime() - obj.time) < 180000) {
            return obj.value;
        }

        return null;
    }

    Events.on(ConnectionManager, 'localusersignedin', clearCache);
    Events.on(ConnectionManager, 'localusersignedout', clearCache);

    return {
        getServerAddress: getServerAddress
    };
});