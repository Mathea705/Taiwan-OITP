// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using WaveHarmonic.Crest.Internal;

namespace WaveHarmonic.Crest.Portals
{
    /// <summary>
    /// Portal rendering modes.
    /// </summary>
    [@GenerateDoc]
    public enum PortalMode
    {
        /// <inheritdoc cref="Generated.PortalMode.Portal"/>
        [Tooltip("A portal to infinite water, rendered from front faces of geometry.")]
        Portal = PortalRenderer.EffectPass.VolumeFrontFace2D,

        /// <inheritdoc cref="Generated.PortalMode.Volume"/>
        [Tooltip("A volume of water rendered which only works when the viewer is outside the volume.\n\nIt uses both front faces and back faces. It is more efficient than VolumeFlyThrough.")]
        Volume,

        /// <inheritdoc cref="Generated.PortalMode.VolumeFlyThrough"/>
        [Tooltip("A volume of water rendered which also works with the viewer inside the volume.\n\nIt uses both front faces and back faces. It also requires the stencil buffer and is less efficient than Volume.")]
        [InspectorName("Volume (Fly-Through)")]
        VolumeFlyThrough,

        /// <inheritdoc cref="Generated.PortalMode.Tunnel"/>
        [Tooltip("Removes the water surface and underwater effect for caves etc.\n\nThe walls most be covered by geometry (eg cave) for this to look correct.")]
        Tunnel,
    }

    /// <summary>
    /// This renderer can remove water inside or outside of geometry.
    /// </summary>
    [System.Serializable]
    public sealed partial class PortalRenderer : MaskRenderer.IMaskProvider
    {
        [@Space(10)]

        [Tooltip("Whether portal rendering is enabled.")]
        [@GenerateAPI(Setter.Custom)]
        [@DecoratedField, SerializeField]
        internal bool _Enabled;

        [Tooltip("Rendering mode of the portal (and water surface).\n\nSee the manual for more details.")]
        [@GenerateAPI(Setter.Custom)]
        [@DecoratedField, SerializeField]
        internal PortalMode _Mode = PortalMode.Portal;

        [Tooltip("Mesh (Mesh Filter) to use to render the portal.\n\nIt will use the Mesh Filter's transform.")]
        [@GenerateAPI(Setter.Custom)]
        [@DecoratedField, SerializeField]
        internal MeshFilter _Geometry;

        [Tooltip("Use the back-faces of the mesh.\n\nUseful for portholes on watercraft or tunnelling through water.")]
        [@Predicated(nameof(_Mode), inverted: false, nameof(PortalMode.VolumeFlyThrough))]
        [@Predicated(nameof(_Mode), inverted: false, nameof(PortalMode.Tunnel))]
        [@GenerateAPI(Getter.Custom, Setter.Custom)]
        [@DecoratedField, SerializeField]
        internal bool _Invert = false;

        [@Space(10)]

        [Tooltip("The maximum resolution of the portal effect.\n\nResolution is derived from the texel size and the size of the largest dimension of the bounds of the geometry.")]
        [@Range(128, 4096)]
        [@GenerateAPI]
        [@SerializeField]
        int _MaximumResolution = 1024;

        [Tooltip("The texel size of the portal effect.\n\nThis is the primary quality control, where lower is higher quality.")]
        [@Range(0.0125f, 1f)]
        [@GenerateAPI]
        [@SerializeField]
        float _TexelSize = 0.02f;


        const string k_DrawPortal = "Portal";
        // This must match the Stencil Ref value for front and back face pass in
        // Underwater.shader and Portals.shader.
        internal const int k_StencilValueVolume = 5;

        const string k_FrontFaceTextureName = "_CrestPortalFrontFace";
        const string k_BackFaceTextureName = "_CrestPortalBackFace";

        internal enum VolumePass
        {
            FrontFace,
            BackFace,
            Tunnel,
        }

