# 🎵 Music Meta Writer

**A modern, cross-platform audio batch exporter/converter & metadata editor**  
Built with **Avalonia UI** + **FFmpeg** – Clean. Fast. Beautiful.

![App Screenshot](https://i.imgur.com/StHNiBw.png)

## ✨ Features

- 🎧 **Batch export** to MP3 · WAV · FLAC · AIFF  
- 🔊 **Loudness normalization** (EBU R128 loudnorm + ReplayGain)  
- ⚙️ **Bit-depth conversion** (16/24-bit with smart dithering)  
- 🖼️ **Cover art replacement** (single or bulk)  
- ✏️ **Metadata editor** with pattern-based filename generation  
- 📂 **Folder & multi-file loading** with progress feedback  
- 🌙 **Dark/Light/System theme** support  
- 📝 **Detailed logging** + export history  
- 🚀 **FFmpeg auto-download** on first launch (Windows/macOS/Linux)

## 📸 Screenshots

<p align="center">
  <img src="https://i.imgur.com/StHNiBw.png" width="45%" alt="Main Window"/>
  <img src="https://i.imgur.com/lyDdQBj.png" width="45%" alt="Light Theme"/>
</p>

## 🚀 Quick Start

1. Download latest release from [Releases](https://github.com/spartokos99/MusicMetaWriter/releases)
2. Extract & run `MusicMetaWriter.exe` (Windows) / `.app` (macOS) / executable (Linux)
3. First launch will automatically download **FFmpeg** (~35–80 MB)
4. Load files or a folder via the buttons on the top left → start tweaking & exporting!

## 🛠️ Requirements

- **Windows** 10/11  
- **macOS** 11+  
- **Linux** (tested on Ubuntu 22.04+ / Fedora)  
- **.NET 8.0** Desktop Runtime (usually auto-installed)

## 🏗️ Build from Source

```bash
# 1. Clone the repo
git clone https://github.com/spartokos99/MusicMetaWriter.git
cd MusicMetaWriter_CP

# 2. Restore & build
dotnet restore
dotnet build --configuration Release

# 3. Run
dotnet run --project MusicMetaWriter_CP --configuration Release