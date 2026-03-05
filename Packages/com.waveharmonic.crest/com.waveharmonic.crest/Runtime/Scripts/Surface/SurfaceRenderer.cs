// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WaveHarmonic.Crest.Internal;

namespace WaveHarmonic.Crest
{
    enum WaterMeshType
    {
        [Tooltip("Chunks implemented as a clip-map.")]
        Chunks,

        [Tooltip("A single quad.\n\nOptimal for demanding platforms like mobile. Displacement will only contribute to normals.")]
        Quad,
    }

    /// <summary>
    /// Renders the water surface.
    /// </summary>
    [System.Serializable]
    public sealed partial class SurfaceRenderer
    {
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _Version = 0;
#pragma warning restore 414

        [@Space(10)]

        [Tooltip("Whether the underwater effect is enabled.\n\nAllocates/releases resources if state has changed.")]
        [@GenerateAPI(Getter.Custom, Setter.Custom)]
        [@DecoratedField, SerializeField]
        internal bool _Enabled = true;

        [@Label("Mesh")]
        [Tooltip("The meshing solution for the water surface.")]
        [@DecoratedField]
        [@SerializeField]
        WaterMeshType _MeshType;

        [Tooltip("The water chunk renderers will have this layer.")]
        [@Layer]
        [@GenerateAPI]
        [SerializeField]
        internal int _Layer = 4; // Water

        [Tooltip("Material to use for the water surface.")]
        [@AttachMaterialEditor(order: 0)]
        [@MaterialField("Crest/Water", name: "Water", title: "Create Water Material")]
        [@GenerateAPI]
        [SerializeField]
        internal Material _Material = null;

        [Tooltip("Underwater will copy from this material if set.\n\nUseful for overriding properties for the underwater effect. To see what properties can be overriden, see the disabled properties on the underwater material. This does not affect the surface.")]
        [@AttachMaterialEditor(order: 1)]
        [@MaterialField("Crest/Water", name: "Water (Below)", title: "Create Water Material", parent: "_Material")]
        [@GenerateAPI]
        [SerializeField]
        internal Material _VolumeMaterial = null;

        [Tooltip("Template for water chunks as a prefab.\n\nThe only requirements are that the prefab must contain a MeshRenderer at the root and not a MeshFilter or WaterChunkRenderer. MR values will be overwritten where necessary and the prefabs are linked in edit mode.")]
        [@PrefabField(title: "Create Chunk Prefab", name: "Water Chunk")]
        [SerializeField]
        internal GameObject _ChunkTemplate;

        [@Space(10)]

        [Tooltip("Have the water surface cast shadows for albedo (both foam and custom).")]
        [@GenerateAPI(Getter.Custom)]
        [@DecoratedField, SerializeField]
        internal bool _CastShadows;

        [@Heading("Culling")]

        [Tooltip("Whether 'Water Body' components will cull the water tiles.\n\nDisable if you want to use the 'Material Override' feature and still have an ocean.")]
        [@GenerateAPI]
        [@DecoratedField, SerializeField]
        internal bool _WaterBodyCulling = true;

        [Tooltip("How many frames to distribute the chunk bounds calculation.\n\nThe chunk bounds are calculated per frame to ensure culling is correct when using inputs that affect displacement. Some performance can be saved by distributing the load over several frames. The higher the frames, the longer it will take - lowest being instant.")]
        [@Range(1, 30, Range.Clamp.Minimum)]
        [@GenerateAPI]
        [SerializeField]
        internal int _TimeSliceBoundsUpdateFrameCount = 1;

        [@Heading("Advanced")]

        [Tooltip("Rules to exclude cameras from surface rendering.\n\nThese are exclusion rules, so for all cameras, select Nothing. These rules are applied on top of the Layer rules.")]
        [@DecoratedField]
        [@GenerateAPI]
        [SerializeField]
        internal WaterCameraExclusion _CameraExclusions = WaterCameraExclusion.Hidden | WaterCameraExclusion.Reflection;

