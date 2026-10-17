namespace FeelMotion;

/// <summary>Which clock an action's handle advances on — maps onto one of LitMotion's own
/// MotionScheduler.Update* variants (Documentation/Architecture.md).</summary>
public enum MotionTimescaleMode
{
    Scaled,
    Unscaled,
    Realtime
}
