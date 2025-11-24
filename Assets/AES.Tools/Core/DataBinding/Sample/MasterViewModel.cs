namespace AES.Tools.Sample
{
    public class MasterViewModel
    {

        public PlayerViewModel PlayerViewModel { get; private set; }
        public InventoryViewModel InventoryViewModel { get; private set; }
        
        public MasterViewModel(PlayerConfig config)
        {
            PlayerViewModel = new PlayerViewModel(config);
            InventoryViewModel = new InventoryViewModel(config);
        }
    }
}


