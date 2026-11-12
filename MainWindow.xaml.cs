using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;

namespace MEABasicSaveEditor
{
    public partial class MainWindow : Window
    {
        private readonly SaveManager _mgr = new SaveManager();
        private ICollectionView _armoryView;
        private string _activeArmoryCategory = "Weapons";
        private bool _isSyncingLevelXp = false;
        private bool _isSyncingNexusAvp = false;

        public MainWindow()
        {
            InitializeComponent();
            _mgr.OnLog += LogStatus;
            ArmoryItemModel.OnTierChangeRequested = SetArmoryItemTier;

            // Real-time synchronization of header strip and Ryder Level / XP
            TxtRyderLevel.TextChanged += (s, e) =>
            {
                if (TxtHeaderLevel != null)
                {
                    TxtHeaderLevel.Text = TxtRyderLevel.Text.Trim();
                }

                if (!_isSyncingLevelXp && int.TryParse(TxtRyderLevel.Text, out int lvl) && lvl >= 1)
                {
                    _isSyncingLevelXp = true;
                    TxtCurrentXp.Text = SaveManager.GetBaseXpForLevel(lvl).ToString();
                    _isSyncingLevelXp = false;
                }
            };

            TxtCurrentXp.TextChanged += (s, e) =>
            {
                if (!_isSyncingLevelXp && int.TryParse(TxtCurrentXp.Text, out int xp) && xp >= 0)
                {
                    int calcLvl = SaveManager.GetLevelForXp(xp);
                    _isSyncingLevelXp = true;
                    TxtRyderLevel.Text = calcLvl.ToString();
                    if (TxtHeaderLevel != null) TxtHeaderLevel.Text = calcLvl.ToString();
                    _isSyncingLevelXp = false;
                }
            };

            // Two-way synchronization of Nexus Level, AVP, and Cryo Pod Points
            TxtNexusLevel.TextChanged += (s, e) =>
            {
                if (!_isSyncingNexusAvp && int.TryParse(TxtNexusLevel.Text, out int nl) && nl >= 1)
                {
                    _isSyncingNexusAvp = true;
                    TxtAvpPoints.Text = ((Math.Max(1, Math.Min(29, nl)) - 1) * 800).ToString();
                    _isSyncingNexusAvp = false;
                }
                if (int.TryParse(TxtNexusLevel.Text, out int nlvl) && TxtCryoPods != null)
                {
                    TxtCryoPods.Text = Math.Max(0, nlvl - 1).ToString();
                }
            };

            TxtAvpPoints.TextChanged += (s, e) =>
            {
                if (!_isSyncingNexusAvp && int.TryParse(TxtAvpPoints.Text, out int avp) && avp >= 0)
                {
                    int calcNl = Math.Max(1, Math.Min(29, 1 + (avp / 800)));
                    _isSyncingNexusAvp = true;
                    TxtNexusLevel.Text = calcNl.ToString();
                    _isSyncingNexusAvp = false;
                    if (TxtCryoPods != null)
                    {
                        TxtCryoPods.Text = Math.Max(0, calcNl - 1).ToString();
                    }
                }
            };

            TxtPathfinderName.TextChanged += (s, e) =>
            {
                if (TxtHeaderPathfinder != null)
                {
                    TxtHeaderPathfinder.Text = TxtPathfinderName.Text.Trim();
                }
            };

            // Preload game item catalog in background
            System.Threading.Tasks.Task.Run(() => SaveManager.EnsureCatalogLoaded());
        }

