using AES.Tools.Commands;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Sample
{
    public class PlayerViewModel
    {
        public ObservableProperty<string> Name { get; }
        public ObservableProperty<int> Level { get; }
        public ObservableProperty<float> Hp { get; }
        public ObservableProperty<bool> IsAlive { get; }
        public ObservableList<string> Inventory { get; }

        public Command LevelUpCommand { get; }
        public Command<string> AddItemCommand { get; }
        public Command<float> DamageCommand { get; }
        public AsyncCommand<string> AsyncEchoCommand { get; }

        private readonly PlayerConfig _config;

        public PlayerViewModel(PlayerConfig config)
        {
            _config = config;

            Name = new ObservableProperty<string>(config != null ? config.defaultName : "Player");
            Level = new ObservableProperty<int>(config != null ? config.defaultLevel : 1);
            Hp = new ObservableProperty<float>(config != null ? config.defaultHp : 100f);
            IsAlive = new ObservableProperty<bool>(true);
            Inventory = new ObservableList<string>();

            LevelUpCommand = new Command(
                LevelUp,
                () => IsAlive.Value
            );

            DamageCommand = new Command<float>(
                Damage,
                amount => IsAlive.Value && amount > 0
            );

            AddItemCommand = new Command<string>(
                AddItem,
                item => !string.IsNullOrEmpty(item));

            AsyncEchoCommand = new AsyncCommand<string>(
                async msg => {
                    await UniTask.Delay(1000);
                    Debug.Log(msg);
                },
                msg => !string.IsNullOrEmpty(msg)
            );
            
            Hp.OnValueChanged += hp => IsAlive.Value = hp > 0;


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

        public void LevelUp()
        {
            Level.Value++;
        }

        public void Damage(float amount)
        {
            if (amount <= 0) return;
            Hp.Value -= amount;

            Hp.Value = Mathf.Clamp(Hp.Value, 0, 100);
        }

        public void AddItem(string item)
        {
            if (!string.IsNullOrEmpty(item))
                Inventory.Add(item);
        }
    }
}