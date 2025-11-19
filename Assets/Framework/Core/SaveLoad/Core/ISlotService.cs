namespace Core.Systems.Storage.Core
{
    public interface ISlotService
    {
        string CurrentSlotId { get; }
        void SetSlot(string slotId);
    }

    public sealed class SlotService : ISlotService
    {
        public string CurrentSlotId { get; private set; } = "default";

        public void SetSlot(string slotId)
        {
            CurrentSlotId = string.IsNullOrEmpty(slotId) ? "default" : slotId;
        }
    }
}