        private void LogStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtFooterStatus.Text = message;
            });
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var initialDir = SaveManager.DefaultSaveDirectory;
                if (!Directory.Exists(initialDir))
                {
                    initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                var dlg = new OpenFileDialog
                {
                    Title = "Open Mass Effect Andromeda Save File",
                    InitialDirectory = initialDir,
                    Filter = "Mass Effect Andromeda Saves (*Save*)|*Save*|All Files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dlg.ShowDialog() == true)
                {
                    LoadSaveFile(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSaveFile(string filePath)
        {
            bool ok = _mgr.LoadSave(filePath);
            if (!ok)
            {
                MessageBox.Show($"Failed to load save file:\n{filePath}", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveManager.EnsureCatalogLoaded();
            UpdateUiFromLoadedSave();
            TxtFooterStatus.Text = $"Save loaded: {Path.GetFileName(filePath)}";
        }

        private void UpdateUiFromLoadedSave()
        {
            _isSyncingLevelXp = true;
            _isSyncingNexusAvp = true;

            try
            {
                var h = _mgr.HeaderInfo;
                string displayFileName = Path.GetFileName(_mgr.OriginalLivePath ?? _mgr.CurrentFilePath ?? "Save");

                // Update Header Strip
                TxtHeaderFileName.Text = displayFileName;
                TxtHeaderPathfinder.Text = h.CharacterName;
                TxtHeaderLevel.Text = h.Level.ToString();
                TxtHeaderPlaytime.Text = h.PlayTimeFormatted;

                // Role-Play Card
                TxtPathfinderName.Text = h.CharacterName;
                if (TxtActiveProfileDisplay != null) TxtActiveProfileDisplay.Text = h.ProfileName;

                // Campaign Card
                TxtPlayTime.Text = h.PlayTimeFormatted;
                TxtLocation.Text = h.Location;
                CmbDifficulty.SelectedIndex = Math.Max(0, Math.Min(4, h.DifficultyIndex));
                TxtCareerId.Text = h.CareerId;

                // Gameplay Card
                TxtRyderLevel.Text = h.Level.ToString();
                TxtCurrentXp.Text = h.CurrentXp.ToString();
                TxtSkillPoints.Text = h.SkillPoints.ToString();
                TxtNexusLevel.Text = h.NexusLevel.ToString();
                TxtAvpPoints.Text = h.AvpPoints.ToString();
                TxtItemLimit.Text = h.ItemLimit.ToString();

                // Progression Card
                if (TxtCryoPods != null) TxtCryoPods.Text = Math.Max(0, h.NexusLevel - 1).ToString();
                TxtCredits.Text = h.Credits.ToString();
                TxtMilkyWayRD.Text = h.MilkyWayRD.ToString();
                TxtHeleusRD.Text = h.HeleusRD.ToString();
                TxtRemnantRD.Text = h.RemnantRD.ToString();

                // Profiles Tab
                if (_mgr.ProfileRanks != null && _mgr.ProfileRanks.Count >= 7)
                {
                    UpdateProfileUi();
                }

                // Resources Tab
                IcMinerals.ItemsSource = null;
                IcMinerals.ItemsSource = _mgr.Minerals;

                IcTechMaterials.ItemsSource = null;
                IcTechMaterials.ItemsSource = _mgr.TechMaterials;

                IcBioMaterials.ItemsSource = null;
                IcBioMaterials.ItemsSource = _mgr.BioMaterials;

                if (TxtRemnantKeys != null)
                {
                    TxtRemnantKeys.Text = _mgr.GetRemnantKeys().ToString();
                }
            }
            finally
            {
                _isSyncingLevelXp = false;
                _isSyncingNexusAvp = false;
            }

            FilterAugmentations(TxtSearchAugmentations?.Text);
            FilterSalvage(TxtSearchSalvage?.Text);

            // Armory Tab
            _armoryView = CollectionViewSource.GetDefaultView(_mgr.ArmoryItems);
            if (_armoryView != null)
            {
                _armoryView.Filter = FilterArmoryItem;
            }
            DgArmoryItems.ItemsSource = _armoryView;
            PopulateArmorySubCategories();
            UpdateCategoryBadges();
            UpdateArmoryItemCount();

            // Setup Add Item Catalog Dropdown
            UpdateAddCatalogItems();
            UpdateResAddCatalogItems();

            // Enable Controls
            BtnSave.IsEnabled = true;
            BtnReload.IsEnabled = true;
            PanelWelcomePrompt.Visibility = Visibility.Collapsed;
            MainTabs.Visibility = Visibility.Visible;
        }

        #region Armory UI Handlers

        private bool FilterArmoryItem(object obj)
        {
            if (!(obj is ArmoryItemModel item)) return false;

            // Category filter
            if (!string.Equals(item.Category, _activeArmoryCategory, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Technology Origin filter
            if (CmbArmoryOrigin != null && CmbArmoryOrigin.SelectedItem is ComboBoxItem cbiOrigin && cbiOrigin.Content is string origin && !origin.StartsWith("All", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(item.TechOrigin, origin, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Sub-category filter
            if (CmbArmorySubCategory != null && CmbArmorySubCategory.SelectedItem is string subCat && !subCat.StartsWith("All", StringComparison.OrdinalIgnoreCase))
            {
                bool match = false;
                if (subCat == "Assault Rifles" && item.SubCategory == "Assault Rifle") match = true;
                else if (subCat == "Pistols / SMGs" && item.SubCategory == "Pistol / SMG") match = true;
                else if (subCat == "Shotguns" && item.SubCategory == "Shotgun") match = true;
                else if (subCat == "Sniper Rifles" && item.SubCategory == "Sniper Rifle") match = true;
                else if (subCat == "Melee" && item.SubCategory == "Melee") match = true;
                else if (subCat == "Helmets" && item.SubCategory == "Helmet") match = true;
                else if (subCat == "Chest Pieces" && item.SubCategory == "Chest") match = true;
                else if (subCat == "Arms" && item.SubCategory == "Arms") match = true;
                else if (subCat == "Legs" && item.SubCategory == "Legs") match = true;
                else if (subCat == "Full Outfits" && item.SubCategory == "Full Armor") match = true;
                else if (subCat == "AR Mods" && item.SubCategory == "AR Mod") match = true;
                else if (subCat == "Pistol Mods" && item.SubCategory == "Pistol Mod") match = true;
                else if (subCat == "Shotgun Mods" && item.SubCategory == "Shotgun Mod") match = true;
                else if (subCat == "Sniper Mods" && item.SubCategory == "Sniper Mod") match = true;
                if (!match) return false;
            }

            // Search filter
            if (TxtArmorySearch != null && !string.IsNullOrWhiteSpace(TxtArmorySearch.Text))
            {
                string search = TxtArmorySearch.Text.Trim();
                bool matchesName = item.DisplayName != null && item.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesPart = item.PartitionName != null && item.PartitionName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchesName && !matchesPart) return false;
            }

            return true;
        }

        private void FilterArmoryCategory_Changed(object sender, RoutedEventArgs e)
        {
            if (RbFilterWeapons?.IsChecked == true) _activeArmoryCategory = "Weapons";
            else if (RbFilterWeaponMods?.IsChecked == true) _activeArmoryCategory = "Weapon Mods";
            else if (RbFilterArmor?.IsChecked == true) _activeArmoryCategory = "Armor";
            else if (RbFilterFusionMods?.IsChecked == true) _activeArmoryCategory = "Fusion Mods";
            else if (RbFilterConsumables?.IsChecked == true) _activeArmoryCategory = "Consumables";

            PopulateArmorySubCategories();
            _armoryView?.Refresh();
            UpdateArmoryItemCount();

            // Sync Add Item Category dropdown
            if (CmbAddCategory != null)
            {
                foreach (ComboBoxItem item in CmbAddCategory.Items)
                {
                    if (string.Equals(item.Content?.ToString(), _activeArmoryCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        CmbAddCategory.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void PopulateArmorySubCategories()
        {
            if (CmbArmorySubCategory == null) return;
            var list = new List<string>();
            switch (_activeArmoryCategory)
            {
                case "Weapons":
                    list.AddRange(new[] { "All Weapons", "Assault Rifles", "Pistols / SMGs", "Shotguns", "Sniper Rifles", "Melee" });
                    break;
                case "Armor":
                    list.AddRange(new[] { "All Armor", "Helmets", "Chest Pieces", "Arms", "Legs", "Full Outfits" });
                    break;
                case "Weapon Mods":
                    list.AddRange(new[] { "All Mods", "AR Mods", "Pistol Mods", "Shotgun Mods", "Sniper Mods" });
                    break;
                case "Fusion Mods":
                    list.AddRange(new[] { "All Fusion Mods" });
                    break;
                case "Consumables":
                    list.AddRange(new[] { "All Consumables" });
                    break;
                default:
                    list.Add("All Items");
                    break;
            }
            CmbArmorySubCategory.ItemsSource = list;
            CmbArmorySubCategory.SelectedIndex = 0;
        }

        private void CmbArmorySubCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _armoryView?.Refresh();
            UpdateArmoryItemCount();
        }

        private void CmbArmoryOrigin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _armoryView?.Refresh();
            UpdateArmoryItemCount();
        }

        private void TxtArmorySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _armoryView?.Refresh();
            UpdateArmoryItemCount();
        }

        private void BtnClearArmorySearch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtArmorySearch != null) TxtArmorySearch.Text = "";
        }

        private void UpdateCategoryBadges()
        {
            if (_mgr?.ArmoryItems == null) return;
            int weapons = _mgr.ArmoryItems.Count(i => i.Category == "Weapons");
            int mods = _mgr.ArmoryItems.Count(i => i.Category == "Weapon Mods");
            int armor = _mgr.ArmoryItems.Count(i => i.Category == "Armor");
            int fusions = _mgr.ArmoryItems.Count(i => i.Category == "Fusion Mods");
            int consumables = _mgr.ArmoryItems.Count(i => i.Category == "Consumables");

            if (RbFilterWeapons != null) RbFilterWeapons.Content = $"\U0001F52B Weapons ({weapons})";
            if (RbFilterWeaponMods != null) RbFilterWeaponMods.Content = $"\u2699 Weapon Mods ({mods})";
            if (RbFilterArmor != null) RbFilterArmor.Content = $"\U0001F6E1 Armor ({armor})";
            if (RbFilterFusionMods != null) RbFilterFusionMods.Content = $"\U0001F52E Fusion Mods ({fusions})";
            if (RbFilterConsumables != null) RbFilterConsumables.Content = $"\u26A1 Consumables ({consumables})";

        }

        private void UpdateArmoryItemCount()
        {
            int count = _armoryView?.Cast<object>().Count() ?? 0;
            if (TxtArmoryCount != null)
            {
                TxtArmoryCount.Text = $"{count} in View";
            }


            if (PanelArmoryEmpty != null)
            {
                PanelArmoryEmpty.Visibility = (count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BtnArmoryMaxTier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ArmoryItemModel item)
            {
                string oldTier = item.TierDisplay;
                SetArmoryItemTier(item, 10);
                _armoryView?.Refresh();
                UpdateCategoryBadges();
                ShowAlert($"★ Upgraded {item.DisplayName} from {oldTier} to Tier X (Max Tier)!");
            }
        }

        public static void SetArmoryItemTier(ArmoryItemModel item, int targetTierRank)
        {
            if (item == null || targetTierRank < 1 || targetTierRank > 10) return;

            // Search catalog for item with matching name, category, and target tier
            var match = SaveManager.FullCatalog.FirstOrDefault(c =>
                string.Equals(c.Category, item.Category, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Name, item.DisplayName, StringComparison.OrdinalIgnoreCase) &&
                c.TierRank == targetTierRank);

            if (match != null)
            {
                item.PartitionName = match.PartitionName;
                item.Tier = match.Tier;
            }
            else
            {
                string suffix = targetTierRank < 10 ? $"_0{targetTierRank}$1" : $"_10$1";
                item.PartitionName = System.Text.RegularExpressions.Regex.Replace(item.PartitionName, @"_0?\d+([a-z]?)$", suffix);
                item.Tier = targetTierRank - 1;
            }

            if (item.ItemData != null)
            {
                item.ItemData.PartitionName = item.PartitionName;
                if (MEA.GameInfo.InfoManager.Items != null &&
                    MEA.GameInfo.InfoManager.Items.TryGetValue(item.PartitionName, out var def))
                {
                    SaveManager.SetItemDefinition(item.ItemData, def);
                }
            }

            item.OnPropertyChanged(nameof(item.PartitionName));
            item.OnPropertyChanged(nameof(item.FriendlySubtitle));
            item.OnPropertyChanged(nameof(item.Tier));
            item.OnPropertyChanged(nameof(item.TierDisplay));
            item.OnPropertyChanged(nameof(item.TierRank));
            item.OnPropertyChanged(nameof(item.TierIndex));
            item.OnPropertyChanged(nameof(item.CanUpgradeTier));
        }

        private void UpgradeArmoryItemToMax(ArmoryItemModel item)
        {
            if (item == null) return;
            SetArmoryItemTier(item, 10);
        }

        private void BtnArmoryQtyDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ArmoryItemModel item)
            {
                item.Quantity = Math.Max(1, item.Quantity - 1);
            }
        }

        private void BtnArmoryQtyUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ArmoryItemModel item)
            {
                item.Quantity++;
            }
        }

        private void BtnAddQtyDown_Click(object sender, RoutedEventArgs e)
        {
            if (TxtAddQuantity != null && int.TryParse(TxtAddQuantity.Text, out int q))
            {
                TxtAddQuantity.Text = Math.Max(1, q - 1).ToString();
            }
        }

        private void BtnAddQtyUp_Click(object sender, RoutedEventArgs e)
        {
            if (TxtAddQuantity != null && int.TryParse(TxtAddQuantity.Text, out int q))
            {
                TxtAddQuantity.Text = (q + 1).ToString();
            }
        }

        private void BtnAddQtyPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && TxtAddQuantity != null)
            {
                TxtAddQuantity.Text = btn.Tag.ToString();
            }
        }

        private void BtnMaxConsumables_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr?.ArmoryItems == null) return;
            int count = 0;
            foreach (var item in _mgr.ArmoryItems.Where(i => i.Category == "Consumables"))
            {
                item.Quantity = 100;
                count++;
            }
            _armoryView?.Refresh();
            ShowAlert($"⚡ Set all {count} consumables to 100 in inventory!");
        }

        private void CmbAddCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAddCatalogItems();
        }

        private void TxtCatalogSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAddCatalogItems();
        }

        private void UpdateAddCatalogItems()
        {
            if (CmbAddCatalogItem == null || CmbAddCategory == null) return;

            string selectedCat = (CmbAddCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weapons";
            string search = TxtCatalogSearch?.Text?.Trim() ?? "";

            var query = SaveManager.FullCatalog.Where(c => string.Equals(c.Category, selectedCat, StringComparison.OrdinalIgnoreCase) && !c.PartitionName.Contains("_mp"));
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         c.PartitionName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var distinctNames = query.Select(c => c.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            CmbAddCatalogItem.ItemsSource = distinctNames;
            if (distinctNames.Count > 0)
            {
                CmbAddCatalogItem.SelectedIndex = 0;
            }
            else
            {
                if (CmbAddTier != null) CmbAddTier.ItemsSource = null;
            }

            if (TxtAddQuantity != null)
            {
                TxtAddQuantity.Text = (selectedCat == "Consumables") ? "100" : "1";
            }

            UpdateAddTiers();
        }

        private void CmbAddCatalogItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAddTiers();
        }

        private void UpdateAddTiers()
        {
            if (CmbAddTier == null || CmbAddCategory == null) return;

            string selectedCat = (CmbAddCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weapons";
            string selectedName = CmbAddCatalogItem?.SelectedItem as string;

            bool isTierable = (selectedCat != "Consumables" && selectedCat != "Fusion Mods");
            if (LblAddTier != null) LblAddTier.Visibility = isTierable ? Visibility.Visible : Visibility.Collapsed;
            if (CmbAddTier != null) CmbAddTier.Visibility = isTierable ? Visibility.Visible : Visibility.Collapsed;

            bool isConsumable = (selectedCat == "Consumables");
            if (LblAddQty != null) LblAddQty.Visibility = isConsumable ? Visibility.Visible : Visibility.Collapsed;
            if (BtnAddQtyDown != null) BtnAddQtyDown.Visibility = isConsumable ? Visibility.Visible : Visibility.Collapsed;
            if (TxtAddQuantity != null) TxtAddQuantity.Visibility = isConsumable ? Visibility.Visible : Visibility.Collapsed;
            if (BtnAddQtyUp != null) BtnAddQtyUp.Visibility = isConsumable ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(selectedName))
            {
                CmbAddTier.ItemsSource = null;
                return;
            }

            var tiers = SaveManager.FullCatalog
                .Where(c => string.Equals(c.Category, selectedCat, StringComparison.OrdinalIgnoreCase) 
                         && string.Equals(c.Name, selectedName, StringComparison.OrdinalIgnoreCase)
                         && !c.PartitionName.Contains("_mp"))
                .OrderBy(c => c.TierRank)
                .ToList();

            if (tiers.Count == 0)
            {
                CmbAddTier.ItemsSource = null;
                CmbAddTier.IsEnabled = false;
            }
            else
            {
                CmbAddTier.ItemsSource = tiers;
                CmbAddTier.DisplayMemberPath = "TierDisplay";
                CmbAddTier.SelectedIndex = 0;
                CmbAddTier.IsEnabled = isTierable;
            }
        }

        private void BtnAddItemToInventory_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.CurrentSave == null) return;

            string category = (CmbAddCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weapons";
            var selectedTierItem = CmbAddTier?.SelectedItem as CatalogItemModel;
            string partition = selectedTierItem?.PartitionName;

            if (string.IsNullOrEmpty(partition))
            {
                string selectedName = CmbAddCatalogItem?.SelectedItem as string;
                if (!string.IsNullOrEmpty(selectedName))
                {
                    var match = SaveManager.FullCatalog.FirstOrDefault(c => 
                        string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.Name, selectedName, StringComparison.OrdinalIgnoreCase));
                    partition = match?.PartitionName;
                }
            }

            if (string.IsNullOrEmpty(partition))
            {
                MessageBox.Show("Please select an item from the catalog dropdown to add.", "Item Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int qty = 1;
            if (category == "Consumables")
            {
                if (int.TryParse(TxtAddQuantity?.Text, out int parsedQty))
                {
                    qty = Math.Max(1, parsedQty);
                }
            }

            var added = _mgr.AddItemToArmory(partition, category, qty);
            if (added != null)
            {
                _armoryView?.Refresh();
                UpdateCategoryBadges();
                UpdateArmoryItemCount();
                string tierStr = (added.TierDisplay != "--") ? $" ({added.TierDisplay})" : "";
                string qtyStr = (category == "Consumables") ? $" (x{qty})" : "";
                ShowAlert($"✓ Added {added.DisplayName}{tierStr}{qtyStr} to inventory.");
            }
        }

        private void BtnDeleteArmoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ArmoryItemModel item)
            {
                _mgr.RemoveItemFromArmory(item);
                _armoryView?.Refresh();
                UpdateCategoryBadges();
                UpdateArmoryItemCount();
                ShowAlert($"✓ Removed {item.DisplayName} from inventory.");
            }
        }

        #endregion

        #region Resources Add Item Bar Handlers

        private void CmbResAddCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateResAddCatalogItems();
        }

        private void TxtResCatalogSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResAddCatalogItems();
        }

        private void CmbResAddCatalogItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateResAddTiers();
        }

        private void UpdateResAddCatalogItems()
        {
            if (CmbResAddCatalogItem == null || CmbResAddCategory == null) return;

            string selectedCat = (CmbResAddCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weapons";
            string search = TxtResCatalogSearch?.Text?.Trim().ToLowerInvariant() ?? "";

            var items = SaveManager.FullCatalog
                .Where(c => string.Equals(c.Category, selectedCat, StringComparison.OrdinalIgnoreCase) && !c.PartitionName.Contains("_mp"))
                .Where(c => string.IsNullOrEmpty(search) || c.Name.ToLowerInvariant().Contains(search))
                .Select(c => c.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            CmbResAddCatalogItem.ItemsSource = items;
            if (items.Count > 0)
            {
                CmbResAddCatalogItem.SelectedIndex = 0;
            }
            else
            {
                CmbResAddCatalogItem.SelectedIndex = -1;
            }
            UpdateResAddTiers();
        }

        private void UpdateResAddTiers()
        {
            if (CmbResAddTier == null || CmbResAddCategory == null) return;

            string selectedCat = (CmbResAddCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weapons";
            string selectedName = CmbResAddCatalogItem?.SelectedItem as string;

            bool isTierable = (selectedCat != "Consumables" && selectedCat != "Fusion Mods");
            if (LblResAddTier != null) LblResAddTier.Visibility = isTierable ? Visibility.Visible : Visibility.Collapsed;
            if (CmbResAddTier != null) CmbResAddTier.Visibility = isTierable ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(selectedName))
            {
                CmbResAddTier.ItemsSource = null;
                return;
            }

            var tiers = SaveManager.FullCatalog
                .Where(c => string.Equals(c.Category, selectedCat, StringComparison.OrdinalIgnoreCase) 
                         && string.Equals(c.Name, selectedName, StringComparison.OrdinalIgnoreCase)
                         && !c.PartitionName.Contains("_mp"))
                .OrderBy(c => c.TierRank)
                .ToList();

            if (tiers.Count == 0)
            {
                CmbResAddTier.ItemsSource = null;
                CmbResAddTier.IsEnabled = false;
            }
            else
            {
                CmbResAddTier.ItemsSource = tiers;
                CmbResAddTier.DisplayMemberPath = "TierDisplay";
                CmbResAddTier.SelectedIndex = 0;
                CmbResAddTier.IsEnabled = isTierable;
            }
        }

        private void BtnResAddQtyDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtResAddQuantity.Text, out int q) && q > 1)
                TxtResAddQuantity.Text = (q - 1).ToString();
        }

        private void BtnResAddQtyUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtResAddQuantity.Text, out int q))
                TxtResAddQuantity.Text = (q + 1).ToString();
        }

        private void BtnResAddItemToInventory_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.CurrentSave == null) return;

            string category = (CmbResAddCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weapons";
            var selectedTierItem = CmbResAddTier?.SelectedItem as CatalogItemModel;
            string partition = selectedTierItem?.PartitionName;

            if (string.IsNullOrEmpty(partition))
            {
                string selectedName = CmbResAddCatalogItem?.SelectedItem as string;
                if (!string.IsNullOrEmpty(selectedName))
                {
                    var match = SaveManager.FullCatalog.FirstOrDefault(c => 
                        string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.Name, selectedName, StringComparison.OrdinalIgnoreCase));
                    partition = match?.PartitionName;
                }
            }

            if (string.IsNullOrEmpty(partition))
            {
                MessageBox.Show("Please select an item from the catalog dropdown to add.", "Item Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int qty = 1;
            if (int.TryParse(TxtResAddQuantity.Text, out int parsedQty))
            {
                qty = Math.Max(1, parsedQty);
            }

            var added = _mgr.AddItemToArmory(partition, category, qty);
            if (added != null)
            {
                _armoryView?.Refresh();
                UpdateCategoryBadges();
                UpdateArmoryItemCount();
                string tierStr = (added.TierDisplay != "--") ? $" ({added.TierDisplay})" : "";
                ShowAlert($"✓ Added {added.DisplayName}{tierStr} (x{qty}) to inventory.");
            }
        }

        #endregion

        #region Profiles UI Handlers

        private bool _isUpdatingProfileUi = false;

        public void UpdateProfileUi()
        {
            if (_isUpdatingProfileUi || _mgr?.ProfileRanks == null || _mgr.ProfileRanks.Count < 7) return;
            _isUpdatingProfileUi = true;
            try
            {
                int activeId = _mgr.HeaderInfo.ProfileId;
                int activeRank = (activeId >= 0 && activeId < _mgr.ProfileRanks.Count) ? _mgr.ProfileRanks[activeId].Rank : 1;
                string activeName = _mgr.HeaderInfo.ProfileName;

                if (TxtActiveProfileOverviewTitle != null)
                {
                    TxtActiveProfileOverviewTitle.Text = $"{activeName} • Rank {activeRank}";

                    string desc = "";
                    switch (activeId)
                    {
                        case 0: desc = $"Soldier Rank {activeRank}: Grants Marksman Focus (+{10 + activeRank * 2}% weapon damage, clip size & accuracy) and Combat Hardening (+{15 + activeRank * 5} damage resistance)."; break;
                        case 1: desc = $"Engineer Rank {activeRank}: Grants AI Combat Drone construct, +{20 + activeRank * 5}% construct health/damage, and EMP shield siphoning."; break;
                        case 2: desc = $"Adept Rank {activeRank}: Grants Biotic Echoes Blast (detonations cause chain reaction blasts) and +{15 + activeRank * 5}% biotic force & duration."; break;
                        case 3: desc = $"Sentinel Rank {activeRank}: Grants Tech Armor (damage reduction barrier), +{15 + activeRank * 5}% combo detonation damage, and accelerated shield restore."; break;
                        case 4: desc = $"Vanguard Rank {activeRank}: Grants Siphoning Biotic Charge (restores 100% shields), +{20 + activeRank * 5}% melee damage & force, and sprint boost."; break;
                        case 5: desc = $"Infiltrator Rank {activeRank}: Grants Battlefield Awareness (seeing enemies through walls), +{25 + activeRank * 5}% weak point / headshot bonus, and cloak evade."; break;
                        case 6:
                        default: desc = $"Explorer Rank {activeRank}: Grants Biotic Blink Evade (teleport phase through solid barriers), +{8 + activeRank * 2}% universal power recharge, and balanced combat stats."; break;
                    }
                    if (TxtActiveProfileOverviewDesc != null)
                    {
                        TxtActiveProfileOverviewDesc.Text = desc;
                    }
                }

                // Update Stepper Rank Texts
                int rSoldier = _mgr.ProfileRanks[0].Rank;
                int rEngineer = _mgr.ProfileRanks[1].Rank;
                int rAdept = _mgr.ProfileRanks[2].Rank;
                int rSentinel = _mgr.ProfileRanks[3].Rank;
                int rVanguard = _mgr.ProfileRanks[4].Rank;
                int rInfiltrator = _mgr.ProfileRanks[5].Rank;
                int rExplorer = _mgr.ProfileRanks[6].Rank;

                if (TxtRankSoldier != null) TxtRankSoldier.Text = $"Rank {rSoldier}";
                if (TxtRankSentinel != null) TxtRankSentinel.Text = $"Rank {rSentinel}";
                if (TxtRankEngineer != null) TxtRankEngineer.Text = $"Rank {rEngineer}";
                if (TxtRankVanguard != null) TxtRankVanguard.Text = $"Rank {rVanguard}";
                if (TxtRankAdept != null) TxtRankAdept.Text = $"Rank {rAdept}";
                if (TxtRankInfiltrator != null) TxtRankInfiltrator.Text = $"Rank {rInfiltrator}";
                if (TxtRankExplorer != null) TxtRankExplorer.Text = $"Rank {rExplorer}";

                // Update Dynamic Rank-Specific Descriptions
                if (TxtDescSoldier != null) TxtDescSoldier.Text = GetProfileRankDescription(0, rSoldier);
                if (TxtDescSentinel != null) TxtDescSentinel.Text = GetProfileRankDescription(3, rSentinel);
                if (TxtDescEngineer != null) TxtDescEngineer.Text = GetProfileRankDescription(1, rEngineer);
                if (TxtDescVanguard != null) TxtDescVanguard.Text = GetProfileRankDescription(4, rVanguard);
                if (TxtDescAdept != null) TxtDescAdept.Text = GetProfileRankDescription(2, rAdept);
                if (TxtDescInfiltrator != null) TxtDescInfiltrator.Text = GetProfileRankDescription(5, rInfiltrator);
                if (TxtDescExplorer != null) TxtDescExplorer.Text = GetProfileRankDescription(6, rExplorer);

                // Dynamic Archetype Colors
                string activeAccentHex = "#00D2FF";
                string activeBgHex = "#142A3E";
                string activeHeroPillBg = "#0C253D";
                switch (activeId)
                {
                    case 0: // Soldier (Combat - Red)
                        activeAccentHex = "#EF4444";
                        activeBgHex = "#260B10";
                        activeHeroPillBg = "#3B0E17";
                        break;
                    case 1: // Engineer (Tech - Amber)
                        activeAccentHex = "#F59E0B";
                        activeBgHex = "#261906";
                        activeHeroPillBg = "#3B2608";
                        break;
                    case 2: // Adept (Biotics - Purple)
                        activeAccentHex = "#A855F7";
                        activeBgHex = "#220A30";
                        activeHeroPillBg = "#340F4B";
                        break;
                    case 3: // Sentinel (Tech + Biotic - Teal/Cyan)
                        activeAccentHex = "#06B6D4";
                        activeBgHex = "#06222B";
                        activeHeroPillBg = "#09323F";
                        break;
                    case 4: // Vanguard (Combat + Biotic - Magenta)
                        activeAccentHex = "#EC4899";
                        activeBgHex = "#2B091B";
                        activeHeroPillBg = "#3F0C27";
                        break;
                    case 5: // Infiltrator (Combat + Tech - Orange)
                        activeAccentHex = "#F97316";
                        activeBgHex = "#281206";
                        activeHeroPillBg = "#3E1C08";
                        break;
                    case 6: // Explorer (All-Rounder - Cyan)
                    default:
                        activeAccentHex = "#00D2FF";
                        activeBgHex = "#0C253D";
                        activeHeroPillBg = "#123759";
                        break;
                }

                var activeAccentBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(activeAccentHex);
                var activeBgBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(activeBgHex);
                var activePillBgBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(activeHeroPillBg);
                var activeBadgeBgBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#26" + activeAccentHex.TrimStart('#'));

                // Recolor Top Hero Card Elements
                if (HeroCardActiveBorder != null)
                {
                    HeroCardActiveBorder.BorderBrush = activeAccentBrush;
                }
                if (BarHeroActiveProfile != null)
                {
                    BarHeroActiveProfile.Background = activeAccentBrush;
                }
                if (BadgeHeroActiveCombat != null)
                {
                    BadgeHeroActiveCombat.BorderBrush = activeAccentBrush;
                    BadgeHeroActiveCombat.Background = activeBadgeBgBrush;
                }
                if (TxtBadgeHeroActiveCombat != null)
                {
                    TxtBadgeHeroActiveCombat.Foreground = activeAccentBrush;
                }
                if (BorderHeroProfilePill != null)
                {
                    BorderHeroProfilePill.BorderBrush = activeAccentBrush;
                    BorderHeroProfilePill.Background = activePillBgBrush;
                }
                if (TxtActiveProfileOverviewTitle != null)
                {
                    TxtActiveProfileOverviewTitle.Foreground = activeAccentBrush;
                }
                if (TxtActiveProfileDisplay != null)
                {
                    TxtActiveProfileDisplay.Foreground = activeAccentBrush;
                }

                // Active badges & Set Active buttons & Glowing Active Card Borders
                UpdateActiveProfileCard(CardSoldier, BadgeActiveSoldier, TxtBadgeActiveSoldier, BtnSetActiveSoldier, activeId == 0, "#EF4444", "#260B10");
                UpdateActiveProfileCard(CardSentinel, BadgeActiveSentinel, TxtBadgeActiveSentinel, BtnSetActiveSentinel, activeId == 3, "#06B6D4", "#06222B");
                UpdateActiveProfileCard(CardEngineer, BadgeActiveEngineer, TxtBadgeActiveEngineer, BtnSetActiveEngineer, activeId == 1, "#F59E0B", "#261906");
                UpdateActiveProfileCard(CardVanguard, BadgeActiveVanguard, TxtBadgeActiveVanguard, BtnSetActiveVanguard, activeId == 4, "#EC4899", "#2B091B");
                UpdateActiveProfileCard(CardAdept, BadgeActiveAdept, TxtBadgeActiveAdept, BtnSetActiveAdept, activeId == 2, "#A855F7", "#220A30");
                UpdateActiveProfileCard(CardInfiltrator, BadgeActiveInfiltrator, TxtBadgeActiveInfiltrator, BtnSetActiveInfiltrator, activeId == 5, "#F97316", "#281206");
                UpdateActiveProfileCard(CardExplorer, BadgeActiveExplorer, TxtBadgeActiveExplorer, BtnSetActiveExplorer, activeId == 6, "#00D2FF", "#0C253D");
            }
            finally
            {
                _isUpdatingProfileUi = false;
            }
        }

        private string GetProfileRankDescription(int pIdx, int rank)
        {
            rank = Math.Max(1, Math.Min(6, rank));
            switch (pIdx)
            {
                case 0: // Soldier
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{8 + rank * 2}% Weapon Damage  |  +{9 + rank}% Weapon Accuracy  |  +{8 + rank * 2}% Clip Size\n" +
                           $"  +{6 + rank * 4} Damage Resistance\n" +
                           $"• Marksman's Focus: Every enemy killed grants +{1.5 + rank * 0.5:0.#} Damage Resistance & +{rank}% weapon damage (stacks up to 5x).";

                case 1: // Engineer
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{15 + rank * 5}% Construct Health & Damage  |  +{10 + rank * 5}% Tech Power Damage\n" +
                           $"  +{16 + rank * 4}% Construct Health Regen  |  +{8 + rank * 2}% Tech Recharge Speed\n" +
                           $"• Combat Drone: Deploys an automated combat drone that fires EMP bolts, stripping enemy shields and self-destructing on death.";

                case 2: // Adept
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{16 + rank * 4}% Biotic Force  |  +{11 + rank * 4}% Biotic AoE Damage  |  +{12 + rank * 3}% Biotic AoE Radius\n" +
                           $"  +{8 + rank * 2}% Biotic Power Recharge Speed\n" +
                           $"• Biotic Echoes: Detonating biotic combos triggers cascading chain-reaction explosions affecting all nearby foes.";

                case 3: // Sentinel
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{21 + rank * 4}% Combo Detonation Damage  |  +{8 + rank * 2}% Tech Recharge Speed\n" +
                           $"  +{12 + rank * 3}% Power Shield Restoration  |  +{6 + rank * 4}% Power Restoration & Defense\n" +
                           $"• Tech Armor: Energy barrier absorbs incoming attacks (+{6 + rank * 4} Damage Resistance) and pulses on shield depletion.";

                case 4: // Vanguard
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{16 + rank * 4}% Maximum Shields  |  +{16 + rank * 4}% Melee Damage  |  +{16 + rank * 4}% Melee Force\n" +
                           $"  +{8 + rank * 2}% Biotic Recharge Speed  |  +{rank * 4}% Sprint Speed\n" +
                           $"• Siphoning Strike: Melee attacks restore {8 + rank * 2}% shields; Biotic Charge refills 100% shields instantly.";

                case 5: // Infiltrator
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{20 + rank * 5}% Weak Point & Headshot Bonus  |  +{12 + rank * 3}% Weapon Accuracy\n" +
                           $"  +{7 + rank * 3}% Tech Power Damage  |  +{10 + rank * 5}% Tactical Cloak Duration\n" +
                           $"• Battlefield Awareness: Highlights enemies in bright red through solid cover, and dodges grant Cloak Evade camouflage.";

                case 6: // Explorer
                default:
                    return $"• Rank {rank} Grants:\n" +
                           $"  +{6 + rank * 2}% Weapon Damage  |  +{7 + rank * 3}% Universal Power Damage\n" +
                           $"  +{9 + rank * 3} Damage Resistance  |  +{8 + rank * 2}% Universal Power Recharge Speed\n" +
                           $"• Biotic Blink Evade: Phasing teleport dash passes Ryder directly through physical walls, terrain, and combat barriers.";
            }
        }

        private void UpdateActiveProfileCard(Border card, Border badge, TextBlock badgeTxt, Button setBtn, bool isActive, string accentHex, string bgHex)
        {
            if (badge != null)
            {
                badge.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
                if (isActive)
                {
                    var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(accentHex);
                    badge.BorderBrush = brush;
                    badge.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#26" + accentHex.TrimStart('#'));
                    if (badgeTxt != null) badgeTxt.Foreground = brush;
                }
            }
            if (setBtn != null) setBtn.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;
            if (card != null)
            {
                if (isActive)
                {
                    card.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(accentHex);
                    card.BorderThickness = new Thickness(2);
                    card.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(bgHex);
                }
                else
                {
                    card.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#253E5C");
                    card.BorderThickness = new Thickness(1);
                    card.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#A60A131F");
                }
            }
        }


        private void BtnRankDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag != null && int.TryParse(el.Tag.ToString(), out int pIdx))
            {
                if (_mgr?.ProfileRanks != null && pIdx >= 0 && pIdx < _mgr.ProfileRanks.Count)
                {
                    _mgr.ProfileRanks[pIdx].Rank = Math.Max(1, _mgr.ProfileRanks[pIdx].Rank - 1);
                    UpdateProfileUi();
                }
            }
        }

        private void BtnRankUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag != null && int.TryParse(el.Tag.ToString(), out int pIdx))
            {
                if (_mgr?.ProfileRanks != null && pIdx >= 0 && pIdx < _mgr.ProfileRanks.Count)
                {
                    _mgr.ProfileRanks[pIdx].Rank = Math.Min(6, _mgr.ProfileRanks[pIdx].Rank + 1);
                    UpdateProfileUi();
                }
            }
        }

        private void BtnSetActiveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag != null && int.TryParse(el.Tag.ToString(), out int pIdx))
            {
                _mgr.HeaderInfo.ProfileId = pIdx;
                if (_mgr.RyderData?.Progression != null)
                {
                    _mgr.RyderData.Progression.CurrentProfileId = pIdx;
                }
                if (TxtActiveProfileDisplay != null)
                {
                    TxtActiveProfileDisplay.Text = _mgr.HeaderInfo.ProfileName;
                }
                UpdateProfileUi();
                ShowAlert($"★ Active combat profile set to {_mgr.HeaderInfo.ProfileName}!");
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            if (OverlayAboutModal != null)
            {
                OverlayAboutModal.Visibility = Visibility.Visible;
            }
        }

                private void BtnGoToProfiles_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabs != null)
            {
                MainTabs.SelectedIndex = 2; // Profiles Tab
            }
        }

        private void TxtItemLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mgr?.HeaderInfo == null || TxtItemLimit == null) return;
            if (int.TryParse(TxtItemLimit.Text, out int limit) && limit > 0)
            {
                _mgr.HeaderInfo.ItemLimit = limit;

            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }

        private void BtnCloseAbout_Click(object sender, RoutedEventArgs e)
        {
            if (OverlayAboutModal != null)
            {
                OverlayAboutModal.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.CurrentSave == null || string.IsNullOrEmpty(_mgr.CurrentFilePath))
            {
                MessageBox.Show("No save file is currently open.", "Cannot Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var h = _mgr.HeaderInfo;

            // Pull Role-Play
            h.CharacterName = TxtPathfinderName.Text.Trim();
            // h.ProfileId is already kept in sync when selecting a profile on the Profiles tab

            // Pull Campaign
            h.DifficultyIndex = CmbDifficulty.SelectedIndex >= 0 ? CmbDifficulty.SelectedIndex : 2;

            // Pull Gameplay
            if (int.TryParse(TxtRyderLevel.Text, out int lvl)) h.Level = Math.Max(1, lvl);
            if (int.TryParse(TxtCurrentXp.Text, out int xp)) h.CurrentXp = xp;
            if (int.TryParse(TxtSkillPoints.Text, out int sp)) h.SkillPoints = sp;
            if (int.TryParse(TxtItemLimit.Text, out int il)) h.ItemLimit = il;

            // Pull Progression (Nexus Level & AVP independently)
            if (int.TryParse(TxtNexusLevel.Text, out int nl)) h.NexusLevel = Math.Max(1, nl);
            if (int.TryParse(TxtAvpPoints.Text, out int avp)) h.AvpPoints = Math.Max(0, avp);

            if (int.TryParse(TxtCredits.Text, out int cr)) h.Credits = cr;
            if (int.TryParse(TxtMilkyWayRD.Text, out int mw)) h.MilkyWayRD = mw;
            if (int.TryParse(TxtHeleusRD.Text, out int hel)) h.HeleusRD = hel;
            if (int.TryParse(TxtRemnantRD.Text, out int rem)) h.RemnantRD = rem;

            // Pull Special Items (Remnant Decryption Keys)
            if (TxtRemnantKeys != null && int.TryParse(TxtRemnantKeys.Text, out int rk))
            {
                _mgr.SetRemnantKeys(rk);
            }

            // Pull Profiles
            if (_mgr.ProfileRanks != null && _mgr.ProfileRanks.Count >= 7)
            {
                for (int i = 0; i < 7; i++)
                {
                    _mgr.ProfileRanks[i].Rank = Math.Max(1, Math.Min(6, _mgr.ProfileRanks[i].Rank));
                }
            }

            bool ok = _mgr.Save();
            if (ok)
            {
                // Update header values
                TxtHeaderPathfinder.Text = h.CharacterName;
                TxtHeaderLevel.Text = h.Level.ToString();

                string backupMsg = !string.IsNullOrEmpty(_mgr.LastBackupPath)
                    ? $" (Backup created: {Path.GetFileName(_mgr.LastBackupPath)})"
                    : "";

                ShowAlert($"✓ Saved successfully directly to game save!{backupMsg}");
                TxtFooterStatus.Text = $"Save completed at {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                MessageBox.Show("Failed to save changes. Check write permissions or log file.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_mgr.CurrentFilePath)) return;

            var targetPath = _mgr.OriginalLivePath ?? _mgr.CurrentFilePath;
            LoadSaveFile(targetPath);
            ShowAlert("✓ Save file reloaded from disk.");
        }

        private void BtnRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var targetPath = _mgr.OriginalLivePath ?? _mgr.CurrentFilePath;
                var dlg = new RestoreBackupDialog(_mgr, targetPath) { Owner = this };
                if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.SelectedBackupPath))
                {
                    bool ok = _mgr.LoadBackupForRestore(dlg.SelectedBackupPath, dlg.SelectedTargetLivePath);
                    if (ok)
                    {
                        UpdateUiFromLoadedSave();
                        ShowAlert($"✓ Backup '{Path.GetFileName(dlg.SelectedBackupPath)}' loaded into editor! Review changes and click '💾 Save' to apply.");
                        TxtFooterStatus.Text = $"Backup loaded: {Path.GetFileName(dlg.SelectedBackupPath)} (Ready to save)";
                    }
                    else
                    {
                        MessageBox.Show("Failed to load backup file.", "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup restore: {ex.Message}", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAlert(string message)
        {
            TxtAlertMessage.Text = message;
            BannerAlert.Visibility = Visibility.Visible;
        }

        private void BtnDismissAlert_Click(object sender, RoutedEventArgs e)
        {
            BannerAlert.Visibility = Visibility.Collapsed;
        }

        #region Resources UI Handlers

        private void FilterAugmentations(string query)
        {
            if (DgAugmentations == null || _mgr.Augmentations == null) return;
            var list = _mgr.Augmentations.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                list = list.Where(a => (a.FriendlyName != null && a.FriendlyName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                       (a.Description != null && a.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            DgAugmentations.ItemsSource = list.ToList();
        }

        private void FilterSalvage(string query)
        {
            if (DgSalvage == null || _mgr.Salvage == null) return;
            var list = _mgr.Salvage.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                list = list.Where(s => (s.FriendlyName != null && s.FriendlyName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                       (s.Description != null && s.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            DgSalvage.ItemsSource = list.ToList();
        }

        private void TxtSearchAugmentations_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAugmentations(TxtSearchAugmentations?.Text);
        }

        private void TxtSearchSalvage_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterSalvage(TxtSearchSalvage?.Text);
        }

        private void BtnAdd1Key_Click(object sender, RoutedEventArgs e)
        {
            int current = int.TryParse(TxtRemnantKeys?.Text, out int val) ? val : 0;
            TxtRemnantKeys.Text = (current + 1).ToString();
            _mgr.SetRemnantKeys(current + 1);
        }

        private void BtnAdd5Keys_Click(object sender, RoutedEventArgs e)
        {
            int current = int.TryParse(TxtRemnantKeys?.Text, out int val) ? val : 0;
            TxtRemnantKeys.Text = (current + 5).ToString();
            _mgr.SetRemnantKeys(current + 5);
        }

        private void BtnMaxKeys_Click(object sender, RoutedEventArgs e)
        {
            TxtRemnantKeys.Text = "9";
            _mgr.SetRemnantKeys(9);
        }

        private void BtnMaxMinerals_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Minerals == null) return;
            foreach (var m in _mgr.Minerals) m.Quantity = 9999;
            IcMinerals.Items.Refresh();
            ShowAlert("✓ All 17 Minerals set to 9999.");
        }

        private void BtnClearMinerals_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Minerals == null) return;
            foreach (var m in _mgr.Minerals) m.Quantity = 0;
            IcMinerals.Items.Refresh();
            ShowAlert("✓ All Minerals set to 0.");
        }

        private void BtnMaxTechMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.TechMaterials == null) return;
            foreach (var t in _mgr.TechMaterials) t.Quantity = 999;
            IcTechMaterials.Items.Refresh();
            ShowAlert("✓ All Tech Materials set to 999.");
        }

        private void BtnClearTechMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.TechMaterials == null) return;
            foreach (var t in _mgr.TechMaterials) t.Quantity = 0;
            IcTechMaterials.Items.Refresh();
            ShowAlert("✓ All Tech Materials set to 0.");
        }

        private void BtnMaxBioMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.BioMaterials == null) return;
            foreach (var b in _mgr.BioMaterials) b.Quantity = 999;
            IcBioMaterials.Items.Refresh();
            ShowAlert("✓ All Bio Materials set to 999.");
        }

        private void BtnClearBioMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.BioMaterials == null) return;
            foreach (var b in _mgr.BioMaterials) b.Quantity = 0;
            IcBioMaterials.Items.Refresh();
            ShowAlert("✓ All Bio Materials set to 0.");
        }

        private void BtnAdd10Augmentations_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Augmentations == null) return;
            foreach (var a in _mgr.Augmentations) a.Quantity += 10;
            FilterAugmentations(TxtSearchAugmentations?.Text);
            ShowAlert("✓ Added 10 to all Augmentations.");
        }

        private void BtnMaxAugmentations_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Augmentations == null) return;
            foreach (var a in _mgr.Augmentations) a.Quantity = 50;
            FilterAugmentations(TxtSearchAugmentations?.Text);
            ShowAlert("✓ All Augmentations set to 50.");
        }

        private void BtnClearAugmentations_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Augmentations == null) return;
            foreach (var a in _mgr.Augmentations) a.Quantity = 0;
            FilterAugmentations(TxtSearchAugmentations?.Text);
            ShowAlert("✓ All Augmentations set to 0.");
        }

        private void BtnMaxSalvage_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Salvage == null) return;
            foreach (var s in _mgr.Salvage) s.Quantity = 100;
            FilterSalvage(TxtSearchSalvage?.Text);
            ShowAlert("✓ All Salvage items set to 100.");
        }

        private void BtnClearSalvage_Click(object sender, RoutedEventArgs e)
        {
            if (_mgr.Salvage == null) return;
            foreach (var s in _mgr.Salvage) s.Quantity = 0;
            FilterSalvage(TxtSearchSalvage?.Text);
            ShowAlert("✓ All Salvage items set to 0.");
        }

        private void BtnClearAugmentationsSearch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtSearchAugmentations != null) TxtSearchAugmentations.Text = "";
        }

        private void BtnClearSalvageSearch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtSearchSalvage != null) TxtSearchSalvage.Text = "";
        }

        #endregion
    }
}
