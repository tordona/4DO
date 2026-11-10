# 4DO (Modernized Fork)

4DO is an open-source, low-level emulator for the 3DO Interactive Multiplayer system.

This repository is a modernized fork of the original 4DO project:
- **Repository**: [https://github.com/tordona/4DO](https://github.com/tordona/4DO)
- **Original SourceForge Project**: [https://sourceforge.net/projects/fourdo/](https://sourceforge.net/projects/fourdo/)
- **Original Website & Project Status**: [https://fourdo.com/project-status/](https://fourdo.com/project-status/)

---

## Purpose

This fork updates the 4DO source code to work with updated Visual Studio and platform toolset:

- Upgraded the solution to Visual Studio 2026 and the C++ platform toolset from v100 to v145.
- Dropped 32-bit (x86) builds.
- Added native x64 builds.
- Added native ARM64 builds.
- Updated managed projects to .NET Framework 4.8.1 for ARM64 compatibility.

---

## Requirements

### Runtime Requirements

To run 4DO, you will need:
- **Microsoft Visual C++ Redistributable (v14 / modern toolset)**:
  - **x64**: [https://aka.ms/vc14/vc_redist.x64.exe](https://aka.ms/vc14/vc_redist.x64.exe)
  - **ARM64**: [https://aka.ms/vc14/vc_redist.arm64.exe](https://aka.ms/vc14/vc_redist.arm64.exe)
- **Microsoft .NET Framework 4.8.1** (pre-installed on Windows 11 22H2 and later; available for Windows 10).
- A valid **3DO BIOS ROM** image.

### Building from Source

To compile 4DO yourself:
- **Visual Studio 2026** with the following workloads:
  - .NET desktop development (.NET Framework 4.8.1 targeting pack)
  - Desktop development with C++ (MSVC v145 x64/x86 build tools)
  - MSVC v145 ARM64 build tools (if building for ARM64)
- Open `FourDO.sln` and select your desired configuration (`Release|x64` or `Release|ARM64`).

---

## Credits & Acknowledgments

4DO exists thanks to the dedicated work of the original emulator developers and community contributors:

- **4DO Authors**:
  - **JohnnyDude** — Founder and lead developer of 4DO.
  - **Viktor** — Founder of 3DOPlay, who merged 3DOPlay's core emulation improvements into 4DO.

- **FreeDO Authors**:
  - The core emulation engine in 4DO is based on the open-source FreeDO project released on November 10, 2010.
  - **Felix Lazarev**
  - **Alexander Troosh**
  - **Maxim Grishin**
  - **Allen Wright**
  - **John Sammons**

- **Additional Thanks**:
  - **BryWI** — Testing, feedback, and general support for 4DO.
  - **hankystyles** & **Blake** — Assistance with early emulation work.

---

## License

4DO is released under the **GNU General Public License v2** (GPL-2.0). See [LICENSE](https://www.gnu.org/licenses/old-licenses/gpl-2.0.html) for the full text.
