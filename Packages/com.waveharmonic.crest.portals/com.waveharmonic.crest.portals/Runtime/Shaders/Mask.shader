// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

Shader "Hidden/Crest/Portals/Mask"
{
    HLSLINCLUDE
    #pragma vertex Vertex
    #pragma fragment Fragment
    // #pragma enable_d3d11_debug_symbols

    #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Settings.Crest.hlsl"
    #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Constants.hlsl"
    ENDHLSL

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition"
        }

        Tags { "RenderPipeline"="HDRenderPipeline" }

        Blend Off
        ZTest Always
        ZWrite Off

        Pass
        {
            Name "Back Faces"
            Cull Front

            HLSLPROGRAM
            #define d_Mask k_Crest_MaskInsidePortal

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Mask.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Front Faces"
            Cull Back

            HLSLPROGRAM
            #define d_Mask 0

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Mask.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal"
        }

        Tags { "RenderPipeline"="UniversalPipeline" }

        Blend Off
        ZTest Always
        ZWrite Off

        Pass
        {
            Name "Back Faces"
            Cull Front

            HLSLPROGRAM
            #define d_Mask k_Crest_MaskInsidePortal

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Mask.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Front Faces"
            Cull Back

            HLSLPROGRAM
            #define d_Mask 0

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Mask.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        Blend Off
        ZTest Always
        ZWrite Off

        Pass
        {
            Name "Back Faces"
            Cull Front

            HLSLPROGRAM
            #define d_Mask k_Crest_MaskInsidePortal

            #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Legacy/Core.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Mask.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Front Faces"
            Cull Back

            HLSLPROGRAM
            #define d_Mask 0

            #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Legacy/Core.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Mask.hlsl"
            ENDHLSL
        }
    }
}
