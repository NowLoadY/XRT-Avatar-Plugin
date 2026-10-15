// Configure this private copy before wearing the attachment.
string RELAY_URL = "";       // HTTPS origin, without trailing slash.
string RELAY_CLIENT_TOKEN = "";
key PAIRED_OWNER = NULL_KEY;  // The avatar UUID permitted to use this copy.
integer SHOW_CAPTIONS = FALSE;
integer PUBLISH_TEXT_SOURCE = FALSE; // Makes display text available to scripts in this linkset.
integer XRT_TEXT_SOURCE = 1481790465;
vector CAPTION_COLOR = <0.933333, 0.941176, 0.952941>;
float CAPTION_ALPHA = 0.94;

string instance;
string generation;
string session;
integer exchangeSequence;
integer commandSequence;
integer resetSequence;
integer requested;
integer stopPending;
integer ready;
integer helloSent;
string status = "unavailable";
list outgoing;
key requestId = NULL_KEY;
integer sentCount;
integer discardResponse;
float nextRequest;
float requestStarted;
float lastResponse;
float lastHostMessage;
string caption;
string captionDisplay;
string publishedText;
string publishedDisplay;
float publishedUntil;
integer publishReset;
string captionIdentity;
integer captionRevision;
float captionUntil;
list retiredCaptions;
integer displayMode;
integer displayRevision = -1;
integer publishedRevision = -1;
string headerText;
string footerText;
list displayMessages; // id, text, local deadline
float lastPublish;
integer dialogChannel;
integer dialogListener;
float dialogUntil;
string dialogAction;

closeControls()
{
    if (dialogListener) llListenRemove(dialogListener);
    dialogListener = 0;
    dialogAction = "";
    dialogUntil = 0.0;
}

showControls()
{
    closeControls();
    string label = "Disconnected";
    if (ready)
    {
        if (status == "off") label = "Off";
        else if (status == "starting") label = "Starting…";
        else if (status == "active") label = "On";
        else if (status == "stopping") label = "Stopping…";
        else if (status == "error") label = "Check XRTranslate";
    }
    if (stopPending) label = "Stopping…";
    list buttons = ["Close"];
    if (ready && status != "unavailable" && status != "stopping" && !stopPending)
    {
        dialogAction = "Start";
        if (requested) dialogAction = "Stop";
        buttons += [dialogAction];
    }
    dialogChannel = -1 - (integer)llFrand(1000000000.0);
    dialogListener = llListen(dialogChannel, "", llGetOwner(), "");
    dialogUntil = llGetTime() + 30.0;
    llDialog(llGetOwner(), "Translation: " + label, buttons, dialogChannel);
}

string jsonString(string value)
{
    // llList2Json interprets JSON-looking strings unless they are explicitly quoted.
    value = llDumpList2String(llParseStringKeepNulls(value, ["\\"], []), "\\\\");
    value = llDumpList2String(llParseStringKeepNulls(value, ["\""], []), "\\\"");
    value = llDumpList2String(llParseStringKeepNulls(value, ["\n"], []), "\\n");
    value = llDumpList2String(llParseStringKeepNulls(value, [llUnescapeURL("%0D")], []), "\\r");
    value = llDumpList2String(llParseStringKeepNulls(value, ["\t"], []), "\\t");
    return "\"" + value + "\"";
}

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

