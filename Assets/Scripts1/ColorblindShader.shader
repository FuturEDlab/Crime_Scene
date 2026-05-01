Shader "Custom/ColorblindShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mode ("Mode", Int) = 0
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            int _Mode;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float r = col.r;
                float g = col.g;
                float b = col.b;

                // Deuteranopia (Red-Green - missing green)
                if (_Mode == 1)
                {
                    col.r = r * 0.625 + g * 0.375;
                    col.g = r * 0.7 + g * 0.3;
                    col.b = b;
                }
                // Protanopia (Red-Green - missing red)
                else if (_Mode == 2)
                {
                    col.r = r * 0.567 + g * 0.433;
                    col.g = r * 0.558 + g * 0.442;
                    col.b = b * 0.242 + g * 0.758;
                }
                // Tritanopia (Blue-Yellow - missing blue)
                else if (_Mode == 3)
                {
                    col.r = r;
                    col.g = g * 0.7 + b * 0.3;
                    col.b = g * 0.142 + b * 0.858;
                }
                // Grayscale
                else if (_Mode == 4)
                {
                    float gray = r * 0.299 + g * 0.587 + b * 0.114;
                    col.r = gray;
                    col.g = gray;
                    col.b = gray;
                }

                return col;
            }
            ENDCG
        }
    }
} 