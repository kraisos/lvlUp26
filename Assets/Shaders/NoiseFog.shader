Shader "Custom/NoiseFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.7, 0.75, 0.8, 1)
        _Density ("Density", Range(0, 1)) = 0.5
        _NoiseScale ("Noise Scale", Range(0.1, 20)) = 3.0
        _ScrollSpeed ("Scroll Speed", Range(0, 2)) = 0.3
        _FadeRadius ("Fade Radius", Range(0, 1)) = 0.8
        _MaskEdgeSoftness ("Mask Edge Softness", Range(0.001, 2.0)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float3 worldPos : TEXCOORD1;
            };

            float4 _FogColor;
            float _Density;
            float _NoiseScale;
            float _ScrollSpeed;
            float _FadeRadius;
            float _MaskEdgeSoftness;

            // Mask volume globals (set by MaskVolume.cs)
            #define MAX_VOLUMES 16
            float4x4 _MaskMatrices[MAX_VOLUMES];
            float4   _MaskShapeData[MAX_VOLUMES];
            int      _MaskCount;

            float DistanceToBoxEdge(float3 localPos)
            {
                float3 d = abs(localPos) - 0.5;
                return -max(d.x, max(d.y, d.z));
            }

            float DistanceToSphereEdge(float3 localPos)
            {
                return 0.5 - length(localPos);
            }

            float DistanceToConeEdge(float3 localPos, float coneTopRadius)
            {
                float yDist = min(localPos.y + 0.5, 0.5 - localPos.y);
                float t = localPos.y + 0.5;
                float radiusAtHeight = lerp(0.5, 0.5 * coneTopRadius, t);
                float radialDist = radiusAtHeight - length(localPos.xz);
                if (localPos.y < -0.5 || localPos.y > 0.5)
                    return yDist;
                return min(yDist, radialDist);
            }

            float GetMaxDistanceInsideMasks(float3 worldPos)
            {
                float maxDist = -999.0;
                for (int idx = 0; idx < _MaskCount; idx++)
                {
                    int shape = (int)_MaskShapeData[idx].x;
                    if (shape < 0) continue;
                    float3 localPos = mul(_MaskMatrices[idx], float4(worldPos, 1.0)).xyz;
                    float d = -999.0;
                    if (shape == 0) d = DistanceToBoxEdge(localPos);
                    else if (shape == 1) d = DistanceToSphereEdge(localPos);
                    else if (shape == 2) d = DistanceToConeEdge(localPos, _MaskShapeData[idx].y);
                    maxDist = max(maxDist, d);
                }
                return maxDist;
            }

            // Hash function for procedural noise
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Value noise
            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = frac(sin(dot(i + float2(0, 0), float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion — two octaves
            float fbm(float2 p)
            {
                float v = 0.0;
                v += 0.6 * vnoise(p);
                v += 0.4 * vnoise(p * 2.03 + float2(1.7, 9.2));
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Mask volume clipping
                float maskDist = GetMaxDistanceInsideMasks(i.worldPos);
                if (maskDist < 0)
                    discard;
                float maskFade = saturate(maskDist / _MaskEdgeSoftness);

                float t = _Time.y * _ScrollSpeed;

                // Two noise layers at different scales and scroll directions
                float2 coords1 = i.worldPos.xz * _NoiseScale * 0.1 + float2(t, t * 0.7);
                float2 coords2 = i.worldPos.xz * _NoiseScale * 0.05 + float2(-t * 0.5, t * 0.3);

                float n1 = fbm(coords1);
                float n2 = fbm(coords2);
                float noise = saturate(n1 * 0.6 + n2 * 0.4);

                // Radial fade from center of the quad
                float2 centered = i.uv - 0.5;
                float dist = length(centered) * 2.0;
                float fade = 1.0 - smoothstep(_FadeRadius, 1.0, dist);

                float alpha = noise * _Density * fade * maskFade;
                return float4(_FogColor.rgb, alpha);
            }

            ENDCG
        }
    }
}
