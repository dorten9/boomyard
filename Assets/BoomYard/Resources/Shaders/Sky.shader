// Night/dusk sky for the arenas: a vertical gradient with a warm glow on the horizon and faint stars.
Shader "BoomYard/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.06, 0.07, 0.11, 1)
        _Horizon ("Horizon", Color) = (0.35, 0.23, 0.18, 1)
        _Bottom ("Bottom", Color) = (0.05, 0.045, 0.045, 1)
        _Stars ("Stars", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Horizon, _Bottom;
            half _Stars;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            float hash (float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                fixed3 c = y > 0 ? lerp(_Horizon.rgb, _Top.rgb, pow(saturate(y * 2.2), 0.6)) : lerp(_Horizon.rgb, _Bottom.rgb, saturate(-y * 6));
                float3 cell = floor(d * 220.0);
                float s = hash(cell);
                c += step(0.9975, s) * saturate(y * 3) * _Stars * (0.4 + 0.6 * hash(cell + 7.1));
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
