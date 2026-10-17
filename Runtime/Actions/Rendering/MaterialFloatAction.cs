using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rendering/Material/Property (Float)")]
    public sealed class MaterialFloatAction : FloatPropertyMotionAction<Material, NoOptions, FloatMotionAdapter>
    {
        [SerializeField] string _propertyName = "";

        /// <summary>Name of the shader float property to animate (e.g. "_Alpha").</summary>
        public string PropertyName => _propertyName;

        public override string Validate() => Target != null && !Target.HasProperty(_propertyName)
            ? $"Material property '{_propertyName}' not found on material '{Target.name}'."
            : null;

        protected override float GetValue(Material target) => target.GetFloat(_propertyName);

        protected override void SetValue(Material target, in float value) => target.SetFloat(_propertyName, value);
    }
}
