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
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GhostColor;
            float _FresnelPower;
            float _FresnelIntensity;
            float _AlphaBase;
            float _PulseSpeed;
            float _PulseAmount;
            float _DistortionStrength;
            float _DistortionSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Vertex displacement for wavering effect
                float wave = sin(_Time.y * _DistortionSpeed + v.vertex.y * 3.0) * _DistortionStrength;
                float3 displaced = v.vertex.xyz + v.normal * wave;
                
                o.vertex = UnityObjectToClipPos(float4(displaced, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(WorldSpaceViewDir(v.vertex));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample base texture
                fixed4 texCol = tex2D(_MainTex, i.uv);
                
                // Fresnel (rim lighting)
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.worldViewDir);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                fresnel *= _FresnelIntensity;
                
                // Pulsing alpha
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float alpha = _AlphaBase + pulse + fresnel * _GhostColor.a;
                alpha = saturate(alpha);
                
                // Final color: blend texture with ghost color, boost edges
                float3 finalColor = lerp(texCol.rgb * _GhostColor.rgb, _GhostColor.rgb, fresnel);
                finalColor += _GhostColor.rgb * fresnel * 0.5; // Extra glow on edges
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
