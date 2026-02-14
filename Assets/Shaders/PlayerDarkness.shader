Shader "Custom/PlayerDarkness"
{
    Properties
    {
        _Color ("Darkness Color", Color) = (0,0,0,1)
        _BaseFog ("Base Fog", Range(0, 1)) = 0.2
        _CenterFog ("Center Fog", Range(0, 2)) = 0.7
        _CenterPower ("Center Power", Range(0.5, 4)) = 1.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            float4 _Color;
            float _BaseFog;
            float _CenterFog;
            float _CenterPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float ndv = saturate(abs(dot(normalize(i.worldNormal), viewDir)));

                float centerWeight = pow(ndv, _CenterPower);
                float fogAmount = saturate(_BaseFog + _CenterFog * centerWeight);
                float alpha = fogAmount * _Color.a;

                return float4(_Color.rgb, alpha);
            }

            ENDCG
        }
    }
}
