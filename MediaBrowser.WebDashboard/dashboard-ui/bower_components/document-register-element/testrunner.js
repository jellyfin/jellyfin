console.log('Loading: test.html');
var page = require('webpage').create();
var url = 'index.html';
page.open(url, function (status) {
  if (status === 'success') {
    setTimeout(function () {
      var results = page.evaluate(function() {
        // remove the first node with the total from the following counts
        var passed = Math.max(0, document.querySelectorAll('.pass').length - 1);
        return {
          // retrieve the total executed tests number
          total: ''.concat(
            passed,
            ' blocks (',
            document.querySelector('#wru strong').textContent.replace(/\D/g, ''),
            ' single tests)'
          ),
          passed: passed,
          failed: Math.max(0, document.querySelectorAll('.fail').length - 1),
          failures: [].map.call(document.querySelectorAll('.fail'), function (node) {
            return node.textContent;
          }),
          errored: Math.max(0, document.querySelectorAll('.error').length - 1),
          errors: [].map.call(document.querySelectorAll('.error'), function (node) {
            return node.textContent;
          })
        };
      });
      console.log('- - - - - - - - - -');
      console.log('total:   ' + results.total);
      console.log('- - - - - - - - - -');
      console.log('passed:  ' + results.passed);
      if (results.failed) {
        console.log('failures: \n' + results.failures.join('\n'));
      } else {
        console.log('failed: ' + results.failed);
      }
      if (results.errored) {
        console.log('errors: \n' + results.errors.join('\n'));
      } else {
        console.log('errored: ' + results.errored);
      }
      console.log('- - - - - - - - - -');
      if (0 < results.failed + results.errored) {
        status = 'failed';
      }
      phantom.exit(0);
    }, 2000);
  } else {
    phantom.exit(1);
  }
});