using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// <summary>
/// A stack of HelpBox messages (error/warning/info) under FeelMotionPlayerEditor's Debug section —
/// surfaces authoring problems (FeelMotionAction.Validate()) and playback notices (e.g. "this action
/// loops indefinitely, no progress bar") that would otherwise be silent or log-only. Rebuilds its
/// children only when the message set actually changes, so the Debug panel's ~50ms poll doesn't
/// thrash UIElements every tick.
/// </summary>
sealed class InfoBoxList : VisualElement
{
    readonly List<(string Message, HelpBoxMessageType Type)> _current = new();

    public InfoBoxList()
    {
        style.marginTop = 2f;
    }

    public void SetMessages(IReadOnlyList<(string Message, HelpBoxMessageType Type)> messages)
    {
        if (Matches(messages)) return;

        _current.Clear();
        Clear();

        foreach (var (message, type) in messages)
        {
            _current.Add((message, type));
            Add(new HelpBox(message, type));
        }
    }

    bool Matches(IReadOnlyList<(string Message, HelpBoxMessageType Type)> messages)
    {
        if (messages.Count != _current.Count) return false;

        for (var i = 0; i < messages.Count; i++)
            if (messages[i] != _current[i])
                return false;

        return true;
    }
}
