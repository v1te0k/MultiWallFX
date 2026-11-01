# MultiWall FX 🖥️⚡

> A modern, hardware-accelerated Multi-Monitor Wallpaper & Lock Screen Manager for Windows with DirectX smooth transitions, shuffle effects, and intelligent resource optimization.

Made with ❤️ by [**v1te0k**](https://github.com/v1te0k).

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

