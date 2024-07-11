Shader "Unlit/BlackMask"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Color("Color (RGBA)", Color) = (1, 1, 1, 1)
    }
    SubShader{
        Color[_Color]
        ZWrite On
        Stencil {
            Ref 1
            Comp NotEqual
        }
        Pass {}
    }
}
