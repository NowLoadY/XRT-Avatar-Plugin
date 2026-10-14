# ChilloutVR installation

## Avatar setup

1. Use a ChilloutVR CCK project and finish the avatar's normal animator setup.
   The CCK's **Create Animator** and **Attach created Override to Avatar**
   controls provide this setup for an avatar using generated settings.
2. Download [the ChilloutVR Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.1/XRT-Avatar-Plugin-ChilloutVR-0.1.0-preview.1.unitypackage).
   In Unity, use **Assets → Import Package → Custom Package** and import it.
3. Save the scene and select the avatar root with its `CVRAvatar` component.
4. Choose **Tools → XRT-Avatar-Plugin → ChilloutVR → Install on selected avatar**.
5. Save the scene and upload through the CCK.

The installer copies the final controller, preserves animation overrides and
adds one Bool toggle. Original assets remain available. Status and protocol
version are Int parameters in the copied controller. All three names use the
CCK's `#` local-parameter prefix.

Install after generating the avatar's animator. To regenerate it later, remove
XRT, regenerate and attach the controller, then install XRT again. Existing
installation assets are retained on removal. Use **Remove from selected avatar**
to restore the original controller and remove the owned menu entry.

## Connect XRTranslate

1. Run an [XRTranslate development build](https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md) with Avatar controls (planned for XRTranslate 0.3.4). Enable OSC in **Settings → Implementation → OSC** in ChilloutVR.
2. In XRTranslate, enable Avatar translation controls and select **ChilloutVR**.
3. Wear the installed avatar and wait for the connection to show **Off**.
4. Enable **XRT Translation** in Advanced Avatar Settings to start. Disable it
   to stop that session.

Use ChilloutVR's default OSCQuery service prefix. XRTranslate discovers its
OSCQuery HTTP endpoint and uses the advertised OSC destination. When switching
avatars, changing an Avatar profile or reconnecting, the host resets the toggle
and verifies it before accepting a new start. Saved profile values do not serve
as permission to resume audio capture.

Translation language, capture source and recognition services are configured in
XRTranslate. A busy capture source remains owned by its existing session.

## Translation output

Text delivery through OSC Chatbox is configured in
[XRTranslate's OSC plugin](https://github.com/NowLoadY/XRTranslate/blob/main/docs/plugin-architecture.md).
The Avatar adapter owns the menu control and its parameters.

## References

- [CCK avatar settings](https://docs.chilloutvr.net/cck/components/cvr-avatar/)
- [OSC and Chatbox endpoints](https://docs.chilloutvr.net/chilloutvr/game/osc/)
- [OSC launch arguments](https://docs.chilloutvr.net/chilloutvr/game/launch-arguments/)