        internal enum EffectPass
        {
            VolumeFrontFace2D = UnderwaterRenderer.EffectPass.Reflections + 1,
            VolumeFrontFace3D,
            VolumeFrontFaceVolume,
            VolumeBackFace,
            VolumeScene,
            VolumeNegative,
            VolumeNegativeFogAfter,
            Tunnel,
        }

        internal enum MeniscusPass
        {
            FullScreen = 1,
            Front,
            Back,
            Length,
        }

        static partial class ShaderIDs
        {
            // Global
            public static readonly int s_Portal = Shader.PropertyToID("_Crest_Portal");
            public static readonly int s_PortalFogAfterTexture = Shader.PropertyToID("_Crest_PortalFogAfterTexture");
            public static readonly int s_PortalFogBeforeTexture = Shader.PropertyToID("_Crest_PortalFogBeforeTexture");
            public static readonly int s_TemporaryMaskColor = Shader.PropertyToID("_Crest_TemporaryMaskColor");
            public static readonly int s_TemporaryMaskDepth = Shader.PropertyToID("_Crest_TemporaryMaskDepth");
            public static readonly int s_MaskColorSource = Shader.PropertyToID("_Crest_MaskColorSource");
            public static readonly int s_PortalInverted = Shader.PropertyToID("_Crest_PortalInverted");

            public static readonly int s_StencilReference = Shader.PropertyToID("_Crest_StencilReference");
            public static readonly int s_StencilComparison = Shader.PropertyToID("_Crest_StencilComparison");
        }

        static Material s_MaskMaterial;

        /// <summary>
        /// Whether the <see cref="PortalRenderer"/> is active (enabled and valid).
        /// </summary>
        public bool Active => _Enabled && _UnderWater.Enabled && _Geometry != null;

        internal bool RequiresFullScreenMask => Active && (NegativeVolume || Mode is PortalMode.Tunnel);
        internal bool NegativeVolume => _Mode == PortalMode.Volume && _Invert;
        internal bool HasBackFace => _Mode is PortalMode.Volume or PortalMode.VolumeFlyThrough or PortalMode.Tunnel;

        internal Material _PortalMaterial;

        RTHandle _FogAfterTexture;
        RTHandle _FogBeforeTexture;

        internal WaterRenderer _Water;
        internal UnderwaterRenderer _UnderWater;

        AfterVolumeMaskPass _AfterVolumeMaskPass;

        internal static void DisableWaterMaskKeywords()
        {
            Shader.SetGlobalInteger(ShaderIDs.s_Portal, 0);
        }

        internal void OnEnable()
        {
            if (_PortalMaterial == null && WaterResources.Instance.Shaders._Portals != null)
            {
                _PortalMaterial = new(WaterResources.Instance.Shaders._Portals);
            }

            Enable();
        }

        internal void OnDisable()
        {
            Disable();
        }

        internal void OnDestroy()
        {
            Helpers.Destroy(_PortalMaterial);

            Release();
        }

        internal void OnBeginCameraRendering(Camera camera)
        {
            UpdateDisplacedSurfaceData();
        }

        internal void OnEndCameraRendering(Camera camera)
        {
            Shader.SetGlobalInteger(ShaderIDs.s_Portal, 0);
        }

        void Enable()
        {
            _AfterVolumeMaskPass ??= new(this);

            _UnderWater.UseStencilBuffer = Active && _Mode == PortalMode.VolumeFlyThrough;

            // Was called at start of UnderWaterRenderer.OnMaskPass.
            _Water._Mask.Add(UnderwaterRenderer.k_VolumeMaskQueue - 1, this);

            if (_AfterVolumeMaskPass.Required)
            {
                // Was at end of UnderWaterRenderer.PopulateMask.
                _Water._Mask.Add(UnderwaterRenderer.k_VolumeMaskQueue + 1, _AfterVolumeMaskPass);
            }

            MaskRenderer.s_OnAllocate -= Allocate;
            MaskRenderer.s_OnAllocate += Allocate;
            MaskRenderer.s_OnReAllocate -= ReAllocate;
            MaskRenderer.s_OnReAllocate += ReAllocate;
            MaskRenderer.s_OnRelease -= Release;
            MaskRenderer.s_OnRelease += Release;

            _UnderWater._NeedsColorTexture = NegativeVolume;
        }

