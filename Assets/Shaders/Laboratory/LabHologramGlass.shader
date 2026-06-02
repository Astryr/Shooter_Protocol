Shader "Laboratory/Hologram Glass"
{
    Properties
    {
        [Header(Glass)]
        _BaseColor ("Tint Color", Color) = (0.15, 0.75, 1, 0.25)
        _RimColor ("Rim Color", Color) = (0.4, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _Alpha ("Alpha", Range(0, 1)) = 0.35
        [Header(Scanlines)]
        _ScanlineDensity ("Scanline Density", Float) = 80
        _ScanlineSpeed ("Scanline Speed", Float) = 1.5
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "LabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _Alpha;
                float _ScanlineDensity;
                float _ScanlineSpeed;
                float _ScanlineIntensity;
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
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _RimPower);

                float scan = sin((input.positionOS.y + _Time.y * _ScanlineSpeed) * _ScanlineDensity) * 0.5 + 0.5;
                scan = lerp(1.0, scan, _ScanlineIntensity);

                half3 col = _BaseColor.rgb * scan + _RimColor.rgb * fresnel;
                half alpha = saturate(_Alpha * _BaseColor.a + fresnel * 0.5) * scan;

                col = MixFog(col, input.fogFactor);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
