Instructions for deploying a new build:

1. Download source code

2. In resources folder, remove dashboard-ui, then re-add from the WebDashboard project, making sure to link the files, rather than copy. This will pick up any web client changes since the last deployment.

3. Repeat the process for swagger-ui, if ServiceStack has been updated since the last release. If in doubt, just do it.

4. Commit and push the changes to the Mac project

5. Build the installer

6. Proceed as normal and tag the builds in github