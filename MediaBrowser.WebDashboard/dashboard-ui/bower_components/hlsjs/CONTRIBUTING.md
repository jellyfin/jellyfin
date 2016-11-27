# Contributing to hls.js

:+1::tada: First off, thanks for taking the time to contribute! :tada::+1:

#### **Did you find a bug?**

* **Ensure the bug was not already reported** by searching on GitHub under [Issues](https://github.com/dailymotion/hls.js/issues).

* If you're unable to find an open issue addressing the problem, [open a new one](https://github.com/dailymotion/hls.js/issues/new). Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample** or an **executable test case** demonstrating the expected behavior that is not occurring.

#### **Did you write a patch that fixes a bug?**

 - First, checkout the repository and install required dependencies

```sh
git clone https://github.com/dailymotion/hls.js.git
# setup dev environement
cd hls.js
npm install
# build dist/hls.js, watch file change for rebuild and launch demo page
npm run dev
# lint
npm run lint
# test
npm run test
```

 - Use [EditorConfig](http://editorconfig.org/) or at least stay consistent to the file formats defined in the `.editorconfig` file.
 - Develop in a topic branch, not master
 - Don't commit the updated `dist/hls.js` file in your PR. We'll take care of generating an updated build right before releasing a new tagged version.

 Thanks! :heart: :heart: :heart:
