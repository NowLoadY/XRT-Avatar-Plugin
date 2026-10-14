// Put this receiver in a dedicated prim in the same linkset as XRT.lsl.
// Set PUBLISH_TEXT_SOURCE = TRUE in XRT.lsl. Customize this object in Build.
integer XRT_TEXT_SOURCE = 1481790465;
vector TEXT_COLOR = <0.933333, 0.941176, 0.952941>;
float TEXT_ALPHA = 0.94;
float expires;

default
{
    state_entry() { llSetText("", ZERO_VECTOR, 0.0); }
    link_message(integer sender, integer number, string message, key id)
    {
        if (number != XRT_TEXT_SOURCE) return;
        if (llJsonValueType(message, ["text"]) != JSON_STRING ||
            llJsonValueType(message, ["display_text"]) != JSON_STRING ||
            llJsonValueType(message, ["expires_ms"]) != JSON_NUMBER) return;
        integer remaining = (integer)llJsonGetValue(message, ["expires_ms"]);
        if (remaining > 8000) remaining = 8000;
        // Custom lettering can consume "text"; native hover text uses "display_text".
        string text = llJsonGetValue(message, ["display_text"]);
        if (remaining <= 0 || !llGetAttached()) text = "";
        llSetText(text, TEXT_COLOR, TEXT_ALPHA);
        if (text == "") { expires = 0.0; llSetTimerEvent(0.0); }
        else { expires = llGetTime() + (float)remaining / 1000.0; llSetTimerEvent(0.2); }
    }
    timer()
    {
        if (llGetTime() >= expires || !llGetAttached())
        {
            llSetText("", ZERO_VECTOR, 0.0);
            llSetTimerEvent(0.0);
        }
    }
    attach(key avatar)
    {
        llSetText("", ZERO_VECTOR, 0.0);
        llSetTimerEvent(0.0);
    }
    changed(integer change)
    {
        if (change & (CHANGED_OWNER | CHANGED_REGION | CHANGED_TELEPORT | CHANGED_REGION_START))
        {
            llSetText("", ZERO_VECTOR, 0.0);
            llSetTimerEvent(0.0);
        }
    }
}
