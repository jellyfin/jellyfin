To update the nuget packages, follow the below procedure:

1. Build the solution in release mode. This will update the contents of the dll's folder
2. Open each nuspec file, and increment the versions of each, as well as each of the MB dependencies.

For example, in MediaBrowser.Common.Internal, increment <version>, as well as <dependency id="MediaBrowser.Common" version

This is quickest using notepad++. It can also be done with nuget package explorer.

By keeping all the version numbers the same, it makes this largely a mindless activity. If we allow each package to have their own version, this process will be slower and prone to human error.

3. Once this is done, publish the packages using nuget package explorer. File -> Publish.

4. Check the nuspec files in right away, otherwise there will be merge conflicts.