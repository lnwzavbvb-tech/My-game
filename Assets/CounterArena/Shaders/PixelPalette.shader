Shader "CounterArena/PixelPalette"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                // Discrete color bands without noisy full-screen dithering.
                c = floor(saturate(c) * 31 + .5) / 31;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
