Shader "UI/GitBankGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Gradient)]
        _GradientColor1 ("Gradient Color 1", Color) = (1,1,1,1)
        _GradientColor2 ("Gradient Color 2", Color) = (0,0,0,1)

        [Space]
        _Angle ("Angle", Range(-180, 180)) = 0
        _Offset ("Offset", Range(0, 1)) = 0.5
        _Softness ("Softness", Range(0.01, 1)) = 0.5

        [Space]
        [Toggle] _UseGradient ("Use Gradient", Float) = 1
        [Toggle] _AffectAlpha ("Affect Alpha", Float) = 0

        [Header(UI Compatibility)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Header(Stencil)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIGitBankGradient"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_UI_CLIP_RECT
            #pragma multi_compile _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;

            float4 _Color;
            float4 _GradientColor1;
            float4 _GradientColor2;

            float _Angle;
            float _Offset;
            float _Softness;
            float _UseGradient;
            float _AffectAlpha;

            float4 _ClipRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                if (_UseGradient > 0.5)
                {
                    float rad = _Angle * (3.141592653589793 / 180.0);
                    float2 dir = float2(cos(rad), sin(rad));

                    float2 p = i.uv - 0.5;

                    float maxProj = 0.5 * (abs(dir.x) + abs(dir.y));
                    float t = 0.5;

                    if (maxProj > 0.0001)
                    {
                        t = clamp(0.5 + 0.5 * (dot(p, dir) / maxProj), 0.0, 1.0);
                    }

                    float lower = _Offset - _Softness * 0.5;
                    float upper = _Offset + _Softness * 0.5;

                    float g = smoothstep(lower, upper, t);
                    float4 grad = lerp(_GradientColor1, _GradientColor2, g);

                    if (_AffectAlpha > 0.5)
                    {
                        col *= grad;
                    }
                    else
                    {
                        col.rgb *= grad.rgb;
                    }
                }

                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}