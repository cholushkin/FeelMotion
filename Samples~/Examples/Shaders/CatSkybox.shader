Shader "FeelMotion/URPCloudSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _TopColor("Top Color (HDR)", Color) = (0.2, 0.4, 0.8, 1)
        _BottomColor("Bottom Color (HDR)", Color) = (0.9, 0.6, 0.4, 1)
        _GradientPower("Gradient Power", Range(0.1, 5)) = 1.0

        [Header(Clouds)]
        _CloudColor("Cloud Color (HDR)", Color) = (1, 1, 1, 1)
        _CloudDensity("Cloud Density", Range(0, 1)) = 0.5
        _CloudScale("Cloud Scale", Range(0.1, 10)) = 2.0
        _CloudSpeed("Cloud Scroll Speed", Float) = 0.05
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Background" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }
        
        LOD 100
        ZWrite On
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "SkyboxPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                float _GradientPower;
                float4 _CloudColor;
                float _CloudDensity;
                float _CloudScale;
                float _CloudSpeed;
            CBUFFER_END

            // Простая процедурная функция шума (без внешних текстур)
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.45);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Плавное сглаживание (smoothstep)

                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Фрактальный шум (FBM) для пушистых облаков
            float FBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * Noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Рендерим скайбокс на дальнем плане
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                
                // Передаем направление взгляда (позицию вершины на сфере)
                o.viewDir = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Нормализуем вектор направления
                float3 dir = normalize(i.viewDir);

                // 1. Вертикальный градиент неба (на основе высоты Y от -1 до 1)
                float height = dir.y * 0.5 + 0.5; // переводим в диапазон 0...1
                float gradientFactor = pow(height, _GradientPower);
                half4 skyColor = lerp(_BottomColor, _TopColor, gradientFactor);

                // 2. Генерация облаков по сферическим координатам (X и Z) с прокруткой по времени
                if (dir.y > -0.1) // Рисуем облака только выше нижней полусферы
                {
                    float2 uv = dir.xz / (dir.y + 0.4); // Искажение перспективы для неба
                    uv *= _CloudScale;
                    
                    // Анимация движения облаков по горизонтали
                    uv.x += _Time.y * _CloudSpeed;

                    float n = FBM(uv);
                    
                    // Вырезаем плотность облаков
                    float cloudMask = saturate((n - (1.0 - _CloudDensity)) / max(0.01, _CloudDensity));
                    
                    // Плавное затухание облаков у самого низа горизонта
                    float horizonFade = saturate(dir.y * 5.0);
                    cloudMask *= horizonFade;

                    // Накладываем облака поверх неба
                    skyColor = lerp(skyColor, _CloudColor, cloudMask * _CloudColor.a);
                }

                return skyColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}