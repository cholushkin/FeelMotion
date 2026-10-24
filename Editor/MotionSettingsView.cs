using System;
using LitMotion;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// Renders a FeelMotionAction's "Motion" block (SerializableMotionSettings&lt;,&gt;) ourselves instead
/// of LitMotion.Editor's own drawer: same grouped layout, plus an easings-cheatsheet link and an
/// always-visible AnimationCurve field so a preset Ease can still be tweaked as a curve.
///
/// FixedString-valued settings (TMP/UI Text, StringValue) fall back to LitMotion's own drawer — its
/// FixedString editing is internal to that assembly and can't be reproduced here.
static class MotionSettingsView
{
    const string EasingsCheatsheetUrl = "https://cholushkin.github.io/cheatsheets/easings.html";

    public static VisualElement Build(SerializedProperty settingsProperty, string label)
    {
        var startValue = settingsProperty.FindPropertyRelative("startValue");
        if (startValue != null && startValue.type.StartsWith("FixedString", StringComparison.Ordinal))
            return new PropertyField(settingsProperty, label);

        var foldout = new Foldout { text = label };
        foldout.BindProperty(settingsProperty);

        Group(foldout, group =>
        {
            AddPropertyField(group, settingsProperty, "startValue");
            AddPropertyField(group, settingsProperty, "endValue");
            AddPropertyField(group, settingsProperty, "duration");
        });

        Group(foldout, group =>
        {
            var ease = settingsProperty.FindPropertyRelative("ease");
            var curve = settingsProperty.FindPropertyRelative("customEaseCurve");

            var easeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var easeField = new PropertyField(ease) { style = { flexGrow = 1f } };
            easeRow.Add(easeField);

            var cheatsheetButton = new Button(() => Application.OpenURL(EasingsCheatsheetUrl))
            {
                text = "?",
                tooltip = "Open the easings cheatsheet",
                style = { width = 18f, height = 18f, marginLeft = 2f }
            };
            easeRow.Add(cheatsheetButton);
            group.Add(easeRow);

            // Always visible — unlike LitMotion's own drawer, which hides this unless
            // ease == CustomAnimationCurve. Lets a user dial in a curve starting from any preset.
            group.Add(new PropertyField(curve));

            SyncEaseAndCurve(group, ease, curve);
        });

        Group(foldout, group =>
        {
            AddPropertyField(group, settingsProperty, "delay");
            AddPropertyField(group, settingsProperty, "delayType");
        });

        Group(foldout, group =>
        {
            AddPropertyField(group, settingsProperty, "loops");
            AddPropertyField(group, settingsProperty, "loopType");
        });

        var options = settingsProperty.FindPropertyRelative("options");
        if (options != null && options.hasChildren)
        {
            Group(foldout, group =>
            {
                group.style.paddingLeft = 15f;
                group.Add(new PropertyField(options));
            });
        }

        Group(foldout, group =>
        {
            var additionalSettings = settingsProperty.FindPropertyRelative("additionalSettings");
            if (additionalSettings == null) return;

            var inner = new Foldout { text = "Additional Settings", style = { marginBottom = 3f } };
            inner.BindProperty(additionalSettings);
            inner.style.marginLeft = 15f;
            AddPropertyField(inner, settingsProperty, "cancelOnError");
            AddPropertyField(inner, settingsProperty, "skipValuesDuringDelay");
            AddPropertyField(inner, settingsProperty, "immediateBind");
            inner.Add(new PropertyField(settingsProperty.FindPropertyRelative("schedulerType"), "Scheduler"));
            group.Add(inner);
        });

        return foldout;
    }

    static void Group(VisualElement root, Action<VisualElement> configure)
    {
        var group = new Box { style = { marginBottom = 3f } };
        configure(group);
        root.Add(group);
    }

    static void AddPropertyField(VisualElement root, SerializedProperty property, string name)
    {
        var relative = property.FindPropertyRelative(name);
        if (relative != null) root.Add(new PropertyField(relative));
    }

    /// Keeps the curve matching the selected preset Ease, and switches Ease to CustomAnimationCurve the
    /// moment the user edits the curve directly. Polling-based — Unity's binding system re-syncs a bound
    /// control via SetValueWithoutNotify, which a value-changed callback can't tell apart from a real edit.
    static void SyncEaseAndCurve(VisualElement root, SerializedProperty ease, SerializedProperty curve)
    {
        var lastEase = (Ease)ease.enumValueIndex;
        AnimationCurve lastSyncedCurve = null;
        var initialized = false;

        void ApplyEaseCurve(Ease value)
        {
            var generated = BuildCurveForEase(value);
            curve.animationCurveValue = generated;
            curve.serializedObject.ApplyModifiedProperties();
            lastSyncedCurve = generated;
        }

        root.schedule.Execute(() =>
        {
            var currentEase = (Ease)ease.enumValueIndex;

            if (!initialized || currentEase != lastEase)
            {
                initialized = true;
                lastEase = currentEase;
                if (currentEase != Ease.CustomAnimationCurve) ApplyEaseCurve(currentEase);
                else lastSyncedCurve = curve.animationCurveValue;
                return;
            }

            if (currentEase == Ease.CustomAnimationCurve) return; // nothing to auto-sync; already custom

            if (!CurvesEqual(curve.animationCurveValue, lastSyncedCurve))
            {
                // The curve changed without the ease dropdown changing — a direct edit by the user.
                lastEase = Ease.CustomAnimationCurve;
                ease.enumValueIndex = (int)Ease.CustomAnimationCurve;
                ease.serializedObject.ApplyModifiedProperties();
                lastSyncedCurve = curve.animationCurveValue;
            }
        }).Every(100);
    }

    static AnimationCurve BuildCurveForEase(Ease ease)
    {
        if (ease == Ease.Linear) return AnimationCurve.Linear(0f, 0f, 1f, 1f);

        const int sampleCount = 24;
        var keys = new Keyframe[sampleCount + 1];
        for (var i = 0; i <= sampleCount; i++)
        {
            var t = i / (float)sampleCount;
            keys[i] = new Keyframe(t, EaseUtility.Evaluate(t, ease));
        }

        var curve = new AnimationCurve(keys);
        for (var i = 0; i < keys.Length; i++)
            curve.SmoothTangents(i, 0f);
        return curve;
    }

    static bool CurvesEqual(AnimationCurve a, AnimationCurve b)
    {
        if (a == null || b == null) return a == b;
        if (a.length != b.length) return false;

        for (var i = 0; i < a.length; i++)
        {
            var ka = a[i];
            var kb = b[i];
            if (!Mathf.Approximately(ka.time, kb.time) || !Mathf.Approximately(ka.value, kb.value)
                || !Mathf.Approximately(ka.inTangent, kb.inTangent) || !Mathf.Approximately(ka.outTangent, kb.outTangent))
                return false;
        }

        return true;
    }
}
