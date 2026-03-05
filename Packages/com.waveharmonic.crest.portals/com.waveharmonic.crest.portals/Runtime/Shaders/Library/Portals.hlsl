// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

#ifndef CREST_WATER_VOLUME_INCLUDED
#define CREST_WATER_VOLUME_INCLUDED

#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Constants.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Globals.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Depth.hlsl"
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Library/Utility/Helpers.hlsl"

#include "Packages/com.waveharmonic.crest.portals/Runtime/Shaders/Library/Macros.hlsl"

TEXTURE2D_X(_Crest_PortalFogAfterTexture);
TEXTURE2D_X(_Crest_PortalFogBeforeTexture);

#if d_Crest_NegativeVolumePass
TEXTURE2D_X(_Crest_MaskColorSource);
#endif

#define m_CrestPortal (_Crest_Portal >= 2)
#define m_CrestPortalNone (_Crest_Portal < 2)
#define m_CrestPortal2D (_Crest_Portal == 2)
#define m_CrestPortalWithBackFace (_Crest_Portal > 2)
#define m_CrestPortalNegativeVolume (_Crest_Portal > 3)
#define m_CrestPortalTunnel (_Crest_Portal == 5)

#if !defined(m_Return)
#define m_Return return
#endif

#if (CREST_LEGACY_UNDERWATER != 1)
#if d_Crest_WaterSurface
#include "Packages/com.waveharmonic.crest/Runtime/Shaders/Surface/Data.hlsl"
#endif
#endif

m_PortalNameSpace

#if d_Crest_NegativeVolumePass
half FixMaskForNegativeVolume(half newMask, const float2 i_PositionSS)
{
    half oldMask = LOAD_TEXTURE2D_X(_Crest_MaskColorSource, i_PositionSS).r;
    if (oldMask != newMask)
    {
        newMask *= 2.0;
    }

    return newMask;
}
#endif

void EvaluateMask(const float4 i_positionCS)
{
    const float rawFogAfter = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, i_positionCS.xy);

    if (m_CrestPortalNegativeVolume)
    {
        // Keep surface when back-face not in view.
        if (rawFogAfter == 0.0)
        {
            m_Return;
        }

        const float rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, i_positionCS.xy);

#if d_Crest_NegativeVolumePass
        // Must be inside portal as if no portal on screen, we would have discarded.
        if (rawFogBefore == 0.0)
        {
            discard;
        }

        // Is pixel closer than front-face or pixel further than back-face (ie outside the
        // volume).
        if (i_positionCS.z > rawFogBefore || i_positionCS.z < rawFogAfter)
        {
            discard;
        }
#else
        // Camera and pixel is within the volume. We need to discard to avoid issues when
        // surface is at eye level.
        if (rawFogBefore == 0.0 && rawFogAfter < i_positionCS.z)
        {
            discard;
        }

        // Is pixel further than front-face and pixel closer than back back-face (ie is
        // within the volume).
        if (rawFogBefore > i_positionCS.z && rawFogAfter < i_positionCS.z)
        {
            discard;
        }
#endif
    }
    else
    {
        // Discard any pixels in front of the volume geometry otherwise the mask will be incorrect at eye level.
        if (rawFogAfter > 0.0 && rawFogAfter < i_positionCS.z)
        {
            discard;
        }
    }
}

