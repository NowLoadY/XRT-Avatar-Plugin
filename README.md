# XRT-Avatar-Plugin

连接虚拟社交平台与 [XRTranslate](https://github.com/NowLoadY/XRTranslate) 的开源游戏插件。
通过角色菜单、按钮或附件控制翻译，并将字幕接入平台支持的显示组件。

## 功能

- 在游戏内开启、关闭翻译，查看实际运行状态。
- 复用 XRTranslate 的语言、音源、识别、翻译和输出设置。
- 按连接独立管理翻译会话，断开后清理该会话及其显示。

## 平台连接器

| 平台 | 游戏组件 | 使用说明 |
|---|---|---|
| VRChat | Expressions 菜单与本地角色参数 | [安装](docs/adapters/vrchat/installation.md) |
| ChilloutVR | CCK 角色菜单与本地参数 | [安装](docs/adapters/chilloutvr/installation.md) |
| Overte | 角色脚本、Tablet 控件与本地字幕 | [安装](docs/adapters/overte/installation.md) |
| Resonite | ResoniteLink 角色控件桥接与文本组件 | [安装](docs/adapters/resonite/installation.md) |
| Second Life | LSL 附件控件、浮空字幕与连接桥接 | [安装](docs/adapters/secondlife/installation.md) |

当前为开发预览，游戏内验收情况见各平台指南。ResoniteLink 用于具有相应权限的本人主持会话；Second Life 需要 HTTPS 中继。

游戏组件与连接协议由本项目维护；音频和翻译运行时由 XRTranslate 提供。
OSC Chatbox 由 XRTranslate 的 OSC 插件提供。架构和开发规范见[文档索引](docs/README.md)。

## 许可证

[AGPL-3.0-only](LICENSE)。本项目为独立社区项目。
