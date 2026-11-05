Shader "UI/Rounded Corners"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Параметры скругления
        _Radius ("Corner Radius (Pixels)", Range(0, 500)) = 20
        _Softness ("Edge Softness", Range(0, 10)) = 1.0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
     
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                half2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Radius;
            float _Softness;
            
            // ОБЯЗАТЕЛЬНОЕ ОБЪЯВЛЕНИЕ РАЗМЕРА ТЕКСТУРЫ ДЛЯ РАБОТЫ В FRAGMENT SHADER
            float4 _MainTex_TexelSize; 

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                
                #ifdef UNITY_HALF_TEXEL_OFFSET
                OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
                #endif
                
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex; 

            fixed4 frag(v2f IN) : SV_Target
            {
                // Получаем размер текстуры в пикселях из объявленной переменной
                float2 textureSize = _MainTex_TexelSize.zw; // width, height in pixels
                
                // Переводим UV координаты в пиксельные
                float2 pixelCoord = IN.texcoord * textureSize;
                
                // Вычисляем расстояние до ближайшего края для каждого угла
                float distLeft = pixelCoord.x;
                float distRight = textureSize.x - pixelCoord.x;
                float distTop = textureSize.y - pixelCoord.y;
                float distBottom = pixelCoord.y;
                
                // Минимальное расстояние до любого края определяет, находимся ли мы в углу
                float minDistX = min(distLeft, distRight);
                float minDistY = min(distTop, distBottom);
                
                // Если мы не в зоне влияния радиуса (не в углу), оставляем непрозрачность 1
                if (minDistX > _Radius || minDistY > _Radius)
                {
                    half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    clip(color.a);
                    return color;
                }
                
                // Мы в углу — рассчитываем дистанцию от центра скругленного угла
                float cornerCenterX = (distLeft < distRight) ? _Radius : (textureSize.x - _Radius);
                float cornerCenterY = (distTop < distBottom) ? (textureSize.y - _Radius) : _Radius;
                
                float dx = pixelCoord.x - cornerCenterX;
                float dy = pixelCoord.y - cornerCenterY;
                
                float distanceFromCornerCenter = sqrt(dx*dx + dy*dy);
                
                // Считаем альфу на основе расстояния и мягкости
                float alpha = saturate((_Radius - distanceFromCornerCenter) / max(_Softness, 0.001));
                
                half4 finalColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                finalColor.a *= alpha;
                
                // Применяем клип-рект
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                #ifdef UNITY_UI_CLIP_RECT
                clip(finalColor.a);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}