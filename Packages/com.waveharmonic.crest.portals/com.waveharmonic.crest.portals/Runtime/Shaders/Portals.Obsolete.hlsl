// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Macros.hlsl"
#include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Library/Portals.hlsl"

m_CrestNameSpace

struct Attributes
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes input)
{
    // This will work for all pipelines.
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(input.positionOS);

    return o;
}

half4 Fragment(Varyings input)
{
#if d_Tunnel
    // For when underwater, but outside of portal.
    if (LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, input.positionCS.xy) > 0.0) discard;
#endif

    return 1.0;
}

m_CrestNameSpaceEnd

m_CrestVertex
m_CrestFragment(half4)
