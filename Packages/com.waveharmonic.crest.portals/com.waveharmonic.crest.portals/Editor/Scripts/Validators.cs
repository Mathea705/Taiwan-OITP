// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

using WaveHarmonic.Crest.Editor;
using static WaveHarmonic.Crest.Editor.ValidatedHelper;

namespace WaveHarmonic.Crest.Portals.Editor
{
    static class Validators
    {
        [Validator(typeof(WaterRenderer))]
        static bool Validate(WaterRenderer target, ShowMessage messenger)
        {
            return Validate(target._Portals, messenger, target);
        }

        static bool Validate(PortalRenderer target, ShowMessage messenger, WaterRenderer water)
        {
            var isValid = true;

            if (!target._Enabled)
            {
                return isValid;
            }

            if (target._Geometry == null)
            {
                messenger
                (
                    "<i>Portals</i> requires a <i>Mesh Filter</i> be set to <i>Geometry</i>.",
                    "Set <i>Geometry</i> or disable <i>Portals</i>.",
                    MessageType.Error, water,
                    (x, y) => y.boolValue = false,
                    $"{nameof(WaterRenderer._Portals)}.{nameof(PortalRenderer._Enabled)}"
                );

                isValid = false;
            }

            return isValid;
        }
    }
}
