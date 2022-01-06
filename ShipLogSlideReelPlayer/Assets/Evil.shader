Shader "dgarro/Evil"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
        {
            Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert_img
                #pragma fragment frag

                #include "UnityCG.cginc"

                sampler2D _MainTex;

                fixed4 frag(v2f_img i) : SV_Target
                {
                    fixed4 color = tex2D(_MainTex, i.uv);
                    color.rgb = 1 - color.rgb;
                    color.rgb = GammaToLinearSpace(color.rgb);
                    return color;
                }
                ENDCG
            }
        }
}