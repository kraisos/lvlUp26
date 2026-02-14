Shader "Custom/ClippedByVolumeSoftEdge"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _CutColor ("Cut Color", Color) = (0,0,0,1)
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.05
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        // Pass 1: Render back faces (the cut surface) - stays hard
        Pass
        {
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            #define MAX_VOLUMES 16
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            float4 _CutColor;
            
            float4x4 _MaskMatrices[MAX_VOLUMES];
            float4 _MaskShapeData[MAX_VOLUMES];
            int _MaskCount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
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
                if (localPos.y < -0.5 || localPos.y > 0.5)
                    return -1.0;
                
                float t = localPos.y + 0.5;
                float radiusAtHeight = lerp(0.5, 0.5 * coneTopRadius, t);
                
                return radiusAtHeight - length(localPos.xz);
            }
            
            bool IsInsideAnyMask(float3 worldPos)
            {
                for (int i = 0; i < _MaskCount; i++)
                {
                    int shape = (int)_MaskShapeData[i].x;
                    if (shape < 0) continue;
                    
                    float3 localPos = mul(_MaskMatrices[i], float4(worldPos, 1.0)).xyz;
                    
                    if (shape == 0 && DistanceToBoxEdge(localPos) > 0) return true;
                    if (shape == 1 && DistanceToSphereEdge(localPos) > 0) return true;
                    if (shape == 2 && DistanceToConeEdge(localPos, _MaskShapeData[i].y) > 0) return true;
                }
                return false;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                if (!IsInsideAnyMask(i.worldPos))
                    discard;
                
                return _CutColor;
            }
            ENDCG
        }
        
        // Pass 2: Render front faces with soft edge
        Pass
        {
            Cull Back
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            #define MAX_VOLUMES 16
            
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
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _EdgeSoftness;
            
            float4x4 _MaskMatrices[MAX_VOLUMES];
            float4 _MaskShapeData[MAX_VOLUMES];
            int _MaskCount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
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
                
                for (int i = 0; i < _MaskCount; i++)
                {
                    int shape = (int)_MaskShapeData[i].x;
                    if (shape < 0) continue;
                    
                    float3 localPos = mul(_MaskMatrices[i], float4(worldPos, 1.0)).xyz;
                    float dist = -999.0;
                    
                    if (shape == 0) dist = DistanceToBoxEdge(localPos);
                    else if (shape == 1) dist = DistanceToSphereEdge(localPos);
                    else if (shape == 2) dist = DistanceToConeEdge(localPos, _MaskShapeData[i].y);
                    
                    maxDist = max(maxDist, dist);
                }
                
                return maxDist;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float dist = GetMaxDistanceInsideMasks(i.worldPos);
                
                // Completely outside - discard
                if (dist < 0)
                    discard;
                
                // Calculate soft alpha based on distance to edge
                float alpha = 1.0; //saturate(dist / _EdgeSoftness);
                
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0.2, dot(normalize(i.worldNormal), lightDir));
                
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.rgb *= ndotl;
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
}
