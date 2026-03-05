// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WaveHarmonic.Crest.Editor.Settings
{
    [System.Serializable]
    class PlatformSettings
    {
        const string k_OverrideTooltip = "Override the feature for this platform";

        [HideInInspector]
        [SerializeField]
        internal bool _Default;


        [@Heading("Simulations", alwaysVisible: true)]

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideAlbedoSimulation;

        [@Label("Albedo")]
        [@DecoratedField]
        [SerializeField]
        internal bool _AlbedoSimulation = true;

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideAbsorptionSimulation;

        [@Label("Absorption")]
        [@DecoratedField]
        [SerializeField]
        internal bool _AbsorptionSimulation = true;

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideScatteringSimulation;

        [@Label("Scattering")]
        [@DecoratedField]
        [SerializeField]
        internal bool _ScatteringSimulation = true;

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideShadowSimulation;

        [@Label("Shadow")]
        [@DecoratedField]
        [SerializeField]
        internal bool _ShadowSimulation = true;


        [@Heading("Surface Material", alwaysVisible: true)]

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideNormalMaps;

        [@DecoratedField]
        [SerializeField]
        internal bool _NormalMaps = true;

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverridePlanarReflections;

        [@DecoratedField]
        [SerializeField]
        internal bool _PlanarReflections = true;

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideFoamBioluminescence;

        [@DecoratedField]
        [SerializeField]
        internal bool _FoamBioluminescence = true;

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideCausticsForceDistortion;

        [@DecoratedField]
        [SerializeField]
        internal bool _CausticsForceDistortion = true;


        [@Heading("Rendering", alwaysVisible: true)]

        [Tooltip(k_OverrideTooltip)]
        [@Predicated(nameof(_Default), inverted: true, hide: true)]
        [@InlineToggle]
        [SerializeField]
        internal bool _OverrideSimpleTransparency;

        [Tooltip("Refraction like transparency without requiring the Opaque or Depth Texture.\n\nRequires a populated Water Depth Simulation to render correctly. See the Main sample for a working scene.")]
        [@DecoratedField]
        [SerializeField]
        internal bool _SimpleTransparency;

        PlatformSettings Default => ProjectSettings.Instance._PlatformSettings;

        public bool AlbedoSimulation => _Default || _OverrideAlbedoSimulation ? _AlbedoSimulation : Default.AlbedoSimulation;
        public bool AbsorptionSimulation => _Default || _OverrideAbsorptionSimulation ? _AbsorptionSimulation : Default.AbsorptionSimulation;
        public bool ScatteringSimulation => _Default || _OverrideScatteringSimulation ? _ScatteringSimulation : Default.ScatteringSimulation;
        public bool ShadowSimulation => _Default || _OverrideShadowSimulation ? _ShadowSimulation : Default.ShadowSimulation;
        public bool NormalMaps => _Default || _OverrideNormalMaps ? _NormalMaps : Default.NormalMaps;
        public bool PlanarReflections => _Default || _OverridePlanarReflections ? _PlanarReflections : Default.PlanarReflections;
        public bool FoamBioluminescence => _Default || _OverrideFoamBioluminescence ? _FoamBioluminescence : Default.FoamBioluminescence;
        public bool CausticsForceDistortion => _Default || _OverrideCausticsForceDistortion ? _CausticsForceDistortion : Default.CausticsForceDistortion;
        public bool SimpleTransparency => _Default || _OverrideSimpleTransparency ? _SimpleTransparency : Default.SimpleTransparency;
    }

    [FilePath(k_Path, FilePathAttribute.Location.ProjectFolder)]
    sealed partial class ProjectSettings : ScriptableSingleton<ProjectSettings>
    {
#pragma warning disable IDE0032 // Use auto property

        [@Heading("Variant Stripping", Heading.Style.Settings)]

        [@Group]

        [@DecoratedField, SerializeField]
        bool _DebugEnableStrippingLogging;

        [@Predicated(nameof(_DebugEnableStrippingLogging))]
        [@DecoratedField, SerializeField]
        bool _DebugOnlyLogRemainingVariants;

        [Tooltip("Whether to strip broken variants.\n\nCurrently, the only known case is the point cookie variant being broken on Xbox.")]
        [@DecoratedField, SerializeField]
        bool _StripBrokenVariants = true;

        [@Heading("Features", Heading.Style.Settings)]

        [@Group]

        [Tooltip("Whether to use full precision sampling for half precision platforms (typically mobile).\n\nThis will solve rendering artifacts like minor bumps and staircasing.")]
        [@DecoratedField, SerializeField]
        bool _FullPrecisionDisplacementOnHalfPrecisionPlatforms = true;

        [Tooltip("Whether to render atmospheric scattering (ie fog) for pixels receiving aquatic scattering (underwater only).\n\nWhen disabled, if a pixel is receiving aquatic scattering, then it will not receive atmospheric scattering.")]
        [@DecoratedField, SerializeField]
        bool _RenderAtmosphericScatteringWhenUnderWater;

        [Tooltip("Renders the underwater effect after transparency and uses the more expensive mask.\n\nYou may need this if rendering the underwater to multiple cameras. The other benefit is that transparent objects will be fogged (albeit incorrectly).\n\nThe downsides are that there can be artifacts if waves are very choppy, has a less impressive meniscus, and generally more expensive to execute.")]
        [@DecoratedField, SerializeField]
        bool _LegacyUnderwater;

        [@Space(10)]

        [@PlatformTabs]
        [SerializeField]
        internal int _Platforms;

        [@Label("Overriden Settings for Windows, Mac and Linux")]
        [@Predicated(nameof(_Platforms), inverted: true, (int)BuildTargetGroup.Standalone, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsDesktop = new();

        [@Label("Overriden Settings for Dedicated Server")]
        [@Predicated(nameof(_Platforms), inverted: true, -2, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsServer = new();

        [@Label("Overriden Settings for Android")]
        [@Predicated(nameof(_Platforms), inverted: true, (int)BuildTargetGroup.Android, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsAndroid = new();

        [@Label("Overriden Settings for iOS")]
        [@Predicated(nameof(_Platforms), inverted: true, (int)BuildTargetGroup.iOS, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsIOS = new();

        [@Label("Overriden Settings for tvOS")]
        [@Predicated(nameof(_Platforms), inverted: true, (int)BuildTargetGroup.tvOS, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsTVOS = new();

        [@Label("Overriden Settings for visionOS")]
        [@Predicated(nameof(_Platforms), inverted: true, (int)BuildTargetGroup.VisionOS, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsVisionOS = new();

        // Web has hard limitations on number of sampled textures. Set defaults with that
        // in mind so the surface renders.
        [@Label("Overriden Settings for Web")]
        [@Predicated(nameof(_Platforms), inverted: true, (int)BuildTargetGroup.WebGL, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettingsWeb = new()
        {
            _OverrideAbsorptionSimulation = true,
            _AbsorptionSimulation = false,
            _OverrideAlbedoSimulation = true,
            _AlbedoSimulation = false,
            _OverrideScatteringSimulation = true,
            _ScatteringSimulation = false,
            _OverrideShadowSimulation = true,
            _ShadowSimulation = false,

            _OverrideCausticsForceDistortion = true,
            _CausticsForceDistortion = false,
            _OverridePlanarReflections = true,
            _PlanarReflections = false,
            _OverrideFoamBioluminescence = true,
            _FoamBioluminescence = false,
        };

        // This will show if nothing else shows.
        [@Label("Default Settings")]
        [@Predicated(nameof(_Platforms), inverted: false, (int)BuildTargetGroup.Standalone, hide: true)]
        [@Predicated(nameof(_Platforms), inverted: false, Reflected.BuildTargetGroup.k_Server, hide: true)]
        [@Predicated(nameof(_Platforms), inverted: false, (int)BuildTargetGroup.Android, hide: true)]
        [@Predicated(nameof(_Platforms), inverted: false, (int)BuildTargetGroup.iOS, hide: true)]
        [@Predicated(nameof(_Platforms), inverted: false, (int)BuildTargetGroup.WebGL, hide: true)]
        [@Predicated(nameof(_Platforms), inverted: false, (int)BuildTargetGroup.tvOS, hide: true)]
        [@Predicated(nameof(_Platforms), inverted: false, (int)BuildTargetGroup.VisionOS, hide: true)]
        [@Stripped(Stripped.Style.PlatformTab, indent: true)]
        [SerializeField]
        internal PlatformSettings _PlatformSettings = new() { _Default = true };

#pragma warning restore IDE0032 // Use auto property

        internal const string k_Path = "ProjectSettings/Packages/com.waveharmonic.crest/Settings.asset";

        internal enum State
        {
            Dynamic,
            Disabled,
            Enabled,
        }

        internal static ProjectSettings Instance => instance;

        internal bool StripBrokenVariants => _StripBrokenVariants;
        internal bool DebugEnableStrippingLogging => _DebugEnableStrippingLogging;
        internal bool LogStrippedVariants => _DebugEnableStrippingLogging && !_DebugOnlyLogRemainingVariants;
        internal bool LogKeptVariants => _DebugEnableStrippingLogging && _DebugOnlyLogRemainingVariants;
        internal bool FullPrecisionDisplacementOnHalfPrecisionPlatforms => _FullPrecisionDisplacementOnHalfPrecisionPlatforms;
        internal bool RenderAtmosphericScatteringWhenUnderWater => _RenderAtmosphericScatteringWhenUnderWater;
        internal bool LegacyUnderwater => _LegacyUnderwater;

        internal PlatformSettings CurrentPlatformSettings =>
#if   PLATFORM_STANDALONE
            _PlatformSettingsDesktop;
#elif PLATFORM_SERVER
            _PlatformSettingsServer;
#elif PLATFORM_ANDROID
            _PlatformSettingsAndroid;
#elif PLATFORM_IOS
            _PlatformSettingsIOS;
#elif PLATFORM_TVOS
            _PlatformSettingsTVOS;
#elif PLATFORM_VISIONOS
            _PlatformSettingsVisionOS;
#else
            _PlatformSettings;
#endif

        internal bool _IsPlatformTabChange;

        void OnEnable()
        {
            // Fixes not being editable.
            hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
        }


        internal static void Save()
        {
            instance.Save(saveAsText: true);
        }

        [@OnChange(skipIfInactive: false)]
        void OnChange(string path, object previous)
        {
            _IsPlatformTabChange = path == nameof(_Platforms);

            if (path.StartsWithNoAlloc("_PlatformSettings"))
            {
                UpdateSymbols();
                return;
            }

            switch (path)
            {
                case nameof(_FullPrecisionDisplacementOnHalfPrecisionPlatforms):
                case nameof(_RenderAtmosphericScatteringWhenUnderWater):
                case nameof(_LegacyUnderwater):
                    UpdateSymbols();
                    break;
            }
        }

        void UpdateScriptingSymbols()
        {
            ScriptingSymbols.Set(ProjectSymbols.k_LegacyUnderwaterScriptingSymbol, _LegacyUnderwater);
            ScriptingSymbols.Set(ProjectSymbols.k_SimpleTransparencyScriptingSymbol, CurrentPlatformSettings.SimpleTransparency);
        }

        void UpdateSymbols()
        {
            UpdateScriptingSymbols();
            ShaderSettingsGenerator.Generate();
        }

        sealed class ProjectSymbols : AssetModificationProcessor
        {
            public const string k_LegacyUnderwaterScriptingSymbol = "d_Crest_LegacyUnderwater";
            public const string k_SimpleTransparencyScriptingSymbol = "d_Crest_SimpleTransparency";

            static FileSystemWatcher s_Watcher;

            // Will run on load and recompile preventing symbol removal in player settings.
            [InitializeOnLoadMethod]
            static void OnLoad()
            {
                if (Instance != null)
                {
                    Instance.UpdateScriptingSymbols();
                }

                Directory.CreateDirectory(Path.GetDirectoryName(k_Path));

                s_Watcher = new(Path.GetDirectoryName(k_Path))
                {
                    Filter = Path.GetFileName(k_Path),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                s_Watcher.Changed -= OnChanged;
                s_Watcher.Changed += OnChanged;
            }

            // Handle external edits. Possibly unreliable, but not important if fails.
            static void OnChanged(object sender, FileSystemEventArgs e)
            {
                EditorApplication.delayCall += () =>
                {
                    if (Instance != null && Instance._IsPlatformTabChange)
                    {
                        return;
                    }

                    // Destroy instance to reflect changes.
                    Helpers.Destroy(Instance);
                    typeof(ScriptableSingleton<ProjectSettings>)
                        .GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic)
                        .SetValue(null, null);
                    Instance.UpdateSymbols();
                };
            }

            static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                // Only remove symbols if this file is deleted.
                if (Path.GetFullPath(path) == GetCurrentFileName())
                {
                    ScriptingSymbols.Remove(ScriptingSymbols.Symbols.Where(x => x.StartsWith("d_Crest_")).ToArray());
                }

                return AssetDeleteResult.DidNotDelete;
            }

            static string GetCurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = null)
            {
                return fileName;
            }
        }
    }

    sealed class SettingsProvider : UnityEditor.SettingsProvider
    {
        static readonly string[] s_ShaderGraphs = new string[]
        {
            "Packages/com.waveharmonic.crest/Runtime/Shaders/Surface/Water.shadergraph",
            "Packages/com.waveharmonic.crest/Shared/Shaders/Lit.shadergraph",
            "Packages/com.waveharmonic.crest.paint/Samples/Colorado/Shaders/SpeedTree8_PBRLit.shadergraph",
            "Packages/com.waveharmonic.crest.paint/Samples/Colorado/Shaders/Environment (Splat Map).shadergraph",
        };

        UnityEditor.Editor _Editor;

        SettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {
            // Empty
        }

        static bool IsSettingsAvailable()
        {
            return File.Exists(ProjectSettings.k_Path);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            _Editor = UnityEditor.Editor.CreateEditor(ProjectSettings.Instance);
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Helpers.Destroy(_Editor);
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            ProjectSettings.Save();
        }

        public override void OnGUI(string searchContext)
        {
            if (_Editor.target == null)
            {
                Helpers.Destroy(_Editor);
                _Editor = UnityEditor.Editor.CreateEditor(ProjectSettings.Instance);
                return;
            }

            // Pad similar to settings header.
            var style = new GUIStyle();
            style.padding.left = 8;
            EditorGUILayout.BeginVertical(style);

            // Same label with as other settings.
            EditorGUIUtility.labelWidth = 251;

            EditorGUI.BeginChangeCheck();

            _Editor.OnInspectorGUI();

            GUILayout.Space(10 * 2);

            if (GUILayout.Button("Repair Shaders"))
            {
                foreach (var path in s_ShaderGraphs)
                {
                    if (!File.Exists(path)) continue;
                    AssetDatabase.ImportAsset(path);
                }
            }

            EditorGUILayout.EndVertical();
        }

        [SettingsProvider]
        static UnityEditor.SettingsProvider Create()
        {
            if (ProjectSettings.Instance)
            {
                var provider = new SettingsProvider("Project/Crest", SettingsScope.Project);
                provider.keywords = GetSearchKeywordsFromSerializedObject(new(ProjectSettings.Instance));
                return provider;
            }

            // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
            return null;
        }
    }

    [CustomEditor(typeof(ProjectSettings))]
    class ProjectSettingsEditor : Inspector
    {
        protected override void OnChange()
        {
            base.OnChange();

            // Commit all changes. Normally settings are written when user hits save or exits
            // without any undo/redo entry and dirty state. No idea how to do the same.
            // SaveChanges and hasUnsavedChanges on custom editor did not work.
            // Not sure if hooking into EditorSceneManager.sceneSaving is correct.
            ProjectSettings.Save();
        }
    }

    partial class ProjectSettings : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        int _Version = 0;

        [SerializeField, HideInInspector]
        internal int _MaterialVersion = MaterialUpgrader.k_MaterialVersion;

        public void OnAfterDeserialize()
        {
            if (_Version == 0)
            {
                _MaterialVersion = 0;
            }

            _Version = 1;
        }

        public void OnBeforeSerialize()
        {

        }
    }
}
