# MultiWall FX 🖥️⚡

> A modern, hardware-accelerated Multi-Monitor Wallpaper & Lock Screen Manager for Windows with DirectX smooth transitions, shuffle effects, and intelligent resource optimization.

Made with ❤️ by [**v1te0k**](https://github.com/v1te0k).

[![Release](https://img.shields.io/github/v/release/v1te0k/MultiWallFX?color=00d4aa&label=Latest%20Release)](https://github.com/v1te0k/MultiWallFX/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue)](https://github.com/v1te0k/MultiWallFX)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 📥 Download & Quick Start

No installation required! Just download and run:

- 📦 **[Download MultiWallFX-v1.0.0-Windows.zip](https://github.com/v1te0k/MultiWallFX/releases/download/v1.0.0/MultiWallFX-v1.0.0-Windows.zip)** (Recommended - includes all configs)
- 🚀 **[Download Standalone MultiWallFX.exe](https://github.com/v1te0k/MultiWallFX/releases/download/v1.0.0/MultiWallFX.exe)** (Direct single executable)

You can also view all versions on the **[Releases Page](https://github.com/v1te0k/MultiWallFX/releases)**.

---

## ✨ Features

- **⚡ Hardware-Accelerated Transitions**: Smooth, 60 FPS transitions (Fade, Slide Left/Right/Up/Down, Zoom) rendered behind desktop icons using Windows `WorkerW`.
- **🎲 Random Time & Shuffle FX**: Unpredictable rotation intervals with customizable Min/Max ranges and intelligent photo shuffling without immediate repeats.
- **🖥️ Independent Multi-Monitor Support**: Configure wallpapers, modes (Random or Sequential), and transitions per monitor independently.
- **🔒 Lock Screen Customization**: Automatically rotate and synchronize Windows lock screen images.
- **🚀 0% GPU / Smart Optimization**: Automatically pauses transitions when a full-screen game or maximized application is running to preserve 100% of GPU and CPU performance.
- **📁 Folder Scanning**: Effortlessly add entire folders and subfolders with automatic image format detection (`.jpg`, `.jpeg`, `.png`, `.bmp`, `.webp`).
- **🪶 Lightweight & Native**: Pure WPF & Win32 C# application with zero bloat and minimal memory footprint.

---

## 🛠️ Requirements & Build

- **OS**: Windows 10 / Windows 11 (64-bit)
- **Framework**: .NET Framework 4.8 (pre-installed on modern Windows)

### Compile from source
Run the included PowerShell build script:
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
The compiled executable will be generated at `./MultiWallFX.exe`.

---

## 👨‍💻 Author

- **GitHub**: [@v1te0k](https://github.com/v1te0k)

---

## 📄 License
This project is open-source and available under the [MIT License](LICENSE).

