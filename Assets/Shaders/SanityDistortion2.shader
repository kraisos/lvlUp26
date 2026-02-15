Shader "Custom/SanityDistortion"
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

            // Procedural hash-based noise (no texture needed)
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Value noise with smooth interpolation
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion - layered noise for organic shapes
            float fbm(float2 p)
            {
                float val = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 5; i++)
                {
                    val += amp * valueNoise(p * freq);
                    freq *= 2.17;
                    amp *= 0.5;
                    p += float2(1.7, 9.2); // domain shift per octave
                }
                return val;
            }

            // Ridged FBM - creates sharp crease lines that look like tentacles
            float ridgedFbm(float2 p)
            {
                float val = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                float prev = 1.0;
                for (int i = 0; i < 5; i++)
                {
                    float n = valueNoise(p * freq);
                    n = 1.0 - abs(n * 2.0 - 1.0); // ridge: sharp creases
                    n = n * n;                       // sharpen further
                    n *= prev;                       // successive ridges narrow
                    prev = n;
                    val += n * amp;
                    freq *= 2.13;
                    amp *= 0.55;
                    p += float2(2.3, 5.1);
                }
                return val;
            }

            // Domain-warped tentacle pattern
            float tentacleNoise(float2 p, float time)
            {
                // First warp: use fbm to distort coordinates (creates organic flow)
                float2 q = float2(
                    fbm(p + float2(0.0, 0.0) + time * 0.15),
                    fbm(p + float2(5.2, 1.3) - time * 0.12)
                );

                // Second warp: feed warped coords back (deep swirling structure)
                float2 r = float2(
                    ridgedFbm(p + 3.0 * q + float2(1.7, 9.2) + time * 0.1),
                    ridgedFbm(p + 3.0 * q + float2(8.3, 2.8) - time * 0.08)
                );

                // Final ridged noise with double-warped domain = tentacle shapes
                return ridgedFbm(p + 2.5 * r);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;
                float2 center = uv - 0.5;
                float dist = length(center);

                // --- Slight UV distortion for madness feel ---
                float warpStr = _Intensity * 0.025;
                float2 warp;
                warp.x = sin(uv.y * 13.0 + t * 1.7) * warpStr;
                warp.y = cos(uv.x * 11.0 + t * 1.3) * warpStr;
                float2 finalUV = uv + warp;

                // --- Chromatic aberration ---
                float chromaOffset = _Intensity * 0.006;
                float2 chromaDir = normalize(center + 0.001);
                fixed r = tex2D(_MainTex, finalUV + chromaDir * chromaOffset).r;
                fixed g = tex2D(_MainTex, finalUV).g;
                fixed b = tex2D(_MainTex, finalUV - chromaDir * chromaOffset).b;
                fixed3 sceneColor = fixed3(r, g, b);

                // --- Tentacle black zones ---

                // Multiple tentacle layers at different scales/speeds
                float tent1 = tentacleNoise(uv * 2.5, t * 0.7);
                float tent2 = tentacleNoise(uv * 4.0 + float2(3.1, 7.4), t * 0.5);

                // Animate tentacles reaching inward from edges
                float angle = atan2(center.y, center.x);
                float radialWarp = sin(angle * 3.0 + t * 0.8) * 0.15 + 
                                   sin(angle * 7.0 - t * 0.5) * 0.08;
                float edgeReach = dist + radialWarp * _Intensity;

                // Combine: tentacle pattern weighted toward edges
                float combinedNoise = tent1 * 0.55 + tent2 * 0.45;

                // Edge creep: tentacles originate from screen edges
                float edgeFactor = smoothstep(0.0, 0.55, edgeReach);

                // Threshold controls progressive fill (capped at ~70%)
                float threshold = 1.0 - _Intensity * 0.7;

                // Combine noise with edge bias
                float darkZone = combinedNoise + edgeFactor * 0.35;

                // Soft-ish transition for organic tentacle edges
                float blackMask = smoothstep(threshold - 0.06, threshold + 0.06, darkZone);

                // Thin crawling sub-tentacles at higher intensity
                float veins = 0.0;
                if (_Intensity > 0.25)
                {
                    float veinPattern = ridgedFbm(uv * 12.0 + float2(t * 0.4, -t * 0.3));
                    veins = veinPattern * smoothstep(0.25, 0.7, _Intensity) * 0.5;
                }

                // Pulse the edges of tentacles for living feel
                float pulse = sin(t * 2.5 + combinedNoise * 10.0) * 0.02 * _Intensity;
                blackMask = saturate(blackMask + pulse);

                // Final composite: blend scene toward black
                fixed3 finalColor = lerp(sceneColor, fixed3(0, 0, 0), saturate(blackMask + veins));

                // Desaturate remaining visible areas slightly at high intensity
                float grey = dot(finalColor, float3(0.299, 0.587, 0.114));
                finalColor = lerp(finalColor, fixed3(grey, grey, grey), _Intensity * 0.4);

                return fixed4(finalColor, 1.0);
            }

            ENDCG
        }
    }
}
