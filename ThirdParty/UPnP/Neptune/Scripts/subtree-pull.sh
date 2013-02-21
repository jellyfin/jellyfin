#!/bin/bash

#git remote add neptune gitplut@plutinosoft.com:Neptune
#git subtree add -P ThirdParty/Neptune -m "added Neptune as subproject" neptune/master
git fetch neptune
git subtree merge -P ThirdParty/Neptune -m "merged Neptune changes" neptune/master

