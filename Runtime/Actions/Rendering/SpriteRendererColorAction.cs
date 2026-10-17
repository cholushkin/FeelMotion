using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rendering/Sprite Renderer/Color")]
    public sealed class SpriteRendererColorAction : ColorPropertyMotionAction<SpriteRenderer, NoOptions, ColorMotionAdapter>
    {
        protected override Color GetValue(SpriteRenderer target) => target.color;

        protected override void SetValue(SpriteRenderer target, in Color value) => target.color = value;
    }
}