        void Disable()
        {
            Shader.SetGlobalInteger(ShaderIDs.s_Portal, 0);

            _Water._Mask?.Remove(this);
            _Water._Mask?.Remove(_AfterVolumeMaskPass);

            MaskRenderer.s_OnAllocate -= Allocate;
            MaskRenderer.s_OnReAllocate -= ReAllocate;
            MaskRenderer.s_OnRelease -= Release;

            if (_UnderWater != null)
            {
                if (_UnderWater._MaskMaterial != null) _UnderWater._MaskMaterial.SetInteger(ShaderIDs.s_StencilReference, 0);
                if (_UnderWater._HorizonMaskMaterial != null) _UnderWater._HorizonMaskMaterial.SetInteger(ShaderIDs.s_StencilReference, 0);
                _UnderWater.UseStencilBuffer = false;
            }

            Release();

            _UnderWater._NeedsColorTexture = false;
        }

        // This is called only in OnEnable and only if portals is enabled. Need to handle runtime
        internal void Allocate()
        {
            // HDRP only needs to allocate textures once. URP uses ReAllocate.
            if (!RenderPipelineHelper.IsHighDefinition)
            {
                return;
            }

            _FogAfterTexture ??= RTHandles.Alloc
            (
                scaleFactor: Vector2.one,
                slices: TextureXR.slices,
                dimension: TextureXR.dimension,
                depthBufferBits: Rendering.GetDefaultDepthBufferBits(),
                colorFormat: GraphicsFormat.None,
                enableRandomWrite: false,
                useDynamicScale: true,
                name: k_FrontFaceTextureName
            );

            if (HasBackFace)
            {
                _FogBeforeTexture ??= RTHandles.Alloc
                (
                    scaleFactor: Vector2.one,
                    slices: TextureXR.slices,
                    dimension: TextureXR.dimension,
                    depthBufferBits: Rendering.GetDefaultDepthBufferBits(),
                    colorFormat: GraphicsFormat.None,
                    enableRandomWrite: false,
                    useDynamicScale: true,
                    name: k_BackFaceTextureName
                );
            }
            else
            {
                _FogBeforeTexture?.Release();
                _FogBeforeTexture = null;
            }
        }

        internal void Release()
        {
            _FogAfterTexture?.Release();
            _FogAfterTexture = null;
            _FogBeforeTexture?.Release();
            _FogBeforeTexture = null;
        }

        internal void ReAllocate(RenderTextureDescriptor descriptor)
        {
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthBufferBits = (int)Rendering.GetDefaultDepthBufferBits();

            RenderPipelineCompatibilityHelper.ReAllocateIfNeeded(ref _FogAfterTexture, descriptor, name: k_FrontFaceTextureName);

            if (HasBackFace)
            {
                RenderPipelineCompatibilityHelper.ReAllocateIfNeeded(ref _FogBeforeTexture, descriptor, name: k_BackFaceTextureName);
            }
            else
            {
                _FogBeforeTexture?.Release();
                _FogBeforeTexture = null;
            }
        }

        void SetUpMaskStencil(Material material)
        {
            var isNegative = NegativeVolume || _Mode is PortalMode.Tunnel;
            material.SetInteger(ShaderIDs.s_StencilReference, _UnderWater.UseStencilBuffer ? k_StencilValueVolume : 0);
            material.SetInteger(ShaderIDs.s_StencilComparison, (int)(isNegative ? CompareFunction.Always : CompareFunction.Equal));
        }

