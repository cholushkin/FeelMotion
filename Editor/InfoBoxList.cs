using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FeelMotion.Editor;

/// Stack of HelpBox messages (error/warning/info) under FeelMotionPlayerEditor's Debug section —
/// surfaces Validate() problems and playback notices that would otherwise be log-only. Rebuilds
/// children only when the message set changes, so the ~50ms poll doesn't thrash UIElements every tick.
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
