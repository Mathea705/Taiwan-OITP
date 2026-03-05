// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace WaveHarmonic.Crest.Portals.Editor
{
#if CREST_DEBUG
    [CustomPreview(typeof(WaterRenderer))]
    sealed class WaterLinePreview : Crest.Editor.TexturePreview
    {
        public override GUIContent GetPreviewTitle() => new("Pre-Computed Displacement (Portal)");
        protected override Texture OriginalTexture => (target as WaterRenderer).Portals.HeightRT;
    }
#endif
}