        internal void RenderMask(Camera camera, CommandBuffer buffer, Material material, MaskRenderer mask, MaterialPropertyBlock properties = null)
        {
            buffer.BeginSample(k_DrawPortal);

            var invert = Invert || _Mode is PortalMode.Tunnel;
            var isNegative = NegativeVolume || _Mode is PortalMode.Tunnel;
            Shader.SetGlobalInteger(ShaderIDs.s_Portal, Mathf.Min((int)_Mode, (int)PortalMode.Volume) + (isNegative ? 1 : 0) + (Mode is PortalMode.Tunnel ? 1 : 0));
            material.SetKeyword("d_Tunnel", _Mode is PortalMode.Tunnel);

            buffer.SetInvertCulling(invert);

            if (HasBackFace)
            {
                if (_Mode is PortalMode.VolumeFlyThrough && !_UnderWater.UseLegacyMask)
                {
                    // Compute near plane water line using back-face, then overwrite with front face.
                    CoreUtils.SetRenderTarget(buffer, _Water._Mask.ColorRTH, _FogBeforeTexture, ClearFlag.All);
                }
                else
                {
                    CoreUtils.SetRenderTarget(buffer, _FogBeforeTexture, ClearFlag.DepthStencil);
                }

                Helpers.ScaleViewport(camera, buffer, _FogBeforeTexture);

                buffer.SetGlobalTexture(ShaderIDs.s_PortalFogBeforeTexture, _FogBeforeTexture);

                buffer.DrawMesh
                (
                    _Geometry.sharedMesh,
                    _Geometry.transform.localToWorldMatrix,
                    _PortalMaterial,
                    submeshIndex: 0,
                    (int)VolumePass.BackFace,
                    properties
                );
            }

            if (!_UnderWater.UseLegacyMask && !NegativeVolume)
            {
                CoreUtils.SetRenderTarget(buffer, _Water._Mask.ColorRTH, _FogAfterTexture, _Mode is PortalMode.VolumeFlyThrough ? ClearFlag.DepthStencil : ClearFlag.All);
            }
            else
            {
                CoreUtils.SetRenderTarget(buffer, _FogAfterTexture, ClearFlag.DepthStencil);
            }

            Helpers.ScaleViewport(camera, buffer, _FogAfterTexture);

            buffer.SetGlobalTexture(ShaderIDs.s_PortalFogAfterTexture, _FogAfterTexture);

            buffer.DrawMesh
            (
                _Geometry.sharedMesh,
                _Geometry.transform.localToWorldMatrix,
                _PortalMaterial,
                submeshIndex: 0,
                (int)VolumePass.FrontFace,
                properties
            );

            buffer.SetInvertCulling(false);

            if (_UnderWater.UseStencilBuffer)
            {
                var depth = mask.DepthRTH;
                // CopyTexture does not like being passed non-created RTs.
                if (!depth.rt.IsCreated()) depth.rt.Create();
                // Copy only the stencil by copying everything and clearing only depth.
                buffer.CopyTexture(_FogBeforeTexture, depth);
                CoreUtils.SetRenderTarget(buffer, depth, ClearFlag.Depth);
            }


            buffer.EndSample(k_DrawPortal);
        }

