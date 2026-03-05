// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Macros.hlsl"
#include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Library/Portals.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Surface/Data.hlsl"

m_CrestNameSpace

struct Attributes
{
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes input)
{
    // This will work for all pipelines.
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(input.positionOS);
    o.positionWS = TransformObjectToWorld(input.positionOS);

    return o;
}

half4 Fragment(Varyings input)
{
    float3 positionWS = input.positionWS;

    // This is all for the volume fly-through. if we ditch it, then can be reverted.
#if d_BackFace
    {
        positionWS = ComputeWorldSpacePosition(input.positionCS.xy / _ScreenParams.xy, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
    }
#endif

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS += _WorldSpaceCameraPos;
#endif

    const float height = SampleWaterLineHeight(positionWS.xz);

    return positionWS.y <= height ? -1 : 1;
}

m_CrestNameSpaceEnd

m_CrestVertex
m_CrestFragment(half4)
