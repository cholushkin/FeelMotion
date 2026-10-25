using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rendering/Material/Property (Int)")]
    public sealed class MaterialIntAction : IntPropertyMotionAction<Material, IntegerOptions, IntMotionAdapter>
    {
        [SerializeField] string _propertyName = "";

        public string PropertyName => _propertyName;

        public override string Validate() => Target != null && !Target.HasProperty(_propertyName)
            ? $"Material property '{_propertyName}' not found on material '{Target.name}'."
            : null;

        // Target is a shared Material asset, not a scene instance — never leave it mutated after
        // playback/preview ends (otherwise the asset keeps the tweened value in the Project forever).
        protected override bool AlwaysRestoreOnStop => true;

        protected override int GetValue(Material target) => target.GetInteger(_propertyName);

        protected override void SetValue(Material target, in int value) => target.SetInteger(_propertyName, value);
    }
}