showCaption()
{
    string text = "";
    string display = "";
    list messages = [];
    list entries = [];
    integer index;
    float now = llGetTime();
    if (displayMode)
    {
        for (index = llGetListLength(displayMessages) - 3; index >= 0; index -= 3)
            if (llList2Float(displayMessages, index + 2) <= now)
                displayMessages = llDeleteSubList(displayMessages, index, index + 2);
        for (index = 0; index < llGetListLength(displayMessages); index += 3)
        {
            string content = llList2String(displayMessages, index + 1);
            text = joinText(text, content);
            entries += [content];
            messages += [llList2Json(JSON_OBJECT, [
                "id", jsonString(llList2String(displayMessages, index)), "text", jsonString(content),
                "ttl_ms", (integer)((llList2Float(displayMessages, index + 2) - now) * 1000.0)])];
        }
        text = joinText(joinText(headerText, text), footerText);
        display = hoverDisplay(headerText, entries, footerText);
    }
    else if (caption != "" && now < captionUntil)
    {
        text = caption;
        display = captionDisplay;
    }
    if (!llGetAttached()) { text = ""; display = ""; messages = []; }
    if (SHOW_CAPTIONS && text != "") llSetText(display, CAPTION_COLOR, CAPTION_ALPHA);
    else if (SHOW_CAPTIONS) llSetText("", ZERO_VECTOR, 0.0);
    if (PUBLISH_TEXT_SOURCE && (publishReset || text != publishedText ||
        (text != "" && (captionUntil != publishedUntil || display != publishedDisplay)) ||
        displayMode && (displayRevision != publishedRevision || now - lastPublish >= 5.0)))
    {
        integer remaining = 0;
        if (text != "")
        {
            remaining = (integer)((captionUntil - now) * 1000.0);
            if (displayMode) remaining = 8000;
        }
        string value = llList2Json(JSON_OBJECT, ["text", jsonString(text),
            "display_text", jsonString(display), "expires_ms", remaining]);
        if (displayMode)
        {
            string header = JSON_NULL;
            string footer = JSON_NULL;
            if (llGetAttached() && headerText != "") header = jsonString(headerText);
            if (llGetAttached() && footerText != "") footer = jsonString(footerText);
            value = llJsonSetValue(value, ["type"], jsonString("display"));
            value = llJsonSetValue(value, ["header"], header);
            value = llJsonSetValue(value, ["messages"], llList2Json(JSON_ARRAY, messages));
            value = llJsonSetValue(value, ["footer"], footer);
            value = llJsonSetValue(value, ["revision"], (string)displayRevision);
        }
        llMessageLinked(LINK_SET, XRT_TEXT_SOURCE, value, NULL_KEY);
        publishedText = text;
        publishedDisplay = display;
        publishedUntil = captionUntil;
        publishedRevision = displayRevision;
        lastPublish = now;
        publishReset = FALSE;
    }
}

receiveDisplay(string frame)
{
    string headerType = llJsonValueType(frame, ["header"]);
    string footerType = llJsonValueType(frame, ["footer"]);
    if ((headerType != JSON_NULL && headerType != JSON_STRING) ||
        (footerType != JSON_NULL && footerType != JSON_STRING) ||
        llJsonValueType(frame, ["messages"]) != JSON_ARRAY ||
        llJsonValueType(frame, ["revision"]) != JSON_NUMBER) return;
    integer revision = (integer)llJsonGetValue(frame, ["revision"]);
    if (revision < 0 || revision <= displayRevision) return;
    list incoming = llJson2List(llJsonGetValue(frame, ["messages"]));
    if (llGetListLength(incoming) > 8) return;
    list values = [];
    list ids = [];
    integer index;
    // Conservatively include this HTTP exchange's transit time in message age.
    integer transit = (integer)((llGetTime() - requestStarted) * 1000.0);
    for (index = 0; index < llGetListLength(incoming); ++index)
    {
        string message = llList2String(incoming, index);
        if (llJsonValueType(message, ["id"]) != JSON_STRING ||
            llJsonValueType(message, ["text"]) != JSON_STRING ||
            llJsonValueType(message, ["ttl_ms"]) != JSON_NUMBER) return;
        string identity = llJsonGetValue(message, ["id"]);
        if (identity == "" || llListFindList(ids, [identity]) != -1) return;
        ids += [identity];
        integer remaining = (integer)llJsonGetValue(message, ["ttl_ms"]);
        if (remaining < 0 || remaining > 15000) return;
        remaining -= transit;
        string content = llGetSubString(llJsonGetValue(message, ["text"]), 0, 255);
        if (remaining > 0 && content != "")
            values += [identity, content, llGetTime() + (float)remaining / 1000.0];
    }
    headerText = "";
    footerText = "";
    if (headerType == JSON_STRING) headerText = llGetSubString(llJsonGetValue(frame, ["header"]), 0, 127);
    if (footerType == JSON_STRING) footerText = llGetSubString(llJsonGetValue(frame, ["footer"]), 0, 127);
    displayMessages = values;
    displayMode = TRUE;
    displayRevision = revision;
    caption = "";
    captionUntil = 0.0;
    publishReset = TRUE;
}