        [Tooltip("How to handle self-intersections of the water surface.\n\nThey can be caused by choppy waves which can cause a flipped underwater effect. When not using the portals/volumes, this fix is only applied when within 2 metres of the water surface. Automatic will disable the fix if portals/volumes are used which is the recommend setting.")]
        [@DecoratedField, SerializeField]
        internal SurfaceSelfIntersectionFixMode _SurfaceSelfIntersectionFixMode = SurfaceSelfIntersectionFixMode.Automatic;

        [Tooltip("Whether to allow sorting using the render queue.\n\nIf you need to change the minor part of the render queue (eg +100), then enable this option. As a side effect, it will also disable the front-to-back rendering optimization for Crest. This option does not affect changing the major part of the render queue (eg AlphaTest, Transparent), as that is always allowed.\n\nRender queue sorting is required for some third-party integrations.")]
        [@Predicated(RenderPipeline.HighDefinition, inverted: true, hide: true)]
        [@GenerateAPI]
        [@DecoratedField, SerializeField]
        internal bool _AllowRenderQueueSorting;

        [@Space(10)]

#if !CREST_DEBUG
        [HideInInspector]
#endif
        [@DecoratedField, SerializeField]
        internal DebugFields _Debug = new();

        [System.Serializable]
        internal sealed class DebugFields
        {
#if !CREST_DEBUG
            [HideInInspector]
#endif
            [Tooltip("Whether to generate water geometry tiles uniformly (with overlaps).")]
            [@DecoratedField, SerializeField]
            public bool _UniformTiles;

#if !CREST_DEBUG
            [HideInInspector]
#endif
            [Tooltip("Disable generating a wide strip of triangles at the outer edge to extend water to edge of view frustum.")]
            [@DecoratedField, SerializeField]
            public bool _DisableSkirt;
        }

        const string k_DrawWaterSurface = "Surface";

        internal WaterRenderer _Water;
        internal Transform Root { get; private set; }
        internal List<WaterChunkRenderer> Chunks { get; } = new();
        internal bool _Rebuild;


        //
        // Level of Detail
        //

        readonly MaterialPropertyBlock[] _PerCascadeMPB = new MaterialPropertyBlock[Lod.k_MaximumSlices];
        internal MaterialPropertyBlock[] PerCascadeMPB { get; private set; }

        // We are computing these values to be optimal based on the base mesh vertex density.
        float _LodAlphaBlackPointFade;
        float _LodAlphaBlackPointWhitePointFade;


        //
        // Culling
        //

        internal readonly Plane[] _CameraFrustumPlanes = new Plane[6];
        bool _CanSkipCulling;
        internal bool _DoneChunkVisibility;


        //
        // Events
        //

        /// <summary>
        /// Invoked after water chunk modification.
        /// </summary>
        /// <remarks>
        /// Gives an opportunity to modify the renderer.
        /// </remarks>
        public static System.Action<Renderer> OnCreateChunkRenderer { get; set; }


        internal Material _MotionVectorMaterial;

        internal Material AboveOrBelowSurfaceMaterial => _VolumeMaterial == null ? _Material : _VolumeMaterial;
        internal bool IsQuadMesh => _MeshType == WaterMeshType.Quad;


        //
        // Facing
        //

        internal enum SurfaceSelfIntersectionFixMode
        {
            [Tooltip("Uses VFACE/IsFrontFace.")]
            Off,

            [Tooltip("Force entire water surface to render as below water.")]
            ForceBelowWater,

            [Tooltip("Force entire water surface to render as above water.")]
            ForceAboveWater,

            [Tooltip("Force entire water surface to render as above or below water if beyond a distance from surface, otherwise use mask/facing.")]
            On,

            [Tooltip("Force entire water surface to render as above or below water if beyond a distance from surface (except in special circumstances like  Portals).")]
            Automatic,
        }

        enum ForceFacing
        {
            None,
            BelowWater,
            AboveWater,
            Facing,
        }


        static partial class ShaderIDs
        {
            public static readonly int s_ForceUnderwater = Shader.PropertyToID("g_Crest_ForceUnderwater");
            public static readonly int s_LodAlphaBlackPointFade = Shader.PropertyToID("g_Crest_LodAlphaBlackPointFade");
            public static readonly int s_LodAlphaBlackPointWhitePointFade = Shader.PropertyToID("g_Crest_LodAlphaBlackPointWhitePointFade");

