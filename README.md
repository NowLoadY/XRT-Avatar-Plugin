# XRT-Avatar-Plugin

连接 VRChat Avatar 与 [XRTranslate](https://github.com/NowLoadY/XRTranslate)
的开源插件。角色表情菜单提供翻译开关，XRTranslate 负责音频、识别、翻译与输出。
使用 VRChat Avatar SDK 和官方 OSC 接口，不修改游戏客户端。

**当前提供配套源码，仍处于开发预览阶段。** 本仓库包含控制协议、无 I/O 的 Rust
控制核心与 Unity 编辑器安装器；XRTranslate 源码已实现宿主适配和共享 OSCQuery 传输。
使用包含“Avatar translation controls”设置项的配套 XRTranslate 构建，安装角色菜单后
再启用宿主控制。Unity 导入、头像上传和 VRChat 实机验证尚未执行，源码交付不代表
已经通过完整游戏环境验证。

## 功能范围

- 首版菜单：`Expressions → XRT → Translation`，只提供翻译启停。
- 语言、音源、模型、字幕与语音输出沿用 XRTranslate 中的配置。
- 请求和实际运行状态分开；停用只取消插件拥有的会话。
- 控制参数仅本地使用、不保存；重连和切换角色后重新启用。
- 后续角色悬浮翻译气泡使用独立的呈现接口。气泡渲染器尚未实现，不属于首版控制包。

官方菜单支持 Toggle 与子菜单，OSC 支持与外部应用交换角色参数。
参见 [Expressions Menu](https://creators.vrchat.com/avatars/expression-menu-and-controls/)
和 [OSC Avatar Parameters](https://docs.vrchat.com/docs/osc-avatar-parameters)。

## 仓库入口

| 路径 | 内容 |
|---|---|
| `packages/dev.nowloady.xrt-avatar/` | 编辑器安装器；生成菜单和参数资产 |
| `crates/avatar-control/` | 状态机、操作归属与类型化动作；无网络或推理依赖 |
| `protocol/avatar-parameters.json` | 控制参数的唯一协议定义 |
| `docs/` | 架构契约、安装说明与宿主集成规范 |
| `tools/generate_protocol.py` | 从协议生成 Rust / C# 常量 |

先阅读[兼容性与交付状态](docs/compatibility.md)，再按[安装说明](docs/installation.md)
在已有的 VRChat Avatar 创作环境中使用源码。宿主实现与对接边界见
[宿主集成](docs/host-integration.md)。完整索引见[文档地图](docs/README.md)。

## 开发

修改协议后运行 `python3 tools/generate_protocol.py`；
`python3 tools/generate_protocol.py --check` 可检查两端常量是否一致。
Rust 生命周期示例位于 `examples/lifecycle.rs`，可用
`cargo run --example lifecycle` 执行。
本仓库不包含 Unity、VRChat SDK、模型文件或翻译服务凭据。

新代码采用 [AGPL-3.0-only](LICENSE)。VRChat、Unity 及其他依赖遵循各自许可证；
本项目为独立社区项目。