clearControl()
{
    closeControls();
    session = "";
    commandSequence = 0;
    resetSequence = 0;
    requested = FALSE;
    stopPending = FALSE;
    ready = FALSE;
    helloSent = FALSE;
    caption = "";
    captionDisplay = "";
    captionUntil = 0.0;
    publishReset = TRUE;
    captionIdentity = "";
    captionRevision = -1;
    retiredCaptions = [];
    displayMode = FALSE;
    displayRevision = -1;
    headerText = "";
    footerText = "";
    displayMessages = [];
    status = "unavailable";
    showCaption();
}

newConnection(float pause)
{
    instance = (string)llGenerateKey();
    generation = "";
    exchangeSequence = 0;
    outgoing = [];
    if (requestId != NULL_KEY) discardResponse = TRUE;
    nextRequest = llGetTime() + pause;
    lastResponse = llGetTime();
    clearControl();
}

queueFrame(string frame)
{
    if (llGetListLength(outgoing) >= 12)
    {
        newConnection(2.0);
        return;
    }
    outgoing += [frame];
}

sendHello()
{
    if (helloSent) return;
    string capabilities = "[]";
    if (SHOW_CAPTIONS || PUBLISH_TEXT_SOURCE) capabilities = "[\"captions\",\"display-v1\"]";
    queueFrame(llList2Json(JSON_OBJECT, [
        "type", "hello", "protocol", 1, "connector", "secondlife", "token", "",
        "client_id", (string)llGetOwner() + ":" + instance,
        "capabilities", capabilities]));
    helloSent = TRUE;
}

setEnabled(integer value)
{
    if (!value && requested) stopPending = TRUE;
    requested = value;
    if (!displayMode) { caption = ""; captionUntil = 0.0; }
    if (ready && session != "")
    {
        ++commandSequence;
        string enabled = JSON_FALSE;
        if (value) enabled = JSON_TRUE;
        queueFrame(llList2Json(JSON_OBJECT, ["type", "set_enabled", "session", session,
            "sequence", commandSequence, "enabled", enabled]));
    }
    showCaption();
}

sendExchange()
{
    if (requestId != NULL_KEY || llGetTime() < nextRequest) return;
    if (session != "" && llGetListLength(outgoing) == 0)
        queueFrame(llList2Json(JSON_OBJECT, ["type", "ping", "session", session]));
    sentCount = llGetListLength(outgoing);
    if (sentCount > 4) sentCount = 4;
    list batch = [];
    if (sentCount > 0) batch = llList2List(outgoing, 0, sentCount - 1);
    ++exchangeSequence;
    string body = llList2Json(JSON_OBJECT, ["instance", instance,
        "sequence", exchangeSequence, "generation", generation,
        "messages", llList2Json(JSON_ARRAY, batch)]);
    discardResponse = FALSE;
    requestId = llHTTPRequest(RELAY_URL + "/v1/client/exchange", [
        HTTP_METHOD, "POST", HTTP_MIMETYPE, "application/json",
        HTTP_CUSTOM_HEADER, "Authorization", "Bearer " + RELAY_CLIENT_TOKEN,
        HTTP_VERIFY_CERT, TRUE, HTTP_BODY_MAXLENGTH, 12288,
        HTTP_VERBOSE_THROTTLE, FALSE], body);
    requestStarted = llGetTime();
    nextRequest = requestStarted + 2.0;
    if (requestId == NULL_KEY) newConnection(45.0);
}

