# Reticle

Hands-free clicking for Windows 10 and 11. Rest the pointer and Reticle clicks for you. Use it with a head mouse, eye
gaze, or any pointing device where pressing a button is hard.

Built on .NET 10 WPF. English and Portuguese (Portugal); Midnight, Daylight and Ocean themes.

## Using it

A floating toolbar docks to an edge of the screen:

| Button | What it does |
|---|---|
| Rest | Pause clicking. Toolbar buttons still respond to dwelling. |
| Left click | Default. Every dwell is a left click. |
| Right click | The next dwell right-clicks, then left click comes back. Choose it twice to lock it (a padlock appears). |
| Double click | Same, with a double click. |
| Drag | The first dwell presses the button and the second releases it. |
| Settings | Target size, toolbar size, dwell time, sound, toolbar position, theme, language, start-up. |

To dwell, hold the pointer still inside the target square (30 to 100 px at 100 % scaling) for the dwell time (0.3 to 5 s). A ring
fills around the pointer and the click lands exactly where the pointer is. Move the pointer out of the square before
the same spot clicks again.

Reticle lives in the notification area, not on the taskbar. Left-click its icon to open Settings; right-click it for
*Settings* and *Close application*.

To start automatically, open Settings → *Start Reticle automatically*. This registers the app with Windows Ease of
Access for the desktop, the sign-in screen, or both. Windows asks for administrator approval the first time.

Settings are saved to `%ProgramData%\Reticle\settings.xml`. The log is at `%LocalAppData%\Reticle\Reticle.log`.

## Building

Requires the .NET 10 SDK on Windows.

```powershell
dotnet build Reticle.slnx
dotnet test Reticle.slnx
./tools/publish.ps1          # dist/Reticle.exe (self-contained, single file) + dist/Reticle.Launcher.exe
./tools/make-icon.ps1        # regenerates src/Reticle/Assets/Reticle.ico
```

Debug builds run from anywhere. Release builds request `uiAccess` so clicks reach elevated (administrator) windows.
Windows starts them only once they are signed by a trusted publisher and installed under Program Files, which is what
the installer is for.

## Download

Get `Reticle-<version>-setup.exe` from the [latest release](../../releases/latest). Full documentation is in the
[wiki](../../wiki).

## Releasing

Push a `v*.*.*` tag (or run **Actions → Release** with a version). The workflow tests, signs, builds the installer
and publishes a GitHub release. See the [Release Process](../../wiki/Release-Process) wiki page.

## Installer

```powershell
winget install JRSoftware.InnoSetup   # once
./tools/build-installer.ps1           # dist/Reticle-<version>-setup.exe
```

The build script signs `Reticle.exe`, `Reticle.Launcher.exe` and the setup exe with a self-signed code-signing
certificate (`CN=Reticle`, created in your user certificate store on first run; the private key is non-exportable and
never written to disk), then compiles `installer/Reticle.iss` with Inno Setup.

`Reticle-<version>-setup.exe` (English or Portuguese) installs to `C:\Program Files\Reticle`, adds a Start menu entry,
and trusts the signing certificate on that machine. Uninstalling removes the files, the Ease of Access registration and
the certificate trust.

Only install it on machines you control. Trusting a self-signed certificate means anything signed with its key is
trusted on that machine. Without the installer, Windows silently drops clicks aimed at elevated windows.

See [docs/design.md](docs/design.md) for the behaviour specification.

## License

[MIT](LICENSE)
