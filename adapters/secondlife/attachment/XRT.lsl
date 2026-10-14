// Configure this private copy before wearing the attachment.
string RELAY_URL = "";       // HTTPS origin, without trailing slash.
string RELAY_CLIENT_TOKEN = "";
key PAIRED_OWNER = NULL_KEY;  // The avatar UUID permitted to use this copy.
integer SHOW_CAPTIONS = FALSE;

string instance;
string generation;
string session;
integer exchangeSequence;
integer commandSequence;
integer requested;
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
string captionIdentity;
integer captionRevision;
float captionUntil;

showStatus()
{
    if (!llGetAttached())
    {
        llSetText("", ZERO_VECTOR, 0.0);
        return;
    }
    vector color = <0.7, 0.7, 0.7>;
    if (status == "active") color = <0.3, 0.9, 0.5>;
    if (status == "error") color = <1.0, 0.4, 0.3>;
    string text = "XRT: " + status;
    if (SHOW_CAPTIONS && caption != "" && llGetTime() < captionUntil)
        text = caption;
    llSetText(text, color, 1.0);
}

clearControl()
{
    session = "";
    commandSequence = 0;
    requested = FALSE;
    ready = FALSE;
    helloSent = FALSE;
    caption = "";
    captionUntil = 0.0;
    captionIdentity = "";
    captionRevision = -1;
    status = "unavailable";
    showStatus();
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
    if (SHOW_CAPTIONS) capabilities = "[\"captions\"]";
    queueFrame(llList2Json(JSON_OBJECT, [
        "type", "hello", "protocol", 1, "connector", "secondlife", "token", "",
        "client_id", (string)llGetOwner() + ":" + instance,
        "capabilities", capabilities]));
    helloSent = TRUE;
}

setEnabled(integer value)
{
    requested = value;
    caption = "";
    if (ready && session != "")
    {
        ++commandSequence;
        string enabled = JSON_FALSE;
        if (value) enabled = JSON_TRUE;
        queueFrame(llList2Json(JSON_OBJECT, ["type", "set_enabled", "session", session,
            "sequence", commandSequence, "enabled", enabled]));
    }
    showStatus();
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
    if (llListFindList(["reset", "state", "caption", "clear", "pong"], [kind]) == -1) return;
    lastHostMessage = llGetTime();
    if (kind == "reset")
    {
        string reset = llJsonGetValue(frame, ["reset"]);
        if (llJsonValueType(frame, ["reset"]) != JSON_NUMBER) return;
        requested = FALSE;
        ready = TRUE;
        caption = "";
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
            if (value != "active") caption = "";
        }
        // Status reports never overwrite a newer touch request.
    }
    else if (kind == "caption" && SHOW_CAPTIONS && requested && status == "active")
    {
        string text = llJsonGetValue(frame, ["text"]);
        string identity = llJsonGetValue(frame, ["stream"]) + ":" + llJsonGetValue(frame, ["id"]);
        integer revision = (integer)llJsonGetValue(frame, ["revision"]);
        if (identity == captionIdentity && revision < captionRevision) return;
        integer lifetime = (integer)llJsonGetValue(frame, ["expires_ms"]);
        if (text != JSON_INVALID && lifetime > 0)
        {
            if (lifetime > 15000) lifetime = 15000;
            caption = text;
            captionIdentity = identity;
            captionRevision = revision;
            captionUntil = llGetTime() + (float)lifetime / 1000.0;
        }
    }
    else if (kind == "clear") caption = "";
    showStatus();
}

startAttachment()
{
    newConnection(2.0);
    llSetText("", ZERO_VECTOR, 0.0);
    if (llGetOwner() != PAIRED_OWNER || llStringLength(RELAY_CLIENT_TOKEN) < 32 ||
        llSubStringIndex(RELAY_URL, "https://") != 0)
    {
        llSetTimerEvent(0.0);
        llOwnerSay("Configure the relay URL, client token and owner UUID in this script.");
        return;
    }
    showStatus();
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
            setEnabled(FALSE);
            nextRequest = 0.0;
            sendExchange();
            llSetTimerEvent(0.0);
            llSetText("", ZERO_VECTOR, 0.0);
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
                if (ready && llGetAttached()) setEnabled(!requested);
                else llOwnerSay("XRT is waiting for its host connection.");
                return;
            }
        }
    }
    timer()
    {
        if (!llGetAttached() || llGetOwner() != PAIRED_OWNER)
        {
            llSetTimerEvent(0.0);
            llSetText("", ZERO_VECTOR, 0.0);
            return;
        }
        if (caption != "" && llGetTime() >= captionUntil)
        {
            caption = "";
            showStatus();
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