            public static readonly int s_BuiltShadowCasterZTest = Shader.PropertyToID("_Crest_BUILTIN_ShadowCasterZTest");

            public static readonly int s_ChunkMeshScaleAlpha = Shader.PropertyToID("_Crest_ChunkMeshScaleAlpha");
            public static readonly int s_ChunkGeometryGridWidth = Shader.PropertyToID("_Crest_ChunkGeometryGridWidth");
            public static readonly int s_ChunkFarNormalsWeight = Shader.PropertyToID("_Crest_ChunkFarNormalsWeight");
            public static readonly int s_ChunkNormalScrollSpeed = Shader.PropertyToID("_Crest_ChunkNormalScrollSpeed");
            public static readonly int s_NormalMapParameters = Shader.PropertyToID("_Crest_NormalMapParameters");
        }

        bool _ForceRenderingOff;

        internal bool ForceRenderingOff
        {
            get => _ForceRenderingOff;
            set
            {
                _ForceRenderingOff = value;

                if (_Enabled)
                {
                    Root.gameObject.SetActive(!_ForceRenderingOff && !IsQuadMesh);
                }
            }
        }

        internal void Initialize()
        {
            Root = Builder.GenerateMesh(_Water, this, Chunks, _Water.LodResolution, _Water._GeometryDownSampleFactor, _Water.LodLevels);

            Root.position = _Water.Position;
            Root.localScale = new(_Water.Scale, 1f, _Water.Scale);

            // Populate MPBs with defaults. Protects against null exceptions etc.
            PerCascadeMPB = _PerCascadeMPB;
            NormalMapParameters = _NormalMapParameters;
            InitializeProperties();

            // Resolution is 4 tiles across.
            var baseMeshDensity = _Water.LodResolution * 0.25f / _Water._GeometryDownSampleFactor;
            // 0.4f is the "best" value when base mesh density is 8. Scaling down from there produces results similar to
            // hand crafted values which looked good when the water is flat.
            _LodAlphaBlackPointFade = 0.4f / (baseMeshDensity / 8f);
            _LodAlphaBlackPointWhitePointFade = 1f - _LodAlphaBlackPointFade - _LodAlphaBlackPointFade;

            Shader.SetGlobalFloat(ShaderIDs.s_LodAlphaBlackPointFade, _LodAlphaBlackPointFade);
            Shader.SetGlobalFloat(ShaderIDs.s_LodAlphaBlackPointWhitePointFade, _LodAlphaBlackPointWhitePointFade);

            UpdateMaterial(_Material, ref _MotionVectorMaterial);

            _CanSkipCulling = false;

            if (RenderPipelineHelper.IsLegacy)
            {
                LegacyOnEnable();
            }

#if UNITY_EDITOR
            EnableWaterLevelDepthTexture();
#endif
        }

        internal void OnDestroy()
        {
#if UNITY_EDITOR
            DisableWaterLevelDepthTexture();
#endif

            // Clean up everything created through the Water Builder.
            // Not every mesh is assigned to a chunk thus we should destroy all of them here.
            for (var i = 0; i < _Meshes?.Length; i++)
            {
                Helpers.Destroy(_Meshes[i]);
            }

            Chunks.Clear();
            CoreUtils.Destroy(_MotionVectorMaterial);
            CoreUtils.Destroy(_DisplacedMaterial);

            if (Root != null)
            {
                CoreUtils.Destroy(Root.gameObject);
                Root = null;
            }

            if (RenderPipelineHelper.IsLegacy)
            {
                LegacyOnDisable();
            }
        }

        void ShowHiddenObjects(bool show)
        {
            foreach (var chunk in Chunks)
            {
                chunk.gameObject.hideFlags = show ? HideFlags.DontSave : HideFlags.HideAndDontSave;
            }
        }

        // Chunk Visibility.
        // check if needed here
        // complicated. cos we would have to either check everything that may need it
        // or have a loop going over an abstraction
        internal void UpdateChunkVisibility(Camera camera)
        {
            if (_DoneChunkVisibility)
            {
                return;
            }

            if (IsQuadMesh)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(camera, _CameraFrustumPlanes);

            foreach (var chunk in Chunks)
            {
                var renderer = chunk.Rend;
                // Can happen in edit mode.
                if (renderer == null) continue;
                chunk._Visible = GeometryUtility.TestPlanesAABB(_CameraFrustumPlanes, renderer.bounds);
            }

            _DoneChunkVisibility = true;
        }