        internal bool RenderEffect(Camera camera, CommandBuffer buffer, Material material, System.Action<CommandBuffer> copyColor, System.Action<CommandBuffer> resetRenderTargets, MaterialPropertyBlock properties = null)
        {
            if (camera.cameraType == CameraType.Reflection)
            {
                return true;
            }

            if (Mode == PortalMode.Tunnel)
            {
                return true;
            }

            buffer.SetInvertCulling(Invert);

            material.SetKeyword("d_Crest_ComputeMask", NegativeVolume && !_Water.Underwater.UseLegacyMask);

            // Draw front-face.
            buffer.DrawMesh
            (
                _Geometry.sharedMesh,
                _Geometry.transform.localToWorldMatrix,
                material,
                submeshIndex: 0,
                // Mode maps to front-face passes.
                shaderPass: NegativeVolume ? (int)EffectPass.VolumeFrontFace2D : (int)_Mode,
                properties
            );

            buffer.SetInvertCulling(false);

            if (NegativeVolume)
            {
                // We need to fill the masked area or it will be fogged/unfogged incorrectly (inverted).
                if (_Water.Underwater.UseLegacyMask)
                {
                    buffer.BeginSample("Crest.DrawMask");
                    var colorID = ShaderIDs.s_TemporaryMaskColor;
                    var depthID = ShaderIDs.s_TemporaryMaskDepth;
                    buffer.GetTemporaryRT(colorID, _Water._Mask.ColorDescriptor);
                    buffer.GetTemporaryRT(depthID, _Water._Mask.DepthDescriptor);
                    buffer.CopyTexture(_Water._Mask.ColorRT, colorID);
                    buffer.CopyTexture(_Water._Mask.DepthRT, depthID);
                    _Water._Mask.ResetRenderTarget(buffer);
                    buffer.SetGlobalTexture(ShaderIDs.s_MaskColorSource, colorID);
                    _Water.Surface.Render(camera, buffer, _UnderWater._MaskMaterial, UnderwaterRenderer.k_ShaderPassWaterSurfaceDepth);
                    // Revert depth.
                    buffer.CopyTexture(depthID, _Water._Mask.DepthRT);
                    buffer.ReleaseTemporaryRT(colorID);
                    buffer.ReleaseTemporaryRT(depthID);
                    resetRenderTargets(buffer);
                    buffer.EndSample("Crest.DrawMask");
                }

                copyColor(buffer);
                buffer.SetInvertCulling(Invert);

                buffer.DrawMesh
                (
                    _Geometry.sharedMesh,
                    _Geometry.transform.localToWorldMatrix,
                    material,
                    submeshIndex: 0,
                    shaderPass: (int)EffectPass.VolumeNegativeFogAfter,
                    properties
                );

                buffer.SetInvertCulling(false);

                // We only sample from our custom color in legacy.
                if (_UnderWater.RenderBeforeTransparency)
                {
                    copyColor(buffer);
                }

                buffer.DrawProcedural
                (
                    Matrix4x4.identity,
                    material,
                    shaderPass: (int)EffectPass.VolumeNegative,
                    MeshTopology.Triangles,
                    vertexCount: 3,
                    instanceCount: 1,
                    properties
                );
            }

            if (_Mode == PortalMode.VolumeFlyThrough)
            {
                // Draw back-face.
                buffer.DrawMesh
                (
                    _Geometry.sharedMesh,
                    _Geometry.transform.localToWorldMatrix,
                    material,
                    submeshIndex: 0,
                    shaderPass: (int)EffectPass.VolumeBackFace,
                    properties
                );

                // Draw over scene.
                buffer.DrawProcedural
                (
                    Matrix4x4.identity,
                    material,
                    shaderPass: (int)EffectPass.VolumeScene,
                    MeshTopology.Triangles,
                    vertexCount: 3,
                    instanceCount: 1,
                    properties
                );
            }

            return false;
        }

        // Returns whether the fullscreen volume pass is required.
        internal bool RenderMeniscus<T>(T commands, Material material) where T : ICommandWrapper
        {
            if (_Water.Underwater.UseLegacyMask && _Water._Portals.Mode == PortalMode.Tunnel)
            {
                return true;
            }

            var offset = _UnderWater.UseLegacyMask ? (int)MeniscusPass.Length : 0;

            material.SetBoolean(ShaderIDs.s_PortalInverted, Invert);

            if (Mode == PortalMode.Tunnel)
            {
                commands.DrawFullScreenTriangle
                (
                    material,
                    pass: offset + (int)MeniscusPass.FullScreen,
                    // Make sure near plane mask is bound.
                    _UnderWater.UseLegacyMask ? null : _Water.Surface._SurfaceDataMPB
                );

                return false;
            }

            commands.SetInvertCulling(Invert);

            commands.DrawMesh
            (
                _Geometry.sharedMesh,
                _Geometry.transform.localToWorldMatrix,
                material,
                pass: offset + (int)MeniscusPass.Front,
                null
            );

            // Skip fly-through volume for now as we do surface edge fading.
            if (NegativeVolume || (false && !_UnderWater.UseLegacyMask && HasBackFace))
            {
                commands.DrawMesh
                (
                    _Geometry.sharedMesh,
                    _Geometry.transform.localToWorldMatrix,
                    material,
                    pass: offset + (int)MeniscusPass.Back,
                    null
                );
            }

            commands.SetInvertCulling(false);

            // Needs full-screen pass.
            return NegativeVolume || Mode is PortalMode.VolumeFlyThrough;
        }