receiveFrame(string frame)
{
    string kind = llJsonGetValue(frame, ["type"]);
    if (kind == "welcome")
    {
        if (llJsonGetValue(frame, ["protocol"]) != "1") return;
        string identity = llJsonGetValue(frame, ["session"]);
        if (identity == JSON_INVALID || identity == "") return;
        clearControl();
        helloSent = TRUE;
        session = identity;
        lastHostMessage = llGetTime();
        return;
    }
    if (session == "" || llJsonGetValue(frame, ["session"]) != session) return;
    if (llListFindList(["reset", "state", "caption", "display", "clear", "pong"], [kind]) == -1) return;
    lastHostMessage = llGetTime();
    if (kind == "reset")
    {
        string reset = llJsonGetValue(frame, ["reset"]);
        if (llJsonValueType(frame, ["reset"]) != JSON_NUMBER || (integer)reset <= resetSequence) return;
        resetSequence = (integer)reset;
        status = "off";
        closeControls();
        requested = FALSE;
        stopPending = FALSE;
        ready = TRUE;
        caption = "";
        captionDisplay = "";
        captionUntil = 0.0;
        publishReset = TRUE;
        captionIdentity = "";
        captionRevision = -1;
        retiredCaptions = [];
        // A reset discards user commands queued before the host reconciliation.
        outgoing = [];
        queueFrame(llList2Json(JSON_OBJECT, ["type", "reset_ack", "session", session,
            "reset", (integer)reset, "enabled", JSON_FALSE]));
    }
    else if (kind == "state")
    {
        string value = llJsonGetValue(frame, ["status"]);
        if (llListFindList(["unavailable", "off", "starting", "active", "stopping", "error"], [value]) != -1)
        {
            status = value;
            if (value == "off" || value == "error" || value == "unavailable") stopPending = FALSE;
            if (value != "active" && !displayMode) caption = "";
        }
        // Status reports never overwrite a newer touch request.
    }
    else if (kind == "display" && (SHOW_CAPTIONS || PUBLISH_TEXT_SOURCE)) receiveDisplay(frame);
    else if (kind == "caption" && !displayMode && (SHOW_CAPTIONS || PUBLISH_TEXT_SOURCE) && requested && status == "active")
    {
        if (llJsonValueType(frame, ["text"]) != JSON_STRING ||
            llJsonValueType(frame, ["stream"]) != JSON_STRING ||
            llJsonValueType(frame, ["id"]) != JSON_STRING ||
            llJsonValueType(frame, ["revision"]) != JSON_NUMBER ||
            llJsonValueType(frame, ["expires_ms"]) != JSON_NUMBER) return;
        string text = llJsonGetValue(frame, ["text"]);
        string identity = llList2Json(JSON_ARRAY, [
            llJsonGetValue(frame, ["stream"]), llJsonGetValue(frame, ["id"])]);
        integer revision = (integer)llJsonGetValue(frame, ["revision"]);
        if (revision < 0 || llListFindList(retiredCaptions, [identity]) != -1 ||
            (identity == captionIdentity && revision <= captionRevision)) return;
        if (identity != captionIdentity && captionIdentity != "")
        {
            retiredCaptions += [captionIdentity];
            if (llGetListLength(retiredCaptions) > 32)
                retiredCaptions = llDeleteSubList(retiredCaptions, 0, 0);
        }
        captionIdentity = identity;
        captionRevision = revision;
        integer lifetime = (integer)llJsonGetValue(frame, ["expires_ms"]);
        if (lifetime > 8000) lifetime = 8000;
        caption = "";
        captionUntil = 0.0;
        if (text != "" && lifetime > 0)
        {
            caption = text;
            captionDisplay = llJsonGetValue(frame, ["display_text"]);
            if (llJsonValueType(frame, ["display_text"]) != JSON_STRING) captionDisplay = text;
            captionUntil = llGetTime() + (float)lifetime / 1000.0;
        }
    }
    else if (kind == "clear")
    {
        caption = "";
        headerText = "";
        footerText = "";
        displayMessages = [];
        publishReset = TRUE;
    }
    showCaption();
}

startAttachment()
{
    newConnection(2.0);
    if (SHOW_CAPTIONS) llSetText("", ZERO_VECTOR, 0.0);
    if (llGetOwner() != PAIRED_OWNER || llStringLength(RELAY_CLIENT_TOKEN) < 32 ||
        llSubStringIndex(RELAY_URL, "https://") != 0)
    {
        llSetTimerEvent(0.0);
        llOwnerSay("Configure the relay URL, client token and owner UUID in this script.");
        return;
    }
    showCaption();
    if (llGetAttached()) llSetTimerEvent(1.0);
    else llSetTimerEvent(0.0);
}

