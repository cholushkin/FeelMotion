using System;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// <summary>
/// Renders a FeelMotionAction's "Randomization" block: a collapsible Foldout listing each
/// FeelMotionRandomizer module (enabled toggle, target parameter, mode, sampling policy, optional
/// seed, distribution, per-component enable+range rows) plus an "Add Randomizer..." button. Styled to
/// match MotionSettingsView's grouped-Box layout. See Documentation/Architecture.md's Randomization
/// section for the module model this renders.
/// </summary>
static class RandomizationSectionView
{
    /// <summary>Walks the action's type hierarchy to find the closed TValue it tweens — the generic
    /// argument of PropertyMotionAction&lt;TObject,TValue,TOptions,TAdapter&gt; (FeelMotion.asmdef) or
    /// ValueMotionAction&lt;TValue,TOptions,TAdapter&gt; (the separate FeelMotion.Value.asmdef,
    /// Architecture.md §9) — so the Randomization section can label Start/End Value component rows
    /// (X/Y/Z, R/G/B/A, ...) and know whether those two parameters are even available. ValueMotionAction
    /// is matched by name/namespace rather than typeof(ValueMotionAction&lt;,,&gt;): FeelMotion.Editor
    /// deliberately doesn't reference FeelMotion.Value (or any other optional action category), the
    /// same isolation every other category's asmdef relies on. Returns null for an action with no value
    /// settings (e.g. a Control action).</summary>
    public static Type GetValueType(Type actionType)
    {
        for (var t = actionType; t != null; t = t.BaseType)
        {
            if (!t.IsGenericType) continue;
            var definition = t.GetGenericTypeDefinition();
            if (definition == typeof(PropertyMotionAction<,,,>)) return t.GetGenericArguments()[1];
            if (definition.Namespace == "FeelMotion" && definition.Name == "ValueMotionAction`3") return t.GetGenericArguments()[0];
        }
        return null;
    }

    public static VisualElement Build(SerializedProperty randomizersProperty, Type valueType)
    {
        var container = new VisualElement();
        var foldout = new Foldout { text = "Randomization" };
        container.Add(foldout);

        var listRoot = new VisualElement();
        foldout.Add(listRoot);

        var lastSignature = "";

        void Rebuild()
        {
            listRoot.Clear();
            for (var i = 0; i < randomizersProperty.arraySize; i++)
                listRoot.Add(BuildModuleRow(randomizersProperty, i, valueType, Rebuild));
            lastSignature = Signature(randomizersProperty);
        }

        Rebuild();

        var addButton = new Button { text = "Add Randomizer...", style = { width = 160f, alignSelf = Align.Center, marginTop = 2f, marginBottom = 2f } };
        addButton.clicked += () => ShowAddMenu(randomizersProperty, valueType, Rebuild);
        foldout.Add(addButton);

        // A module's Parameter also changes how many component rows it needs (e.g. switching Start
        // Value on a Vector3 action from Duration), so the rebuild signature tracks both array size and
        // every element's Parameter — matches the array-size polling idiom already used by
        // FeelMotionPlayerEditor.RefreshIfArraySizeChanged.
        container.schedule.Execute(() =>
        {
            var current = Signature(randomizersProperty);
            if (current != lastSignature) Rebuild();
        }).Every(150);

        return container;
    }

    static string Signature(SerializedProperty randomizersProperty)
    {
        var builder = new StringBuilder();
        builder.Append(randomizersProperty.arraySize);
        for (var i = 0; i < randomizersProperty.arraySize; i++)
        {
            var parameter = randomizersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("_parameter");
            builder.Append(':').Append(parameter.enumValueIndex);
        }
        return builder.ToString();
    }

    static VisualElement BuildModuleRow(SerializedProperty randomizersProperty, int index, Type valueType, Action rebuildSection)
    {
        var element = randomizersProperty.GetArrayElementAtIndex(index);

        var enabledProperty = element.FindPropertyRelative("_enabled");
        var parameterProperty = element.FindPropertyRelative("_parameter");
        var modeProperty = element.FindPropertyRelative("_mode");
        var policyProperty = element.FindPropertyRelative("_policy");
        var useSeedProperty = element.FindPropertyRelative("_useSeed");
        var seedProperty = element.FindPropertyRelative("_seed");
        var distributionProperty = element.FindPropertyRelative("_distribution");
        var componentsProperty = element.FindPropertyRelative("_components");

        var box = new Box { style = { marginBottom = 3f, paddingLeft = 4f, paddingRight = 4f, paddingTop = 3f, paddingBottom = 3f } };

        var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        var enabledToggle = new Toggle { style = { marginRight = 2f } };
        enabledToggle.BindProperty(enabledProperty);
        headerRow.Add(enabledToggle);
        headerRow.Add(new PropertyField(parameterProperty, "") { style = { flexGrow = 1f } });

        var removeButton = new Button { text = "✕", tooltip = "Remove this randomizer", style = { width = 20f } };
        removeButton.clicked += () =>
        {
            randomizersProperty.DeleteArrayElementAtIndex(index);
            randomizersProperty.serializedObject.ApplyModifiedProperties();
            rebuildSection();
        };
        headerRow.Add(removeButton);
        box.Add(headerRow);

        box.Add(new PropertyField(modeProperty));
        box.Add(new PropertyField(policyProperty));

        var seedRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        seedRow.Add(new PropertyField(useSeedProperty, "Use Seed") { style = { flexGrow = 1f } });
        var seedField = new PropertyField(seedProperty, "") { style = { width = 80f } };
        seedField.style.display = useSeedProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        seedRow.Add(seedField);
        box.Add(seedRow);
        box.TrackPropertyValue(useSeedProperty, p => seedField.style.display = p.boolValue ? DisplayStyle.Flex : DisplayStyle.None);

        box.Add(new PropertyField(distributionProperty, "Distribution"));

        var parameter = (RandomizedParameter)parameterProperty.enumValueIndex;
        var labels = ComponentLabelsFor(parameter, valueType);
        EnsureComponentCount(componentsProperty, labels.Length);

        var componentsBox = new VisualElement { style = { marginTop = 2f } };
        for (var i = 0; i < labels.Length; i++)
            componentsBox.Add(BuildComponentRow(componentsProperty, i, labels[i]));
        box.Add(componentsBox);

        return box;
    }

