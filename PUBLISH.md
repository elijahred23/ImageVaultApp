# Publishing ImageVault

Run these commands from the repository root.

## Framework-dependent publish

```bash
dotnet publish ImageVaultApp.csproj -c Release -o ./publish/mvc
dotnet publish src/ImageVault.ImportService/ImageVault.ImportService.csproj -c Release -o ./publish/worker
```

This creates:

- MVC app output: `./publish/mvc`
- Worker/import service output: `./publish/worker`

## Self-contained publish

Use this if the target machine should not need the .NET runtime installed. Replace `osx-arm64` with the runtime identifier for your deployment target.

```bash
dotnet publish ImageVaultApp.csproj -c Release -r osx-arm64 --self-contained true -o ./publish/mvc
dotnet publish src/ImageVault.ImportService/ImageVault.ImportService.csproj -c Release -r osx-arm64 --self-contained true -o ./publish/worker
```
