# FileCopier - Windows File Copy Tool

A simple Windows application that lets you browse for a file and copy it to a predefined UNC network share.

## Features

- File browser dialog to select any file
- Configurable UNC remote destination path
- Optional date-based subfolder creation (`yyyy-MM-dd`)
- Progress bar showing copy progress
- Settings persist between sessions via `appsettings.json`

## Requirements

- Windows 10/11
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later

## Build & Run

```bash
cd FileCopier
dotnet build
dotnet run
```

## Publish as Standalone .exe

To create a single self-contained executable (no .NET install required on target machine):

```bash
cd FileCopier
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../publish
```

The output will be in the `publish/` folder.

## Configuration

Edit `appsettings.json` (next to the .exe) to set the default remote path:

```json
{
  "RemoteFolder": "\\\\server\\share\\folder",
  "CreateSubfolderByDate": true
}
```

You can also change the remote path directly in the application UI.
