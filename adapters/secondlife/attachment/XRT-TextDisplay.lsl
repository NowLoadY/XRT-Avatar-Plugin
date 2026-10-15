// Put this receiver in a dedicated prim in the same linkset as XRT.lsl.
// Set PUBLISH_TEXT_SOURCE = TRUE in XRT.lsl. Customize this object in Build.
integer XRT_TEXT_SOURCE = 1481790465;
string REGION = "full"; // full, header, messages or footer
vector TEXT_COLOR = <0.933333, 0.941176, 0.952941>;
float TEXT_ALPHA = 0.94;
float expires;
integer displayMode;
string headerText;
string footerText;
string legacyText;
list messages; // text, local deadline

string joinText(string first, string second)
{
    if (first == "") return second;
    if (second == "") return first;
    return first + "\n" + second;
}

clearDisplay()
{
    expires = 0.0;
    displayMode = FALSE;
    headerText = "";
    footerText = "";
    legacyText = "";
    messages = [];
    llSetText("", ZERO_VECTOR, 0.0);
    llSetTimerEvent(0.0);
}

renderDisplay()
{
    if (!llGetAttached() || llGetTime() >= expires) { clearDisplay(); return; }
    string text = legacyText;
    if (displayMode)
    {
        text = "";
        integer index;
        for (index = llGetListLength(messages) - 2; index >= 0; index -= 2)
            if (llList2Float(messages, index + 1) <= llGetTime())
                messages = llDeleteSubList(messages, index, index + 1);
        for (index = 0; index < llGetListLength(messages); index += 2)
            text = joinText(text, llList2String(messages, index));
        if (REGION == "header") text = headerText;
        else if (REGION == "footer") text = footerText;
        else if (REGION != "messages") text = joinText(joinText(headerText, text), footerText);
    }
    else if (REGION == "header" || REGION == "footer") text = "";
    llSetText(text, TEXT_COLOR, TEXT_ALPHA);
}

default
{
    state_entry() { clearDisplay(); }
    link_message(integer sender, integer number, string message, key id)
    {
        if (number != XRT_TEXT_SOURCE || llJsonValueType(message, ["expires_ms"]) != JSON_NUMBER) return;
        integer remaining = (integer)llJsonGetValue(message, ["expires_ms"]);
        if (remaining > 8000) remaining = 8000;
        if (remaining <= 0 || !llGetAttached()) { clearDisplay(); return; }
        expires = llGetTime() + (float)remaining / 1000.0;
        displayMode = llJsonGetValue(message, ["type"]) == "display";
        if (displayMode)
        {
            headerText = "";
            footerText = "";
            if (llJsonValueType(message, ["header"]) == JSON_STRING)
                headerText = llJsonGetValue(message, ["header"]);
            if (llJsonValueType(message, ["footer"]) == JSON_STRING)
                footerText = llJsonGetValue(message, ["footer"]);
            list incoming = llJson2List(llJsonGetValue(message, ["messages"]));
            messages = [];
            integer index;
            for (index = 0; index < llGetListLength(incoming) && index < 8; ++index)
            {
                string item = llList2String(incoming, index);
                integer ttl = (integer)llJsonGetValue(item, ["ttl_ms"]);
                if (ttl > 15000) ttl = 15000;
                if (ttl > 0 && llJsonValueType(item, ["text"]) == JSON_STRING)
                    messages += [llJsonGetValue(item, ["text"]), llGetTime() + (float)ttl / 1000.0];
            }
        }
        else
        {
            if (llJsonValueType(message, ["display_text"]) != JSON_STRING) return;
            legacyText = llJsonGetValue(message, ["display_text"]);
        }
        renderDisplay();
        llSetTimerEvent(0.2);
    }
    timer() { renderDisplay(); }
    attach(key avatar) { clearDisplay(); }
    changed(integer change)
    {
        if (change & (CHANGED_OWNER | CHANGED_REGION | CHANGED_TELEPORT | CHANGED_REGION_START)) clearDisplay();
    }
}
