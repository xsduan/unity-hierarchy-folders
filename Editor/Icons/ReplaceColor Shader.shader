Shader "UI/Replace color" {

Properties
{
   _Color ("Replace Color", Color) = (1,1,1)
   _MainTex ("Texture", 2D) = "white"
}

SubShader
{
    Pass
    {
        SetTexture [_MainTex]
        {
            ConstantColor [_Color]
            combine constant + texture, texture
        }
    }
}

}
