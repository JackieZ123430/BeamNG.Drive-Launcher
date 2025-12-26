# BeamNG.Drive Launcher  
# BeamNG.Drive 启动器

一个基于 **Windows / .NET / WPF** 的第三方 **BeamNG.drive 启动器。  
A third-party **BeamNG.drive launcher** based on **Windows / .NET / WPF**.

用于 **可视化管理启动参数、关卡、车辆、UserPath 与 Mods**，  
Designed to **visually manage launch arguments, levels, vehicles, user paths, and mods**,

避免手动敲命令行或频繁修改快捷方式。  
eliminating the need to manually type command lines or repeatedly edit shortcuts.

> 本项目为 **个人开发 + 学习性质**，面向 BeamNG.drive 高级用户。  
> This project is **personal and educational**, targeting advanced BeamNG.drive users.



## ✨ 功能特性  
## ✨ Features

- 🔍 **自动识别 BeamNG.drive 安装路径**  
  🔍 **Automatically detect BeamNG.drive installation path**

  - 支持 Steam 多 Library（读取 `libraryfolders.vdf`）  
    Supports multiple Steam libraries (parses `libraryfolders.vdf`)

- 🧩 **自动读取游戏版本号**  
  🧩 **Automatically read game version**

  - 从 `BeamNG.drive.exe` 文件属性中解析 Product Version  
    Extracts Product Version from `BeamNG.drive.exe` file metadata

- 🗺️ **关卡（Levels）下拉选择**  
  🗺️ **Level selection dropdown**

  - 自动扫描：  
    Automatically scans:
    ```
    BeamNG.drive\content\levels
    ```

- 🚗 **车辆（Vehicles）下拉选择**  
  🚗 **Vehicle selection dropdown**

  - 自动扫描：  
    Automatically scans:
    ```
    BeamNG.drive\content\vehicles
    ```

- 📦 **Mods 列表显示**  
  📦 **Mods list display**

  - 读取：  
    Reads from:
    ```
    %LocalAppData%\BeamNG\BeamNG.drive\current\mods
    ```

- ⚙️ **完整启动参数可视化配置**  
  ⚙️ **Full visual configuration of launch arguments**

  - gfx（dx11 / vulkan / null）  
  - console / headless / luadebug / cefdev  
  - level / vehicle  
  - lua / exec  
  - tcom / tport / tcom-debug  
  - extra args  

- 🧠 **启动参数实时预览**  
  🧠 **Real-time preview of launch arguments**

  - 所见即所得，避免参数拼错  
    What you see is what you get, avoiding argument mistakes

- 🟢 **一键启动 BeamNG.drive**  
  🟢 **One-click launch for BeamNG.drive**

---

## 🖥️ 运行环境  
## 🖥️ System Requirements

- **Windows 7 / 8 / 10 / 11（64-bit）**  
  **Windows 7 / 8 / 10 / 11 (64-bit)**

  > Windows 7 需已安装 .NET Framework 4.8  
  > Windows 7 requires .NET Framework 4.8 installed

- **.NET（桌面版，WPF）**  
  **.NET Desktop Runtime (WPF)**

- **Steam 版 BeamNG.drive**  
  **Steam version of BeamNG.drive**

---

## 🧩 项目结构简述  
## 🧩 Project Structure Overview

