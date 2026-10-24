using System;

namespace FeelMotion
{
    /// Categorizes a FeelMotionAction subclass in the Editor's searchable Add Action menu, e.g.
    /// [FeelMotionActionMenu("Transform/Position")]. No other registration step is needed — the Editor
    /// discovers every non-abstract subclass via TypeCache. See Documentation/ExtensionGuide.md.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FeelMotionActionMenuAttribute : Attribute
    {
        public string MenuName { get; }

        public FeelMotionActionMenuAttribute(string menuName)
        {
            MenuName = menuName;
        }
    }
}
