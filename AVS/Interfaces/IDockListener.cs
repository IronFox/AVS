
namespace AVS
{
    /// <summary>
    /// Implementations of this interface are notified when the vessel is docked or undocked.
    /// </summary>
    public interface IDockListener
    {
        /// <summary>
        /// Notifies the listener that the vessel has been docked.
        /// </summary>
        void OnDock();
        /// <summary>
        /// Notifies the listener that the vessel has been undocked.
        /// </summary>
        void OnUndock();
    }
}
