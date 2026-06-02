Shader "Laboratory/Containment Pulse"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0.03, 0.05, 0.08, 1)
        _BandColor ("Band Emission Color", Color) = (0.2, 1, 0.55, 1)
        _BandCount ("Band Count", Float) = 10
        _BandSharpness ("Band Sharpness", Range(1, 20)) = 8
        _PulseSpeed ("Pulse Speed", Float) = 0.6
        _PulseIntensity ("Pulse Intensity", Range(0, 3)) = 1.2
        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.7
        _Metallic ("Metallic", Range(0, 1)) = 0.5
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
                float4 _BandColor;
                float _BandCount;
                float _BandSharpness;
                float _PulseSpeed;
                float _PulseIntensity;
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
                float v = input.positionOS.y * _BandCount + _Time.y * _PulseSpeed;
                float wave = frac(v);
                float band = pow(saturate(1.0 - abs(wave - 0.5) * 2.0), _BandSharpness);
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2831853);
                half3 emission = _BandColor.rgb * band * _PulseIntensity * pulse;
                half3 albedo = _BaseColor.rgb + emission;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lit = LabApplyLighting(albedo, input.positionWS, normalize(input.normalWS), mainLight.shadowAttenuation);

                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDir)), 3.0);
                lit += fresnel * _BandColor.rgb * 0.35 * pulse;

                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
