#/bin/sh
git checkout gh-pages
git rebase v0.5.x
git push origin gh-pages --force
git checkout v0.5.x
