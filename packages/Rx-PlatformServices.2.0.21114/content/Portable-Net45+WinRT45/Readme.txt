Portable libraries built using Rx do not include the System.Reactive.PlatformServices assembly
containing "platform enlightenments" that depend on platform-specific functionality. (Doing so
would prevent the library to be portable due to the dependencies on a specific platform.)

When including the resulting portable library in another project, please include the platform's
System.Reactive.PlatformServices assembly in order to get the best performance. To include this
assembly, use any of the following options:

1. Select the System.Reactive.PlatformServices assembly from the Visual Studio "Add Reference"
   dialog. This option works for Windows Store apps, .NET 4.5, and Windows Phone 8 projects.

2. For Windows Store apps and Windows Phone 8 projects, use the Reactive Extensions Extension SDK
   which can be found in the "Add Reference" dialog.

3. Use NuGet to include the Rx-Main package (or any package that depends on Rx-Main, such as
   Rx-Xaml) which will automatically include the Rx-PlatformServices enlightenment package.
