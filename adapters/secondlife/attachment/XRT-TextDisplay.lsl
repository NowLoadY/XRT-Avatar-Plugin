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

integer utf8Bytes(string text)
{
    string encoded = llStringToBase64(text);
    integer length = llStringLength(encoded) * 3 / 4;
    if (llGetSubString(encoded, -2, -1) == "==") return length - 2;
    if (llGetSubString(encoded, -1, -1) == "=") return length - 1;
    return length;
}

string hoverText(string text, integer limit)
{
    if (utf8Bytes(text) <= limit) return text;
    integer low = 0;
    integer high = llStringLength(text);
    while (low < high)
    {
        integer middle = (low + high + 1) / 2;
        if (utf8Bytes(llGetSubString(text, 0, middle - 1)) <= limit - 3) low = middle;
        else high = middle - 1;
    }
    if (!low) return "…";
    return llGetSubString(text, 0, low - 1) + "…";
}

string joinText(string first, string second)
{
    if (first == "") return second;
    if (second == "") return first;
    return first + "\n" + second;
}

// Keep a recent complete suffix; banners share the remaining native text budget.
string hoverDisplay(string header, list entries, string footer)
{
    integer count = llGetListLength(entries);
    integer banners = 0;
    if (header != "") ++banners;
    if (footer != "") ++banners;
    integer parts = banners + (count > 0);
    if (!parts) return "";
    integer bannerBudget = (254 - parts + 1) / (banners + 2 * (count > 0));
    header = hoverText(llDumpList2String(llParseStringKeepNulls(header, ["\n"], []), " "), bannerBudget);
    footer = hoverText(llDumpList2String(llParseStringKeepNulls(footer, ["\n"], []), " "), bannerBudget);
    if (!count) return joinText(header, footer);
    integer budget = 254 - utf8Bytes(header) - utf8Bytes(footer) - banners;
    string body = "";
    integer index;
    for (index = count - 1; index >= 0; --index)
    {
        string candidate = joinText(llList2String(entries, index), body);
        if (body == "" || utf8Bytes(candidate) <= budget) body = candidate;
        else index = -1;
    }
    return joinText(joinText(header, hoverText(body, budget)), footer);
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
        list entries = [];
        for (index = 0; index < llGetListLength(messages); index += 2)
            entries += [llList2String(messages, index)];
        if (REGION == "header") text = headerText;
        else if (REGION == "footer") text = footerText;
        else if (REGION == "messages") text = hoverDisplay("", entries, "");
        else text = hoverDisplay(headerText, entries, footerText);
    }
    else if (REGION == "header" || REGION == "footer") text = "";
    llSetText(hoverText(text, 254), TEXT_COLOR, TEXT_ALPHA);
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
