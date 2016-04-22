"use strict";
describe("Fingerprint2", function () {
  describe("new", function () {
    it("creates a new instance of FP2", function () {
      expect(new Fingerprint2()).not.toBeNull();
    });

    it("accepts an empty options object", function () {
      expect(new Fingerprint2({})).not.toBeNull();
    });

    it("uses default options", function () {
      var fp2 = new Fingerprint2();
      expect(fp2.options.swfContainerId).toEqual("fingerprintjs2");
      expect(fp2.options.swfPath).toEqual("flash/compiled/FontList.swf");
    });

    it("allows to override default options", function () {
      var fp2 = new Fingerprint2({swfPath: "newpath"});
      expect(fp2.options.swfContainerId).toEqual("fingerprintjs2");
      expect(fp2.options.swfPath).toEqual("newpath");
    });

    it("allows to add new options", function () {
      var fp2 = new Fingerprint2({excludeUserAgent: true});
      expect(fp2.options.swfContainerId).toEqual("fingerprintjs2");
      expect(fp2.options.swfPath).toEqual("flash/compiled/FontList.swf");
      expect(fp2.options.excludeUserAgent).toBe(true);
    });

    describe("sortPluginsFor", function () {
      it("has default value", function (){
        var fp2 = new Fingerprint2();
        expect(fp2.options.sortPluginsFor).toEqual([/palemoon/i]);
      });

      it("allows to set new array of regexes", function () {
        var fp2 = new Fingerprint2({sortPluginsFor: [/firefox/i, /chrome/i]});
        expect(fp2.options.sortPluginsFor).toEqual([/firefox/i, /chrome/i]);
      });
    });
  });

  describe("get", function () {
    describe("default options", function () {
      it("calculates fingerprint", function (done) {
        var fp2 = new Fingerprint2();
        fp2.get(function(result){
          expect(result).toMatch(/^[0-9a-f]{32}$/i);
          done();
        });
      });

      it("does not try calling flash font detection", function (done) {
        var fp2 = new Fingerprint2();
        spyOn(fp2, "flashFontsKey");
        fp2.get(function(result) {
          expect(fp2.flashFontsKey).not.toHaveBeenCalled();
          done();
        });
      });
    });

    describe("non-default options", function () {
      it("does not use userAgent when excluded", function (done) {
        var fp2 = new Fingerprint2({excludeUserAgent: true});
        spyOn(fp2, "getUserAgent");
        fp2.get(function(result) {
          expect(fp2.getUserAgent).not.toHaveBeenCalled();
          done();
        });
      });

      it("does not use screen resolution when excluded", function (done) {
        var fp2 = new Fingerprint2({excludeScreenResolution: true});
        spyOn(fp2, "getScreenResolution");
        fp2.get(function(result) {
          expect(fp2.getScreenResolution).not.toHaveBeenCalled();
          done();
        });
      });

      it("does not use available screen resolution when excluded", function (done) {
        var fp2 = new Fingerprint2({excludeAvailableScreenResolution: true});
        spyOn(fp2, "getAvailableScreenResolution");
        fp2.get(function(result) {
          expect(fp2.getAvailableScreenResolution).not.toHaveBeenCalled();
          done();
        });
      });

      it("does not use plugins info when excluded", function (done) {
        var fp2 = new Fingerprint2({excludePlugins: true});
        spyOn(fp2, "getRegularPlugins");
        fp2.get(function(result) {
          expect(fp2.getRegularPlugins).not.toHaveBeenCalled();
          done();
        });
      });

      it("does not use IE plugins info when excluded", function (done) {
        var fp2 = new Fingerprint2({excludeIEPlugins: true});
        spyOn(fp2, "getIEPlugins");
        fp2.get(function(result) {
          expect(fp2.getIEPlugins).not.toHaveBeenCalled();
          done();
        });
      });

    });

    describe("returns components", function () {
      it("does it return components as a second argument to callback", function (done) {
        var fp2 = new Fingerprint2();
        fp2.get(function(result, components) {
          expect(components).not.toBeNull();
          done();
        });
      });

      it("checks if returned components is array", function (done) {
        var fp2 = new Fingerprint2();
        fp2.get(function(result, components) {
          expect(components).toBeArrayOfObjects();
          done();
        });
      });

      it("checks if js_fonts component is array", function (done) {
        var fp2 = new Fingerprint2();
        fp2.get(function(result, components) {
          for(var x = 0; x < components.length; x++) {
            if(components[x].key == "js_fonts") {
                expect(components[x].value).toBeArray();
            }
          }
          done();
        });
      });

      it("returns user_agent as the first element", function (done) {
        var fp2 = new Fingerprint2();
        fp2.get(function(result, components) {
          expect(components[0].key).toEqual("user_agent");
          done();
        });
      });
    });

    describe("baseFontArray iteration", function () {
      it("only iterates specified items", function (done) {
        var baseFonts = ["monospace", "sans-serif", "serif"];
        var ctr = 0;
        for (var x in baseFonts) {
          ctr++;
        }

        expect(baseFonts.length).toEqual(3);
        expect(ctr).toEqual(baseFonts.length);

        // Somewhere deep in your JavaScript library...
        Array.prototype.foo = 1;
        Array.prototype.bar = 2;
        ctr = 0;
        for (var x in baseFonts) {
          console.log(x);
          ctr++;
          // Now foo & bar is a part of EVERY array and
          // will show up here as a value of 'x'.
        }

        expect(baseFonts.length).toEqual(3);
        // sadface
        expect(ctr).not.toEqual(baseFonts.length);
        expect(ctr).toEqual(5);
        done();
      });
    });
  });
});