default
{
    state_entry() { startAttachment(); }
    on_rez(integer parameter) { startAttachment(); }
    attach(key avatar)
    {
        if (avatar != NULL_KEY) startAttachment();
        else if (llGetAttached() == 0)
        {
            closeControls();
            setEnabled(FALSE);
            nextRequest = 0.0;
            sendExchange();
            llSetTimerEvent(0.0);
            if (SHOW_CAPTIONS) llSetText("", ZERO_VECTOR, 0.0);
        }
    }
    changed(integer change)
    {
        if (change & (CHANGED_OWNER | CHANGED_REGION | CHANGED_TELEPORT | CHANGED_REGION_START))
            startAttachment();
    }
    touch_start(integer count)
    {
        integer index;
        for (index = 0; index < count; ++index)
        {
            if (llDetectedKey(index) == llGetOwner() && llGetOwner() == PAIRED_OWNER)
            {
                if (llGetAttached()) showControls();
                return;
            }
        }
    }
    listen(integer channel, string name, key speaker, string message)
    {
        if (channel != dialogChannel || !dialogListener || speaker != llGetOwner() ||
            speaker != PAIRED_OWNER || llGetTime() >= dialogUntil) return;
        string action = dialogAction;
        closeControls();
        if (!ready || !llGetAttached() || message != action || action == "") return;
        if (action == "Start" && !requested && !stopPending && status != "stopping") setEnabled(TRUE);
        else if (action == "Stop" && requested) setEnabled(FALSE);
    }
    timer()
    {
        if (dialogListener && llGetTime() >= dialogUntil) closeControls();
        if (!llGetAttached() || llGetOwner() != PAIRED_OWNER)
        {
            llSetTimerEvent(0.0);
            clearControl();
            return;
        }
        if (displayMode) showCaption();
        else if (caption != "" && llGetTime() >= captionUntil)
        {
            caption = "";
            showCaption();
        }
        if (requestId != NULL_KEY && llGetTime() - requestStarted > 60.0)
        {
            requestId = NULL_KEY;
            discardResponse = FALSE;
            newConnection(45.0);
        }
        else if (requestId != NULL_KEY && !discardResponse && llGetTime() - requestStarted > 14.0)
            newConnection(45.0);
        else if (session != "" && (llGetTime() - lastResponse > 14.0 ||
            llGetTime() - lastHostMessage >= 15.0))
            newConnection(2.0);
        sendExchange();
    }
    http_response(key responseId, integer code, list metadata, string body)
    {
        if (responseId != requestId || responseId == NULL_KEY) return;
        requestId = NULL_KEY;
        if (discardResponse)
        {
            discardResponse = FALSE;
            if (code == 420 || code == 429 || code >= 500)
                nextRequest = llGetTime() + 45.0;
            return;
        }
        if (code != 200 || llJsonValueType(body, ["messages"]) != JSON_ARRAY)
        {
            float pause = 2.0;
            if (code == 420 || code == 429 || code >= 500) pause = 45.0;
            newConnection(pause);
            return;
        }
        string epoch = llJsonGetValue(body, ["generation"]);
        if (epoch == JSON_INVALID || epoch == "")
        {
            newConnection(2.0);
            return;
        }
        lastResponse = llGetTime();
        if (sentCount > 0) outgoing = llDeleteSubList(outgoing, 0, sentCount - 1);
        integer peerOnline = llJsonGetValue(body, ["peer_online"]) == JSON_TRUE;
        if (epoch != generation || llJsonGetValue(body, ["reset"]) == JSON_TRUE || !peerOnline)
        {
            outgoing = [];
            clearControl();
            generation = epoch;
            if (peerOnline) sendHello();
            return;
        }
        if (!helloSent) sendHello();
        list frames = llJson2List(llJsonGetValue(body, ["messages"]));
        integer index;
        for (index = 0; index < llGetListLength(frames); ++index)
            receiveFrame(llList2String(frames, index));
    }
}
