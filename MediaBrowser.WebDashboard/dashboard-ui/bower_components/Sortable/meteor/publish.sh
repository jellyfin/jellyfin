#!/bin/bash
# Publish package to Meteor's repository, Atmospherejs.com

# Make sure Meteor is installed, per https://www.meteor.com/install.
# The curl'ed script is totally safe; takes 2 minutes to read its source and check.
type meteor >/dev/null 2>&1 || { curl https://install.meteor.com/ | sh; }

# sanity check: make sure we're in the directory of the script
cd "$( dirname "$0" )"

# publish package, creating it if it's the first time we're publishing
PACKAGE_NAME=$(grep -i name package.js | head -1 | cut -d "'" -f 2)

echo "Publishing $PACKAGE_NAME..."

# Attempt to re-publish the package - the most common operation once the initial release has
# been made. If the package name was changed (rare), you'll have to pass the --create flag.
meteor publish "$@"; EXIT_CODE=$?
if (( $EXIT_CODE == 0 )); then
  echo "Thanks for releasing a new version. You can see it at"
  echo "https://atmospherejs.com/${PACKAGE_NAME/://}"
else
  echo "We have an error. Please post it at https://github.com/RubaXa/Sortable/issues"
fi

exit $EXIT_CODE
