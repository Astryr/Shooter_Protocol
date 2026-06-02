Shader "Laboratory/Grid Pillar"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0.08, 0.12, 0.16, 1)
        _GridColor ("Grid Emission Color", Color) = (0.1, 0.85, 1, 1)
        _GridScale ("Grid Scale", Float) = 6
        _LineWidth ("Line Width", Range(0.5, 4)) = 1.5
        _ScrollSpeed ("Scroll Speed (Y)", Float) = 0.15
        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.55
        _Metallic ("Metallic", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "LabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridScale;
                float _LineWidth;
                float _ScrollSpeed;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.positionOS = input.positionOS.xyz;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = LabCylindricalUV(input.positionOS, _GridScale);
                uv.y += _Time.y * _ScrollSpeed;

                float grid = LabGridMask(uv, _LineWidth);
                half3 baseCol = _BaseColor.rgb;
                half3 gridCol = _GridColor.rgb * grid;
                half3 albedo = baseCol + gridCol;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shadow = mainLight.shadowAttenuation;
                half3 lit = LabApplyLighting(albedo, input.positionWS, normalize(input.normalWS), shadow);

                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half spec = pow(saturate(dot(normalize(input.normalWS), halfDir)), lerp(8.0, 128.0, _Smoothness));
                lit += spec * _Smoothness * mainLight.color * shadow;

                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