bool EvaluateFog(const float2 i_PositionNDC, const half i_Mask, inout float o_RawFogDistance, out float o_FogDistanceOffset)
{
    // Offset is subtracted from fog distance. So subtract to increase fog, add to decrease.
    o_FogDistanceOffset = _ProjectionParams.y;

    if (!m_CrestPortal)
    {
        return true;
    }

    const float2 positionSS = i_PositionNDC.xy * _ScreenSize.xy;

    float rawFogAfter = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, positionSS).r;

    if (m_CrestPortal2D)
    {
        // No fog if plane is not in view. If we wanted to be consistent with the
        // underwater shader, we should also check this for non fly-through volumes too,
        // but being inside a non fly-through volume is undefined behaviour so we can
        // save a variant.
        if (rawFogAfter == 0.0)
        {
            return false;
        }
    }

    if (!m_CrestPortalNegativeVolume)
    {
        // No fog before volume.
        if (rawFogAfter > 0.0 && o_RawFogDistance > rawFogAfter)
        {
            return false;
        }
    }

    if (m_CrestPortal2D)
    {
        o_FogDistanceOffset = Utility::CrestLinearEyeDepth(rawFogAfter);
        return true;
    }

    // At this point, pixel is beyond front face. We are within the volume.
    if (m_CrestPortalWithBackFace)
    {
        // Use the closest of the two.
        float rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, positionSS).r;

        // No negative volume in view. Fogged if inverted.
        if (rawFogAfter == 0.0 && rawFogBefore == 0.0)
        {
            return m_CrestPortalNegativeVolume;
        }

        if (m_CrestPortalNegativeVolume)
        {
            Utility::Swap(rawFogAfter, rawFogBefore);

            // Pixel is closer than BF.
            if (o_RawFogDistance > rawFogBefore)
            {
                // Camera inside negative volume.
                if (rawFogAfter == 0.0)
                {
                    return false;
                }
                // Looking through FF above water.
                else if (i_Mask == CREST_MASK_ABOVE_SURFACE_KEPT)
                {
                    return false;
                }
                else
                {
                    // Get the closest.
                    o_RawFogDistance = max(o_RawFogDistance, rawFogAfter);
                }
            }
            // Pixel is further than BF.
            else
            {
                // Looking from below to sky.
                if (i_Mask == CREST_MASK_BELOW_SURFACE_KEPT)
                {
                    if (rawFogAfter == 0.0)
                    {
                        return false;
                    }
                    else
                    {
                        o_RawFogDistance = rawFogAfter;
                    }
                }
                else
                {
                    // Subtract back face distance.
                    o_FogDistanceOffset = Utility::CrestLinearEyeDepth(rawFogBefore);

                    // Pixel is behind negative volume (FF + BF), but not from above.
                    if (rawFogAfter > 0.0 && i_Mask != CREST_MASK_ABOVE_SURFACE_KEPT)
                    {
                        // Add back near plane to front face distance.
                        o_FogDistanceOffset -= Utility::CrestLinearEyeDepth(rawFogAfter) - _ProjectionParams.y;
                    }
                }
            }
        }
        else
        {
            // Either pixel inside volume or volume wall.
            o_RawFogDistance = max(o_RawFogDistance, rawFogBefore);

            if (rawFogAfter > 0.0)
            {
                o_FogDistanceOffset = Utility::CrestLinearEyeDepth(rawFogAfter);
            }
        }
    }

    return true;
}

// Compiler shows warning when using intermediate returns, disable this:
// "use of potentially uninitialized variable"
#pragma warning(push)
#pragma warning(disable: 4000)
bool EvaluateSurface(const float2 i_PositionNDC, const float i_PixelDepthRaw, const float3 i_PositionWS, inout bool io_UnderWater, inout float io_SceneDepthRaw, out float o_NegativeFogDistance)
{
    o_NegativeFogDistance = _ProjectionParams.y;

    float2 positionNDC = i_PositionNDC;

    // Convert coordinates for Load.
    const float2 positionSS = i_PositionNDC.xy * _ScreenSize.xy;

    if (m_CrestPortalWithBackFace)
    {
        if (m_CrestPortalNegativeVolume)
        {
            const float rawFogAfter = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, positionSS);

            // Keep water when back-face not in view.
            if (rawFogAfter == 0.0)
            {
                return false;
            }

            const float rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, positionSS);

            // Discard water before volume.
            if (rawFogAfter < i_PixelDepthRaw)
            {
                // Camera and pixel is within volume.
                // Is pixel further than frontface and pixel closer than back backface (ie is pixel
                // outside of portal).
                if (rawFogBefore == 0.0 || rawFogBefore > i_PixelDepthRaw)
                {
                    return true;
                }
            }
#if (CREST_LEGACY_UNDERWATER != 1)
            // Surface is after volume.
            else
            {
                if (m_CrestPortalTunnel)
                {
                    io_UnderWater = false;
                    return false;
                }

                o_NegativeFogDistance = Utility::CrestLinearEyeDepth(rawFogAfter);

                if (rawFogBefore > 0.0)
                {
                    o_NegativeFogDistance -= Utility::CrestLinearEyeDepth(rawFogBefore) - _ProjectionParams.y;
                }

#if d_Crest_WaterSurface
                float3 positionWS = ComputeWorldSpacePosition(i_PositionNDC, rawFogAfter, UNITY_MATRIX_I_VP);
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
                positionWS += _WorldSpaceCameraPos;
#endif

                const float height = SampleWaterLineHeight(positionWS.xz);

                io_UnderWater = positionWS.y <= height;
#endif
            }
#endif

            return false;
        }
        else
        {
            const float rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, positionSS);

            // Is backface closer.
            if (rawFogBefore > io_SceneDepthRaw)
            {
                io_SceneDepthRaw = rawFogBefore;
            }

            if (rawFogBefore == 0.0 || rawFogBefore > i_PixelDepthRaw)
            {
                return true;
            }
        }
    }

    // Discard water before volume.
    const float rawFogAfter = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, positionSS.xy);
    if (rawFogAfter > 0.0 && rawFogAfter < i_PixelDepthRaw)
    {
        return true;
    }

    if (m_CrestPortal2D)
    {
        // Discard water when plane is not in view.
        if (rawFogAfter == 0.0)
        {
            return true;
        }
    }

