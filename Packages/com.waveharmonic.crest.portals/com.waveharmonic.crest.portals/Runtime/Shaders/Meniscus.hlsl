// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

#ifndef d_WaveHarmonic_Crest_Meniscus
#define d_WaveHarmonic_Crest_Meniscus

#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Settings.Crest.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Macros.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Constants.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Depth.hlsl"

#include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Library/Portals.hlsl"

// Negative volume.
// A special value is written where it is normally discarded. We can use this to
// detect cutout edge, without false positive of near plane.
#if d_Crest_BackFace
#define k_Crest_MaskSource 2
#define k_Crest_MaskTarget 1
#else
#define k_Crest_MaskSource 1
#define k_Crest_MaskTarget 2
#endif

TEXTURE2D_X(_Crest_WaterMaskTexture);

m_CrestNameSpace

struct Attributes
{
#if d_Crest_Geometry
    float3 positionOS : POSITION;
#else
    uint id : SV_VertexID;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes input)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if d_Crest_Geometry
    // Use actual geometry instead of full screen triangle.
    output.positionCS = TransformObjectToHClip(input.positionOS);
#else
    output.positionCS = GetFullScreenTriangleVertexPosition(input.id, UNITY_RAW_FAR_CLIP_VALUE);
#endif

    return output;
}

// Exists to avoid "use of potentially uninitialized variable" when inlined.
bool DetectIntersection(const float2 i_UV, const float i_RawFogAfter, const float i_RawFogBefore)
{
    bool intersects = false;

    const float rawFogAfter = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, i_UV);

    if ((rawFogAfter == 0.0 && i_RawFogAfter > 0.0) || (rawFogAfter > 0.0 && i_RawFogAfter == 0.0))
    {
#if d_Crest_HasBackFace
        const float rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, i_UV);
        if (rawFogBefore > 0.0 && i_RawFogBefore > 0.0)
#endif
        {
            intersects = true;
        }
    }

    return intersects;
}

float DetectEdge
(
    const float i_Mask,
    const float i_OldMask,
    const int2 i_PositionSS,
    const float2 i_Offset,
    const float i_Magnitude,
    const float i_Scale,
    const float i_RawFogAfter,
    const float i_RawFogBefore
)
{
    const float2 uv = i_PositionSS + i_Offset * i_Magnitude * i_Scale;

    if (DetectIntersection(uv, i_RawFogAfter, i_RawFogBefore))
    {
        return i_Mask;
    }

    float newMask = LOAD_TEXTURE2D_X(_Crest_WaterMaskTexture, uv).r;

    // Sample off screen.
    if (newMask == 0.0)
    {
        return i_Mask;
    }

    // Hole fill intersections.
    if (m_CrestPortalNegativeVolume)
    {
        // Fix intersection cases where hole is filled and near plane below.
        if (i_OldMask == -2.0 && newMask >= 0.0 || newMask == -2.0 && i_OldMask >= 0.0)
        {
            return i_Mask;
        }

        // Fix intersection cases where hole is filled and near plane above.
        if (i_OldMask == 2.0 && newMask <= 0.0 || newMask == 2.0 && i_OldMask <= 0.0)
        {
            return i_Mask;
        }
    }

#if d_Crest_Geometry
    // Is negative volume.
    if (m_CrestPortalNegativeVolume)
    {
        if (i_RawFogBefore > 0.0)
        {
            if (abs(newMask) != k_Crest_MaskTarget)
            {
                // Cannot discard otherwise it will cut search short.
                return i_Mask;
            }
        }

#if d_Crest_FrontFace
        if (i_OldMask > 0.0 && newMask < 0.0)
        {
            return i_Mask;
        }
#endif

#if d_Crest_BackFace
        if (i_OldMask < 0.0 && newMask > 0.0)
        {
            return i_Mask;
        }
#endif
    }

    // No mask means no underwater effect so ignore the value.
    newMask = (newMask == CREST_MASK_NONE ? i_Mask : newMask);
#else
    // For full-screen, clamp so special markings do not draw line.
    newMask = clamp(newMask, CREST_MASK_BELOW_SURFACE, CREST_MASK_ABOVE_SURFACE);
#endif // d_Crest_Geometry

    return newMask;
}

half4 Fragment(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const uint2 positionSS = input.positionCS.xy;

    const float rawFogAfter =
#if d_Crest_FrontFace
        input.positionCS.z;
#else
        LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, positionSS);
#endif

    float rawFogBefore = input.positionCS.z;
#if d_Crest_FrontFace || d_Crest_HasBackFace
    if (m_CrestPortalWithBackFace)
    {
        rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, positionSS);
    }
#endif

#if !d_Crest_Geometry
    if (m_CrestPortalNegativeVolume)
    {
        if (rawFogBefore == 0.0 && rawFogAfter > 0.0)
        {
            // Discard when inside the volume, otherwise it will draw on back faces.
            discard;
        }
    }
    else
    {
        if (rawFogAfter > 0.0)
        {
            // Discard when outside volume (before only, mask handles reset).
            discard;
        }
    }
#endif

    float mask = LOAD_TEXTURE2D_X(_Crest_WaterMaskTexture, positionSS).x;

#if d_Crest_Geometry
    // Prevents geometry from drawing on full-screen meniscus.
    if (m_CrestPortalNegativeVolume)
    {
        if (abs(mask) != k_Crest_MaskSource)
        {
            discard;
        }
    }
#endif

    float weight = 1.0;

    // Render meniscus by checking the mask along the horizon normal which is flipped using the surface normal from
    // mask. Adding the mask value will flip the UV when mask is below surface.
    const float2 offset = (float2)clamp(-mask, -1, 1) * g_Crest_HorizonNormal;

    const float oldMask = mask;

#if d_Crest_Geometry
    // The meniscus at the boundary can be at a distance. We need to scale the offset as 1 pixel at a distance is much
    // larger than 1 pixel up close.
    const float scale = 1.0 - saturate(Utility::CrestLinearEyeDepth(input.positionCS.z) / MENISCUS_MAXIMUM_DISTANCE);

    // Exit early.
    if (scale == 0.0)
    {
        discard;
    }
#else
    // Dummy value.
    const float scale = 1.0;
    mask = clamp(mask, CREST_MASK_BELOW_SURFACE, CREST_MASK_ABOVE_SURFACE);
#endif

    // Sample three pixels along the normal. If the sample is different than the
    // current mask, apply meniscus. Offset must be added to positionSS as floats.
    [unroll]
    for (int i = 1; i <= 3; i++)
    {
        float newMask = DetectEdge(mask, oldMask, positionSS, offset, i, scale, rawFogAfter, rawFogBefore);
        weight *= newMask != mask ? 0.9 : 1.0;
    }

    return weight;
}

m_CrestNameSpaceEnd

m_CrestVertex
m_CrestFragment(half4)

#endif // d_WaveHarmonic_Crest_Meniscus
