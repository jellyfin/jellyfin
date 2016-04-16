#!/bin/sh
rm alameda.min.js.gz
ls -la alameda.js
uglifyjs -c -m -o alameda.min.js alameda.js
ls -la alameda.min.js
gzip alameda.min.js
ls -la alameda.min.js.gz

