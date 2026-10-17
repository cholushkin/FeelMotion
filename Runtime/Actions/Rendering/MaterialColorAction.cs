using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rendering/Material/Property (Color)")]
    public sealed class MaterialColorAction : ColorPropertyMotionAction<Material, NoOptions, ColorMotionAdapter>
    {
        [SerializeField] string _propertyName = "_Color";

        /// <summary>Name of the shader color property to animate.</summary>
        public string PropertyName => _propertyName;

        public override string Validate() => Target != null && !Target.HasProperty(_propertyName)
            ? $"Material property '{_propertyName}' not found on material '{Target.name}'."
            : null;

        protected override Color GetValue(Material target) => target.GetColor(_propertyName);

        protected override void SetValue(Material target, in Color value) => target.SetColor(_propertyName, value);
    }
}
