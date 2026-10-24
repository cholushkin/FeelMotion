using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rendering/Material/Property (Vector)")]
    public sealed class MaterialVectorAction : Vector4PropertyMotionAction<Material, NoOptions, Vector4MotionAdapter>
    {
        [SerializeField] string _propertyName = "";

        public string PropertyName => _propertyName;

        public override string Validate() => Target != null && !Target.HasProperty(_propertyName)
            ? $"Material property '{_propertyName}' not found on material '{Target.name}'."
            : null;

        protected override Vector4 GetValue(Material target) => target.GetVector(_propertyName);

        protected override void SetValue(Material target, in Vector4 value) => target.SetVector(_propertyName, value);
    }
}