        internal void UpdateMaterial(Material material, ref Material motion)
        {
            if (material == null)
            {
                return;
            }

            var enable = !_Water.RenderBeforeTransparency;
            material.SetShaderPassEnabled("Forward", enable);
            material.SetShaderPassEnabled("ForwardAdd", enable);
            material.SetShaderPassEnabled("ForwardBase", enable);
            material.SetShaderPassEnabled("UniversalForward", enable);

            // HDRP will automatically disable this pass for unknown reasons. It might be that
            // we are sampling from the depth texture which does not work with shadow casting.
            if (RenderPipelineHelper.IsHighDefinition)
            {
                material.SetShaderPassEnabled("ShadowCaster", _CastShadows);
            }

            UpdateMotionVectorsMaterial(material, ref motion);
        }

        internal static bool IsTransparent(Material material)
        {
            return RenderPipelineHelper.IsLegacy
                ? material.IsKeywordEnabled("_BUILTIN_SURFACE_TYPE_TRANSPARENT")
                : material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
        }

        void Rebuild()
        {
            OnDestroy();
            Initialize();
            _Rebuild = false;
        }

        internal void DisableChunks()
        {
            foreach (var chunk in Chunks)
            {
                if (chunk.Rend != null) chunk.Rend.enabled = false;
            }
        }

        internal bool ShouldRender(Camera camera)
        {
            if (!_Enabled)
            {
                return false;
            }

            if (!WaterRenderer.ShouldRender(camera, Layer, _CameraExclusions))
            {
                return false;
            }

            // Our planar reflection camera must never render the surface.
            if (camera == _Water.Reflections.ReflectionCamera)
            {
                return false;
            }

            if (Material == null)
            {
                return false;
            }

            return true;
        }

        internal void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!ShouldRender(camera))
            {
                return;
            }

            WritePerCameraMaterialParameters(camera);

            // Motion Vectors.
            if (ShouldRenderMotionVectors(camera) && QueueMotionVectors)
            {
                UpdateChunkVisibility(camera);

                foreach (var chunk in Chunks)
                {
                    chunk.RenderMotionVectors(this, camera);
                }
            }

#pragma warning disable format
#if d_UnityURP
            if (RenderPipelineHelper.IsUniversal)
            {
#if UNITY_EDITOR
                WaterLevelDepthTextureURP.s_Instance?.OnBeginCameraRendering(context, camera);
#endif
                WaterSurfaceRenderPass.Instance?.OnBeginCameraRendering(context, camera);
            }
            else
#endif

