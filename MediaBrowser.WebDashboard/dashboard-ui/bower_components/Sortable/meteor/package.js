// Package metadata file for Meteor.js
'use strict';

var packageName = 'rubaxa:sortable';  // https://atmospherejs.com/rubaxa/sortable
var gitHubPath = 'RubaXa/Sortable';  // https://github.com/RubaXa/Sortable
var npmPackageName = 'sortablejs';  // https://www.npmjs.com/package/sortablejs - optional but recommended; used as fallback if GitHub fails

/* All of the below is just to get the version number of the 3rd party library.
 * First we'll try to read it from package.json. This works when publishing or testing the package
 * but not when running an example app that uses a local copy of the package because the current 
 * directory will be that of the app, and it won't have package.json. Finding the path of a file is hard:
 * http://stackoverflow.com/questions/27435797/how-do-i-obtain-the-path-of-a-file-in-a-meteor-package
 * Therefore, we'll fall back to GitHub (which is more frequently updated), and then to NPMJS.
 * We also don't have the HTTP package at this stage, and if we use Package.* in the request() callback,
 * it will error that it must be run in a Fiber. So we'll use Node futures.
 */
var request = Npm.require('request');
var Future = Npm.require('fibers/future');

var fut = new Future;
var version;

if (!version) try {
  var packageJson = JSON.parse(Npm.require('fs').readFileSync('../package.json'));
  version = packageJson.version;
} catch (e) {
  // if the file was not found, fall back to GitHub
  console.warn('Could not find ../package.json to read version number from; trying GitHub...');
  var url = 'https://api.github.com/repos/' + gitHubPath + '/tags';
  request.get({
    url: url,
    headers: {
      'User-Agent': 'request'  // GitHub requires it
    }
  }, function (error, response, body) {
    if (!error && response.statusCode === 200) {
      var versions = JSON.parse(body).map(function (version) {
        return version['name'].replace(/^\D+/, '')  // trim leading non-digits from e.g. "v4.3.0"
      }).sort();
      fut.return(versions[versions.length -1]);
    } else {
      // GitHub API rate limit reached? Fall back to npmjs.
      console.warn('GitHub request to', url, 'failed:\n ', response && response.statusCode, response && response.body, error || '', '\nTrying NPMJS...');
      url = 'http://registry.npmjs.org/' + npmPackageName + '/latest';
      request.get(url, function (error, response, body) {
        if (!error && response.statusCode === 200)
          fut.return(JSON.parse(body).version);
        else
          fut.throw('Could not get version information from ' + url + ' either (incorrect package name?):\n' + (response && response.statusCode || '') + (response && response.body || '') + (error || ''));
      });
    }
  });

  version = fut.wait();
}

// Now that we finally have an accurate version number...
Package.describe({
  name: packageName,
  summary: 'Sortable: reactive minimalist reorderable drag-and-drop lists on modern browsers and touch devices',
  version: version,
  git: 'https://github.com/RubaXa/Sortable.git',
  documentation: 'README.md'
});

Package.onUse(function (api) {
  api.versionsFrom(['METEOR@0.9.0', 'METEOR@1.0']);
  api.use('templating', 'client');
  api.use('dburles:mongo-collection-instances@0.3.4');  // to watch collections getting created
  api.export('Sortable');  // exported on the server too, as a global to hold the array of sortable collections (for security)
  api.addFiles([
    '../Sortable.js',
    'template.html',  // the HTML comes first, so reactivize.js can refer to the template in it
    'reactivize.js'
  ], 'client');
  api.addFiles('methods-client.js', 'client');
  api.addFiles('methods-server.js', 'server');
});

Package.onTest(function (api) {
  api.use(packageName, 'client');
  api.use('tinytest', 'client');

  api.addFiles('test.js', 'client');
});
