# sanity check: make sure we're in the root directory of the example
cd "$( dirname "$0" )"

# let Meteor find the local package
PACKAGE_DIRS=../../ meteor run "$@"
