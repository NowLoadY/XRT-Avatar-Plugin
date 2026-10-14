# XRT-Avatar-Plugin

连接虚拟社交平台与 [XRTranslate](https://github.com/NowLoadY/XRTranslate) 的开源插件项目。
通过角色菜单等平台交互控制翻译，沿用 XRTranslate 的音频识别、翻译和输出能力。

## 功能

- 通过平台提供的角色交互开启或关闭翻译。
- 沿用 XRTranslate 的语言、音源、模型、字幕和语音输出设置。
- 独立管理角色翻译会话，关闭时不影响其他翻译任务。

## 平台适配

| 平台 | 状态 | 使用说明 |
|---|---|---|
| VRChat | 开发预览 | [适配介绍与安装](docs/adapters/vrchat/README.md) |

各适配器使用目标平台提供的接口，分别维护安装方式、交互协议和兼容范围。
项目架构与开发文档见[文档索引](docs/README.md)。

## 许可证

采用 [AGPL-3.0-only](LICENSE) 许可证。本项目为独立社区项目。
