# 🎵 Music Meta Writer

**A modern, cross-platform audio batch exporter/converter & metadata editor**  
Built with **Avalonia UI** + **FFmpeg** – Clean. Fast. Beautiful.

![App Screenshot](https://i.imgur.com/StHNiBw.png)

## ✨ Features

- ❎ **Cross-Platform supoort** Win / macOS / Linux
- 🎧 **Batch export** to MP3 · WAV · FLAC · AIFF · MP4 (Cover + Custom Media)
- 🔊 **Loudness normalization** (EBU R128 loudnorm + ReplayGain)
- ⚙️ **Bit-depth conversion** (16/24-bit with smart dithering)  
- 🖼️ **Cover art replacement** (single or bulk)  
- ✏️ **Metadata editor** with pattern-based filename generation  
- 📂 **Folder & multi-file loading** with progress feedback  
- 🌙 **Dark/Light/System theme** support  
- 📝 **Detailed logging**

## ⚠️ Warning

**Please be aware that I am a solo developer and this is my first Avalonia project, so bugs/errors may occur.**
> Report any crashes, weird behavior or missing features [here](https://github.com/spartokos99/MusicMetaWriter/issues).
> Your feedback will shape the future of this tool!

Thanks for testing!

## 📸 Screenshots

<p align="center">
  <img src="https://i.imgur.com/StHNiBw.png" width="45%" alt="Main Window"/>
  <img src="https://i.imgur.com/lyDdQBj.png" width="45%" alt="Light Theme"/>
</p>

## 🚀 Quick Start

1. Download latest release from [Releases](https://github.com/spartokos99/MusicMetaWriter/releases)
2. Extract & run `MusicMetaWriter.exe` (Windows) / `.app` (macOS) / executable (Linux)
3. Load files or a folder via the buttons on the top left → start tweaking & exporting!

## 🛠️ Requirements

- **Windows** 10/11 (tested on Win11 x64)
- **macOS** 12+ (tested on Tahoe 26.2 x64)
- **Linux** (not tested - no release package yet)
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
