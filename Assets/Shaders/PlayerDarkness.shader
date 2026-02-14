Shader "Custom/PlayerDarkness"
{
    Properties
    {
        _Color ("Darkness Color", Color) = (0,0,0,1)
        _InnerRadius ("Inner Radius", Float) = 10
        _OuterRadius ("Outer Radius", Float) = 20
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _Color;
            float _InnerRadius;
            float _OuterRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);

                float alpha = smoothstep(_InnerRadius, _OuterRadius, dist);

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
