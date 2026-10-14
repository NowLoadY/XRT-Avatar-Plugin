# XRT-Avatar-Plugin

通过 VRChat 角色的圆形表情菜单，控制 [XRTranslate](https://github.com/NowLoadY/XRTranslate)
的翻译启停。插件使用官方 Avatar SDK 与 OSC / OSCQuery 接口，音频识别、翻译和输出由 XRTranslate 提供。

## 功能

- 在 `Expressions → XRT → Translation` 中开启或关闭翻译。
- 沿用 XRTranslate 的语言、音源、模型、字幕和语音输出设置。
- 独立管理角色翻译会话，关闭时不影响其他翻译任务。

## 开始使用

按[安装指南](docs/installation.md)将菜单添加到自己的角色，并在 XRTranslate 中启用“角色翻译控制”。
在 VRChat 中开启 OSC 后，通过角色菜单操作翻译。

项目处于开发预览阶段，使用前请查看[兼容性说明](docs/compatibility.md)。
架构、控制协议与宿主集成见[文档索引](docs/README.md)。

## 许可证

采用 [AGPL-3.0-only](LICENSE) 许可证。本项目为独立社区项目。
