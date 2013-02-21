#!/bin/bash

#git remote add neptune gitplut@plutinosoft.com:Neptune
#git subtree add -P ThirdParty/Neptune -m "added Neptune as subproject" neptune/master
git subtree split -P ThirdParty/Neptune -b backport_neptune
git push neptune backport_neptune:master
