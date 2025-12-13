namespace AES.Tools.Sample
{
    public class InventoryViewModel
    {
        public ObservableList<string> Inventory { get; }
        
        public Command<string> AddItemCommand { get; }

        private readonly PlayerConfig _config;

        public InventoryViewModel(PlayerConfig config)
        {
            _config = config;

            Inventory = new ObservableList<string>();
            

            AddItemCommand = new Command<string>(
                AddItem,
                item => !string.IsNullOrEmpty(item));
            


            if (config != null)
            {
                foreach (var item in config.defaultItems)
                    Inventory.Add(item);
            }
            else
            {
                Inventory.Add("Sword");
                Inventory.Add("Shield");
            }
        }

        public void AddItem(string item)
        {
            if (!string.IsNullOrEmpty(item))
                Inventory.Add(item);
        }
    }
}