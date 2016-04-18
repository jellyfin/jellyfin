#!/bin/bash

# sanity check to make sure phantomjs exists in the PATH
hash /usr/bin/env phantomjs &> /dev/null
if [ $? -eq 1 ]; then
    echo "ERROR: phantomjs is not installed"
    echo "Please visit http://www.phantomjs.org/"
    exit 1
fi

# sanity check number of args
if [ $# -lt 1 ]
then
    echo "Usage: `basename $0` path_to_runner.html"
    echo
    exit 1
fi

SCRIPTDIR=$(dirname `perl -e 'use Cwd "abs_path";print abs_path(shift)' $0`)
TESTFILE=""
while (( "$#" )); do
    if [ ${1:0:7} == "http://" -o ${1:0:8} == "https://" ]; then
        TESTFILE="$TESTFILE $1"
    else
        TESTFILE="$TESTFILE `perl -e 'use Cwd "abs_path";print abs_path(shift)' $1`"
    fi
    shift
done

# cleanup previous test runs
cd $SCRIPTDIR
rm -f *.xml

# make sure phantomjs submodule is initialized
cd ..
git submodule update --init

# fire up the phantomjs environment and run the test
cd $SCRIPTDIR
/usr/bin/env phantomjs $SCRIPTDIR/phantomjs-testrunner.js $TESTFILE
