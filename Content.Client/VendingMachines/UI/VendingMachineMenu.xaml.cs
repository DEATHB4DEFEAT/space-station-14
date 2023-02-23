using Content.Shared.VendingMachines;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using Robust.Client.UserInterface;

namespace Content.Client.VendingMachines.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class VendingMachineMenu : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public event Action<ItemList.ItemListSelectedEventArgs>? OnItemSelected;

        public VendingMachineMenu()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            VendingContents.OnItemSelected += args =>
            {
                onItemSelected(args);
            };

            VendButton.OnPressed += _ =>
            {
                // Stupid solution... but it works.

                int pos = 0;
                int finalPos = 0;
                foreach (var item in VendingContents)
                {
                    if (item.Selected)
                    {
                        finalPos = pos;
                        pos++;
                        break;
                    }
                    pos++;
                }

                if (finalPos != 0 && finalPos < pos)
                    OnItemSelected?.Invoke(new ItemList.ItemListSelectedEventArgs(finalPos, VendingContents));
            };


            // Placeholder VendingInfo
            var ent = _entityManager.SpawnEntity("VendingMachineCola", MapCoordinates.Nullspace);
            var sprite = _entityManager.GetComponent<SpriteComponent>(ent);
            VendingInfo.AddChild(new SpriteView
            {
                Scale = (3f, 3f),
                Sprite = sprite
            });

            VendingInfo.AddChild(new Control { MinSize = (10, 10) });

            var message = new FormattedMessage();

            message.AddMarkup(Loc.GetString("vending-machine-placeholder-name"));
            var VendingName = new RichTextLabel();
            VendingName.SetMessage(message);
            VendingInfo.AddChild(VendingName);

            VendingInfo.AddChild(new Control { MinSize = (10, 10) });

            message = new FormattedMessage();
            message.AddMarkup(Loc.GetString("vending-machine-placeholder-description"));
            var VendingDescription = new RichTextLabel();
            VendingDescription.SetMessage(message);
            VendingInfo.AddChild(VendingDescription);

            VendButton.Disabled = true;
        }

        private void onItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            ItemList.Item selected = VendingContents[args.ItemIndex];
            VendingMachineInventoryEntry? entry = (VendingMachineInventoryEntry?) selected.Metadata!;

            VendingInfo.Children.Clear();

            var ent = _entityManager.SpawnEntity(entry.ID ?? "VendingMachineCola", MapCoordinates.Nullspace);

            var metaData = _entityManager.GetComponent<MetaDataComponent>(ent);
            _entityManager.TryGetComponent<SpriteComponent>(ent, out var sprite);
            if (sprite != null)
            {
                VendingInfo.AddChild(new SpriteView
                {
                    Scale = (3f, 3f),
                    Sprite = sprite
                });
            }

            VendingInfo.AddChild(new Control { MinSize = (10, 10) });

            var message = new FormattedMessage();

            message.AddMarkup(metaData.EntityName);
            var VendingName = new RichTextLabel();
            VendingName.SetMessage(message);
            VendingInfo.AddChild(VendingName);

            if (metaData.EntityDescription != null)
            {
                VendingInfo.AddChild(new Control { MinSize = (10, 10) });

                message = new FormattedMessage();
                message.AddMarkup(metaData.EntityDescription);
                var VendingDescription = new RichTextLabel();
                VendingDescription.SetMessage(message);
                VendingInfo.AddChild(VendingDescription);
            }

            VendButton.Disabled = false;
        }


        /// <summary>
        /// Populates the list of available items on the vending machine interface
        /// and sets icons based on their prototypes
        /// </summary>
        public void Populate(List<VendingMachineInventoryEntry> inventory)
        {
            if (inventory.Count == 0)
            {
                VendingContents.Clear();
                var outOfStockText = Loc.GetString("vending-machine-component-try-eject-out-of-stock");
                VendingContents.AddItem(outOfStockText);
                SetSizeAfterUpdate(outOfStockText.Length);
                return;
            }

            while (inventory.Count != VendingContents.Count)
            {
                if (inventory.Count > VendingContents.Count)
                    VendingContents.AddItem(string.Empty);
                else
                    VendingContents.RemoveAt(VendingContents.Count - 1);
            }

            var longestEntry = string.Empty;
            var spriteSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();

            for (var i = 0; i < inventory.Count; i++)
            {
                var entry = inventory[i];
                var vendingItem = VendingContents[i];
                vendingItem.Text = string.Empty;
                vendingItem.Icon = null;

                var itemName = entry.ID;
                Texture? icon = null;
                if (_prototypeManager.TryIndex<EntityPrototype>(entry.ID, out var prototype))
                {
                    itemName = prototype.Name;
                    icon = spriteSystem.GetPrototypeIcon(prototype).Default;
                }

                if (itemName.Length > longestEntry.Length)
                    longestEntry = itemName;

                vendingItem.Text = $" [{entry.Amount}] {itemName}";
                vendingItem.Icon = icon;
                vendingItem.Metadata = entry;
            }

            SetSizeAfterUpdate(longestEntry.Length);
        }

        private void SetSizeAfterUpdate(int longestEntryLength)
        {
            SetSize = (Math.Clamp((longestEntryLength + 2) * 12, 500, 600), Math.Clamp(VendingContents.Count * 50, 250, 450));
        }
    }
}