        internal bool RenderLineMask(CommandBuffer commands, RenderTargetIdentifier target)
        {
            CoreUtils.SetRenderTarget(commands, target, ClearFlag.Color);

            {
                if (s_MaskMaterial == null)
                {
                    s_MaskMaterial = new(WaterResources.Instance.Shaders._PortalsMask);
                }

                commands.DrawMesh(_Geometry.sharedMesh, _Geometry.transform.localToWorldMatrix, s_MaskMaterial, 0, -1);
            }

            return true;
        }

        void Rebuild()
        {
            if (!_Enabled) return;
            Disable();
            Enable();
        }

        void SetEnabled(bool previous, bool current)
        {
            if (previous == current) return;
            if (_Water == null || !_Water.isActiveAndEnabled) return;
            if (_Enabled) OnEnable(); else OnDisable();
        }

        bool GetInvert()
        {
            return _Invert && (_Mode is not PortalMode.VolumeFlyThrough and not PortalMode.Tunnel);
        }

        void SetInvert(bool previous, bool current)
        {
            if (previous == current) return;
            if (_Water == null || !_Water.isActiveAndEnabled) return;

            _UnderWater._NeedsColorTexture = NegativeVolume;

            if (_Mode != PortalMode.Volume) return;

            if (NegativeVolume)
            {
                _Water._Mask.Add(UnderwaterRenderer.k_VolumeMaskQueue + 1, _AfterVolumeMaskPass);
            }
            else
            {
                _Water._Mask.Remove(_AfterVolumeMaskPass);
            }
        }

        void SetMode(PortalMode previous, PortalMode current)
        {
            if (previous == current) return;
            if (_Water == null || !_Water.isActiveAndEnabled || !_Enabled) return;
            Rebuild();
        }

        void SetGeometry(MeshFilter previous, MeshFilter current)
        {
            if (previous == current) return;
            if (previous != null && current != null) return;
            if (_Water == null || !_Water.isActiveAndEnabled || !_Enabled) return;
            // TODO: This is overkill. Reduce to bare minimum.
            Rebuild();
        }

#if UNITY_EDITOR
        [@OnChange]
        void OnChange(string propertyPath, object previousValue)
        {
            switch (propertyPath)
            {
                case nameof(_Enabled): SetEnabled((bool)previousValue, _Enabled); break;
                case nameof(_Mode): SetMode((PortalMode)previousValue, _Mode); break;
                case nameof(_Geometry): SetGeometry((MeshFilter)previousValue, _Geometry); break;
                case nameof(_Invert): SetInvert((bool)previousValue, _Invert); break;
            }
        }
#endif
    }

    partial class PortalRenderer
    {
        MaskRenderer.MaskInput MaskRenderer.IMaskProvider.Allocate()
        {
            return MaskRenderer.MaskInput.None;
        }

        MaskRenderer.MaskInput MaskRenderer.IMaskProvider.Write(Camera camera)
        {
            return Active ? MaskRenderer.MaskInput.Zero : MaskRenderer.MaskInput.None;
        }

        void MaskRenderer.IMaskProvider.OnMaskPass(CommandBuffer commands, Camera camera, MaskRenderer mask)
        {
            SetUpMaskStencil(_UnderWater._MaskMaterial);
            SetUpMaskStencil(_UnderWater._HorizonMaskMaterial);
            RenderMask(camera, commands, _UnderWater._MaskMaterial, mask);
        }