#if (CREST_LEGACY_UNDERWATER != 1)
    if (rawFogAfter > 0.0)
    {
        o_NegativeFogDistance = Utility::CrestLinearEyeDepth(rawFogAfter);
    }
#endif

    return false;
}
#pragma warning(pop)

void EvaluateRefraction
(
    const float2 i_RefractedPositionNDC,
    const float i_SceneZRaw,
    const bool i_Underwater,
    inout float io_RefractedSceneDepthRaw,
    inout bool io_Caustics
)
{
    if (!m_CrestPortalWithBackFace)
    {
        return;
    }

    // Causes issues for negative volumes.
    if (m_CrestPortalNegativeVolume)
    {
        return;
    }

    if (i_Underwater)
    {
        return;
    }

    const float rawFogBefore = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, i_RefractedPositionNDC * _ScreenSize.xy);

    // If back-face is closer.
    if (rawFogBefore > io_RefractedSceneDepthRaw)
    {
        io_RefractedSceneDepthRaw = rawFogBefore;
        io_Caustics = false;
    }

    // Sample has landed off the portal (UV wise). Cancel refraction otherwise distance
    // could be too large (refraction artifact).
    if (rawFogBefore == 0.0)
    {
        io_RefractedSceneDepthRaw = i_SceneZRaw;
    }
}

void EvaluateVolume
(
    const float4 i_PositionCS,
    const int2 i_PositionSS,
    const float i_SurfaceDepth,
    const float i_SceneDepth,
    inout float io_RawDepth,
    inout bool io_Caustics,
    inout bool io_UnderWater,
    inout bool io_OutScatterScene,
    inout bool io_ApplyLighting
)
{
    const float rawDepth =
#if d_Crest_PortalWithBackFace
        // 3D has a back face texture for the depth.
        LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogBeforeTexture, i_PositionSS);
#else
        // Volume is rendered using the back face so that is the depth.
        i_PositionCS.z;
#endif // d_Crest_PortalWithBackFace

    // Use backface depth if closest.
    if (io_RawDepth < rawDepth)
    {
        // Cancels out caustics.
        io_Caustics = false;
        io_RawDepth = rawDepth;
    }

#if d_Crest_PortalNegativeVolume
    // No front-face.
    if (rawDepth == 0.0)
    {
        const float rawBackFaceDepth = LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, i_PositionSS);

        // We are in the negative volume. Already handled by front face.
        if (rawBackFaceDepth > 0.0)
        {
            // Apply out-scattering, but not lighting.
            io_UnderWater = false;

            // Scene depth is closer than fog after.
            if (io_RawDepth > rawBackFaceDepth)
            {
                io_UnderWater = true;
                io_OutScatterScene = true;
                io_ApplyLighting = false;
                io_Caustics = false;
            }
        }
    }
    // Front-face closer than scene.
    else if (rawDepth > i_SceneDepth && rawDepth > i_SurfaceDepth)
    {
        // Skip underwater, as it is handled by front-face already.
        io_UnderWater = false;
    }
#endif // d_Crest_PortalNegativeVolume

#if d_Crest_FogBefore
    if (m_CrestPortalNegativeVolume)
    {
        if (LOAD_DEPTH_TEXTURE_X(_Crest_PortalFogAfterTexture, i_PositionSS) > i_SceneDepth)
        {
            // Prevent front-face from applying out-scattering to scene which effectively doubles it.
            io_OutScatterScene = false;
        }
    }
#endif
}

m_PortalNameSpaceEnd

#endif // CREST_WATER_VOLUME_INCLUDED
