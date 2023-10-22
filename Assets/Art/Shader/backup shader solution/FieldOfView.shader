Shader "Unlit/FieldOfView"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader{
        ColorMask 0
        ZWrite On

        Stencil {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass {}
    }
}
