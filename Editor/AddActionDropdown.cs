using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace FeelMotion.Editor
{
    /// <summary>
    /// Searchable, categorized "Add Action" menu. Modeled directly on LitMotion.Animation's own
    /// AddAnimationComponentDropdown (reimplemented, not copied — see Documentation/Architecture.md).
    /// Discovers every non-abstract FeelMotionAction subclass in the project via TypeCache; a new
    /// action type needs nothing more than a [FeelMotionActionMenu] attribute to show up here.
    /// </summary>
    public sealed class AddActionDropdown : AdvancedDropdown
    {
        sealed class Item : AdvancedDropdownItem
        {
            public Item(Type type, string name) : base(name)
            {
                Type = type;
            }

            public Type Type { get; }
        }

        static readonly (Type Type, string MenuName)[] s_cache = TypeCache.GetTypesDerivedFrom<FeelMotionAction>()
            .Where(t => !t.IsAbstract && !t.IsGenericType)
            .Select(t =>
            {
                var attribute = t.GetCustomAttribute<FeelMotionActionMenuAttribute>();
                var menuName = attribute != null ? attribute.MenuName : t.Name;
                return (t, menuName);
            })
            .OrderBy(x => x.menuName)
            .ToArray();

        public event Action<Type> TypeSelected;

        public AddActionDropdown(AdvancedDropdownState state) : base(state)
        {
            var size = minimumSize;
            size.y = 200f;
            minimumSize = size;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Action");

            foreach (var (type, menuName) in s_cache)
            {
                var segments = menuName.Split('/');
                var parent = root;

                for (var i = 0; i < segments.Length; i++)
                {
                    var segment = segments[i];
                    var existing = parent.children.FirstOrDefault(c => c.name == segment);
                    if (existing != null)
                    {
                        parent = existing;
                        continue;
                    }

                    var child = new Item(type, segment);
                    parent.AddChild(child);
                    parent = child;
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is Item actionItem) TypeSelected?.Invoke(actionItem.Type);
        }
    }
}