            if (RenderPipelineHelper.IsLegacy)
            {
                OnBeginCameraRenderingLegacy(camera);
            }
#pragma warning restore format
        }

        internal void OnEndCameraRendering(Camera camera)
        {
            _DoneChunkVisibility = false;

            // Restore in case exclusion culling ran.
            foreach (var chunk in Chunks)
            {
                if (chunk.Rend != null && !chunk._Culled) chunk.Rend.enabled = true;
            }

            if (!WaterRenderer.ShouldRender(camera, Layer))
            {
                return;
            }

            // Our planar reflection camera must never render the surface.
            if (camera == _Water.Reflections.ReflectionCamera)
            {
                return;
            }

            if (RenderPipelineHelper.IsLegacy)
            {
                OnEndCameraRenderingLegacy(camera);
            }
        }

        void InitializeProperties()
        {
            System.Array.Fill(NormalMapParameters, new Vector4(0, 0, 1, 0));

            // Populate MPBs with defaults.
            for (var index = 0; index < PerCascadeMPB.Length; index++)
            {
                var block = new MaterialPropertyBlock();
                block.SetInteger(Lod.ShaderIDs.s_LodIndex, index);
                block.SetFloat(ShaderIDs.s_ChunkFarNormalsWeight, 1f);
                PerCascadeMPB[index] = block;
            }
        }

        void WritePerCameraMaterialParameters(Camera camera)
        {
            if (Material == null)
            {
                return;
            }

            // If no underwater, then no need for underwater surface.
            if (!_Water.Underwater.Enabled)
            {
                Shader.SetGlobalInteger(ShaderIDs.s_ForceUnderwater, (int)ForceFacing.AboveWater);
                return;
            }

            _Water.UpdatePerCameraHeight(camera);

            // Override isFrontFace when camera is far enough from the water surface to fix self-intersecting waves.
            // Hack - due to SV_IsFrontFace occasionally coming through as true for back faces,
            // add a param here that forces water to be in underwater state. I think the root
            // cause here might be imprecision or numerical issues at water tile boundaries, although
            // i'm not sure why cracks are not visible in this case.
            var height = _Water._ViewerHeightAboveWaterPerCamera;

            var value = _SurfaceSelfIntersectionFixMode switch
            {
                SurfaceSelfIntersectionFixMode.On =>
                      !_Water._PerCameraHeightReady
                    ? ForceFacing.None
                    : height < -2f
                    ? ForceFacing.BelowWater
                    : height > 2f
                    ? ForceFacing.AboveWater
                    : ForceFacing.None,
                // Skip for portals as it is possible to see both sides of the surface at any position.
                SurfaceSelfIntersectionFixMode.Automatic =>
                      _Water.Portaled || !_Water._PerCameraHeightReady
                    ? ForceFacing.None
                    : height < -2f
                    ? ForceFacing.BelowWater
                    : height > 2f
                    ? ForceFacing.AboveWater
                    : ForceFacing.None,
                // Always use facing (VFACE).
                SurfaceSelfIntersectionFixMode.Off => ForceFacing.Facing,
                _ => (ForceFacing)_SurfaceSelfIntersectionFixMode,
            };

            Shader.SetGlobalInteger(ShaderIDs.s_ForceUnderwater, (int)value);
        }

        internal void LateUpdate()
        {
            if (_Rebuild)
            {
                Rebuild();
            }

            if (_ForceRenderingOff)
            {
                return;
            }

            LoadCameraData(_Water.CurrentCamera);

            Root.position = _Water.Position;
            Root.localScale = new(_Water.Scale, 1f, _Water.Scale);

            Root.gameObject.SetActive(!IsQuadMesh);

            if (Material != null)
            {
                Material.SetKeyword(new(Material.shader, "_CREST_CUSTOM_MESH"), IsQuadMesh);
            }

            WritePerCascadeInstanceData();

            if (IsQuadMesh)
            {
                LateUpdateQuadMesh();
                return;
            }

            foreach (var chunk in Chunks)
            {
                chunk.UpdateMeshBounds(_Water, this);
            }

            ApplyWaterBodyCulling();

            LateUpdateMotionVectors();

            UpdateMaterial(_Material, ref _MotionVectorMaterial);

            foreach (var body in WaterBody.WaterBodies)
            {
                if (body._Material != null)
                {
                    UpdateMaterial(body._Material, ref body._MotionVectorMaterial);
                }
            }

            foreach (var chunk in Chunks)
            {
                chunk.OnLateUpdate();
            }
        }

        void WritePerCascadeInstanceData()
        {
            var levels = _Water.LodLevels;
            var texel = _Water.LodResolution * 0.25f / _Water._GeometryDownSampleFactor;

            // LOD 0
            {
                // Blend LOD 0 shape in/out to avoid pop, if scale could increase.
                PerCascadeMPB[0].SetFloat(ShaderIDs.s_ChunkMeshScaleAlpha, _Water.ScaleCouldIncrease ? _Water.ViewerAltitudeLevelAlpha : 0f);
            }

            // LOD N
            {
                // Blend furthest normals scale in/out to avoid pop, if scale could reduce.
                var weight = _Water.ScaleCouldDecrease ? _Water.ViewerAltitudeLevelAlpha : 1f;
                PerCascadeMPB[levels - 1].SetFloat(ShaderIDs.s_ChunkFarNormalsWeight, weight);
                NormalMapParameters[levels - 1] = new(0, 0, weight, 0);
            }

            for (var index = 0; index < levels; index++)
            {
                var mpb = PerCascadeMPB[index];

                // geometry data
                // compute grid size of geometry. take the long way to get there - make sure we land exactly on a power of two
                // and not inherit any of the lossy-ness from lossyScale.
                var scale = _Water.CascadeData.Current[index].x;
                var width = scale / texel;

                mpb.SetFloat(ShaderIDs.s_ChunkGeometryGridWidth, width);

                var mul = 1.875f; // fudge 1
                var pow = 1.4f; // fudge 2
                var texelWidth = width / _Water._GeometryDownSampleFactor;
                var speed = new Vector2
                (
                    Mathf.Pow(Mathf.Log(1f + 2f * texelWidth) * mul, pow),
                    Mathf.Pow(Mathf.Log(1f + 4f * texelWidth) * mul, pow)
                );

                mpb.SetVector(ShaderIDs.s_ChunkNormalScrollSpeed, speed);

                var normals = NormalMapParameters[index];
                normals.x = speed.x;
                normals.y = speed.y;
                NormalMapParameters[index] = normals;
            }
        }

        void ApplyWaterBodyCulling()
        {
            var canSkipCulling = WaterBody.WaterBodies.Count == 0 && _CanSkipCulling;

            // Chunk bounds needs to be up-to-date at this point.
            foreach (var tile in Chunks)
            {
                if (tile.Rend == null)
                {
                    continue;
                }

                tile._Culled = false;
                tile.MaterialOverridden = false;

                // If there are local bodies of water, this will do overlap tests between the water tiles
                // and the water bodies and turn off any that don't overlap.
                if (!canSkipCulling)
                {
                    var chunkBounds = tile.Rend.bounds;
                    var chunkUndisplacedBoundsXZ = tile.UnexpandedBoundsXZ;

                    var largestOverlap = 0f;
                    var overlappingOne = false;
                    foreach (var body in WaterBody.WaterBodies)
                    {
                        // If tile has already been excluded from culling, then skip this iteration. But finish this
                        // iteration if the water body has a material override to work out most influential water body.
                        if (overlappingOne && body.AboveSurfaceMaterial == null)
                        {
                            continue;
                        }

                        var bounds = body.AABB;

                        var overlapping =
                            bounds.max.x > chunkBounds.min.x && bounds.min.x < chunkBounds.max.x &&
                            bounds.max.z > chunkBounds.min.z && bounds.min.z < chunkBounds.max.z;
                        if (overlapping)
                        {
                            overlappingOne = true;

                            if (body.AboveSurfaceMaterial != null)
                            {
                                var overlap = 0f;
                                {
                                    // Use the unexpanded bounds to prevent leaking as generally this feature will be
                                    // for an inland body of water where hopefully there is attenuation between it and
                                    // the water to handle the water's displacement. The inland water body will unlikely
                                    // have large displacement but can be mitigated with a decent buffer zone.
                                    var xMin = Mathf.Max(bounds.min.x, chunkUndisplacedBoundsXZ.min.x);
                                    var xMax = Mathf.Min(bounds.max.x, chunkUndisplacedBoundsXZ.max.x);
                                    var zMin = Mathf.Max(bounds.min.z, chunkUndisplacedBoundsXZ.min.y);
                                    var zMax = Mathf.Min(bounds.max.z, chunkUndisplacedBoundsXZ.max.y);
                                    if (xMin < xMax && zMin < zMax)
                                    {
                                        overlap = (xMax - xMin) * (zMax - zMin);
                                    }
                                }

                                // If this water body has the most overlap, then the chunk will get its material.
                                if (overlap > largestOverlap)
                                {
                                    tile.MaterialOverridden = true;
                                    tile.Rend.sharedMaterial = body.AboveSurfaceMaterial;
                                    tile._MotionVectorMaterial = body._MotionVectorMaterial;
                                    largestOverlap = overlap;
                                }
                            }
                            else
                            {
                                tile.MaterialOverridden = false;
                            }
                        }
                    }

                    tile._Culled = _WaterBodyCulling && !overlappingOne && WaterBody.WaterBodies.Count > 0;
                }

                tile.Rend.enabled = !tile._Culled;
            }

            // Can skip culling next time around if water body count stays at 0
            _CanSkipCulling = WaterBody.WaterBodies.Count == 0;
        }

        internal void Render(Camera camera, CommandBuffer buffer, Material material = null, int pass = 0, bool culled = false, MaterialPropertyBlock mpb = null)
        {
            var noMaterial = material == null;

            if (noMaterial && Material == null)
            {
                return;
            }

            if (IsQuadMesh)
            {
                buffer.DrawMesh(Helpers.QuadMesh, Matrix4x4.TRS(Root.position, Quaternion.Euler(90f, 0, 0), new(10000, 10000, 1)), noMaterial ? Material : material, 0, shaderPass: pass, mpb);
                return;
            }

            UpdateChunkVisibility(camera);

            // Spends approx 0.2-0.3ms here on 2018 Dell XPS 15.
            foreach (var chunk in Chunks)
            {
                var renderer = chunk.Rend;

                // Can happen in edit mode.
                if (renderer == null)
                {
                    continue;
                }

                if (!chunk._Visible)
                {
                    continue;
                }

                if (culled && chunk._Culled)
                {
                    continue;
                }

                // Make sure properties are bound for this frame.
                if (!chunk._WaterDataHasBeenBound)
                {
                    chunk.Bind();
                }

                if (noMaterial)
                {
                    material = renderer.sharedMaterial;
                }

                buffer.DrawRenderer(renderer, material, submeshIndex: 0, pass);
            }
        }
    }

    // API
    partial class SurfaceRenderer
    {
        bool GetEnabled()
        {
            return _Enabled && !_Water.IsRunningWithoutGraphics;
        }

        void SetEnabled(bool previous, bool current)
        {
            if (previous == current) return;
            if (_Water == null || !_Water.isActiveAndEnabled) return;
            if (_Enabled) Initialize(); else OnDestroy();
        }

        void SetLayer(int previous, int current)
        {
            if (previous == current) return;

            foreach (var chunk in Chunks)
            {
                chunk.gameObject.layer = current;
            }
        }

        bool GetCastShadows()
        {
            return _CastShadows;
        }

        void SetCastShadows(bool previous, bool current)
        {
            if (previous == current) return;

            foreach (var chunk in Chunks)
            {
                chunk.Rend.shadowCastingMode = current ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }

        void SetAllowRenderQueueSorting(bool previous, bool current)
        {
            if (previous == current) return;

            foreach (var chunk in Chunks)
            {
                chunk.Rend.sortingOrder = current ? chunk._SortingOrder : 0;
            }
        }
    }

    // Motion Vectors
    partial class SurfaceRenderer
    {
        bool _QueueMotionVectors;
        bool QueueMotionVectors => _QueueMotionVectors && !IsQuadMesh;

        bool ShouldRenderMotionVectors(Camera camera)
        {
            // Unity enables this when motion vectors are used - even for SRPs.
            if (!camera.depthTextureMode.HasFlag(DepthTextureMode.MotionVectors))
            {
                return false;
            }

            return true;
        }

        void LateUpdateMotionVectors()
        {
            _QueueMotionVectors = false;

            // Handled by Unity.
            if (RenderPipelineHelper.IsHighDefinition)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (!_Water.WriteMotionVectors)
            {
                return;
            }

            // This will not support WBs with material overrides, but mixing opaque and
            // transparent would be odd.
            if (!IsTransparent(Material))
            {
                return;
            }

            var pool = ArrayPool<Camera>.Shared;
            var cameras = pool.Rent(Camera.allCamerasCount);
            Camera.GetAllCameras(cameras);

            for (var i = 0; i < Camera.allCamerasCount; i++)
            {
                var camera = cameras[i];

                if (!WaterRenderer.ShouldRender(camera, _Layer))
                {
                    continue;
                }

                if (!ShouldRenderMotionVectors(camera))
                {
                    continue;
                }

                _QueueMotionVectors = true;
            }

            pool.Return(cameras);
        }

        void UpdateMotionVectorsMaterial(Material surface, ref Material motion)
        {
            if (!QueueMotionVectors)
            {
                return;
            }

            if (motion == null || motion.shader != surface.shader)
            {
                CoreUtils.Destroy(motion);
                motion = CoreUtils.CreateEngineMaterial(surface.shader);

                // BIRP
                motion.SetShaderPassEnabled("ForwardBase", false);
                motion.SetShaderPassEnabled("ForwardAdd", false);
                motion.SetShaderPassEnabled("Deferred", false);

                // URP
                motion.SetShaderPassEnabled("UniversalForward", false);
                motion.SetShaderPassEnabled("UniversalGBuffer", false);
                motion.SetShaderPassEnabled("Universal2D", false);

                motion.SetShaderPassEnabled("ShadowCaster", false);
                motion.SetShaderPassEnabled("DepthOnly", false);
                motion.SetShaderPassEnabled("DepthNormals", false);
                motion.SetShaderPassEnabled("Meta", false);
                motion.SetShaderPassEnabled("SceneSelectionPass", false);
                motion.SetShaderPassEnabled("Picking", false);
                motion.SetShaderPassEnabled("MotionVectors", true);
            }

            motion.CopyMatchingPropertiesFromMaterial(surface);
            motion.renderQueue = (int)RenderQueue.Geometry;
            motion.SetOverrideTag("RenderType", "Opaque");
            motion.SetFloat(Crest.ShaderIDs.Unity.s_Surface, 0); // SurfaceType.Opaque
            motion.SetFloat(Crest.ShaderIDs.Unity.s_SrcBlend, 1);
            motion.SetFloat(Crest.ShaderIDs.Unity.s_DstBlend, 0);
            motion.SetFloat(ShaderIDs.s_BuiltShadowCasterZTest, 1); // ZTest Never
        }
    }

    partial class SurfaceRenderer
    {
        internal Dictionary<Camera, MaterialPropertyBlock[]> _PerCameraPerCascadeMPB = new();
        internal Dictionary<Camera, Vector4[]> _PerCameraNormalMapParameters = new();

        void LoadCameraData(Camera camera)
        {
            if (!_Water.SeparateViewpoint)
            {
                return;
            }

            if (!_PerCameraPerCascadeMPB.ContainsKey(camera))
            {
                PerCascadeMPB = new MaterialPropertyBlock[Lod.k_MaximumSlices];
                _PerCameraPerCascadeMPB.Add(camera, PerCascadeMPB);
                NormalMapParameters = new Vector4[Lod.k_MaximumSlices];
                _PerCameraNormalMapParameters.Add(camera, NormalMapParameters);
                InitializeProperties();
            }
            else
            {
                PerCascadeMPB = _PerCameraPerCascadeMPB[camera];
                NormalMapParameters = _PerCameraNormalMapParameters[camera];
            }
        }

        internal void RemoveCameraData(Camera camera)
        {
            if (_PerCameraPerCascadeMPB.ContainsKey(camera))
            {
                _PerCameraPerCascadeMPB.Remove(camera);
                _PerCameraNormalMapParameters.Remove(camera);
            }

#if UNITY_EDITOR
            RemoveCameraDataLDT(camera);
#endif
        }
    }

    // Quad
    partial class SurfaceRenderer
    {
        readonly Vector4[] _NormalMapParameters = new Vector4[Lod.k_MaximumSlices];
        Vector4[] NormalMapParameters { get; set; }

        void LateUpdateQuadMesh()
        {
            Shader.SetGlobalVectorArray(ShaderIDs.s_NormalMapParameters, NormalMapParameters);

            var scale = new Vector3(10000 * _Water.Scale, 10000 * _Water.Scale, 1);
            var bounds = Helpers.QuadMesh.bounds;
            bounds.Expand(scale);
            Graphics.RenderMesh
            (
                new()
                {
                    motionVectorMode = MotionVectorGenerationMode.Camera,
                    material = Material,
                    worldBounds = Root.TransformBounds(bounds),
                    layer = Layer,
                    shadowCastingMode = CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    lightProbeUsage = LightProbeUsage.Off,
                    reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox,
                    renderingLayerMask = (uint)Layer,
                },
                Helpers.QuadMesh,
                submeshIndex: 0,
                Matrix4x4.TRS(Root.position, Quaternion.Euler(90f, 0, 0), scale)
            );

            UpdateMaterial(_Material, ref _MotionVectorMaterial);
        }
    }
}
