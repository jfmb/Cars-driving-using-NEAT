Shader "AIPrototype/Volumetric Spotlight Beam"
{
    Properties
    {
        _BeamColor ("Beam Color", Color) = (1, 0.76, 0.46, 1)
        _Intensity ("Intensity", Float) = 0.75
        _Transparency ("Transparency", Range(0, 1)) = 0.24
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "VolumetricBeam"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                half _Intensity;
                half _Transparency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half fadeIn = smoothstep(0.02h, 0.16h, input.uv.y);
                half fadeOut = 1.0h - smoothstep(0.78h, 1.0h, input.uv.y);
                half alpha = saturate(_Transparency * input.color.a * fadeIn * fadeOut * _BeamColor.a);
                return half4(_BeamColor.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
