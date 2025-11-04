# Jellyfin Build Instructions

To build and run the Jellyfin server, use the following command:

```bash
nix-shell -p dotnetCorePackages.dotnet_9.sdk --run "cd Jellyfin.Server && dotnet run"
```

## Notes:
- This command assumes you have Nix package manager installed
- The `dotnet_9.sdk` package provides the required .NET 9 SDK
- The `cd Jellyfin.Server` navigates to the server project directory
- `dotnet run` builds and starts the Jellyfin server

## Troubleshooting:
- If you encounter version conflicts, ensure your Nix environment is up-to-date
- Verify the `Jellyfin.Server` directory exists in your project structure
- Check that the `dotnetCorePackages.dotnet_9.sdk` package is available in your Nix channel
