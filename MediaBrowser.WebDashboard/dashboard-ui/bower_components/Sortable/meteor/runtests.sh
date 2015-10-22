#!/bin/sh
# Test Meteor package before publishing to Atmospherejs.com

# Make sure Meteor is installed, per https://www.meteor.com/install.
# The curl'ed script is totally safe; takes 2 minutes to read its source and check.
type meteor >/dev/null 2>&1 || { curl https://install.meteor.com/ | sh; }

# sanity check: make sure we're in the directory of the script
cd "$( dirname "$0" )"


# delete the temporary files even if Ctrl+C is pressed
int_trap() {
  printf "\nTests interrupted. Cleaning up...\n\n"
}
trap int_trap INT


EXIT_CODE=0

PACKAGE_NAME=$(grep -i name package.js | head -1 | cut -d "'" -f 2)

echo "### Testing $PACKAGE_NAME..."

# provide an invalid MONGO_URL so Meteor doesn't bog us down with an empty Mongo database
if [ $# -gt 0 ]; then
  # interpret any parameter to mean we want an interactive test
  MONGO_URL=mongodb:// meteor test-packages ./
else
  # automated/CI test with phantomjs
  ./node_modules/.bin/spacejam --mongo-url mongodb:// test-packages ./
  EXIT_CODE=$(( $EXIT_CODE + $? ))
fi

exit $EXIT_CODE
