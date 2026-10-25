using System;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// Renders a FeelMotionAction's "Randomization" block: the randomizer list plus an "Add Randomizer..."
/// menu. See Documentation/Architecture.md §12.
static class RandomizationSectionView
{
    /// Finds the TValue closed over by PropertyMotionAction/ValueMotionAction (for Start/End Value
    /// component labels). Matched by name since FeelMotion.Editor doesn't reference FeelMotion.Value.
    /// Null if the action has no value settings.
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

        var initialBuild = true;

        void Rebuild()
        {
            if (!initialBuild)
                listRoot.Unbind();

            listRoot.Clear();

            for (var i = 0; i < randomizersProperty.arraySize; i++)
                listRoot.Add(BuildModuleRow(
                    randomizersProperty, i, valueType, Rebuild));

            lastSignature = Signature(randomizersProperty);

            if (!initialBuild)
                listRoot.Bind(randomizersProperty.serializedObject);

            initialBuild = false;
        }

        Rebuild();

        var addButton = new Button { text = "Add Randomizer...", style = { width = 160f, alignSelf = Align.Center, marginTop = 2f, marginBottom = 2f } };
        addButton.clicked += () => ShowAddMenu(randomizersProperty, valueType, Rebuild);
        foldout.Add(addButton);

        // Signature also tracks each element's Parameter: changing it changes how many component rows render.
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
        removeButton.clicked += () => RemoveRandomizer(randomizersProperty, index, rebuildSection);
        headerRow.Add(removeButton);
        box.Add(headerRow);

        // Reactive, not during Build: growing/applying _components while the row itself is under
        // construction left bound PropertyFields broken until the object was deselected and reselected.
        box.TrackPropertyValue(parameterProperty, p => OnParameterChanged(p, componentsProperty, valueType, rebuildSection));

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

        // Clamped defensively: an Undo/Redo step can momentarily leave the parameter and _components
        // out of sync (they're applied as separate undo records).
        var count = Math.Min(labels.Length, componentsProperty.arraySize);

        var componentsBox = new VisualElement { style = { marginTop = 2f } };
        for (var i = 0; i < count; i++)
            componentsBox.Add(BuildComponentRow(componentsProperty, i, labels[i]));
        box.Add(componentsBox);

        return box;
    }

    static VisualElement BuildComponentRow(SerializedProperty componentsProperty, int index, string label)
    {
        var element = componentsProperty.GetArrayElementAtIndex(index);
        var sourceProperty = element.FindPropertyRelative("Source");

        var container = new VisualElement { style = { marginLeft = 12f, marginBottom = 2f } };

        var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        headerRow.Add(new PropertyField(element.FindPropertyRelative("Enabled"), label) { style = { width = 110f } });
        headerRow.Add(new PropertyField(sourceProperty, "") { style = { flexGrow = 1f } });
        container.Add(headerRow);

        var rangeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        rangeRow.Add(new Label("Min") { style = { marginLeft = 4f, marginRight = 2f, unityTextAlign = TextAnchor.MiddleRight } });
        rangeRow.Add(new PropertyField(element.FindPropertyRelative("Min"), "") { style = { flexGrow = 1f } });
        rangeRow.Add(new Label("Max") { style = { marginLeft = 4f, marginRight = 2f, unityTextAlign = TextAnchor.MiddleRight } });
        rangeRow.Add(new PropertyField(element.FindPropertyRelative("Max"), "") { style = { flexGrow = 1f } });
        container.Add(rangeRow);

        // List mode: pick uniformly at random from this authored set instead of sampling [Min, Max].
        var valuesField = new PropertyField(element.FindPropertyRelative("Values"), "Values") { style = { marginLeft = 4f } };
        container.Add(valuesField);

        void RefreshSourceVisibility()
        {
            var isList = (RandomizationValueSource)sourceProperty.enumValueIndex == RandomizationValueSource.List;
            rangeRow.style.display = isList ? DisplayStyle.None : DisplayStyle.Flex;
            valuesField.style.display = isList ? DisplayStyle.Flex : DisplayStyle.None;
        }

        RefreshSourceVisibility();
        container.TrackPropertyValue(sourceProperty, _ => RefreshSourceVisibility());

        return container;
    }

    /// Duration/Delay are one plain scalar; Start/End Value are shaped by the action's TValue. Empty
    /// when valueType is null, which also keeps ShowAddMenu from offering Start/End Value then.
    static string[] ComponentLabelsFor(RandomizedParameter parameter, Type valueType) => parameter switch
    {
        RandomizedParameter.Duration => new[] { "Duration" },
        RandomizedParameter.Delay => new[] { "Delay" },
        _ => valueType == null ? Array.Empty<string>() : ValueComponentCodec.GetComponentLabels(valueType)
    };

    /// Grows _components to `required` entries, keeping existing ones (switching Start Value from a
    /// Vector2 action to Vector3 keeps authored X/Y, adds a fresh Z) and never shrinking — a leftover
    /// entry beyond what the current parameter needs is simply ignored by RandomizationSet. Pure data
    /// mutation; callers apply.
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
    }

    static void OnParameterChanged(SerializedProperty parameterProperty, SerializedProperty componentsProperty, Type valueType, Action rebuildSection)
    {
        var parameter = (RandomizedParameter)parameterProperty.enumValueIndex;
        EnsureComponentCount(componentsProperty, ComponentLabelsFor(parameter, valueType).Length);
        componentsProperty.serializedObject.ApplyModifiedProperties();
        rebuildSection();
    }

    static void RemoveRandomizer(SerializedProperty randomizersProperty, int index, Action rebuildSection)
    {
        randomizersProperty.DeleteArrayElementAtIndex(index);
        randomizersProperty.serializedObject.ApplyModifiedProperties();
        rebuildSection();
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
