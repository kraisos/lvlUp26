Shader "Custom/NeoGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (0, 0.8, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.0
        _FresnelPower ("Edge Width", Range(0.5, 8)) = 2.5
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.6
        _OutlineThickness ("Outline Thickness", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "NeoGlow"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _GlowIntensity;
                half _FresnelPower;
                half _PulseSpeed;
                half _PulseMin;
                half _OutlineThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Push vertices along normal for slight outline expansion
                float3 expandedPos = input.positionOS.xyz + input.normalOS * _OutlineThickness;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(expandedPos);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Fresnel / rim effect - strongest at edges
                float NdotV = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Pulse animation
                float pulse = lerp(_PulseMin, 1.0, (sin(_Time.y * _PulseSpeed * 3.14159) * 0.5 + 0.5));

                // Final glow
                half3 glowColor = _GlowColor.rgb * _GlowIntensity * fresnel * pulse;
                half glowAlpha = fresnel * pulse * _GlowColor.a;

                // Apply fog
                glowColor = MixFog(glowColor, input.fogFactor);

                return half4(glowColor, glowAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}