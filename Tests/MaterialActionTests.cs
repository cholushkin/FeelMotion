using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace FeelMotion.Tests
{
    /// <summary>
    /// Covers FeelMotionAction.Validate() on the Rendering/Material actions — surfaces a shader
    /// property name that doesn't exist on the assigned material as an authoring error (FeelMotion
    /// issue: "material action can't find a parameter"), instead of silently doing nothing.
    /// </summary>
    public class MaterialActionTests
    {
        Material _material;

        [TearDown]
        public void TearDown()
        {
            if (_material != null) Object.DestroyImmediate(_material);
        }

        [Test]
        public void MaterialColorAction_Validate_ReturnsNullWhenPropertyExists()
        {
            _material = new Material(Shader.Find("Sprites/Default")); // ships with every project; has "_Color"

            var action = new MaterialColorAction().Configure(
                _material, MotionValueMode.Absolute,
                new SerializableMotionSettings<Color, NoOptions> { EndValue = Color.white, Duration = 1f });

            Assert.IsNull(action.Validate());
        }

        [Test]
        public void MaterialColorAction_Validate_ReturnsErrorWhenPropertyMissing()
        {
            _material = new Material(Shader.Find("Unlit/Texture")); // no "_Color" property

            var action = new MaterialColorAction().Configure(
                _material, MotionValueMode.Absolute,
                new SerializableMotionSettings<Color, NoOptions> { EndValue = Color.white, Duration = 1f });

            Assert.IsNotNull(action.Validate());
        }

        [Test]
        public void MaterialColorAction_Validate_ReturnsNullWhenTargetIsUnassigned()
        {
            var action = new MaterialColorAction().Configure(
                null, MotionValueMode.Absolute,
                new SerializableMotionSettings<Color, NoOptions> { EndValue = Color.white, Duration = 1f });

            // No target isn't an authoring error by itself — ValidateTarget() already handles "nothing
            // to do" by returning false; Validate() only reports a problem with an assigned target.
            Assert.IsNull(action.Validate());
        }
    }
}
