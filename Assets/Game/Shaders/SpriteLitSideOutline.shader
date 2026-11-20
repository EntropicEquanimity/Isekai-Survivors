Shader "Isekai/Sprite Lit Side Outline"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _OutlineColor("Outline Color", Color) = (0.24,0.86,0.4,1)
        _OutlineSourceColor("Outline Source Color", Color) = (0.1215686,0.1019608,0.1019608,1)
        _OutlineTolerance("Outline Source Tolerance", Range(0, 0.2)) = 0.04
        _DarkThreshold("Dark Pixel Threshold", Range(0, 0.4)) = 0.18
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties, kept for sprite compatibility.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half4 _OutlineSourceColor;
                half _OutlineTolerance;
                half _DarkThreshold;
            CBUFFER_END

            float2 TexelOf(Texture2D tex)
            {
                float w, h;
                tex.GetDimensions(w, h);
                return 1.0 / max(float2(w, h), 1.0);
            }

            bool IsOutlinePixel(float2 uv, half4 mainTex)
            {
                if (mainTex.a <= 0.001h) return false;

                // The texel size comes from the bound texture itself: a sprite's texture is set per renderer, and a
                // uniform declared outside the material's constant buffer is not refreshed for each SRP-batched draw.
                float2 texel = TexelOf(_MainTex);
                float2 stepX = float2(texel.x, 0);
                float2 stepY = float2(0, texel.y);
                bool nearTransparent =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + stepX).a <= 0.001h ||
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - stepX).a <= 0.001h ||
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + stepY).a <= 0.001h ||
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - stepY).a <= 0.001h;

                return nearTransparent;
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS);
#if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(input.positionOS);
                o.normalWS = TransformObjectToWorldDir(input.normal);
#endif
                o.uv = input.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 raw = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 main = input.color * raw;
                if (IsOutlinePixel(input.uv, raw)) main.rgb = _OutlineColor.rgb;

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

#if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = input.normalWS;
#endif
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half4 _OutlineSourceColor;
                half _OutlineTolerance;
                half _DarkThreshold;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half4 _OutlineSourceColor;
                half _OutlineTolerance;
                half _DarkThreshold;
            CBUFFER_END

            float2 TexelOf(Texture2D tex)
            {
                float w, h;
                tex.GetDimensions(w, h);
                return 1.0 / max(float2(w, h), 1.0);
            }

            bool IsOutlinePixel(float2 uv, half4 mainTex)
            {
                if (mainTex.a <= 0.001h) return false;

                // The texel size comes from the bound texture itself: a sprite's texture is set per renderer, and a
                // uniform declared outside the material's constant buffer is not refreshed for each SRP-batched draw.
                float2 texel = TexelOf(_MainTex);
                float2 stepX = float2(texel.x, 0);
                float2 stepY = float2(0, texel.y);
                bool nearTransparent =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + stepX).a <= 0.001h ||
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - stepX).a <= 0.001h ||
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + stepY).a <= 0.001h ||
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - stepY).a <= 0.001h;

                return nearTransparent;
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                half4 raw = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 main = input.color * raw;
                if (IsOutlinePixel(input.uv, raw)) main.rgb = _OutlineColor.rgb;

#if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;
                InitializeSurfaceData(main.rgb, main.a, surfaceData);
                InitializeInputData(input.uv, inputData);
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor)) return debugColor;
#endif
                return main;
            }
            ENDHLSL
        }
    }
}
