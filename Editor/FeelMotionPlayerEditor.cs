using System;
using System.Collections.Generic;
using LitMotion;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// <summary>
/// Custom Inspector for FeelMotionPlayer: searchable Add Action menu, per-action enable/remove/move/
/// duplicate/preview, and Play/Pause/Rewind/Drop debug controls. Built with UIElements
/// (CreateInspectorGUI), porting the row composition proven by LitMotion.Animation's own editor
/// (reimplemented, not copied) — see Documentation/Architecture.md and ActionRowView's own doc comment
/// for why this replaced the previous IMGUI implementation (foldout/checkbox hit-test bug).
/// </summary>
[CustomEditor(typeof(FeelMotionPlayer))]
public sealed class FeelMotionPlayerEditor : UnityEditor.Editor
{
    SerializedProperty _actionsProperty;
    int _prevArraySize;

    /// <summary>Orange — used by both the per-action and the common progress bar whenever the thing
    /// they represent has no final duration (an infinitely-looping action, or a session that contains
    /// one).</summary>
    static readonly Color InfiniteProgressColor = new(1f, 0.65f, 0f);

    /// <summary>Blue, matching CaptionDurationColor below — used whenever the represented length is
    /// predictable/finite (a one-shot or finitely-looping action, or a session with no infinite action).</summary>
    static readonly Color FiniteProgressColor = new(0.435f, 0.659f, 0.863f);

    const string CaptionDurationColor = "#6FA8DC";

    AddActionDropdown _dropdown;
    VisualElement _actionsRoot;
    readonly List<IMotionActionRuntime> _activePreviews = new();
    readonly Dictionary<ActionRowView, IMotionActionRuntime> _previewByRow = new();
    readonly List<(ActionRowView Row, FeelMotionAction Action, Func<string> ResolveLabel)> _liveRows = new();
    readonly Dictionary<FeelMotionAction, string> _lastLoggedValidationError = new();
    bool _wasSessionActive;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        _actionsProperty = serializedObject.FindProperty("_actions");
        _prevArraySize = _actionsProperty.arraySize;

        _dropdown = new AddActionDropdown(new AdvancedDropdownState());
        _dropdown.TypeSelected += OnActionTypeSelected;

        _actionsRoot = new VisualElement();

        root.Add(CreateSettingsPanel());
        _actionsRoot.Add(CreateActionsPanel());
        root.Add(_actionsRoot);
        root.Add(CreateEventsPanel());
        root.Add(CreateDebugPanel());

