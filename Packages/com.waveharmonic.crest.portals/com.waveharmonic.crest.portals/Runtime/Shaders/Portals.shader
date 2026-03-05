// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

Shader "Hidden/Crest/Water Volume Geometry"
{
    HLSLINCLUDE
    #pragma vertex Vertex
    #pragma fragment Fragment
    // #pragma enable_d3d11_debug_symbols

    #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Settings.Crest.hlsl"
    ENDHLSL

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition"
        }

        Tags { "RenderPipeline"="HDRenderPipeline" }

        Pass
        {
            Name "Front Faces"
            Cull Back

            Stencil
            {
                // Must match k_StencilValueVolume in:
                // Scripts/Underwater/UnderwaterRenderer.Mask.cs
                Ref 5
                Pass Replace
            }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#if (CREST_LEGACY_UNDERWATER != 1)
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.hlsl"
#else
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Back Faces"
            Cull Front

            Stencil
            {
                // Must match k_StencilValueVolume in:
                // Scripts/Underwater/UnderwaterRenderer.Mask.cs
                Ref 5
                Pass Replace
            }

            HLSLPROGRAM
            #define d_BackFace 1

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#if (CREST_LEGACY_UNDERWATER != 1)
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.hlsl"
#else
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Tunnel"
            Cull Front

            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #define d_Tunnel 1
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
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

        Pass
        {
            Name "Front Faces"
            Cull Back

            Stencil
            {
                // Must match k_StencilValueVolume in:
                // Scripts/Underwater/UnderwaterRenderer.Mask.cs
                Ref 5
                Pass Replace
            }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if (CREST_LEGACY_UNDERWATER != 1)
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.hlsl"
#else
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Back Faces"
            Cull Front

            Stencil
            {
                // Must match k_StencilValueVolume in:
                // Scripts/Underwater/UnderwaterRenderer.Mask.cs
                Ref 5
                Pass Replace
            }

            HLSLPROGRAM
            #define d_BackFace 1

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if (CREST_LEGACY_UNDERWATER != 1)
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.hlsl"
#else
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Tunnel"
            Cull Front

            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #define d_Tunnel 1
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        Pass
        {
            Name "Front Faces"
            Cull Back

            Stencil
            {
                // Must match k_StencilValueVolume in:
                // Scripts/Underwater/UnderwaterRenderer.Mask.cs
                Ref 5
                Pass Replace
            }

            HLSLPROGRAM
            #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Legacy/Core.hlsl"

#if (CREST_LEGACY_UNDERWATER != 1)
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.hlsl"
#else
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Back Faces"
            Cull Front

            Stencil
            {
                // Must match k_StencilValueVolume in:
                // Scripts/Underwater/UnderwaterRenderer.Mask.cs
                Ref 5
                Pass Replace
            }

            HLSLPROGRAM
            #define d_BackFace 1

            #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Legacy/Core.hlsl"

#if (CREST_LEGACY_UNDERWATER != 1)
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.hlsl"
#else
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
#endif
            ENDHLSL
        }

        Pass
        {
            Name "Tunnel"
            Cull Front

            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #define d_Tunnel 1
            #include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Legacy/Core.hlsl"
            #include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Portals.Obsolete.hlsl"
            ENDHLSL
        }
    }
}
