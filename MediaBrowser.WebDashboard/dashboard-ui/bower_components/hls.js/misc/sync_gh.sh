#/bin/sh
git checkout gh-pages
git rebase master
git push origin gh-pages --force
git checkout master