        return root;
    }

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        StopActivePreviews();

        if (!EditorApplication.isPlayingOrWillChangePlaymode && target != null)
            ((FeelMotionPlayer)target).Drop();

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            StopActivePreviews();
            if (target != null) ((FeelMotionPlayer)target).Drop();
        }
    }

    void StopActivePreviews()
    {
        foreach (var runtime in _activePreviews)
        {
            try { runtime.Stop(MotionStopMode.Cancel); }
            catch (Exception) { /* best-effort cleanup only */ }
        }
        _activePreviews.Clear();

        foreach (var row in _previewByRow.Keys)
        {
            row.IsPreviewing = false;
            row.Progress = 0f;
        }
        _previewByRow.Clear();
    }

    static VisualElement CreateBox(string label)
    {
        var box = new Box
        {
            style =
            {
                marginTop = 6f, marginBottom = 2f, paddingLeft = 4f,
                alignItems = Align.Stretch, flexDirection = FlexDirection.Column, flexGrow = 1f
            }
        };
        box.Add(new Label(label) { style = { marginTop = 5f, marginBottom = 3f, unityFontStyleAndWeight = FontStyle.Bold } });
        return box;
    }

    VisualElement CreateSettingsPanel()
    {
        var box = CreateBox("Settings");

        // User-settable name for the whole effect, mirroring each action row's own "Label" field.
        box.Add(new PropertyField(serializedObject.FindProperty("_label"), "Label"));

        var playbackMode = serializedObject.FindProperty("_playbackMode");
        box.Add(new PropertyField(playbackMode));
        box.Add(new PropertyField(serializedObject.FindProperty("_autoPlay")));

        var sequenceDelayField = new PropertyField(serializedObject.FindProperty("_sequenceDelay"));
        sequenceDelayField.style.display = playbackMode.enumValueIndex == (int)MotionPlaybackMode.Sequential ? DisplayStyle.Flex : DisplayStyle.None;
        box.Add(sequenceDelayField);
        box.TrackPropertyValue(playbackMode, p => sequenceDelayField.style.display = p.enumValueIndex == (int)MotionPlaybackMode.Sequential ? DisplayStyle.Flex : DisplayStyle.None);

        box.Add(new PropertyField(serializedObject.FindProperty("_stopOnDisable")));
        box.Add(new PropertyField(serializedObject.FindProperty("_restoreOnDisable")));

        box.Add(new PropertyField(serializedObject.FindProperty("_durationMultiplier")));
        box.Add(new PropertyField(serializedObject.FindProperty("_intensityMultiplier")));

        return box;
    }

    VisualElement CreateActionsPanel()
    {
        var box = CreateBox("Actions");
        _liveRows.Clear();

        var typeOccurrence = new Dictionary<Type, int>();
        for (var i = 0; i < _actionsProperty.arraySize; i++)
        {
            var elementProperty = _actionsProperty.GetArrayElementAtIndex(i);
            var row = CreateActionRow(elementProperty.Copy(), i, typeOccurrence);
            box.Add(row);
        }

        var addButton = new Button { text = "Add Action...", style = { width = 160f, alignSelf = Align.Center } };
        addButton.clicked += () => _dropdown.Show(addButton.worldBound);
        box.Add(addButton);

        box.schedule.Execute(RefreshIfArraySizeChanged).Every(100);

        return box;
    }

    void RefreshIfArraySizeChanged()
    {
        if (_actionsProperty.arraySize == _prevArraySize) return;

        if (_prevArraySize < _actionsProperty.arraySize)
            DeduplicateManagedReferences();

        _prevArraySize = _actionsProperty.arraySize;
        RebuildActionsPanel();
    }

    /// <summary>After any growth of the SerializeReference array (Undo/Redo, paste), make sure no two
    /// elements alias the same managed object — same defensive pass LitMotion.Animation's editor runs
    /// after its own component list grows.</summary>
    void DeduplicateManagedReferences()
    {
        var seen = new HashSet<object>();
        var dirty = false;

        for (var i = 0; i < _actionsProperty.arraySize; i++)
        {
            var element = _actionsProperty.GetArrayElementAtIndex(i);
            var value = element.managedReferenceValue;
            if (value == null || seen.Add(value)) continue;

            var clone = JsonUtility.FromJson(JsonUtility.ToJson(value), value.GetType());
            element.managedReferenceValue = clone;
            dirty = true;
        }

        if (dirty) serializedObject.ApplyModifiedProperties();
    }

    void RebuildActionsPanel()
    {
        _actionsRoot.Clear();
        _actionsRoot.Add(CreateActionsPanel());
    }

    ActionRowView CreateActionRow(SerializedProperty elementProperty, int index, Dictionary<Type, int> typeOccurrence)
    {
        var row = new ActionRowView();

        if (string.IsNullOrEmpty(elementProperty.managedReferenceFullTypename))
        {
            row.Text = "(Missing)";
            row.Icon = (Texture2D)EditorGUIUtility.IconContent("Error").image;
            row.PreviewButton.style.display = DisplayStyle.None;
            row.Add(new HelpBox(
                "The type referenced in SerializeReference is missing. You may have renamed the type or moved it to a different namespace or assembly.",
                HelpBoxMessageType.Error));
        }
        else
        {
            // Every FindPropertyRelative below must happen before the NextVisible loop further down —
            // NextVisible mutates elementProperty's own cursor in place, so anything resolved relative
            // to it afterward would read the wrong path.
            var displayNameProperty = elementProperty.FindPropertyRelative("_displayName");
            var enabledProperty = elementProperty.FindPropertyRelative("_enabled");
            var actionSettingsProperty = elementProperty.FindPropertyRelative("_actionSettings");
            var targetProperty = elementProperty.FindPropertyRelative("_target");
            var action = elementProperty.managedReferenceValue as FeelMotionAction;
            var actionType = action.GetType();
            var niceTypeName = ObjectNames.NicifyVariableName(actionType.Name);

            typeOccurrence.TryGetValue(actionType, out var occurrence);
            occurrence++;
            typeOccurrence[actionType] = occurrence;

            string ResolveLabel() => string.IsNullOrEmpty(displayNameProperty.stringValue)
                ? $"{niceTypeName} {occurrence}"
                : displayNameProperty.stringValue;

            string ResolveCaption()
            {
                var duration = action.GetDuration();
                var durationText = double.IsPositiveInfinity(duration) ? "∞" : $"{duration:0.##}s";
                var loopCount = action.LoopCount;
                var loopSuffix = loopCount < 0 ? " ×∞" : loopCount > 1 ? $" ×{loopCount}" : "";

                // Literal "<type>" brackets are escaped so they can't be mistaken for one of the real
                // <size>/<color> tags sitting right next to them in the same rich-text string.
                return $"{ResolveLabel()}<size=9><noparse><{niceTypeName}></noparse></size> " +
                       $"<color={CaptionDurationColor}>{durationText}{loopSuffix}</color>";
            }

            void RefreshCaptionAndColor()
            {
                row.Text = ResolveCaption();
                // Blue for a finite loop count (one-shot or "xN"), orange only for an infinite loop
                // ("x∞") — matches the "no final duration" coloring on the player's common bar below.
                row.ProgressColor = action.LoopCount < 0 ? InfiniteProgressColor : FiniteProgressColor;
            }

            if (action != null) _liveRows.Add((row, action, ResolveLabel));

            RefreshCaptionAndColor();
            // Duration/looping come from several possible nested settings fields depending on the
            // concrete action type (a tween's SerializableMotionSettings, DelayAction's own duration,
            // ActionSettings.StartDelay, ...) — a periodic refresh covers all of them generically
            // instead of tracking each action type's specific fields one by one.
            row.schedule.Execute(RefreshCaptionAndColor).Every(150);

            row.EnabledToggle.BindProperty(enabledProperty);

            if (targetProperty is { propertyType: SerializedPropertyType.ObjectReference, objectReferenceValue: not null })
                row.Icon = (Texture2D)EditorGUIUtility.ObjectContent(targetProperty.objectReferenceValue, targetProperty.objectReferenceValue.GetType()).image;

            row.Foldout.BindProperty(elementProperty);

            // User-settable override for the default "{Type} {n}" label used in the caption/messages.
            row.Add(new PropertyField(displayNameProperty, "Label"));

            // Every action gets this common block, consistently labeled, regardless of type.
            if (actionSettingsProperty != null)
                row.Add(new PropertyField(actionSettingsProperty, "Action Settings"));

            var endProperty = elementProperty.GetEndProperty();
            var isFirst = true;
            while (elementProperty.NextVisible(isFirst))
            {
                if (SerializedProperty.EqualContents(elementProperty, endProperty)) break;
                isFirst = false;
                if (elementProperty.name is "_enabled" or "_displayName" or "_actionSettings") continue;

                // "_settings" is PropertyMotionAction's/ValueMotionAction's own LitMotion
                // SerializableMotionSettings field — render it ourselves (MotionSettingsView) instead
                // of LitMotion's default drawer so the ease cheatsheet button/always-visible curve
                // apply consistently wherever a concrete action happens to have one.
                row.Add(elementProperty.name == "_settings"
                    ? MotionSettingsView.Build(elementProperty.Copy(), "Motion")
                    : new PropertyField(elementProperty, elementProperty.displayName));
            }

            row.PreviewButton.AddManipulator(new Clickable(() => TogglePreview(row, _actionsProperty.GetArrayElementAtIndex(index))));
        }

        row.ContextMenuButton.AddManipulator(CreateContextMenu(index, requireLeftClick: true));
        row.Foldout.Q<Toggle>().AddManipulator(CreateContextMenu(index, requireLeftClick: false));

        return row;
    }

    void TogglePreview(ActionRowView row, SerializedProperty elementProperty)
    {
        // Second click on a row that's already previewing stops it — previously this always started a
        // new runtime on top of the old one, so a looping preview could never be stopped from here.
        if (_previewByRow.TryGetValue(row, out var activeRuntime))
        {
            StopPreview(row, activeRuntime);
            return;
        }

        serializedObject.ApplyModifiedProperties();

        if (!(elementProperty.managedReferenceValue is FeelMotionAction action)) return;

        var context = ((FeelMotionPlayer)target).PlaybackContext;

        IMotionActionRuntime runtime;
        try
        {
            runtime = action.CreateRuntime(context);
            if (runtime == null || !runtime.ValidateTarget())
            {
                Debug.LogWarning($"FeelMotion: '{action.DisplayName}' can't be previewed — ValidateTarget() failed.");
                return;
            }
            runtime.Play();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return;
        }

        _activePreviews.Add(runtime);
        _previewByRow[row] = runtime;
        row.IsPreviewing = true;
        runtime.Completed += () =>
        {
            _activePreviews.Remove(runtime);
            _previewByRow.Remove(row);
        };

        row.schedule.Execute(() =>
        {
            var handle = runtime.Handle;
            row.Progress = handle.IsActive()
                ? action.GetProgress(handle)
                : runtime.State == MotionActionState.Completed ? 1f : 0f;

            if (runtime.State is MotionActionState.Completed or MotionActionState.Cancelled)
            {
                row.IsPreviewing = false;
                _previewByRow.Remove(row);
            }
        }).Every(10).Until(() => runtime.State is MotionActionState.Completed or MotionActionState.Cancelled);
    }

    void StopPreview(ActionRowView row, IMotionActionRuntime runtime)
    {
        try { runtime.Stop(MotionStopMode.Cancel); }
        catch (Exception ex) { Debug.LogException(ex); }

        _activePreviews.Remove(runtime);
        _previewByRow.Remove(row);
        row.IsPreviewing = false;
        row.Progress = 0f;
    }

    ContextualMenuManipulator CreateContextMenu(int index, bool requireLeftClick)
    {
        var manipulator = new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Remove", _ =>
            {
                _actionsProperty.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                _prevArraySize = _actionsProperty.arraySize;
                RebuildActionsPanel();
            });

            evt.menu.AppendAction("Move Up", _ =>
            {
                _actionsProperty.MoveArrayElement(index, index - 1);
                serializedObject.ApplyModifiedProperties();
                RebuildActionsPanel();
            }, index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Move Down", _ =>
            {
                _actionsProperty.MoveArrayElement(index, index + 1);
                serializedObject.ApplyModifiedProperties();
                RebuildActionsPanel();
            }, index < _actionsProperty.arraySize - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Duplicate", _ =>
            {
                var source = _actionsProperty.GetArrayElementAtIndex(index).managedReferenceValue;
                var clone = source == null ? null : JsonUtility.FromJson(JsonUtility.ToJson(source), source.GetType());
                _actionsProperty.InsertArrayElementAtIndex(index + 1);
                _actionsProperty.GetArrayElementAtIndex(index + 1).managedReferenceValue = clone;
                serializedObject.ApplyModifiedProperties();
                _prevArraySize = _actionsProperty.arraySize;
                RebuildActionsPanel();
            });
        });

        if (requireLeftClick)
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

        return manipulator;
    }

    void OnActionTypeSelected(Type type)
    {
        serializedObject.Update();
        var index = _actionsProperty.arraySize;
        _actionsProperty.InsertArrayElementAtIndex(index);
        _actionsProperty.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
        serializedObject.ApplyModifiedProperties();
        _prevArraySize = _actionsProperty.arraySize;
        RebuildActionsPanel();
    }

    VisualElement CreateEventsPanel()
    {
        var foldout = new Foldout { text = "Events", style = { marginTop = 4f } };
        foldout.Add(new PropertyField(serializedObject.FindProperty("_onPlay")));
        foldout.Add(new PropertyField(serializedObject.FindProperty("_onComplete")));
        foldout.Add(new PropertyField(serializedObject.FindProperty("_onStop")));
        return foldout;
    }

    VisualElement CreateDebugPanel()
    {
        var box = CreateBox("Debug");

        var stateLabel = new Label();
        box.Add(stateLabel);

        var progressBar = new ProgressBar { lowValue = 0f, highValue = 1f, style = { height = 16f } };
        var progressFill = progressBar.Q(className: AbstractProgressBar.progressUssClassName);
        box.Add(progressBar);

        var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        buttonRow.Add(new Button(() => ((FeelMotionPlayer)target).Play()) { text = "Play", style = { flexGrow = 1f } });
        buttonRow.Add(new Button(() => ((FeelMotionPlayer)target).Pause()) { text = "Pause", style = { flexGrow = 1f } });
        buttonRow.Add(new Button(() =>
        {
            StopActivePreviews(); // a looping per-action preview isn't owned by the player's own session
            ((FeelMotionPlayer)target).Rewind();
        }) { text = "Rewind", style = { flexGrow = 1f } });
        buttonRow.Add(new Button(() =>
        {
            StopActivePreviews();
            ((FeelMotionPlayer)target).Drop();
        }) { text = "Drop", style = { flexGrow = 1f } });
        box.Add(buttonRow);

        var infoBoxList = new InfoBoxList();
        box.Add(infoBoxList);

        var messages = new List<(string Message, HelpBoxMessageType Type)>();

        box.schedule.Execute(() =>
        {
            if (target == null) return;
            var player = (FeelMotionPlayer)target;
            stateLabel.text = $"State: {player.State}";
            progressBar.value = player.Progress;

            // Orange when at least one enabled action loops infinitely — the session then has no final
            // duration (Sequential's master handle never completes; Parallel's average never settles),
            // so the bar's own 0-1 reading stops being a meaningful "time left". Blue otherwise.
            var hasInfiniteAction = false;
            foreach (var action in player.Actions)
            {
                if (action is { Enabled: true } && action.LoopCount < 0) { hasInfiniteAction = true; break; }
            }
            progressFill.style.backgroundColor = hasInfiniteAction ? InfiniteProgressColor : FiniteProgressColor;

            var sessionActive = player.State is MotionPlayerState.Playing or MotionPlayerState.Paused;

            messages.Clear();
            foreach (var (row, action, resolveLabel) in _liveRows)
            {
                if (row.IsPreviewing) continue; // a standalone preview owns this row's bar right now

                // Loops forever just like a finite one — GetProgress reads a loop's own Delay/Duration,
                // which stay meaningful regardless of how many times (or whether infinitely) it repeats.
                if (sessionActive) row.Progress = player.GetActionProgress(action);
                else if (_wasSessionActive) row.Progress = 0f; // clear once on the Playing/Paused -> idle transition

                var error = action.Validate();
                if (error != null)
                {
                    messages.Add(($"{resolveLabel()}: {error}", HelpBoxMessageType.Error));
                    if (!_lastLoggedValidationError.TryGetValue(action, out var previous) || previous != error)
                    {
                        Debug.LogError($"FeelMotion: {resolveLabel()}: {error}", target);
                        _lastLoggedValidationError[action] = error;
                    }
                }
                else
                {
                    _lastLoggedValidationError.Remove(action);
                }
            }
            infoBoxList.SetMessages(messages);

            _wasSessionActive = sessionActive;
        }).Every(50);

        return box;
    }
}
