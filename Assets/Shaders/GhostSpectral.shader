Shader "Custom/GhostSpectral"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GhostColor ("Ghost Color", Color) = (0.5, 0.8, 1.0, 0.5)
        _FresnelPower ("Fresnel Power", Range(0.1, 5.0)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1.5
        _AlphaBase ("Base Alpha", Range(0, 1)) = 0.3
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.1
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _DistortionSpeed ("Distortion Speed", Range(0, 5)) = 1.0
        _FadeOut ("Fade Out", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 200
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert nofog
        #pragma target 3.0

        sampler2D _MainTex;

        fixed4 _GhostColor;
        half _FresnelPower;
        half _FresnelIntensity;
        half _AlphaBase;
        half _PulseSpeed;
        half _PulseAmount;
        half _DistortionStrength;
        half _DistortionSpeed;
        half _FadeOut;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v)
        {
            float wave = sin(_Time.y * _DistortionSpeed + v.vertex.y * 3.0) * _DistortionStrength;
            v.vertex.xyz += normalize(v.normal) * wave;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 texCol = tex2D(_MainTex, IN.uv_MainTex);

            float3 n = normalize(IN.worldNormal);
            float3 v = normalize(IN.viewDir);
            half fresnel = pow(1.0h - saturate(dot(n, v)), max(_FresnelPower, 0.001h)) * _FresnelIntensity;

            half pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
            half alpha = saturate(_AlphaBase + pulse + fresnel * _GhostColor.a);

            fixed3 baseGhost = texCol.rgb * _GhostColor.rgb;
            fixed3 finalColor = lerp(baseGhost, _GhostColor.rgb, fresnel);
            finalColor += _GhostColor.rgb * fresnel * 0.5;

            o.Albedo = finalColor;
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Emission = finalColor * fresnel * 0.15 * _FadeOut;
            o.Alpha = alpha * _FadeOut;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
