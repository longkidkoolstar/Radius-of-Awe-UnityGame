// Unity Shader: Sprites-WonderMask
// A custom 2D Sprite shader that reveals pixels only within the Wonder Radius.
// Uses global shader properties _WonderCenter, _WonderRadius, and _WonderFeather
// set by the WonderRadiusController C# script.
//
// Usage:
//   1. Create a Material using this shader (Sprites/WonderMask).
//   2. Assign it to SpriteRenderers on "Wonder" layer objects.
//   3. The WonderRadiusController script will feed _WonderCenter / _WonderRadius
//      as global shader properties every frame.
//   4. Pixels outside the radius are discarded; pixels at the edge are feathered.

Shader "Sprites/WonderMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _Feather ("Edge Feather", Range(0.01, 2.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            // Per-material properties
            sampler2D _MainTex;
            fixed4 _Color;
            float _Feather;

            // Global properties (set by WonderRadiusController.cs every frame)
            float4 _WonderCenter;   // xy = world position
            float  _WonderRadius;   // world-space radius
            float  _WonderFeather;  // override feather from C# (optional)

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample the sprite texture
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Calculate world-space distance from Wonder center
                float2 diff = IN.worldPos.xy - _WonderCenter.xy;
                float dist = length(diff);

                // Use the C#-provided feather if available, otherwise use material feather
                float featherVal = _WonderFeather > 0 ? _WonderFeather : _Feather;

                // Smooth falloff: 1 inside, 0 outside, smooth transition at edge
                float mask = 1.0 - smoothstep(_WonderRadius - featherVal, _WonderRadius, dist);

                // Apply mask to alpha
                col.a *= mask;

                // Discard fully transparent pixels to avoid blending artifacts
                if (col.a < 0.003)
                    discard;

                // Premultiply alpha for correct sprite blending
                col.rgb *= col.a;

                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
