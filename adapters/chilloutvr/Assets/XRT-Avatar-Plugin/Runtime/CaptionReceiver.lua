-- CVR avatar parameters are synchronized by the game; the local receiver owns
-- the larger display buffer. Re-read a held pointer to accept late byte updates.
local UnityEngine = require("UnityEngine")
local avatar, material
local cached = {}
local nextPoll = 0
local cleared = true
local lastPointer, lastWord0, lastWord1
local lastChange = 0
local idleSeconds = 20
local wordNames = {}
local properties = {}
for i = 0, 1 do wordNames[i] = "XRT/Caption/Word" .. i end
for i = 0, 255 do properties[i] = "_Byte" .. i end

local function clear()
    for i = 0, 255 do
        if cached[i] ~= 0 then material.SetFloat(properties[i], 0) end
        cached[i] = 0
    end
    cleared = true
end

function Start()
    if not RunningInAvatar or not IsValid(BoundObjects.captionRenderer) then
        Script.Destroy("XRT caption requires an avatar and caption renderer.")
        return
    end
    material = BoundObjects.captionRenderer.material
    clear()
end

function Update()
    if not IsValid(material) then return end
    local now = UnityEngine.Time.unscaledTime
    if now < nextPoll then return end
    nextPoll = now + 0.05
    if not IsValid(avatar) then avatar = AvatarAPI.CurrentAvatar end
    if not IsValid(avatar) then return end
    local pointer = avatar.GetParameterAsInt("XRT/Caption/Pointer")
    if pointer == nil or pointer < 0 or (pointer > 31 and pointer ~= 255) then
        if not cleared and now - lastChange >= idleSeconds then clear() end
        return
    end
    if pointer == 255 then
        lastPointer, lastWord0, lastWord1 = pointer, nil, nil
        if not cleared then clear() end
        return
    end
    local word0 = avatar.GetParameterAsInt(wordNames[0])
    local word1 = avatar.GetParameterAsInt(wordNames[1])
    if pointer ~= lastPointer or word0 ~= lastWord0 or word1 ~= lastWord1 then
        lastPointer, lastWord0, lastWord1 = pointer, word0, word1
        lastChange = now
    end
    -- A disconnected host cannot send its final clear. Keep an expired bus
    -- blank until fresh data arrives instead of redrawing the held packet.
    if now - lastChange >= idleSeconds then
        if not cleared then clear() end
        return
    end
    local start = pointer * 8
    for i = 0, 1 do
        local word = word0
        if i == 1 then word = word1 end
        if word ~= nil then
            -- Lua numbers represent all int32 values exactly. Modulo also
            -- extracts bytes from negative two's-complement OSC integers.
            for byte = 0, 3 do
                local value = math.floor(word / (256 ^ (3 - byte))) % 256
                local index = start + i * 4 + byte
                if cached[index] ~= value then
                    material.SetFloat(properties[index], value)
                    cached[index] = value
                end
            end
        end
    end
    cleared = false
end

function OnDisable()
    if IsValid(material) then clear() end
end

function OnDestroy()
    if IsValid(material) then UnityEngine.Object.Destroy(material) end
    material = nil
end
