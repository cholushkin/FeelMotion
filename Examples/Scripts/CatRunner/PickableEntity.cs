using UnityEngine;

namespace FeelMotion.Samples
{
    /// Coin/Fish/Heart — consuming it awards the matching HUD reward, then falls through to the base
    /// consume-motion playback.
    public sealed class PickableEntity : Entity
    {
        public enum PickupKind
        {
            Coin,
            Fish,
            Heart,
        }

        [SerializeField] private PickupKind _pickupKind;

        protected override void OnConsume()
        {
            switch (_pickupKind)
            {
                case PickupKind.Coin:
                    GameHud.Instance?.AddCoin();
                    break;
                case PickupKind.Fish:
                    GameHud.Instance?.AddFish();
                    break;
                case PickupKind.Heart:
                    GameHud.Instance?.AddHeart();
                    break;
            }

            base.OnConsume();
        }
    }
}