        sealed class AfterVolumeMaskPass : MaskRenderer.IMaskProvider
        {
            readonly WaterRenderer _Water;
            readonly PortalRenderer _Portal;
            readonly UnderwaterRenderer _UnderWater;

            public bool Required => _Portal.NegativeVolume || _Portal._Mode is PortalMode.Tunnel;

            public AfterVolumeMaskPass(PortalRenderer portal)
            {
                _Water = portal._Water;
                _Portal = portal;
                _UnderWater = portal._UnderWater;
            }

            public MaskRenderer.MaskInput Allocate()
            {
                // Let underwater allocate.
                return MaskRenderer.MaskInput.None;
            }

            public void OnMaskPass(CommandBuffer commands, Camera camera, MaskRenderer mask)
            {
                if (_Portal._Mode is PortalMode.Tunnel)
                {
                    // Remove underwater inside portal.
                    commands.DrawMesh
                    (
                        _Portal._Geometry.sharedMesh,
                        _Portal._Geometry.transform.localToWorldMatrix,
                        _Portal._PortalMaterial,
                        submeshIndex: 0,
                        (int)VolumePass.Tunnel,
                        null
                    );
                }
            }

            public MaskRenderer.MaskInput Write(Camera camera)
            {
                // Underwater is earlier in the queue.
                if (!_UnderWater._MaskRead)
                {
                    return MaskRenderer.MaskInput.None;
                }

                return _Portal.NegativeVolume
                    ? MaskRenderer.MaskInput.Depth
                    : _Portal._Mode is PortalMode.Tunnel
                    ? MaskRenderer.MaskInput.Both
                    : MaskRenderer.MaskInput.None;
            }
        }
    }

    // Surface Data.
    partial class PortalRenderer
    {
        RenderTexture _HeightRT;
        internal RenderTexture HeightRT => _HeightRT;
        CommandBuffer _BeforeRenderingCommands;
        Material _DisplacedMaterial;

        SurfaceRenderer.SurfaceDataParameters _SurfaceDataParameters;

        internal void UpdateDisplacedSurfaceData()
        {
            if (_UnderWater.UseLegacyMask)
            {
                return;
            }

            if (_Geometry == null)
            {
                return;
            }

            // Tunnel has no special meniscus handling.
            if (Mode == PortalMode.Tunnel)
            {
                return;
            }

            Helpers.SetGlobalBoolean(SurfaceRenderer.ShaderIDs.s_WaterLineFlatWater, _Water.Surface.IsQuadMesh);

            if (_Water.Surface.IsQuadMesh)
            {
                return;
            }

            if (_DisplacedMaterial == null)
            {
                _DisplacedMaterial = new(WaterResources.Instance.Shaders._UnderwaterMask);
            }

            _BeforeRenderingCommands ??= new();
            var commands = _BeforeRenderingCommands;
            commands.name = "Crest.DrawMask/Portal";
            commands.Clear();

            var bounds = _Geometry.transform.TransformBounds(_Geometry.sharedMesh.bounds);

            _Water.Surface.UpdateDisplacedSurfaceData
            (
                commands,
                bounds,
                "_Crest_WaterLinePortal",
                ref _HeightRT,
                _TexelSize,
                _MaximumResolution,
                out _SurfaceDataParameters
            );

            foreach (var chunk in _Water.Surface.Chunks)
            {
                if (!bounds.Intersects(chunk.Rend.bounds))
                {
                    continue;
                }

                commands.DrawMesh
                (
                    chunk._Mesh,
                    chunk.transform.localToWorldMatrix,
                    _DisplacedMaterial,
                    submeshIndex: 0,
                    shaderPass: SurfaceRenderer.k_SurfaceDataShaderPass,
                    chunk._MaterialPropertyBlock
                );
            }

            Graphics.ExecuteCommandBuffer(commands);
        }
    }
}
