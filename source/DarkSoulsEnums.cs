using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAP
{
    public class DarkSoulsEnums
    {
        public enum ItemCategory
        {
            Armor,
            Consumables,
            KeyItems,
            MeleeWeapons,
            RangedWeapons,
            Rings,
            Shields,
            Spells,
            SpellTools,
            UpgradeMaterials,
            UsableItems,
            MysteryWeapons,
            MysteryArmor,
            MysteryGoods,
            Trap
        }

        public static class ItemCategoryHexValues
        {
            public static int Accessory = 0x20000000;
            public static int Weapon = 0x00000000;
            public static int Armor = 0x10000000;
            public static int Good = 0x40000000;
        }

        public static class DarkSoulsEnumUtils
        {
            public static int GetItemCategoryHexValue(ItemCategory category)
            {
                switch (category)
                {
                    case ItemCategory.Armor: return ItemCategoryHexValues.Armor;
                    case ItemCategory.Consumables: return ItemCategoryHexValues.Good;
                    case ItemCategory.KeyItems: return ItemCategoryHexValues.Good;
                    case ItemCategory.MeleeWeapons: return ItemCategoryHexValues.Weapon;
                    case ItemCategory.RangedWeapons: return ItemCategoryHexValues.Weapon;
                    case ItemCategory.Rings: return ItemCategoryHexValues.Accessory;
                    case ItemCategory.Shields: return ItemCategoryHexValues.Weapon;
                    case ItemCategory.Spells: return ItemCategoryHexValues.Good;
                    case ItemCategory.SpellTools: return ItemCategoryHexValues.Weapon;
                    case ItemCategory.UpgradeMaterials: return ItemCategoryHexValues.Good;
                    case ItemCategory.UsableItems: return ItemCategoryHexValues.Good;
                    case ItemCategory.MysteryWeapons: return ItemCategoryHexValues.Weapon;
                    case ItemCategory.MysteryArmor: return ItemCategoryHexValues.Armor;
                    case ItemCategory.MysteryGoods: return ItemCategoryHexValues.Good;
                    case ItemCategory.Trap: return 0x33333333;
                }
                return 0x0;
            }
            public static EquipType ToShopEquipType(ItemCategory category)
            {
                switch (category)
                {
                    case ItemCategory.Armor: return EquipType.Armor;
                    case ItemCategory.Consumables: return EquipType.Good;
                    case ItemCategory.KeyItems: return EquipType.Good;
                    case ItemCategory.MeleeWeapons: return EquipType.Weapon;
                    case ItemCategory.RangedWeapons: return EquipType.Weapon;
                    case ItemCategory.Rings: return EquipType.Accessory;
                    case ItemCategory.Shields: return EquipType.Weapon;
                    case ItemCategory.Spells: return EquipType.Attunement;
                    case ItemCategory.SpellTools: return EquipType.Weapon;
                    case ItemCategory.UpgradeMaterials: return EquipType.Good;
                    case ItemCategory.UsableItems: return EquipType.Good;
                    case ItemCategory.MysteryWeapons: return EquipType.Weapon;
                    case ItemCategory.MysteryArmor: return EquipType.Armor;
                    case ItemCategory.MysteryGoods: return EquipType.Good;
                    case ItemCategory.Trap: return EquipType.Good;
                }
                return EquipType.Good;
            }

            internal static ShopType ToShopType(ItemCategory category)
            {
                switch (category)
                {
                    case ItemCategory.Armor: return ShopType.Shop;
                    case ItemCategory.Consumables: return ShopType.Shop;
                    case ItemCategory.KeyItems: return ShopType.Shop;
                    case ItemCategory.MeleeWeapons: return ShopType.Shop;
                    case ItemCategory.RangedWeapons: return ShopType.Shop;
                    case ItemCategory.Rings: return ShopType.Shop;
                    case ItemCategory.Shields: return ShopType.Shop;
                    case ItemCategory.Spells: return ShopType.Magic;
                    case ItemCategory.SpellTools: return ShopType.Shop;
                    case ItemCategory.UpgradeMaterials: return ShopType.Shop;
                    case ItemCategory.UsableItems: return ShopType.Shop;
                    case ItemCategory.MysteryWeapons: return ShopType.Shop;
                    case ItemCategory.MysteryArmor: return ShopType.Shop;
                    case ItemCategory.MysteryGoods: return ShopType.Shop;
                    case ItemCategory.Trap: return ShopType.Shop;
                }
                return ShopType.Shop;
            }
        }
        public enum ShopType
        {
            Shop = 0,
            Enhancement = 1,
            Magic = 2,
            Miracle = 3,
            Information = 4,
            SAN = 5
        }
        public enum EquipType
        {
            Weapon = 0,
            Armor = 1,
            Accessory = 2,
            Good = 3,
            Attunement = 4,
        }
        public enum ItemUpgrade
        {
            None = 0,
            Unique = 1,
            Armor = 2,
            Infusable = 3,
            InfusableRestricted = 4,
            PyroFlame = 5,
            PyroFlameAscended = 6
        }
    }
}
