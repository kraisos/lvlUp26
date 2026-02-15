Shader "Custom/SanityDistortionold"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _Intensity ("Distortion Intensity", Range(0, 1)) = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                // Nether-portal-style swirl: layered sine waves at different scales
                float2 center = uv - 0.5;
                float dist = length(center);

                // Slow, large warps + faster smaller warps
                float warpStrength = _Intensity * 0.04;
                float2 warp = float2(0, 0);
                warp.x += sin(uv.y * 10.0 + t * 1.5) * warpStrength;
                warp.y += cos(uv.x * 10.0 + t * 1.3) * warpStrength;
                warp.x += sin(uv.y * 25.0 + t * 3.0) * warpStrength * 0.4;
                warp.y += cos(uv.x * 25.0 + t * 2.7) * warpStrength * 0.4;

                // Radial twist (spins slightly toward edges)
                float twist = sin(dist * 12.0 - t * 2.0) * _Intensity * 0.02;
                float s = sin(twist);
                float c = cos(twist);
                float2 twisted = float2(
                    center.x * c - center.y * s,
                    center.x * s + center.y * c
                );
                float2 twistOffset = twisted - center;

                float2 finalUV = uv + warp + twistOffset;

                // Chromatic aberration: split RGB channels slightly
                float chromaOffset = _Intensity * 0.008;
                float2 chromaDir = normalize(center + 0.001);
                fixed r = tex2D(_MainTex, finalUV + chromaDir * chromaOffset).r;
                fixed g = tex2D(_MainTex, finalUV).g;
                fixed b = tex2D(_MainTex, finalUV - chromaDir * chromaOffset).b;

                // Slight purple tint at high intensity
                fixed3 tint = lerp(fixed3(1, 1, 1), fixed3(0.85, 0.7, 1.0), _Intensity * 0.4);

                return fixed4(r * tint.r, g * tint.g, b * tint.b, 1.0);
            }

            ENDCG
        }
    }
}
