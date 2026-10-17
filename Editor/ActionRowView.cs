using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// <summary>
/// One action's row in FeelMotionPlayerEditor: a Foldout (triangle) with an icon, a bold "enabled"
/// Toggle used as the row's name label, a live progress bar, and a context-menu button — composed
/// so the triangle, the checkbox and the context menu each have their own, non-overlapping pickable
/// region. Modeled on LitMotion.Animation's AnimationComponentView (reimplemented, not copied) —
/// see Documentation/Architecture.md. That composition is what fixes the IMGUI editor's foldout-vs-
/// checkbox hit-test bug: Foldout's own triangle/header keeps driving expand/collapse, while only the
/// enabled Toggle's checkmark glyph is pickable, so the two controls never fight over the same click.
/// </summary>
public sealed class ActionRowView : VisualElement
{
    VisualElement _container;
    readonly VisualElement _icon;
    readonly Toggle _enabledToggle;
    readonly ProgressBar _progressBar;
    readonly VisualElement _progressFill;
    readonly VisualElement _previewButton;
    readonly VisualElement _contextMenuButton;

    public Foldout Foldout { get; }
    public Toggle EnabledToggle => _enabledToggle;
    public VisualElement ContextMenuButton => _contextMenuButton;
    public VisualElement PreviewButton => _previewButton;

    /// <summary>True while a standalone per-action preview (FeelMotionPlayerEditor.TogglePreview) is
    /// driving this row's progress bar — the full-session live-progress poll skips the row while this
    /// is set, so the two updaters never fight over the same bar.</summary>
    public bool IsPreviewing { get; set; }

    public override VisualElement contentContainer => _container;

    public string Text
    {
        get => _enabledToggle.text;
        set => _enabledToggle.text = value;
    }

    public float Progress
    {
        get => _progressBar.value;
        set => _progressBar.value = value;
    }

    public Texture2D Icon
    {
        set => _icon.style.backgroundImage = value;
    }

    /// <summary>Progress-fill color — set to a distinct color for a looping action so its row stands
    /// out from a one-shot tween's. Defaults to white.</summary>
    public Color ProgressColor
    {
        set => _progressFill.style.backgroundColor = value;
    }

    public ActionRowView()
    {
        _container = this;

        var root = new HelpBox { style = { flexDirection = FlexDirection.Column, flexGrow = 1f } };
        root.Clear(); // HelpBox ships with its own icon+label children we don't want here
        Add(root);

        Foldout = new Foldout { style = { marginLeft = 15f, paddingRight = 24f, alignSelf = Align.Stretch } };
        root.Add(Foldout);

        var headerRow = Foldout.Q(className: Foldout.checkmarkUssClassName).parent;

        _icon = new VisualElement
        {
            style =
            {
                width = 16f, height = 16f,
                marginTop = 1f, marginRight = 2f,
                backgroundImage = (Texture2D)EditorGUIUtility.IconContent("ScriptableObject Icon").image
            }
        };
        headerRow.Add(_icon);

        _enabledToggle = new Toggle { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1f } };
        _enabledToggle.Q(className: Toggle.checkmarkUssClassName).style.marginRight = 6f;
        // The caption uses <size>/<color> tags for the type/duration suffix — make sure that's honored
        // rather than shown as literal text, regardless of this control's own rich-text default.
        var toggleLabel = _enabledToggle.Q<Label>(className: Toggle.textUssClassName);
        if (toggleLabel != null) toggleLabel.enableRichText = true;
        _enabledToggle.schedule.Execute(() =>
        {
            // Only the checkbox glyph itself is pickable — everything else in the toggle (its label,
            // input row, the toggle as a whole) must let clicks fall through to the Foldout header
            // underneath, or the triangle becomes unclickable wherever the toggle overlaps it.
            _enabledToggle.pickingMode = PickingMode.Ignore;
            _enabledToggle.Q(className: Toggle.inputUssClassName).pickingMode = PickingMode.Ignore;
            _enabledToggle.Q(className: Toggle.textUssClassName).pickingMode = PickingMode.Ignore;
            _enabledToggle.Q(className: Toggle.checkmarkUssClassName).pickingMode = PickingMode.Position;
        });
        headerRow.Add(_enabledToggle);

        _progressBar = new ProgressBar
        {
            lowValue = 0f,
            highValue = 1f,
            style = { height = 4f, position = Position.Absolute, top = 22f, left = 24f, right = 44f, alignSelf = Align.Stretch }
        };
        var background = _progressBar.Q(className: AbstractProgressBar.backgroundUssClassName);
        background.style.borderTopWidth = 0f;
        background.style.borderBottomWidth = 0f;
        background.style.borderLeftWidth = 0f;
        background.style.borderRightWidth = 0f;
        _progressFill = _progressBar.Q(className: AbstractProgressBar.progressUssClassName);
        _progressFill.style.backgroundColor = Color.white;
        _progressFill.style.minWidth = 0f;
        _progressBar.schedule.Execute(() => _progressFill.style.display = _progressBar.value > _progressBar.lowValue ? DisplayStyle.Flex : DisplayStyle.None).Every(10);
        root.Add(_progressBar);

        _previewButton = new VisualElement
        {
            style =
            {
                height = 15f, width = 15f, top = 4f, right = 22f, position = Position.Absolute,
                backgroundImage = (Texture2D)EditorGUIUtility.IconContent("PlayButton").image
            }
        };
        root.Add(_previewButton);

        _contextMenuButton = new VisualElement
        {
            style =
            {
                height = 15f, width = 15f, top = 4f, right = 4f, position = Position.Absolute,
                backgroundImage = (Texture2D)EditorGUIUtility.IconContent("_Menu").image
            }
        };
        root.Add(_contextMenuButton);

        _container = Foldout.contentContainer;
    }

    public new void SetEnabled(bool enabled)
    {
        Foldout.contentContainer.SetEnabled(enabled && _enabledToggle.value);
        _enabledToggle.SetEnabled(enabled);
        _icon.SetEnabled(enabled);
        _previewButton.SetEnabled(enabled);
        _contextMenuButton.SetEnabled(enabled);
        _progressBar.SetEnabled(enabled);
    }
}
