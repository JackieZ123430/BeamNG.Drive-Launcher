# BeamNG.Drive Launcher

一个基于 **Windows / .NET / WPF** 的第三方 **BeamNG.drive 启动器**，  
用于 **可视化管理启动参数、关卡、车辆、UserPath 与 Mods**，  
避免手动敲命令行或频繁修改快捷方式。

> 本项目为 **个人开发 + 学习性质**，面向 BeamNG.drive 高级用户。

---

## ✨ 功能特性

- 🔍 **自动识别 BeamNG.drive 安装路径**
  - 支持 Steam 多 Library（读取 `libraryfolders.vdf`）
- 🧩 **自动读取游戏版本号**
  - 从 `BeamNG.drive.exe` 文件属性中解析 Product Version
- 🗺️ **关卡（Levels）下拉选择**
  - 自动扫描：
    ```
    BeamNG.drive\content\levels
    ```
- 🚗 **车辆（Vehicles）下拉选择**
  - 自动扫描：
    ```
    BeamNG.drive\content\vehicles
    ```
- 📦 **Mods 列表显示**
  - 读取：
    ```
    %LocalAppData%\BeamNG\BeamNG.drive\current\mods
    ```
- ⚙️ **完整启动参数可视化配置**
  - gfx（dx11 / vulkan / null）
  - console / headless / luadebug / cefdev
  - level / vehicle
  - lua / exec
  - tcom / tport / tcom-debug
  - extra args
- 🧠 **启动参数实时预览**
  - 所见即所得，避免参数拼错
- 🟢 **一键启动 BeamNG.drive**

---

## 🖥️ 运行环境

- Windows 10 / 11
- .NET（桌面版，WPF）
- Steam 版 BeamNG.drive

---

## 🧩 项目结构简述

