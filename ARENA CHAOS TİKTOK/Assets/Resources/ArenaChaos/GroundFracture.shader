Shader "ArenaChaos/GroundFracture"
{
    Properties
    {
        [HDR] _Tint("Energy", Color) = (1, .42, .045, 1)
        _Reveal("Reveal", Float) = 1
        _Intensity("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Reveal;
                float _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv; o.color=v.color; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float edge=saturate(1-abs(i.uv.y*2-1));
                float reveal=smoothstep(i.uv.x-.10,i.uv.x+.03,_Reveal);
                half3 hot=lerp(_Tint.rgb,half3(1,.95,.74),pow(edge,9)*.85);
                return half4(hot*_Intensity*i.color.rgb,pow(edge,1.5)*reveal*i.color.a*_Tint.a);
            }
            ENDHLSL
        }
    }
}
