// Shared proportional SDF display. Platform adapters only animate the byte properties.
Shader "XRT/Avatar Caption"
{
    Properties
    {
        [NoScaleOffset] _Atlas ("Glyph atlas", 2D) = "black" {}
        [NoScaleOffset] _GlyphMap ("Unicode map", 2D) = "black" {}
        _Color ("Text", Color) = (0.208, 0.259, 0.302, 1)
        _OutlineColor ("Text outline", Color) = (0.04, 0.05, 0.07, 0.85)
        _OutlineWidth ("Outline width", Range(0, 0.25)) = 0
        _BackgroundColor ("Background", Color) = (0, 0, 0, 0)
        _BorderColor ("Border", Color) = (0.25, 0.34, 0.39, 1)
        _BorderWidth ("Border width / height", Range(0, 0.08)) = 0
        _FontSize ("Text size", Float) = 0.0186
        [Toggle] _NaturalLayout ("Natural layout", Float) = 0
        _Columns ("Columns", Range(8, 32)) = 32
        _CellAspect ("Cell height / width", Float) = 1.25
        _Padding ("Padding", Range(0, 0.2)) = 0.035
        _CornerRadius ("Corner radius", Range(0, 0.2)) = 0.035
        [Toggle] _HideWhenEmpty ("Hide when empty", Float) = 1
        // bytes:start
        [HideInInspector] _Byte0 ("Byte 0", Float) = 0
        [HideInInspector] _Byte1 ("Byte 1", Float) = 0
        [HideInInspector] _Byte2 ("Byte 2", Float) = 0
        [HideInInspector] _Byte3 ("Byte 3", Float) = 0
        [HideInInspector] _Byte4 ("Byte 4", Float) = 0
        [HideInInspector] _Byte5 ("Byte 5", Float) = 0
        [HideInInspector] _Byte6 ("Byte 6", Float) = 0
        [HideInInspector] _Byte7 ("Byte 7", Float) = 0
        [HideInInspector] _Byte8 ("Byte 8", Float) = 0
        [HideInInspector] _Byte9 ("Byte 9", Float) = 0
        [HideInInspector] _Byte10 ("Byte 10", Float) = 0
        [HideInInspector] _Byte11 ("Byte 11", Float) = 0
        [HideInInspector] _Byte12 ("Byte 12", Float) = 0
        [HideInInspector] _Byte13 ("Byte 13", Float) = 0
        [HideInInspector] _Byte14 ("Byte 14", Float) = 0
        [HideInInspector] _Byte15 ("Byte 15", Float) = 0
        [HideInInspector] _Byte16 ("Byte 16", Float) = 0
        [HideInInspector] _Byte17 ("Byte 17", Float) = 0
        [HideInInspector] _Byte18 ("Byte 18", Float) = 0
        [HideInInspector] _Byte19 ("Byte 19", Float) = 0
        [HideInInspector] _Byte20 ("Byte 20", Float) = 0
        [HideInInspector] _Byte21 ("Byte 21", Float) = 0
        [HideInInspector] _Byte22 ("Byte 22", Float) = 0
        [HideInInspector] _Byte23 ("Byte 23", Float) = 0
        [HideInInspector] _Byte24 ("Byte 24", Float) = 0
        [HideInInspector] _Byte25 ("Byte 25", Float) = 0
        [HideInInspector] _Byte26 ("Byte 26", Float) = 0
        [HideInInspector] _Byte27 ("Byte 27", Float) = 0
        [HideInInspector] _Byte28 ("Byte 28", Float) = 0
        [HideInInspector] _Byte29 ("Byte 29", Float) = 0
        [HideInInspector] _Byte30 ("Byte 30", Float) = 0
        [HideInInspector] _Byte31 ("Byte 31", Float) = 0
        [HideInInspector] _Byte32 ("Byte 32", Float) = 0
        [HideInInspector] _Byte33 ("Byte 33", Float) = 0
        [HideInInspector] _Byte34 ("Byte 34", Float) = 0
        [HideInInspector] _Byte35 ("Byte 35", Float) = 0
        [HideInInspector] _Byte36 ("Byte 36", Float) = 0
        [HideInInspector] _Byte37 ("Byte 37", Float) = 0
        [HideInInspector] _Byte38 ("Byte 38", Float) = 0
        [HideInInspector] _Byte39 ("Byte 39", Float) = 0
        [HideInInspector] _Byte40 ("Byte 40", Float) = 0
        [HideInInspector] _Byte41 ("Byte 41", Float) = 0
        [HideInInspector] _Byte42 ("Byte 42", Float) = 0
        [HideInInspector] _Byte43 ("Byte 43", Float) = 0
        [HideInInspector] _Byte44 ("Byte 44", Float) = 0
        [HideInInspector] _Byte45 ("Byte 45", Float) = 0
        [HideInInspector] _Byte46 ("Byte 46", Float) = 0
        [HideInInspector] _Byte47 ("Byte 47", Float) = 0
        [HideInInspector] _Byte48 ("Byte 48", Float) = 0
        [HideInInspector] _Byte49 ("Byte 49", Float) = 0
        [HideInInspector] _Byte50 ("Byte 50", Float) = 0
        [HideInInspector] _Byte51 ("Byte 51", Float) = 0
        [HideInInspector] _Byte52 ("Byte 52", Float) = 0
        [HideInInspector] _Byte53 ("Byte 53", Float) = 0
        [HideInInspector] _Byte54 ("Byte 54", Float) = 0
        [HideInInspector] _Byte55 ("Byte 55", Float) = 0
        [HideInInspector] _Byte56 ("Byte 56", Float) = 0
        [HideInInspector] _Byte57 ("Byte 57", Float) = 0
        [HideInInspector] _Byte58 ("Byte 58", Float) = 0
        [HideInInspector] _Byte59 ("Byte 59", Float) = 0
        [HideInInspector] _Byte60 ("Byte 60", Float) = 0
        [HideInInspector] _Byte61 ("Byte 61", Float) = 0
        [HideInInspector] _Byte62 ("Byte 62", Float) = 0
        [HideInInspector] _Byte63 ("Byte 63", Float) = 0
        [HideInInspector] _Byte64 ("Byte 64", Float) = 0
        [HideInInspector] _Byte65 ("Byte 65", Float) = 0
        [HideInInspector] _Byte66 ("Byte 66", Float) = 0
        [HideInInspector] _Byte67 ("Byte 67", Float) = 0
        [HideInInspector] _Byte68 ("Byte 68", Float) = 0
        [HideInInspector] _Byte69 ("Byte 69", Float) = 0
        [HideInInspector] _Byte70 ("Byte 70", Float) = 0
        [HideInInspector] _Byte71 ("Byte 71", Float) = 0
        [HideInInspector] _Byte72 ("Byte 72", Float) = 0
        [HideInInspector] _Byte73 ("Byte 73", Float) = 0
        [HideInInspector] _Byte74 ("Byte 74", Float) = 0
        [HideInInspector] _Byte75 ("Byte 75", Float) = 0
        [HideInInspector] _Byte76 ("Byte 76", Float) = 0
        [HideInInspector] _Byte77 ("Byte 77", Float) = 0
        [HideInInspector] _Byte78 ("Byte 78", Float) = 0
        [HideInInspector] _Byte79 ("Byte 79", Float) = 0
        [HideInInspector] _Byte80 ("Byte 80", Float) = 0
        [HideInInspector] _Byte81 ("Byte 81", Float) = 0
        [HideInInspector] _Byte82 ("Byte 82", Float) = 0
        [HideInInspector] _Byte83 ("Byte 83", Float) = 0
        [HideInInspector] _Byte84 ("Byte 84", Float) = 0
        [HideInInspector] _Byte85 ("Byte 85", Float) = 0
        [HideInInspector] _Byte86 ("Byte 86", Float) = 0
        [HideInInspector] _Byte87 ("Byte 87", Float) = 0
        [HideInInspector] _Byte88 ("Byte 88", Float) = 0
        [HideInInspector] _Byte89 ("Byte 89", Float) = 0
        [HideInInspector] _Byte90 ("Byte 90", Float) = 0
        [HideInInspector] _Byte91 ("Byte 91", Float) = 0
        [HideInInspector] _Byte92 ("Byte 92", Float) = 0
        [HideInInspector] _Byte93 ("Byte 93", Float) = 0
        [HideInInspector] _Byte94 ("Byte 94", Float) = 0
        [HideInInspector] _Byte95 ("Byte 95", Float) = 0
        [HideInInspector] _Byte96 ("Byte 96", Float) = 0
        [HideInInspector] _Byte97 ("Byte 97", Float) = 0
        [HideInInspector] _Byte98 ("Byte 98", Float) = 0
        [HideInInspector] _Byte99 ("Byte 99", Float) = 0
        [HideInInspector] _Byte100 ("Byte 100", Float) = 0
        [HideInInspector] _Byte101 ("Byte 101", Float) = 0
        [HideInInspector] _Byte102 ("Byte 102", Float) = 0
        [HideInInspector] _Byte103 ("Byte 103", Float) = 0
        [HideInInspector] _Byte104 ("Byte 104", Float) = 0
        [HideInInspector] _Byte105 ("Byte 105", Float) = 0
        [HideInInspector] _Byte106 ("Byte 106", Float) = 0
        [HideInInspector] _Byte107 ("Byte 107", Float) = 0
        [HideInInspector] _Byte108 ("Byte 108", Float) = 0
        [HideInInspector] _Byte109 ("Byte 109", Float) = 0
        [HideInInspector] _Byte110 ("Byte 110", Float) = 0
        [HideInInspector] _Byte111 ("Byte 111", Float) = 0
        [HideInInspector] _Byte112 ("Byte 112", Float) = 0
        [HideInInspector] _Byte113 ("Byte 113", Float) = 0
        [HideInInspector] _Byte114 ("Byte 114", Float) = 0
        [HideInInspector] _Byte115 ("Byte 115", Float) = 0
        [HideInInspector] _Byte116 ("Byte 116", Float) = 0
        [HideInInspector] _Byte117 ("Byte 117", Float) = 0
        [HideInInspector] _Byte118 ("Byte 118", Float) = 0
        [HideInInspector] _Byte119 ("Byte 119", Float) = 0
        [HideInInspector] _Byte120 ("Byte 120", Float) = 0
        [HideInInspector] _Byte121 ("Byte 121", Float) = 0
        [HideInInspector] _Byte122 ("Byte 122", Float) = 0
        [HideInInspector] _Byte123 ("Byte 123", Float) = 0
        [HideInInspector] _Byte124 ("Byte 124", Float) = 0
        [HideInInspector] _Byte125 ("Byte 125", Float) = 0
        [HideInInspector] _Byte126 ("Byte 126", Float) = 0
        [HideInInspector] _Byte127 ("Byte 127", Float) = 0
        [HideInInspector] _Byte128 ("Byte 128", Float) = 0
        [HideInInspector] _Byte129 ("Byte 129", Float) = 0
        [HideInInspector] _Byte130 ("Byte 130", Float) = 0
        [HideInInspector] _Byte131 ("Byte 131", Float) = 0
        [HideInInspector] _Byte132 ("Byte 132", Float) = 0
        [HideInInspector] _Byte133 ("Byte 133", Float) = 0
        [HideInInspector] _Byte134 ("Byte 134", Float) = 0
        [HideInInspector] _Byte135 ("Byte 135", Float) = 0
        [HideInInspector] _Byte136 ("Byte 136", Float) = 0
        [HideInInspector] _Byte137 ("Byte 137", Float) = 0
        [HideInInspector] _Byte138 ("Byte 138", Float) = 0
        [HideInInspector] _Byte139 ("Byte 139", Float) = 0
        [HideInInspector] _Byte140 ("Byte 140", Float) = 0
        [HideInInspector] _Byte141 ("Byte 141", Float) = 0
        [HideInInspector] _Byte142 ("Byte 142", Float) = 0
        [HideInInspector] _Byte143 ("Byte 143", Float) = 0
        [HideInInspector] _Byte144 ("Byte 144", Float) = 0
        [HideInInspector] _Byte145 ("Byte 145", Float) = 0
        [HideInInspector] _Byte146 ("Byte 146", Float) = 0
        [HideInInspector] _Byte147 ("Byte 147", Float) = 0
        [HideInInspector] _Byte148 ("Byte 148", Float) = 0
        [HideInInspector] _Byte149 ("Byte 149", Float) = 0
        [HideInInspector] _Byte150 ("Byte 150", Float) = 0
        [HideInInspector] _Byte151 ("Byte 151", Float) = 0
        [HideInInspector] _Byte152 ("Byte 152", Float) = 0
        [HideInInspector] _Byte153 ("Byte 153", Float) = 0
        [HideInInspector] _Byte154 ("Byte 154", Float) = 0
        [HideInInspector] _Byte155 ("Byte 155", Float) = 0
        [HideInInspector] _Byte156 ("Byte 156", Float) = 0
        [HideInInspector] _Byte157 ("Byte 157", Float) = 0
        [HideInInspector] _Byte158 ("Byte 158", Float) = 0
        [HideInInspector] _Byte159 ("Byte 159", Float) = 0
        [HideInInspector] _Byte160 ("Byte 160", Float) = 0
        [HideInInspector] _Byte161 ("Byte 161", Float) = 0
        [HideInInspector] _Byte162 ("Byte 162", Float) = 0
        [HideInInspector] _Byte163 ("Byte 163", Float) = 0
        [HideInInspector] _Byte164 ("Byte 164", Float) = 0
        [HideInInspector] _Byte165 ("Byte 165", Float) = 0
        [HideInInspector] _Byte166 ("Byte 166", Float) = 0
        [HideInInspector] _Byte167 ("Byte 167", Float) = 0
        [HideInInspector] _Byte168 ("Byte 168", Float) = 0
        [HideInInspector] _Byte169 ("Byte 169", Float) = 0
        [HideInInspector] _Byte170 ("Byte 170", Float) = 0
        [HideInInspector] _Byte171 ("Byte 171", Float) = 0
        [HideInInspector] _Byte172 ("Byte 172", Float) = 0
        [HideInInspector] _Byte173 ("Byte 173", Float) = 0
        [HideInInspector] _Byte174 ("Byte 174", Float) = 0
        [HideInInspector] _Byte175 ("Byte 175", Float) = 0
        [HideInInspector] _Byte176 ("Byte 176", Float) = 0
        [HideInInspector] _Byte177 ("Byte 177", Float) = 0
        [HideInInspector] _Byte178 ("Byte 178", Float) = 0
        [HideInInspector] _Byte179 ("Byte 179", Float) = 0
        [HideInInspector] _Byte180 ("Byte 180", Float) = 0
        [HideInInspector] _Byte181 ("Byte 181", Float) = 0
        [HideInInspector] _Byte182 ("Byte 182", Float) = 0
        [HideInInspector] _Byte183 ("Byte 183", Float) = 0
        [HideInInspector] _Byte184 ("Byte 184", Float) = 0
        [HideInInspector] _Byte185 ("Byte 185", Float) = 0
        [HideInInspector] _Byte186 ("Byte 186", Float) = 0
        [HideInInspector] _Byte187 ("Byte 187", Float) = 0
        [HideInInspector] _Byte188 ("Byte 188", Float) = 0
        [HideInInspector] _Byte189 ("Byte 189", Float) = 0
        [HideInInspector] _Byte190 ("Byte 190", Float) = 0
        [HideInInspector] _Byte191 ("Byte 191", Float) = 0
        [HideInInspector] _Byte192 ("Byte 192", Float) = 0
        [HideInInspector] _Byte193 ("Byte 193", Float) = 0
        [HideInInspector] _Byte194 ("Byte 194", Float) = 0
        [HideInInspector] _Byte195 ("Byte 195", Float) = 0
        [HideInInspector] _Byte196 ("Byte 196", Float) = 0
        [HideInInspector] _Byte197 ("Byte 197", Float) = 0
        [HideInInspector] _Byte198 ("Byte 198", Float) = 0
        [HideInInspector] _Byte199 ("Byte 199", Float) = 0
        [HideInInspector] _Byte200 ("Byte 200", Float) = 0
        [HideInInspector] _Byte201 ("Byte 201", Float) = 0
        [HideInInspector] _Byte202 ("Byte 202", Float) = 0
        [HideInInspector] _Byte203 ("Byte 203", Float) = 0
        [HideInInspector] _Byte204 ("Byte 204", Float) = 0
        [HideInInspector] _Byte205 ("Byte 205", Float) = 0
        [HideInInspector] _Byte206 ("Byte 206", Float) = 0
        [HideInInspector] _Byte207 ("Byte 207", Float) = 0
        [HideInInspector] _Byte208 ("Byte 208", Float) = 0
        [HideInInspector] _Byte209 ("Byte 209", Float) = 0
        [HideInInspector] _Byte210 ("Byte 210", Float) = 0
        [HideInInspector] _Byte211 ("Byte 211", Float) = 0
        [HideInInspector] _Byte212 ("Byte 212", Float) = 0
        [HideInInspector] _Byte213 ("Byte 213", Float) = 0
        [HideInInspector] _Byte214 ("Byte 214", Float) = 0
        [HideInInspector] _Byte215 ("Byte 215", Float) = 0
        [HideInInspector] _Byte216 ("Byte 216", Float) = 0
        [HideInInspector] _Byte217 ("Byte 217", Float) = 0
        [HideInInspector] _Byte218 ("Byte 218", Float) = 0
        [HideInInspector] _Byte219 ("Byte 219", Float) = 0
        [HideInInspector] _Byte220 ("Byte 220", Float) = 0
        [HideInInspector] _Byte221 ("Byte 221", Float) = 0
        [HideInInspector] _Byte222 ("Byte 222", Float) = 0
        [HideInInspector] _Byte223 ("Byte 223", Float) = 0
        [HideInInspector] _Byte224 ("Byte 224", Float) = 0
        [HideInInspector] _Byte225 ("Byte 225", Float) = 0
        [HideInInspector] _Byte226 ("Byte 226", Float) = 0
        [HideInInspector] _Byte227 ("Byte 227", Float) = 0
        [HideInInspector] _Byte228 ("Byte 228", Float) = 0
        [HideInInspector] _Byte229 ("Byte 229", Float) = 0
        [HideInInspector] _Byte230 ("Byte 230", Float) = 0
        [HideInInspector] _Byte231 ("Byte 231", Float) = 0
        [HideInInspector] _Byte232 ("Byte 232", Float) = 0
        [HideInInspector] _Byte233 ("Byte 233", Float) = 0
        [HideInInspector] _Byte234 ("Byte 234", Float) = 0
        [HideInInspector] _Byte235 ("Byte 235", Float) = 0
        [HideInInspector] _Byte236 ("Byte 236", Float) = 0
        [HideInInspector] _Byte237 ("Byte 237", Float) = 0
        [HideInInspector] _Byte238 ("Byte 238", Float) = 0
        [HideInInspector] _Byte239 ("Byte 239", Float) = 0
        [HideInInspector] _Byte240 ("Byte 240", Float) = 0
        [HideInInspector] _Byte241 ("Byte 241", Float) = 0
        [HideInInspector] _Byte242 ("Byte 242", Float) = 0
        [HideInInspector] _Byte243 ("Byte 243", Float) = 0
        [HideInInspector] _Byte244 ("Byte 244", Float) = 0
        [HideInInspector] _Byte245 ("Byte 245", Float) = 0
        [HideInInspector] _Byte246 ("Byte 246", Float) = 0
        [HideInInspector] _Byte247 ("Byte 247", Float) = 0
        [HideInInspector] _Byte248 ("Byte 248", Float) = 0
        [HideInInspector] _Byte249 ("Byte 249", Float) = 0
        [HideInInspector] _Byte250 ("Byte 250", Float) = 0
        [HideInInspector] _Byte251 ("Byte 251", Float) = 0
        [HideInInspector] _Byte252 ("Byte 252", Float) = 0
        [HideInInspector] _Byte253 ("Byte 253", Float) = 0
        [HideInInspector] _Byte254 ("Byte 254", Float) = 0
        [HideInInspector] _Byte255 ("Byte 255", Float) = 0
        // bytes:end
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            sampler2D _Atlas, _GlyphMap;
            float4 _Color, _OutlineColor, _BackgroundColor, _BorderColor;
            float _Columns, _CellAspect, _Padding, _CornerRadius, _HideWhenEmpty, _BorderWidth, _FontSize, _OutlineWidth, _NaturalLayout;
            #include "XrtCaptionBytes.hlsl"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 cell : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 glyph : TEXCOORD1;
                float visible : TEXCOORD2;
                float2 size : TEXCOORD3;
                float2 boardUV : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float3 Lookup(float2 bytes)
            {
                bytes = floor(clamp(bytes, 0, 255) + 0.5);
                float2 mapUV = float2((bytes.y + 0.5) / 256, 1 - (bytes.x + 0.5) / 256);
                float3 entry = tex2Dlod(_GlyphMap, float4(mapUV, 0, 0)).rgb;
                float2 address = floor(entry.rg * 255 + 0.5);
                return float3(address.x * 256 + address.y, entry.b, 0);
            }
            float CodeAt(int index)
            {
                float2 bytes = XrtReadCell(index);
                return floor(bytes.x + 0.5) * 256 + floor(bytes.y + 0.5);
            }
            bool IsWord(float code)
            {
                return (code >= 48 && code <= 57) || (code >= 65 && code <= 90) ||
                    (code >= 97 && code <= 122) || (code >= 192 && code <= 687);
            }
            bool IsClosing(float code)
            {
                return code == 33 || code == 44 || code == 46 || code == 58 || code == 59 || code == 63 ||
                    code == 41 || code == 93 || code == 12289 || code == 12290 || code == 65281 ||
                    code == 65292 || code == 65306 || code == 65307 || code == 65311 || code == 65289;
            }
            float Variation(float seed)
            {
                // Stable for this text, independent of time and camera position.
                return frac(sin(seed * 12.9898 + .73) * 43758.5453) * 2 - 1;
            }
            float RoundedBox(float2 uv, float aspect)
            {
                float2 size = float2(aspect, 1);
                float radius = min(_CornerRadius, min(aspect, 1) * 0.5);
                float2 edge = abs(uv - 0.5) * size - (size * 0.5 - radius);
                return length(max(edge, 0)) + min(max(edge.x, edge.y), 0) - radius;
            }
            float2 NaturalPosition(uint index, float2 uv, float2 size)
            {
                // All vertices use the same bounded layout; no clock or random motion is involved.
                float codes[128], advances[128];
                uint first = 128, last = 0;
                float ink = 0;
                [loop] for (uint i = 0; i < 128; i++)
                {
                    codes[i] = CodeAt(i);
                    advances[i] = codes[i] == 0 ? 0 : Lookup(XrtReadCell(i)).y *
                        (1 + Variation(codes[i] * .017 + i * 5.31) * .035);
                    if (codes[i] != 0) { first = min(first, i); last = i; ink += advances[i]; }
                }
                float inset = (_Padding + _BorderWidth) * size.y;
                float2 area = max(size - 2 * inset, 0.00001);
                float spacing = max(_CellAspect, 1.1);
                float candidate = min(max(_FontSize, 0.00001),
                    sqrt(area.x * area.y / max(ink * spacing * 1.12, 1)));
                uint columns = (uint)clamp(floor(_Columns + .5), 8, 32);
                float word = 0, longestWord = 0;
                [loop] for (uint letter = first; letter <= last; letter++)
                {
                    if (letter % columns == 0 || !IsWord(codes[letter])) word = 0;
                    if (IsWord(codes[letter])) word += advances[letter];
                    longestWord = max(longestWord, word);
                }
                candidate = min(candidate, area.x / (longestWord + .75));
                uint row = 0, targetRow = 0;
                float pen = 0, targetPen = 0, targetWidth = 0, widest = 0, rowSeed = 0, targetSeed = 0;
                // Reflow after fitting so shrinking does not leave narrow, over-wrapped columns.
                [unroll] for (uint iteration = 0; iteration < 3; iteration++)
                {
                    row = 0; targetRow = 0; pen = 0; widest = 0; rowSeed = codes[first];
                    float limit = max(area.x / candidate - .75, 1);
                    float balanced = limit;
                    [loop] for (uint current = first; current <= last; current++)
                    {
                        bool newRow = current > first && current % columns == 0;
                        float code = codes[current];
                        float advance = advances[current];
                        if (current == first || newRow)
                        {
                            float sourceWidth = 0;
                            [loop] for (uint scan = current; scan < min(128u, (current / columns + 1) * columns); scan++)
                                sourceWidth += advances[scan];
                            balanced = min(limit, ceil(sourceWidth / max(ceil(sourceWidth / limit), 1)) + .15);
                        }
                        float wordWidth = advance;
                        bool startWord = IsWord(code) && (current % columns == 0 || !IsWord(codes[max((int)current - 1, 0)]));
                        if (startWord)
                        {
                            [loop] for (uint next = current + 1; next < min(128u, (current / columns + 1) * columns); next++)
                            {
                                if (!IsWord(codes[next])) break;
                                wordWidth += advances[next];
                            }
                        }
                        bool wrap = code != 0 && code != 32 && pen > 0 && !IsClosing(code) &&
                            ((!IsWord(code) && pen + advance > balanced) ||
                             (startWord && pen + wordWidth > balanced));
                        if ((newRow && pen > 0) || wrap)
                        {
                            if (targetRow == row) targetWidth = pen;
                            widest = max(widest, pen);
                            row++; pen = 0; rowSeed = code;
                        }
                        if (current == index) { targetRow = row; targetPen = pen; targetSeed = rowSeed; }
                        if (code != 32 || pen > 0) pen += advance;
                    }
                    if (targetRow == row) targetWidth = pen;
                    widest = max(widest, pen);
                    candidate = min(candidate, min(area.x / (widest + 1),
                        area.y / ((row + 1) * spacing + .4 + widest * .026)));
                }
                float font = candidate;
                float spare = max(0, area.x - (targetWidth + 1) * font);
                float seed = targetSeed * .019 + targetRow * 3.7;
                float offset = Variation(seed) * min(font * .22, spare * .18);
                float scale = 1 + Variation(codes[index] * .031 + index * 7.13) * .06;
                // Rotate each letter around its own center, keeping its place in the line.
                float letterAngle = Variation(codes[index] * .043 + index * 11.79 + 29) * .07;
                float letterSine, letterCosine; sincos(letterAngle, letterSine, letterCosine);
                float2 corner = (uv - .5) * scale;
                float2 local = float2(targetPen - targetWidth * .5 + advances[index] * .5, 0) +
                    float2(letterCosine * corner.x - letterSine * corner.y,
                        letterSine * corner.x + letterCosine * corner.y);
                float angle = Variation(seed + 11) * .026;
                float sine, cosine; sincos(angle, sine, cosine);
                float2 glyphPosition = float2(cosine * local.x - sine * local.y, sine * local.x + cosine * local.y) * font;
                glyphPosition += float2(offset, ((row * .5 - targetRow) * spacing + Variation(seed + 23) * .045) * font);
                return size * .5 + glyphPosition;
            }
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = input.uv;
                output.visible = 1;
                output.glyph = float3(0, 0, input.cell.x);
                // Z is the scale reference: X/Y resize the surface, uniform XYZ scaling zooms it.
                float reference = max(length(unity_ObjectToWorld._m02_m12_m22), 0.00001);
                output.size = max(float2(length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21)) / reference, 0.00001);
                float4 position = input.vertex;
                [branch] if (input.cell.x < 0)
                {
                    // An empty buffer also hides its background, once per board vertex.
                    float total = XrtByteTotal();
                    output.visible = lerp(1, step(0.5, total), saturate(_HideWhenEmpty));
                }
                else if (CodeAt((int)input.cell.x) == 0)
                {
                    position.xy = 0;
                    output.visible = 0;
                }
                else
                {
                    uint index = (uint)input.cell.x;
                    [branch] if (_NaturalLayout > .5)
                    {
                        float3 glyph = Lookup(XrtReadCell(index));
                        position.xy = NaturalPosition(index, input.uv, output.size) / output.size - .5;
                        output.glyph.xy = float2(fmod(glyph.x, 128), floor(glyph.x / 128));
                        output.boardUV = position.xy + .5;
                        output.vertex = UnityObjectToClipPos(position);
                        return output;
                    }
                    uint columns = (uint)clamp(floor(_Columns + 0.5), 8, 32);
                    float inset = (_Padding + _BorderWidth) * output.size.y;
                    float width = max(output.size.x - 2 * inset, 0.00001);
                    float fontSize = max(_FontSize, 0.00001);
                    float lineHeight = fontSize * max(_CellAspect, 1);
                    float2 pen = 0;
                    // Source rows retain explicit breaks and header/footer separation.
                    // Additional wrapping depends only on this object's current width.
                    [loop] for (uint current = 0; current <= index; current++)
                    {
                        if (current > 0 && current % columns == 0) { pen.x = 0; pen.y += lineHeight; }
                        float code = CodeAt(current);
                        if (code == 0) continue;
                        float advance = Lookup(XrtReadCell(current)).y * fontSize;
                        if (IsWord(code) && (current % columns == 0 || !IsWord(CodeAt(current - 1))))
                        {
                            float wordWidth = advance;
                            [loop] for (uint next = current + 1; next < min(128u, (current / columns + 1) * columns); next++)
                            {
                                if (!IsWord(CodeAt(next))) break;
                                wordWidth += Lookup(XrtReadCell(next)).y * fontSize;
                            }
                            if (wordWidth <= width && pen.x > 0 && pen.x + wordWidth > width)
                            { pen.x = 0; pen.y += lineHeight; }
                        }
                        if (pen.x > 0 && pen.x + advance > width)
                        { pen.x = 0; pen.y += lineHeight; }
                        if (current == index) break;
                        if (code != 32 || pen.x > 0) pen.x += advance;
                    }
                    float3 glyph = Lookup(XrtReadCell(index));
                    float2 glyphPosition = float2(inset + pen.x + (glyph.y * 0.5 + input.uv.x - 0.5) * fontSize,
                        output.size.y - inset - pen.y - lineHeight * 0.5 + (input.uv.y - 0.5) * fontSize);
                    position.xy = glyphPosition / output.size - 0.5;
                    output.visible = step(0.5, CodeAt(index)) * step(pen.y + lineHeight, output.size.y - 2 * inset + 0.00001);
                    output.glyph.xy = float2(fmod(glyph.x, 128), floor(glyph.x / 128));
                }
                output.boardUV = position.xy + 0.5;
                output.vertex = UnityObjectToClipPos(position);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                [branch] if (input.glyph.z < 0)
                {
                    float distance = RoundedBox(input.boardUV, input.size.x / input.size.y);
                    float softness = max(fwidth(distance), 0.0001);
                    float coverage = saturate(0.5 - distance / softness) * input.visible;
                    float border = _BorderWidth > 0 ? saturate(0.5 + (distance + _BorderWidth) / softness) : 0;
                    float4 background = float4(_BackgroundColor.rgb * _BackgroundColor.a, _BackgroundColor.a);
                    float4 frame = float4(_BorderColor.rgb * _BorderColor.a, _BorderColor.a);
                    return lerp(background, frame, border) * coverage;
                }
                float inset = _Padding + _BorderWidth;
                float2 margin = float2(inset * input.size.y / input.size.x, inset);
                clip(min(input.boardUV - margin, 1 - margin - input.boardUV));
                clip(-RoundedBox(input.boardUV, input.size.x / input.size.y));
                float2 atlasUV = (input.glyph.xy + float2(input.uv.x, 1 - input.uv.y)) / 128;
                atlasUV.y = 1 - atlasUV.y;
                float distance = tex2D(_Atlas, atlasUV).r;
                float softness = max(fwidth(distance), 1.0 / 255);
                float coverage = smoothstep(0.5 - softness * 0.5, 0.5 + softness * 0.5, distance);
                float outline = smoothstep(0.5 - _OutlineWidth - softness * 0.5,
                    0.5 - _OutlineWidth + softness * 0.5, distance) * step(0.0001, _OutlineWidth);
                float4 text = float4(_Color.rgb * _Color.a, _Color.a) * coverage;
                float4 edge = float4(_OutlineColor.rgb * _OutlineColor.a, _OutlineColor.a) * outline;
                return (text + edge * (1 - text.a)) * input.visible;
            }
            ENDCG
        }
    }
    Fallback Off
}
