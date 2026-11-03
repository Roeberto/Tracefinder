using System.Collections.Generic;
using System;
using Godot;
using Tracefinder.Character;
using Tracefinder.Items;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class ShopControl : Control
    {
        private CharacterCore _hero;
        private ClassStats _class;

        public override void _Ready()
        {
            _hero = GameSession.CurrentCharacter();
            _class = GameSession.CharacterClass;
            GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Exploration.tscn");
            Refresh();
        }

        private void Refresh()
        {
            GetNode<Label>("%MoneyLabel").Text = $"Sakiewka: {UiText.Money(_hero.Silver)}    AC {_hero.ComputeMaxAc(_class)}";
            FillArmor();
            FillWeapons();
            FillOther();
        }

        private void Apply(Func<bool> purchase)
        {
            GetNode<Label>("%ShopMessage").Text = purchase() ? "" : "Tego nie da się teraz kupić.";
            CampaignStorage.Save();
            Refresh();
        }

        private static void AddRow(VBoxContainer list, string text, string buttonText, bool enabled, Action pressed, string nodeName = null)
        {
            HBoxContainer row = new();
            row.AddChild(new Label { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart });
            Button button = new() { Text = buttonText, Disabled = !enabled, CustomMinimumSize = new Vector2(96, 0) };
            if (nodeName != null)
                button.Name = nodeName;
            button.Pressed += pressed;
            row.AddChild(button);
            list.AddChild(row);
        }

        private void FillArmor()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%ArmorList");
            Clear(list);
            AddRow(list, "Bez pancerza", _hero.EquippedArmor == null ? "Założony" : "Zdejmij", _hero.EquippedArmor != null, () => Apply(() =>
            {
                _hero.EquippedArmor = null;
                return true;
            }));

            foreach (Armor armor in ShopCatalog.Armors)
            {
                Armor captured = armor;
                ProficiencyRank rank = _class.ArmorRankAt(armor.Category, _hero.Level);
                string note = rank == ProficiencyRank.Untrained ? ", brak biegłości" : "";
                string text = $"{armor.Name}: AC +{armor.AcBonus}, limit ZRĘ {armor.DexCap}{note}, {UiText.Money(armor.PriceSp)}";
                string nodeName = "Armor" + armor.Name.Replace(" ", "");
                if (_hero.OwnedArmors.Contains(armor))
                {
                    bool worn = _hero.EquippedArmor == armor;
                    AddRow(list, text, worn ? "Założony" : "Załóż", !worn, () => Apply(() =>
                    {
                        _hero.EquippedArmor = captured;
                        return true;
                    }), nodeName);
                }
                else if (Shop.IsAvailable(armor.ItemLevel, _hero))
                {
                    AddRow(list, text, "Kup", _hero.Silver >= armor.PriceSp, () => Apply(() => Shop.BuyArmor(_hero, captured)), nodeName);
                }
            }
        }

        private void FillWeapons()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%WeaponList");
            Clear(list);
            foreach (Equipment.Weapon weapon in WeaponList.All)
            {
                Equipment.Weapon captured = weapon;
                CarriedWeapon owned = _hero.Inventory.Find(w => w.Template == weapon);
                string text = $"{weapon.Name}: {UiText.DamageText(weapon, 0)}, {UiText.Money(ShopCatalog.WeaponPriceSp(weapon))}";
                string nodeName = "Weapon" + weapon.Name;
                if (owned != null)
                {
                    bool held = _hero.EquippedWeapon == owned;
                    AddRow(list, text, held ? "W ręku" : "Weź", !held, () => Apply(() =>
                    {
                        _hero.EquippedWeapon = owned;
                        return true;
                    }), nodeName);
                }
                else
                {
                    AddRow(list, text, "Kup", _hero.Silver >= ShopCatalog.WeaponPriceSp(weapon), () => Apply(() => Shop.BuyWeapon(_hero, captured)), nodeName);
                }
            }

            CarriedWeapon held2 = _hero.EquippedWeapon;
            list.AddChild(new HSeparator());
            list.AddChild(new Label { Text = held2 == null ? "Runy: najpierw weź broń do ręki." : $"Runy broni w ręku ({held2.Name})" });
            if (held2 == null)
                return;

            if (held2.PotencyRune == 0 && Shop.IsAvailable(ShopCatalog.PotencyRuneLevel, _hero))
                AddRow(list, $"Potency +1: premia do ataku, {UiText.Money(ShopCatalog.PotencyRunePriceSp)}", "Kup", _hero.Silver >= ShopCatalog.PotencyRunePriceSp, () => Apply(() => Shop.BuyPotencyRune(_hero, held2)), "RunePotency");
            if (held2.PotencyRune > 0 && held2.StrikingRune == 0 && Shop.IsAvailable(ShopCatalog.StrikingRuneLevel, _hero))
                AddRow(list, $"Striking: dodatkowa kość obrażeń, {UiText.Money(ShopCatalog.StrikingRunePriceSp)}", "Kup", _hero.Silver >= ShopCatalog.StrikingRunePriceSp, () => Apply(() => Shop.BuyStrikingRune(_hero, held2)), "RuneStriking");
            if (held2.PotencyRune > 0)
                list.AddChild(new Label { Text = $"Założone: potency +{held2.PotencyRune}" + (held2.StrikingRune > 0 ? ", striking" : "") });
        }

        private void FillOther()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%OtherList");
            Clear(list);
            foreach (Potion potion in ShopCatalog.Potions)
            {
                Potion captured = potion;
                if (!Shop.IsAvailable(potion.ItemLevel, _hero))
                    continue;
                int count = _hero.Potions.GetValueOrDefault(potion.Name);
                string flat = potion.Flat > 0 ? $"+{potion.Flat}" : "";
                string text = $"{potion.Name} ({potion.Dice}{potion.Die.ToString().ToLowerInvariant()}{flat} HP), masz {count}, {UiText.Money(potion.PriceSp)}";
                AddRow(list, text, "Kup", _hero.Silver >= potion.PriceSp, () => Apply(() => Shop.BuyPotion(_hero, captured)), "Potion" + potion.Name.Replace(" ", ""));
            }

            list.AddChild(new HSeparator());
            if (_hero.HasShield)
            {
                string state = _hero.ShieldUsable ? "sprawna" : "uszkodzona, odpoczynek w osadzie ją naprawi";
                list.AddChild(new Label { Text = $"Stalowa tarcza: {_hero.ShieldHp} z {ShopCatalog.ShieldMaxHp} HP, {state}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            }
            else
            {
                AddRow(list, $"Stalowa tarcza: AC +{ShopCatalog.ShieldAcBonus} po podniesieniu, {UiText.Money(ShopCatalog.ShieldPriceSp)}", "Kup", _hero.Silver >= ShopCatalog.ShieldPriceSp, () => Apply(() => Shop.BuyShield(_hero)), "BuyShield");
            }
        }

        private static void Clear(Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