    static VisualElement BuildComponentRow(SerializedProperty componentsProperty, int index, string label)
    {
        var element = componentsProperty.GetArrayElementAtIndex(index);
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginLeft = 12f } };

        var enabledField = new PropertyField(element.FindPropertyRelative("Enabled"), label) { style = { width = 110f } };
        row.Add(enabledField);
        row.Add(new Label("Min") { style = { marginLeft = 4f, marginRight = 2f, unityTextAlign = TextAnchor.MiddleRight } });
        row.Add(new PropertyField(element.FindPropertyRelative("Min"), "") { style = { flexGrow = 1f } });
        row.Add(new Label("Max") { style = { marginLeft = 4f, marginRight = 2f, unityTextAlign = TextAnchor.MiddleRight } });
        row.Add(new PropertyField(element.FindPropertyRelative("Max"), "") { style = { flexGrow = 1f } });

        return row;
    }

    /// <summary>Duration/Delay are always one plain scalar; Start/End Value are shaped by the action's
    /// own TValue (via ValueComponentCodec's labels) — empty when valueType is null/unsupported, which
    /// also keeps ShowAddMenu from ever offering Start/End Value in that case.</summary>
    static string[] ComponentLabelsFor(RandomizedParameter parameter, Type valueType) => parameter switch
    {
        RandomizedParameter.Duration => new[] { "Duration" },
        RandomizedParameter.Delay => new[] { "Delay" },
        _ => valueType == null ? Array.Empty<string>() : ValueComponentCodec.GetComponentLabels(valueType)
    };

    /// <summary>Grows _components to <paramref name="required"/> entries, leaving any already-present
    /// entries untouched (so switching Start Value on a Vector2 action to a Vector3 one keeps X/Y as
    /// authored and only adds a fresh, enabled Z) — never shrinks, since a leftover entry beyond what
    /// the current parameter needs is simply ignored by RandomizationSet.</summary>
    static void EnsureComponentCount(SerializedProperty componentsProperty, int required)
    {
        if (componentsProperty.arraySize >= required) return;

        var start = componentsProperty.arraySize;
        componentsProperty.arraySize = required;
        for (var i = start; i < required; i++)
        {
            var element = componentsProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Enabled").boolValue = true;
            element.FindPropertyRelative("Min").floatValue = -1f;
            element.FindPropertyRelative("Max").floatValue = 1f;
        }

        componentsProperty.serializedObject.ApplyModifiedProperties();
    }

    static void ShowAddMenu(SerializedProperty randomizersProperty, Type valueType, Action rebuildSection)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Duration"), false, () => AddRandomizer(randomizersProperty, RandomizedParameter.Duration, valueType, rebuildSection));
        menu.AddItem(new GUIContent("Delay"), false, () => AddRandomizer(randomizersProperty, RandomizedParameter.Delay, valueType, rebuildSection));

        if (valueType != null && ValueComponentCodec.GetComponentLabels(valueType).Length > 0)
        {
            menu.AddItem(new GUIContent("Start Value"), false, () => AddRandomizer(randomizersProperty, RandomizedParameter.StartValue, valueType, rebuildSection));
            menu.AddItem(new GUIContent("End Value"), false, () => AddRandomizer(randomizersProperty, RandomizedParameter.EndValue, valueType, rebuildSection));
        }

        menu.ShowAsContext();
    }

    static void AddRandomizer(SerializedProperty randomizersProperty, RandomizedParameter parameter, Type valueType, Action rebuildSection)
    {
        var index = randomizersProperty.arraySize;
        randomizersProperty.InsertArrayElementAtIndex(index);
        var element = randomizersProperty.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_enabled").boolValue = true;
        element.FindPropertyRelative("_parameter").enumValueIndex = (int)parameter;
        element.FindPropertyRelative("_mode").enumValueIndex = (int)RandomizationMode.Offset;
        element.FindPropertyRelative("_policy").enumValueIndex = (int)RandomizationSamplingPolicy.OncePerPlay;
        element.FindPropertyRelative("_useSeed").boolValue = false;
        element.FindPropertyRelative("_seed").intValue = 0;
        element.FindPropertyRelative("_distribution").managedReferenceValue = new UniformDistribution();

        var labels = ComponentLabelsFor(parameter, valueType);
        var componentsProperty = element.FindPropertyRelative("_components");
        componentsProperty.arraySize = labels.Length;
        for (var i = 0; i < labels.Length; i++)
        {
            var componentElement = componentsProperty.GetArrayElementAtIndex(i);
            componentElement.FindPropertyRelative("Enabled").boolValue = true;
            componentElement.FindPropertyRelative("Min").floatValue = -1f;
            componentElement.FindPropertyRelative("Max").floatValue = 1f;
        }

        randomizersProperty.serializedObject.ApplyModifiedProperties();
        rebuildSection();
    }
}
