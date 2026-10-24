namespace FeelMotion;

/// Which clock an action's handle advances on — maps onto one of LitMotion's own MotionScheduler.Update*
/// variants (Documentation/Architecture.md).
public enum MotionTimescaleMode
{
    Scaled,
    Unscaled,
    Realtime
}
