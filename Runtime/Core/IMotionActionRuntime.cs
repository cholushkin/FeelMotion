using System;
using LitMotion;

namespace FeelMotion
{
    /// <summary>
    /// A fresh instance of this is created by FeelMotionAction.CreateRuntime() at the start of every
    /// Play() call. It owns the handle and any captured values for that one play only — nothing here
    /// is ever shared across replays, which is what makes repeated/overlapping Play() calls safe.
    /// </summary>
    public interface IMotionActionRuntime : IMotionActionRunner
    {
        MotionActionState State { get; }

        MotionHandle Handle { get; }

        /// <summary>Fires exactly once, when this action's own playback finishes — naturally or via Stop(Complete*).</summary>
        event Action Completed;

        /// <summary>
        /// Checked by the owning session before Play() is ever called. Must be cheap and
        /// side-effect-free. Play() may assume this already returned true.
        /// </summary>
        bool ValidateTarget();

        /// <summary>Writes the value captured at Play() back to the target. Independent of Stop().</summary>
        void RestoreInitialValues();
    }
}
