# ChilloutVR compatibility

| Surface | Implementation |
| --- | --- |
| Avatar menu | CCK Advanced Avatar Settings Bool toggle |
| Parameters | Local `#XRT/Enabled`, `#XRT/Status`, `#XRT/Version` |
| Connector | Shared OSCQuery discovery, typed snapshots and OSC writes |
| Peer selection | `ChilloutVR-` service prefix and ChilloutVR HOST_INFO name |
| Lifecycle | Avatar and Avatar-profile changes retire the current binding |
| Translation | XRTranslate-owned session using the shared control state machine |

The installer targets the public CCK 4.0.1 source API distributed through the
[official CCK download service](https://api.chilloutvr.net/1/public/cck/info).
The CCK's version response selects Unity 2022.3.58f1 for that release. SDK code
is not included in this repository.

The SDK types and fields have been checked against that source archive. Unity
compilation, avatar upload and in-game acceptance have not been performed for
this adapter. Use the platform's [current setup guidance](https://docs.chilloutvr.net/cck/setup/)
when preparing an authoring environment.

The installer uses the CCK's `CVRAvatar.avatarSettings`,
`CVRAdvancedSettingsEntry` and `CVRAdvancesAvatarSettingGameObjectToggle` APIs.
Its scripts compile in the project's Editor assembly because the CCK's public
components are supplied through the project's default assembly.

The host accepts the native OSCQuery prefix. A custom `--osc-query-prefix`
requires restoring the native prefix for automatic selection. Peer names select
protocol behavior; they are not an authentication mechanism.
