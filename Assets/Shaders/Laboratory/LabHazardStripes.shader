Shader "Laboratory/Hazard Stripes"
{
    Properties
    {
        [Header(Stripes)]
        _ColorA ("Stripe Color A", Color) = (0.95, 0.75, 0.05, 1)
        _ColorB ("Stripe Color B", Color) = (0.05, 0.05, 0.06, 1)
        _StripeScale ("Stripe Scale", Float) = 14
        _StripeWidth ("Stripe Width", Range(0.2, 2)) = 0.85
        _StripeAngle ("Angle (degrees)", Range(0, 180)) = 35
        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.25
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
                float4 _ColorA;
                float4 _ColorB;
                float _StripeScale;
                float _StripeWidth;
                float _StripeAngle;
                float _Smoothness;
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
                float2 uv = LabCylindricalUV(input.positionOS, 1.0);
                float angleRad = _StripeAngle * 0.01745329252;
                float mask = LabDiagonalStripes(uv, angleRad, _StripeScale, _StripeWidth);
                half3 albedo = lerp(_ColorB.rgb, _ColorA.rgb, mask);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lit = LabApplyLighting(albedo, input.positionWS, normalize(input.normalWS), mainLight.shadowAttenuation);
                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
