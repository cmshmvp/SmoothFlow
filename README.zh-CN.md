<img src="assets/SmoothFlow-icon.png" alt="SmoothFlow 鼠标滚轮图标" width="72" height="72">

# SmoothFlow

[English](README.md) · 简体中文

SmoothFlow 是一个为 Windows 有刻度鼠标滚轮设计的托盘工具。轻拨一格，页面会逐渐起步、减速并停下；连续拨动时，可以用加速上限控制翻阅长页面的速度。

**[下载 SmoothFlow 0.5.0 Windows x64 便携版](https://github.com/cmshmvp/SmoothFlow/releases/download/v0.5.0/SmoothFlow-0.5.0-win-x64.zip)** · [所有版本](https://github.com/cmshmvp/SmoothFlow/releases)

## 界面语言

在设置窗口左侧的 **界面语言** 下拉框中可选择「跟随系统」（默认）或直接选择语言。选择立即生效，下次打开也会记住。设置窗口、托盘菜单、帮助和提示支持英语、简体中文、繁体中文、日语、韩语、西班牙语、法语、德语、葡萄牙语、意大利语和俄语。其他显示语言使用英语。更改 Windows 显示语言后，从托盘退出并重新打开 SmoothFlow 即可。

## 开始使用

1. 把 ZIP 完整解压到一个可写、准备长期保留的文件夹，运行 `SmoothFlow.exe`。
2. 确认窗口顶部的 **已启用** 已勾选，把指针移到要阅读的页面上，轻拨滚轮。首次运行默认开启反向滚动，习惯传统滚轮方向时可取消 **反转滚轮方向**。
3. 点窗口右上角的叉会收起到系统托盘。双击托盘图标可重新打开设置，按 **Ctrl+Alt+S** 可随时暂停或恢复。

首次运行默认开启 **登录时启动**。取消勾选即可关闭登录启动。设置保存在程序旁的 `settings.xml`。卸载时先取消登录启动，从托盘菜单选择「退出」，再删除解压目录。

## 调整手感

| 设置 | 默认值 | 作用 |
| --- | --- | --- |
| 滚动距离 | 1.00 倍 | 每拨一格的总滚动量 |
| 动画时长 | 380 ms | 分配这段位移的时间；数值越大，滑行越舒展 |
| 最大加速倍率 | 1.3 倍 | 连续快速拨轮时的加速上限 |
| 动画缓动 | 开 | 让滚动逐渐起步、减速收尾 |
| 托盘菜单「细步进」 | 开 | 细分输出，减少慢滚结束时隔一会儿又挪一下的感觉 |

应用配置旁的 **添加** 按钮 可以按进程名添加应用配置。例如，只想让微信聊天记录滑得久一点，可以给 `Weixin.exe` 单独调高动画时长。应用如何处理小幅滚轮输入，也会影响最终的画面效果。

## 兼容性

适用于 Windows 10/11 x64，使用系统的 .NET Framework 4.x 和 Windows Forms。高精度滚轮的小增量和带 Ctrl、Shift、Alt 或 Win 的组合输入会交给原应用处理。管理员权限窗口会使用原始滚动。检测到 SmoothScroll 同时运行时，SmoothFlow 会暂停，以免两个程序重复处理滚轮。

## 从源码构建

在 Windows x64 上，打开项目目录并执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

脚本调用系统的 .NET Framework C# 编译器，生成 `SmoothFlow.exe`，并自动运行回归测试。测试包含单格滚动、连续拨动、改变方向、切换目标窗口、双轴滚动，以及 2,080 组经典模式逐帧对照。结果写入 `self-test-results.txt`。

生成可分发的 ZIP 与 SHA-256 校验文件：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

输出位于 `dist/`。许可条款见 [MIT License](LICENSE)。
