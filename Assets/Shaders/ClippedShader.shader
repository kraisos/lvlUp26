Shader "Custom/ClippedByVolumeSoftEdge"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.1
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _CutColor ("Cut Color", Color) = (0,0,0,1)
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.05
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        CGINCLUDE
            #include "UnityCG.cginc"
            
            #define MAX_VOLUMES 16
            
            float4x4 _MaskMatrices[MAX_VOLUMES];
            float4 _MaskShapeData[MAX_VOLUMES];
            int _MaskCount;

            sampler2D _MainTex;
            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            float _EdgeSoftness;
            
            struct Input
            {
                float2 uv_MainTex;
                float3 worldPos;
            };
            
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
            
            void surf (Input IN, inout SurfaceOutputStandard o)
            {
                float dist = GetMaxDistanceInsideMasks(IN.worldPos);

                // Completely outside - discard
                if (dist < 0)
                    discard;
                 
                // Calculate soft alpha based on distance to edge   
                float alphaFactor = saturate(dist / _EdgeSoftness);
                alphaFactor = 1.0; // Disable soft edge for now, can be re-enabled later

                // Albedo comes from a texture tinted by color
                fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a * alphaFactor;
            }
        ENDCG

        // Pass 1: Render back faces (Inside) - Same material but flipped normals
        // Cull Front
        // CGPROGRAM
        // #pragma surface surf Standard fullforwardshadows alpha:fade vertex:vertBack
        // #pragma target 3.0
        
        // void vertBack(inout appdata_full v) {
        //     v.normal = -v.normal;
        // }
        // ENDCG
        
        // Pass 2: Render front faces (Outside)
        Cull Back
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0
        ENDCG
    }
    FallBack "Diffuse"
}

