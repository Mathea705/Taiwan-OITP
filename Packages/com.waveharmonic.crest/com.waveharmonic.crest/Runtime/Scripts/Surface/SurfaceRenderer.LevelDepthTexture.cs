// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaveHarmonic.Crest
{
    partial class SurfaceRenderer
    {
        RenderTexture _WaterLevelDepthTexture;
        internal RenderTexture WaterLevelDepthTexture { get; private set; }
        RenderTargetIdentifier _WaterLevelDepthTarget;
        Material _WaterLevelDepthMaterial;

        internal RenderTexture GetWaterLevelDepthTexture(Camera camera)
        {
            // Do not use SeparateViewpoint here, as this is called outside the camera loop.
            if (_Water.SingleViewpoint)
            {
                return WaterLevelDepthTexture;
            }
            else if (_PerCameraLevelDepthTexture.ContainsKey(camera))
            {
                return _PerCameraLevelDepthTexture[camera];
            }

            return null;
        }

        const string k_WaterLevelDepthTextureName = "Crest Water Level Depth Texture";

        void ExecuteWaterLevelDepthTexture(Camera camera, CommandBuffer buffer)
        {
            if (_Water.SingleViewpoint && _WaterLevelDepthTexture == null)
            {
                _WaterLevelDepthTexture = new(0, 0, 0);
                WaterLevelDepthTexture = _WaterLevelDepthTexture;
            }

            LoadCameraDataLDT(camera);

            WaterLevelDepthTexture.name = k_WaterLevelDepthTextureName;

            if (_WaterLevelDepthMaterial == null)
            {
                _WaterLevelDepthMaterial = new(Shader.Find("Hidden/Crest/Editor/Water Level (Depth)"));
            }

            var descriptor = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight)
            {
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                depthBufferBits = 32,
            };

            // Depth buffer.
            buffer.GetTemporaryRT(Helpers.ShaderIDs.s_MainTexture, descriptor);
            CoreUtils.SetRenderTarget(buffer, Helpers.ShaderIDs.s_MainTexture, ClearFlag.Depth);

            Render(camera, buffer, _WaterLevelDepthMaterial);

            // Depth texture.
            // Always release to handle screen size changes.
            WaterLevelDepthTexture.Release();
            descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
            descriptor.depthBufferBits = 0;
            WaterLevelDepthTexture.descriptor = descriptor;
            WaterLevelDepthTexture.Create();

            _WaterLevelDepthTarget = new
            (
                WaterLevelDepthTexture,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );

            // Convert.
            Helpers.Blit(buffer, _WaterLevelDepthTarget, Rendering.BIRP.UtilityMaterial, (int)Rendering.BIRP.UtilityPass.Copy);

            buffer.ReleaseTemporaryRT(Helpers.ShaderIDs.s_MainTexture);
        }

        void EnableWaterLevelDepthTexture()
        {
            if (Application.isPlaying) return;

#if d_UnityURP
            if (RenderPipelineHelper.IsUniversal)
            {
                WaterLevelDepthTextureURP.Enable(_Water, this);
            }
#endif

#if d_UnityHDRP
            if (RenderPipelineHelper.IsHighDefinition)
            {
                WaterLevelDepthTextureHDRP.Enable(_Water, this);
            }
#endif
        }

        void DisableWaterLevelDepthTexture()
        {
            if (Application.isPlaying) return;

#if d_UnityHDRP
            WaterLevelDepthTextureHDRP.Disable();
#endif
        }
    }

    // Multiple Viewpoints
    // Technically, this should always store an RT per camera, as it is screen-space.
    // But having it opt-in is not a bad idea.
    partial class SurfaceRenderer
    {
        internal Dictionary<Camera, RenderTexture> _PerCameraLevelDepthTexture = new();

        void LoadCameraDataLDT(Camera camera)
        {
            if (_Water.SingleViewpoint)
            {
                return;
            }

            if (!_PerCameraLevelDepthTexture.ContainsKey(camera))
            {
                var descriptor = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight)
                {
                    graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                    depthBufferBits = 32,
                };

                WaterLevelDepthTexture = new(descriptor);
                _PerCameraLevelDepthTexture.Add(camera, WaterLevelDepthTexture);
            }
            else
            {
                WaterLevelDepthTexture = _PerCameraLevelDepthTexture[camera];
            }
        }

        internal void RemoveCameraDataLDT(Camera camera)
        {
            if (_PerCameraLevelDepthTexture.ContainsKey(camera))
            {
                _PerCameraLevelDepthTexture[camera].Release();
                Helpers.Destroy(_PerCameraLevelDepthTexture[camera]);
                _PerCameraLevelDepthTexture.Remove(camera);
            }
        }
    }
}

#endif
