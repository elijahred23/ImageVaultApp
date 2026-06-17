# ImageVault.ImportService

Background importer for JSON files that contain image source arrays.

## Configuration

Set `ConnectionStrings:ImageVaultDb` to the same database used by the web app.
Set `ImageImporter:UserId` to the ImageVault user that should own imported images.

## Folders

By default the service creates and watches folders under `DropFolders`:

- `process`: imports images with `IsNSFW = false`
- `process-nsfw`: imports images with `IsNSFW = true`
- `processing`: file is moved here while records are being inserted
- `processed`: file is moved here after a successful import
- `error`: file is moved here if import fails
- `logs`: contains `image-importer.log`

JSON files must contain an array of image source strings:

```json
[
  "https://example.com/image-1.jpg",
  "data:image/png;base64,..."
]
```

The JSON file name without `.json` is used as both the imported image title and description.

## Run

```bash
dotnet run --project src/ImageVault.ImportService/ImageVault.ImportService.csproj
```
