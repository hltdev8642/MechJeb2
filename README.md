# MechJeb2

Anatid Robotics and Multiversal Mechatronics proudly presents the first flight assistant autopilot: MechJeb

MechJeb2 is a mod for the game Kerbal Space Program. To learn how to use it, [visit the wiki][wiki]. For more
info, [visit this KSP forum post][post].

[wiki]: https://github.com/MuMech/MechJeb2/wiki

[post]: http://forum.kerbalspaceprogram.com/index.php?/topic/154834-122-anatid-robotics-mumech-mechjeb-autopilot-260-12-dec-2016/

## Table of Contents

- [MechJeb2](#mechjeb2)
    - [Table of Contents](#table-of-contents)
    - [Install](#install)
        - [Manual install](#manual-install)
            - [Download](#download)
            - [Unpack](#unpack)
        - [Via CKAN](#via-ckan)
            - [Development version of Mechjeb](#development-version-of-mechjeb)
    - [Common Issues](#common-issues)
    - [New Features (2025–2026)](#new-features-20252026)
    - [Development](#development)
        - [Maintainers](#maintainers)
        - [Code Standards](#code-standards)
        - [Third-party libraries](#third-party-libraries)
        - [Build](#build)
            - [Linux](#linux)
            - [Windows](#windows)
    - [License](#license)

## Install

### Manual install

#### Download

Download from Jenkins:
<https://ksp.sarbian.com/jenkins/job/MechJeb2-Release/>

#### Unpack

Unzip the zip in KSP GameData directory. You should have something that looks like that :

    Kerbal Space Program
    -- GameData
       -- MechJeb2
          -- Bundles
          -- Icons
          -- Localization
          -- Parts
          -- Plugins

### Via CKAN

CKAN has all the release of MechJeb, just install it as usual.

#### Development version of Mechjeb

If you want the unstable dev version of MechJeb then :

1. Open CKAN settings (Settings => CKAN Settings)
2. Press the New button
3. Select the MechJeb-dev line, click OK and exit the options.
4. Refresh
5. Select "Mechjeb2 - DEV RELEASE" in the list
6. Then "Go to Change" to install

## Common Issues

1. Why is the Mechjeb menu not showing?

   Make sure you have the part on your ship (AR202 case in the Control section).

2. (Windows) I cannot find Mechjeb anywhere, there aren't even parts in the R&D facility!

   Some Windows protection and anti-virus software can sometimes block KSP from loading MechJeb.
   You should install KSP outside the `C:\Program Files (x86)\`
   directory. [Steam has an option to change the install directory](https://support.steampowered.com/kb_article.php?ref=7710-tdlc-0426)
   of a game or you can just copy the directory somewhere else.

3. Why is some Mechjeb function not available?

   Science and career mode requires you to unlock some specific node in the Research and Development tree.
   You also may need to upgrade the tracking station to level 2 (game code restriction we can't do much about).

4. How do I report a bug?

   Check if your problem has already been reported: <https://github.com/MuMech/MechJeb2/issues>
   If you found a problem which is similar to yours, feel free to add more information to the existing issue.

   **If you cannot find the problem**, get
   a [log](https://forum.kerbalspaceprogram.com/index.php?/topic/83212-how-to-get-support-read-first/#Logs) and create a
   new issue with a descriptive title of the problem.

## New Features (2025–2026)

This fork adds the following features on top of the upstream MechJeb2 release:

### Autopilot Enhancements
- **Atmospheric Drag-Compensating Ascent** — Dynamic pressure (Q) feedback loop for the ascent autopilot, producing more efficient gravity turns.
- **Rendezvous Proximity Operations Mode** — Automatic transition from rendezvous guidance to docking autopilot when close to the target.
- **Auto-Warp to Entry Interface** — Configurable auto-warp to a target entry altitude before executing landing guidance.
- **Launch Window Planner** — Lambert-targeting-based launch window calculator integrated into the ascent planning UI.
- **Docking Port Auto-Selection** — Automatically finds and targets the nearest-aligned docking port during approach.
- **Hoverslam Terrain-Relative Navigation** — PQS raycast terrain altitude feed for the hoverslam autopilot, enabling precision landings on uneven terrain.

### Flight Computer
- **Multi-Node Maneuver Sequences** — Chain multiple maneuver nodes with configurable coast times for complex mission profiles.
- **Maneuver Node Drag-Handle Preview** — Ghost orbit preview while dragging maneuver node handles (conic patch rendering).
- **Aerobrake Calculator with Thermal Load** — Computes peak heating, thermal load, and safe entry corridors for aerobraking passes.
- **RCS Translation Hold (No Target)** — Surface-relative velocity hold via RCS, useful for rover waypoint navigation and precision translation.
- **Launch Window Synchronization** — Co-planar phasing burn calculator for rendezvous with target vessels in different orbits.

### New Plugin Modules
- **CommNet / DSN Link Planner** — Signal strength prediction, antenna power budgeting, relay orbit analysis, and link budget visualization.
- **Thermal Management Window** — Real-time part temperature monitoring, heat flux display, radiator status, and overheat warnings.
- **Satellite Coverage Mapper** — 2D ground-track coverage swath overlay, revisit-time analysis, and coverage-gap identification.
- **Orbital Construction / Station Module** — Vessel tree hierarchy view, module-by-module docking guidance, center-of-mass calculator.
- **Scripted Autopilot Sequencer** — Event-sequence mission planner for automated multi-step flight profiles.

### UI/UX
- **3D Trajectory Overlay** — Colored trajectory rendering in the Map View, showing predicted orbital paths, aerobrake tracks, and transfer trajectories.
- **Window Layout Presets** — Named snapshot layouts (Ascent, Landing, Docking, Orbital, Custom) that save/restore window positions and visibility.
- **RPM Integration** — RasterPropMonitor MFD integration exposing MJ2 functions in IVA cockpit displays (requires separate RasterPropMonitor install).

## Development

### Maintainers

- [@sarbian](https://github.com/sarbian)
- [@lamont-granquist](https://github.com/lamont-granquist)

### Code Standards

1. [No var](https://docs.microsoft.com/en-us/visualstudio/ide/reference/convert-var-to-explicit-type): use explicit
   types.
2. Prefer single lines, when possible: especially if-else blocks!
3. [No null-conditional operators](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-):
   Unity 4.x has
   a [custom == for checking object nulls](https://blog.unity.com/technology/custom-operator-should-we-keep-it).
4. Assembly version needs to remain at 2.5.1.0; file version can be incremented.

### Third-party libraries

- [ALGLIB](https://www.alglib.net/)
- [NSubstitute](https://nsubstitute.github.io/)
- [xunit](https://xunit.net/)
- [RasterPropMonitor](https://github.com/FirstPersonKSP/RasterPropMonitor) (required by `MechJebRPM` for IVA/RPM integration)

### Build

#### Linux

The project uses Mono and Make to build the addon, make sure you have both installed. You need Nuget to download the external dependencies.

You can also use the [flake.nix](./flake.nix) with direnv or by runnning `nix develop` to set up the development environment.

1. (optional) Set your KSP directory

```sh
export KSPDIR="${XDG_DATA_HOME}/Steam/SteamApps/common/Kerbal Space Program"
```

2. Fetch external packages

```
nuget restore
```

3. Build the mod

```sh
make build
```

4. (optional) Install the mod into your KSP directory

```sh
make install
```

#### Windows

##### Quick build (recommended)

1. Set the `KSPDIR` environment variable to your KSP install path:

   ```powershell
   $env:KSPDIR = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
   ```

2. Build:

   ```powershell
   dotnet build MechJeb2.sln
   ```

3. (optional) Assemble the mod into `_export\GameData\`:

   ```powershell
   .\_export\Export-MechJeb2.ps1 -Configuration Debug
   ```

   Copy `_export\GameData` over your KSP install's `GameData` folder to deploy.

##### Full setup (legacy)

1. Install the version of Unity that KSP uses (Currently 2019.2.2f1).

2. Configure your system environment variables:

   - `KSPDIR` — path to your KSP install (usually `C:\Program Files (x86)\Steam\SteamApps\Common\Kerbal Space Program`)
   - `MONO` — path to Unity's mono.exe (usually `C:\Program Files\Unity\Hub\Editor\2019.2.2f1\Editor\Data\MonoBleedingEdge\bin\mono.exe`)
   - `PDB2MDB` — path to pdb2mdb.exe (usually `C:\Program Files\Unity\Hub\Editor\2019.2.2f1\Editor\Data\MonoBleedingEdge\lib\mono\4.5\pdb2mdb.exe`)

3. Load `MechJeb2.sln` and add the KSP managed assembly reference path to each project (MechJeb2, MechJebLib,
   MechJebLibBindings, MechJebLibTest): the folder is usually
   `C:\Program Files (x86)\Steam\SteamApps\Common\Kerbal Space Program\KSP_x64_Data\Managed`.

4. Run `nuget restore` to fetch external dependencies (JetBrains.Annotations, etc.).

##### Notes

- The `RasterPropMonitor` assembly is required by the `MechJebRPM` project for IVA integration.
  If you don't have RPM installed, build only `MechJeb2.csproj`:
  ```powershell
  dotnet build MechJeb2/MechJeb2.csproj
  ```
- The export script `Export-MechJeb2.ps1` builds the solution and assembles all DLLs,
  assets, configs, and localization into a ready-to-deploy `_export\GameData\` folder.
  Run it without `-SkipBuild` to rebuild automatically.

## License

Licensed under the [GNU General Public License, Version 3](LICENSE.md).

Portions (in the "MechJebLib" directory) are placed in the public domain and are documented in
the affected source code headers.
