using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using MEA.FileFormats;
using MEA.GameInfo;
using MEA.SaveFormats;
using MEA.SaveFormats.Agents;
using MEA.SaveFormats.Components;
using MEA.SaveFormats.ComponentContainerAgents;
using MEA.SaveFormats.CustomizedParameters;
using MEA.SaveFormats.Data;
using ItemData = MEA.SaveFormats.Items.ItemData;
using GearItemData = MEA.SaveFormats.Items.GearItemData;
using RangedWeaponItemData = MEA.SaveFormats.Items.RangedWeaponItemData;

namespace MEABasicSaveEditor
{
    public class SaveManager
    {
        public SaveFile CurrentSave { get; private set; }
        public string CurrentFilePath { get; private set; }
        public string OriginalLivePath { get; private set; }
        public string WorkingFilePath { get; private set; }
        public string LastBackupPath { get; private set; }
        public PartyManagerAgent PartyAgent { get; private set; }
        public PlotAgent PlotAgent { get; private set; }
        public PartyMember RyderData { get; private set; }
        public SaveHeaderInfo HeaderInfo { get; private set; } = new SaveHeaderInfo();

        public List<ProfileRankModel> ProfileRanks { get; private set; } = new List<ProfileRankModel>();
        public List<ResourceItemModel> Minerals { get; private set; } = new List<ResourceItemModel>();
        public List<ResourceItemModel> TechMaterials { get; private set; } = new List<ResourceItemModel>();
        public List<ResourceItemModel> BioMaterials { get; private set; } = new List<ResourceItemModel>();
        public List<ResourceItemModel> Salvage { get; private set; } = new List<ResourceItemModel>();
        public List<ResourceItemModel> Augmentations { get; private set; } = new List<ResourceItemModel>();
        public List<ResourceItemModel> SpecialItems { get; private set; } = new List<ResourceItemModel>();

        public ObservableCollection<ArmoryItemModel> ArmoryItems { get; } = new ObservableCollection<ArmoryItemModel>();
        public List<uint> DeletedArmoryItemIds { get; } = new List<uint>();
        public static List<CatalogItemModel> FullCatalog { get; private set; } = new List<CatalogItemModel>();

        public static readonly (string Partition, string Name)[] FusionModsList = new[]
        {
            ("game/weapons/mods/gear/gearmod_adrenaline", "FUSION MOD OF ADRENALINE"),
            ("game/weapons/mods/gear/gearmod_assault", "FUSION MOD OF ASSAULT"),
            ("game/weapons/mods/gear/gearmod_battering", "FUSION MOD OF BATTERING"),
            ("game/weapons/mods/gear/gearmod_bioticsmastery", "FUSION MOD OF BIOTIC MASTERY"),
            ("game/weapons/mods/gear/gearmod_constructs", "FUSION MOD OF ROBOTICS"),
            ("game/weapons/mods/gear/gearmod_health", "FUSION MOD OF HEALTH"),
            ("game/weapons/mods/gear/gearmod_hovering", "FUSION MOD OF HOVERING"),
            ("game/weapons/mods/gear/gearmod_rapiddeployment", "FUSION MOD OF RAPID DEPLOYMENT"),
            ("game/weapons/mods/gear/gearmod_resistance", "FUSION MOD OF RESISTANCE"),
            ("game/weapons/mods/gear/gearmod_rupture", "FUSION MOD OF RUPTURE"),
            ("game/weapons/mods/gear/gearmod_shielding", "FUSION MOD OF SHIELDING"),
            ("game/weapons/mods/gear/gearmod_techmastery", "FUSION MOD OF TECH MASTERY")
        };

        public static readonly (string Partition, string Name)[] CoreConsumablesList = new[]
        {
            ("game/items/consumables/missilelauncher/consumable_missilelauncher", "COBRA RPG"),
            ("game/items/consumables/adrenaline/consumable_adrenaline", "OVERDRIVE PACK"),
            ("game/items/consumables/shieldcapacitor/consumable_shieldcapacitor", "SHIELD CAPACITORS"),
            ("game/items/consumables/incendiaryammo/consumable_incendiaryammo", "INCENDIARY AMMO"),
            ("game/items/consumables/cryoammo/consumable_cryoammo", "CRYO AMMO"),
            ("game/items/consumables/disruptorammo/consumable_disruptorammo", "DISRUPTOR AMMO"),
            ("game/items/consumables/hazardresistance/consumable_hazardresistance", "BACKUP LIFE SUPPORT")
        };

        public event Action<string> OnLog;

        public static readonly string DefaultSaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "BioWare", "Mass Effect Andromeda", "Save");

        public static readonly string BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ME Andromeda Save Editor Backup");

        public static readonly Guid Perk_TradeCapacity1 = new Guid("7f3fe532-1560-48a8-93d7-473a7f9a0169");
        public static readonly Guid Perk_TradeCapacity2 = new Guid("25a6409c-4335-4c7b-8880-ba78f0af0159");
        public static readonly Guid AVP_IntGuid = new Guid("d70a72ca-be57-4f1b-887f-91500cf303a4");
        public static readonly Guid NexusLevel_IntGuid1 = new Guid("ac11b116-6f7e-7bdd-64c0-365988970391");
        public static readonly Guid NexusLevel_IntGuid2 = new Guid("0d3d64df-b97d-a903-6f20-4e00635f030d");
        public static readonly Guid NexusLevel_IntGuid3 = new Guid("ac5db2e4-c4c3-dc8a-87fc-8c52d9f90379");

        #region Resource Catalog Definition
        private static readonly List<ResourceItemModel> KnownMinerals = new List<ResourceItemModel>
        {
            new ResourceItemModel { FriendlyName = "Aluminum (Al)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Aluminum_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Beryllium (Be)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Beryllium_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Cadmium (Cd)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Cadmium_01", Rarity = 1 },
            new ResourceItemModel { FriendlyName = "Copper (Cu)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Copper_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Element Zero (Ez)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_ElementZero_01", Rarity = 3 },
            new ResourceItemModel { FriendlyName = "Fluorite (F)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Flourite_01", Rarity = 1 },
            new ResourceItemModel { FriendlyName = "Graphite (C)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Graphite_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Iridium (Ir)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Iridium_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Iron (Fe)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Iron_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Lithium (Li)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Lithium_01", Rarity = 1 },
            new ResourceItemModel { FriendlyName = "Magnesium (Mg)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Magnesium_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Nickel (Ni)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Nickel_01", Rarity = 1 },
            new ResourceItemModel { FriendlyName = "Platinum (Pt)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Platinum_01", Rarity = 2 },
            new ResourceItemModel { FriendlyName = "Silicon (Si)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Silicon_01", Rarity = 0 },
            new ResourceItemModel { FriendlyName = "Titanium (Ti)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Titanium_01", Rarity = 2 },
            new ResourceItemModel { FriendlyName = "Uranium (U)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Uranium_01", Rarity = 1 },
            new ResourceItemModel { FriendlyName = "Vanadium (V)", Category = "Minerals", PartitionName = "Game/Items/Research/Materials/Mineral/RM_Vanadium_01", Rarity = 2 }
        };

        private static readonly List<ResourceItemModel> KnownTechMaterials = new List<ResourceItemModel>
        {
            new ResourceItemModel { FriendlyName = "Angaran Meditation Crystal", Category = "Tech Materials", PartitionName = "Game/Items/Research/Materials/Faction/RM_Angara_01", Rarity = 2 },
            new ResourceItemModel { FriendlyName = "Kett Alloy", Category = "Tech Materials", PartitionName = "Game/Items/Research/Materials/Faction/RM_Kett_01", Rarity = 2 },
            new ResourceItemModel { FriendlyName = "Omni-Gel Canister", Category = "Tech Materials", PartitionName = "Game/Items/Research/Materials/Faction/RM_MilkyWay_01", Rarity = 2 },
            new ResourceItemModel { FriendlyName = "Remnant Polymer", Category = "Tech Materials", PartitionName = "Game/Items/Research/Materials/Faction/RM_Remnant_01", Rarity = 2 }
        };

        private static readonly List<ResourceItemModel> KnownBioMaterials = new List<ResourceItemModel>
        {
            new ResourceItemModel { FriendlyName = "Eiroch Fluid Sac", Category = "Bio Materials", PartitionName = "game/items/research/materials/hunting/rm_fiendrare_01", Rarity = 2, Description = "Volatile glandular fluid harvested from apex Eirochs" },
            new ResourceItemModel { FriendlyName = "Remnant Core", Category = "Bio Materials", PartitionName = "game/items/research/materials/hunting/rm_remnantbehemoth", Rarity = 3, Description = "Rare power core harvested from a Remnant Behemoth" },
            new ResourceItemModel { FriendlyName = "Renderable Plates", Category = "Bio Materials", PartitionName = "game/items/research/materials/hunting/rm_hide_01", Rarity = 1, Description = "Toughened hide plates from wild Adhi & Eirochs" },
            new ResourceItemModel { FriendlyName = "Scale Fibers", Category = "Bio Materials", PartitionName = "game/items/research/materials/hunting/rm_scales_01", Rarity = 1, Description = "Interlocking reptilian scales from Rylkor predators" },
            new ResourceItemModel { FriendlyName = "Shell Filaments", Category = "Bio Materials", PartitionName = "game/items/research/materials/hunting/rm_shell_01", Rarity = 1, Description = "Flexible carapace fibers from Kaerkyns & Taurgs" },
            new ResourceItemModel { FriendlyName = "Soft Chitin", Category = "Bio Materials", PartitionName = "game/items/research/materials/hunting/rm_chitin_01", Rarity = 1, Description = "Pliant insectoid exoskeleton from Spitbugs & Shemrys" }
        };

        private static readonly List<ResourceItemModel> KnownSpecialItems = new List<ResourceItemModel>
        {
            new ResourceItemModel
            {
                FriendlyName = "Remnant Decryption Key",
                Category = "Special Items",
                PartitionName = "game/items/plotitems/remdukoskip_item",
                Rarity = 3,
                Description = "Allows immediate bypass of Remnant Monolith & Vault decryption puzzles"
            }
        };

        private static readonly List<ResourceItemModel> KnownSalvage = new List<ResourceItemModel>
        {
            new ResourceItemModel { FriendlyName = "Generic Salvage", Category = "Salvage", PartitionName = "game/items/stackables/salvage", Rarity = 0, Description = "Assorted salvaged scrap and spare parts" },
            new ResourceItemModel { FriendlyName = "Adaptive Optics", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_veryrare_1", Rarity = 3, Description = "High-value Angaran sensor lenses" },
            new ResourceItemModel { FriendlyName = "Ballistic Drift Stabilizer", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_veryrare_2", Rarity = 3, Description = "High-value targeting gyroscope" },
            new ResourceItemModel { FriendlyName = "Soil Reducer", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_veryrare_3", Rarity = 3, Description = "High-value terraforming enzyme processor" },
            new ResourceItemModel { FriendlyName = "Comm Array", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_rare_2", Rarity = 2, Description = "Moderate-value Angaran radio transceiver" },
            new ResourceItemModel { FriendlyName = "Grav-Torque Pad", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_rare_3", Rarity = 2, Description = "Moderate-value anti-gravity stabilizer pad" },
            new ResourceItemModel { FriendlyName = "Nav-Coil Fibers", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_rare_4", Rarity = 2, Description = "Moderate-value electromagnetic coils" },
            new ResourceItemModel { FriendlyName = "Scourge Drift Sensor", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_rare_1", Rarity = 2, Description = "Moderate-value Scourge energy sensor" },
            new ResourceItemModel { FriendlyName = "Adaptive Polymer Wrap", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_common_3", Rarity = 0, Description = "Common weatherproofing insulation wrap" },
            new ResourceItemModel { FriendlyName = "Angaran Ceramics", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_common_5", Rarity = 0, Description = "Common handmade Angaran pottery relics" },
            new ResourceItemModel { FriendlyName = "Bioweave Canvas", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_common_1", Rarity = 0, Description = "Common woven bio-fiber cloth" },
            new ResourceItemModel { FriendlyName = "Moisture Filters", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_common_4", Rarity = 0, Description = "Common water purification membrane" },
            new ResourceItemModel { FriendlyName = "Resin Caps", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_angara_common_2", Rarity = 0, Description = "Common organic sealant containers" },
            new ResourceItemModel { FriendlyName = "Enzymatic Injector", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_veryrare_1", Rarity = 3, Description = "High-value Kett genetic dispersal tool" },
            new ResourceItemModel { FriendlyName = "Reflexive Coupling", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_veryrare_3", Rarity = 3, Description = "High-value neuro-mechanical joint" },
            new ResourceItemModel { FriendlyName = "Ur-Exo Power Supply", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_veryrare_2", Rarity = 3, Description = "High-value Kett heavy exosuit battery" },
            new ResourceItemModel { FriendlyName = "Bioelectric Infusers", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_rare_1", Rarity = 2, Description = "Moderate-value organic energy conductors" },
            new ResourceItemModel { FriendlyName = "Bionacent Converter", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_rare_2", Rarity = 2, Description = "Moderate-value cellular biomass adapter" },
            new ResourceItemModel { FriendlyName = "Opto-Electric Module", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_rare_3", Rarity = 2, Description = "Moderate-value photonic fiber matrix" },
            new ResourceItemModel { FriendlyName = "Uncharged Bioreader", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_rare_4", Rarity = 2, Description = "Moderate-value DNA diagnostic scanner" },
            new ResourceItemModel { FriendlyName = "Bioelectric Restraints", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_common_3", Rarity = 0, Description = "Common Kett prisoner nerve shackles" },
            new ResourceItemModel { FriendlyName = "Enzymatic Vials", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_common_1", Rarity = 0, Description = "Common caustic chemical containers" },
            new ResourceItemModel { FriendlyName = "Impact Restrictors", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_common_5", Rarity = 0, Description = "Common inertial armor damping pads" },
            new ResourceItemModel { FriendlyName = "Kett Sealant", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_common_4", Rarity = 0, Description = "Common bone-resin bonding glue" },
            new ResourceItemModel { FriendlyName = "Protein Sludge Pack", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_kett_common_2", Rarity = 0, Description = "Common synthetic nutritional paste" },
            new ResourceItemModel { FriendlyName = "Coolant Cells", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_veryrare_3", Rarity = 3, Description = "High-value super-conductive cryogenic core" },
            new ResourceItemModel { FriendlyName = "Nanocrystal Modules", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_veryrare_2", Rarity = 3, Description = "High-value crystalline storage drive" },
            new ResourceItemModel { FriendlyName = "Quarian Nav Processor", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_veryrare_4", Rarity = 3, Description = "High-value starship guidance computer" },
            new ResourceItemModel { FriendlyName = "Settlement Core Relay", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_veryrare_5", Rarity = 3, Description = "High-value Initiative outpost power relay" },
            new ResourceItemModel { FriendlyName = "Shield Focusing Module", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_veryrare_1", Rarity = 3, Description = "High-value kinetic barrier amplifier" },
            new ResourceItemModel { FriendlyName = "Beacon Optics", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_2", Rarity = 2, Description = "Moderate-value navigational focal glass" },
            new ResourceItemModel { FriendlyName = "Dampening Coils", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_5", Rarity = 2, Description = "Moderate-value vibration absorption ring" },
            new ResourceItemModel { FriendlyName = "Eezo Shunt", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_6", Rarity = 2, Description = "Moderate-value mass effect core conduit" },
            new ResourceItemModel { FriendlyName = "Flexible Polymer Casing", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_8", Rarity = 2, Description = "Moderate-value insulated armor shell" },
            new ResourceItemModel { FriendlyName = "MEF Suppressor", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_4", Rarity = 2, Description = "Moderate-value field harmonic silencer" },
            new ResourceItemModel { FriendlyName = "Modified Energy Cell", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_1", Rarity = 2, Description = "Moderate-value high-drain lithium battery" },
            new ResourceItemModel { FriendlyName = "Portable Rebreather", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_7", Rarity = 2, Description = "Moderate-value emergency oxygen filter" },
            new ResourceItemModel { FriendlyName = "Small Power Core", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_rare_3", Rarity = 2, Description = "Moderate-value generator fuel cell" },
            new ResourceItemModel { FriendlyName = "Asari Comm-Link", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_8", Rarity = 0, Description = "Common civilian communications earpiece" },
            new ResourceItemModel { FriendlyName = "Biodisinfectant", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_1", Rarity = 0, Description = "Common medical sanitation canister" },
            new ResourceItemModel { FriendlyName = "ExoGeni M-Spec Magnets", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_5", Rarity = 0, Description = "Common industrial electro-magnets" },
            new ResourceItemModel { FriendlyName = "Leather Wrapping", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_4", Rarity = 0, Description = "Common grip and stock handle wrap" },
            new ResourceItemModel { FriendlyName = "Repair Actuals", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_3", Rarity = 0, Description = "Common hardware diagnostic chips" },
            new ResourceItemModel { FriendlyName = "Salarian Magnifier", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_9", Rarity = 0, Description = "Common laboratory precision microscope" },
            new ResourceItemModel { FriendlyName = "Terran Rivets", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_7", Rarity = 0, Description = "Common structural alloy fasteners" },
            new ResourceItemModel { FriendlyName = "Turian Calibration Coil", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_6", Rarity = 0, Description = "Common precision weapon alignment coil" },
            new ResourceItemModel { FriendlyName = "Welding Electrodes", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_milky_way_common_2", Rarity = 0, Description = "Common construction arc-welding rod" },
            new ResourceItemModel { FriendlyName = "Algorithm Fragment Scan", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_veryrare_4", Rarity = 3, Description = "High-value Remnant cryptographic scan" },
            new ResourceItemModel { FriendlyName = "Classification Circuit", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_veryrare_1", Rarity = 3, Description = "High-value Remnant logic processor" },
            new ResourceItemModel { FriendlyName = "Sprouted Seed Vial", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_veryrare_5", Rarity = 3, Description = "Ancient preserved botanical sample" },
            new ResourceItemModel { FriendlyName = "Universal Semiconductor", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_veryrare_3", Rarity = 3, Description = "Intact quantum superconducting wafer" },
            new ResourceItemModel { FriendlyName = "Vault-Forged Relay", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_veryrare_2", Rarity = 3, Description = "Subterranean Remnant power conductor" },
            new ResourceItemModel { FriendlyName = "Eezo-Inlaid Circuitry", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_rare_4", Rarity = 2, Description = "Moderate-value mass effect printed circuit" },
            new ResourceItemModel { FriendlyName = "Intact Neuro-Coil", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_rare_1", Rarity = 2, Description = "Moderate-value synthetic neural node" },
            new ResourceItemModel { FriendlyName = "Observer Lens", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_rare_2", Rarity = 2, Description = "Moderate-value Observer optical receptor" },
            new ResourceItemModel { FriendlyName = "Remnant Processing Core", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_rare_3", Rarity = 2, Description = "Moderate-value central logic sphere" },
            new ResourceItemModel { FriendlyName = "Inorganic Lubricant", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_common_1", Rarity = 0, Description = "Common low-friction mechanical gel" },
            new ResourceItemModel { FriendlyName = "Nitrate Infuser", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_common_4", Rarity = 0, Description = "Common automated chemical mixer" },
            new ResourceItemModel { FriendlyName = "Rarefied Filaments", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_common_3", Rarity = 0, Description = "Common micro-thin conductor threads" },
            new ResourceItemModel { FriendlyName = "Reconstituted Components", Category = "Salvage", PartitionName = "game/items/stackables/salvage/salvage_remnant_common_2", Rarity = 0, Description = "Common recycled machine fittings" }
        };

        private static readonly List<ResourceItemModel> KnownAugmentations = new List<ResourceItemModel>
        {
            new ResourceItemModel { FriendlyName = "Bio-Converter", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_bioconverter", Rarity = 3, Description = "Drains 5% HP on empty clip to instantly refill magazine" },
            new ResourceItemModel { FriendlyName = "Shield Oscillator", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_shieldboosters", Rarity = 3, Description = "Restores 25% Shields on clip reload or combat kill" },
            new ResourceItemModel { FriendlyName = "Vintage Heat Sink", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_renweablepowersource", Rarity = 3, Description = "Converts weapon to Mass Effect 1 cooldown recharge" },
            new ResourceItemModel { FriendlyName = "Seeking Plasma System", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_seekingplasma", Rarity = 3, Description = "Converts projectiles to guided plasma bolts that track targets" },
            new ResourceItemModel { FriendlyName = "Beam Emitter", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_beam", Rarity = 2, Description = "Converts weapon firing mode to a constant laser energy beam" },
            new ResourceItemModel { FriendlyName = "Plasma Charge System", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_plasmabooster", Rarity = 2, Description = "Fires charged explosive plasma energy balls" },
            new ResourceItemModel { FriendlyName = "Grenade Launcher", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_grenadelauncher", Rarity = 2, Description = "Converts bullets into lobbed contact-detonated grenades" },
            new ResourceItemModel { FriendlyName = "Sticky Grenade Launcher", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_stickygrenade", Rarity = 2, Description = "Fires sticky explosives that adhere to surfaces & enemies" },
            new ResourceItemModel { FriendlyName = "Ricochet System", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_bouncingprojectile", Rarity = 1, Description = "Projectiles bounce off walls and floors into enemies" },
            new ResourceItemModel { FriendlyName = "Coolant Unit", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_gunprojectile_efficientcooling", Rarity = 1, Description = "Decreases heat generation and increases thermal clip capacity" },
            new ResourceItemModel { FriendlyName = "Concentration Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_special_aimbooster", Rarity = 2, Description = "Increases weapon damage when zooming or scoped in" },
            new ResourceItemModel { FriendlyName = "Equilibrium Regulator", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/gunprojectiles/augmaterial_special_kineticenhancer", Rarity = 2, Description = "Increases damage when health is below 50%" },
            new ResourceItemModel { FriendlyName = "Automatic Fire System", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/firetypes/augmaterial_firetype_automatic", Rarity = 1, Description = "Converts weapon firing mechanism to fully automatic" },
            new ResourceItemModel { FriendlyName = "Burst Fire System", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/firetypes/augmaterial_firetype_burst", Rarity = 1, Description = "Converts weapon firing mechanism to 3-round burst" },
            new ResourceItemModel { FriendlyName = "Single Fire System", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/firetypes/augmaterial_firetype_single", Rarity = 1, Description = "Converts weapon firing mechanism to semi-automatic single fire" },
            new ResourceItemModel { FriendlyName = "Double Mod Extension", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/modslots/augmaterial_modslots_2", Rarity = 3, Description = "+2 Weapon Mod Slots on crafted weapon" },
            new ResourceItemModel { FriendlyName = "Mod Slot Extension", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/modslots/augmaterial_modslots_1", Rarity = 2, Description = "+1 Weapon Mod Slot on crafted weapon" },
            new ResourceItemModel { FriendlyName = "Cryo Condenser", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_coldgenerator", Rarity = 2, Description = "Freezes enemies on melee strike or kill" },
            new ResourceItemModel { FriendlyName = "Electrical Conduits", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_electricalconduits", Rarity = 2, Description = "Converts weapon fire into a continuous lightning arc" },
            new ResourceItemModel { FriendlyName = "Heat Inducer", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_heatgenerator", Rarity = 2, Description = "Burns enemies with incendiary damage on hit" },
            new ResourceItemModel { FriendlyName = "Shield Disruptor", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_electricalgenerator", Rarity = 2, Description = "+25% Bonus electrical damage against enemy shields" },
            new ResourceItemModel { FriendlyName = "Power Booster", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_powerbooster", Rarity = 2, Description = "+25% Power Damage while weapon clip is completely full" },
            new ResourceItemModel { FriendlyName = "Shield Sensors", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_shieldsensors", Rarity = 2, Description = "Boosts damage resistance when shields drop" },
            new ResourceItemModel { FriendlyName = "Battlefield Assist Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_statussensors", Rarity = 2, Description = "Boosts shield capacity by 10% when 3+ enemies are close" },
            new ResourceItemModel { FriendlyName = "Rebalanced Field Coils", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_timedrecoil", Rarity = 1, Description = "Reduces weapon recoil, climb & accuracy bloom" },
            new ResourceItemModel { FriendlyName = "Aerial Lubricant", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_ariallubricant", Rarity = 1, Description = "+35% Jump jet hover duration in mid-air" },
            new ResourceItemModel { FriendlyName = "Aerial Performance Optimizer", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_arialprojectileenhancer", Rarity = 1, Description = "+10% Weapon & power damage while hovering" },
            new ResourceItemModel { FriendlyName = "Aerial Stabilizer", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/special/augmaterial_special_arialstabilizer", Rarity = 1, Description = "+35 Damage Resistance while hovering in combat" },
            new ResourceItemModel { FriendlyName = "Kinetic Coil", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_damage", Rarity = 2, Description = "+3% Weapon / Power Damage (Stackable)" },
            new ResourceItemModel { FriendlyName = "Biotic Recharge Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_cooldownreductionbiotic", Rarity = 2, Description = "+5% Biotic power recharge speed cooldown reduction" },
            new ResourceItemModel { FriendlyName = "Combat Recharge Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_cooldownreductioncombat", Rarity = 2, Description = "+5% Combat power recharge speed cooldown reduction" },
            new ResourceItemModel { FriendlyName = "Tech Recharge Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_cooldownreductiontech", Rarity = 2, Description = "+5% Tech power recharge speed cooldown reduction" },
            new ResourceItemModel { FriendlyName = "Biotic Damage Booster", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_damagebiotic", Rarity = 2, Description = "+5% Biotic power damage & force" },
            new ResourceItemModel { FriendlyName = "Combat Power Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_damagecombat", Rarity = 2, Description = "+5% Combat power damage & blast impact" },
            new ResourceItemModel { FriendlyName = "Tech Damage Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_damagetech", Rarity = 2, Description = "+5% Tech power damage & construct damage" },
            new ResourceItemModel { FriendlyName = "Medi-Gel Regulator", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_maxhealth", Rarity = 1, Description = "+5% Maximum Health bonus" },
            new ResourceItemModel { FriendlyName = "Shield Booster", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_maxshields", Rarity = 1, Description = "+5% Maximum Shield points" },
            new ResourceItemModel { FriendlyName = "Anti-Armor Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_powerdamagevsarmor", Rarity = 2, Description = "+10% Power damage bonus against armored foes" },
            new ResourceItemModel { FriendlyName = "Shield Bypass Unit", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_powerdamagevsshields", Rarity = 2, Description = "+10% Power damage bonus against shielded targets" },
            new ResourceItemModel { FriendlyName = "Newtonian Multiplier", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_powerforce", Rarity = 1, Description = "+10% Physics impulse force & enemy stagger" },
            new ResourceItemModel { FriendlyName = "Support Power Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_powersupporteffectiveness", Rarity = 1, Description = "+10% Power duration and buff radius" },
            new ResourceItemModel { FriendlyName = "Shield Accelerator", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_shielddelayreduction", Rarity = 1, Description = "-10% Shield regeneration delay before recharging" },
            new ResourceItemModel { FriendlyName = "Shield Generator", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_shieldsregen", Rarity = 1, Description = "+5% Shield regeneration recovery speed" },
            new ResourceItemModel { FriendlyName = "Duration Boost Module", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_statuseffectduration", Rarity = 1, Description = "+10% Status effect prime duration on targets" },
            new ResourceItemModel { FriendlyName = "Reaction Optimizer", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_combodetonationdamage", Rarity = 2, Description = "+10% Combo detonation explosion damage" },
            new ResourceItemModel { FriendlyName = "Reaction Displacer", Category = "Augmentations", PartitionName = "game/items/research/materials/augmentation/stats/augmaterial_stat_combodetonationradius", Rarity = 2, Description = "+10% Combo detonation explosion blast radius" }
        };
        #endregion

        public bool LoadSave(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    OnLog?.Invoke($"Save file not found: {filePath}");
                    return false;
                }

                // NO backup is created on open. Backup occurs ONLY when the user clicks 'Save'.
                OriginalLivePath = filePath;
                CurrentFilePath = filePath;
                WorkingFilePath = filePath;

                using (var stream = File.OpenRead(filePath))
                {
                    CurrentSave = SaveFile.Read(stream);
                }

                PopulateSaveData(filePath);

                OnLog?.Invoke($"Loaded: {Path.GetFileName(filePath)}");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error loading save: {ex.Message}");
                return false;
            }
        }

        public bool LoadBackupForRestore(string backupFilePath, string targetLivePath)
        {
            if (!File.Exists(backupFilePath)) return false;

            try
            {
                OriginalLivePath = targetLivePath;
                WorkingFilePath = backupFilePath;
                CurrentFilePath = backupFilePath;

                using (var stream = File.OpenRead(backupFilePath))
                {
                    CurrentSave = SaveFile.Read(stream);
                }

                PopulateSaveData(targetLivePath ?? backupFilePath);

                OnLog?.Invoke($"Restored values from '{Path.GetFileName(backupFilePath)}' ready to save to '{Path.GetFileName(targetLivePath)}'");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error restoring backup: {ex.Message}");
                return false;
            }
        }

        private void PopulateSaveData(string contextPath)
        {
            PartyAgent = CurrentSave.Data.Agents.Values.OfType<PartyManagerAgent>().FirstOrDefault();
            PlotAgent = CurrentSave.Data.Agents.Values.OfType<PlotAgent>().FirstOrDefault();

            if (PartyAgent != null && PartyAgent.Squad.MemberRawData.Count > 0)
            {
                var raw = PartyAgent.Squad.MemberRawData[0];
                if (raw.DataBytes != null && raw.DataBytes.Length > 0)
                {
                    var reader = new BitReader(raw.DataBytes);
                    RyderData = new PartyMember(false, false);
                    RyderData.Read(reader);
                }
            }

            ParseMetaHeader(contextPath);

            // Read Profile Training & Level/XP
            if (RyderData?.Progression != null)
            {
                int pid = RyderData.Progression.CurrentProfileId;
                HeaderInfo.ProfileId = (pid >= 0 && pid <= 6) ? pid : 6;
                // If MetaHeader didn't provide a valid level, use RyderData.Progression.Level + 1 (0-indexed in Frostbite)
                if (HeaderInfo.Level <= 1 && RyderData.Progression.Level > 0)
                {
                    HeaderInfo.Level = RyderData.Progression.Level + 1;
                }
                else if (HeaderInfo.Level <= 1 && PartyAgent?.Squad?.MemberSnapshots?.Count > 0 && PartyAgent.Squad.MemberSnapshots[0].Level > 0)
                {
                    HeaderInfo.Level = PartyAgent.Squad.MemberSnapshots[0].Level + 1;
                }
                HeaderInfo.CurrentXp = CalculateCurrentXp(HeaderInfo.Level, RyderData.Progression.Unknown1);
            }
            else if (HeaderInfo.Level <= 1 && PartyAgent?.Squad?.MemberSnapshots?.Count > 0 && PartyAgent.Squad.MemberSnapshots[0].Level > 0)
            {
                HeaderInfo.Level = PartyAgent.Squad.MemberSnapshots[0].Level + 1;
            }

            HeaderInfo.Level = Math.Max(1, HeaderInfo.Level);

            // Read Skill Points
            if (PartyAgent?.Squad?.MemberSnapshots?.Count > 0)
            {
                HeaderInfo.SkillPoints = PartyAgent.Squad.MemberSnapshots[0].SkillPointCount;
            }

            // Read Item Limit
            HeaderInfo.ItemLimit = GetCurrentItemLimit();

            // Read AVP and Nexus Level
            var avp = GetAvp();
            HeaderInfo.AvpPoints = avp;
            HeaderInfo.NexusLevel = GetNexusLevel();

            // Read Credits & Research Points
            HeaderInfo.Credits = GetCurrencyQuantity("stackables/credit");
            HeaderInfo.MilkyWayRD = GetCurrencyQuantity("milkywayrdipoints");
            HeaderInfo.HeleusRD = GetCurrencyQuantity("alienrdipoints");
            HeaderInfo.RemnantRD = GetCurrencyQuantity("remnantrdipoints");

            // Read Difficulty & Gender from ProfOpts in the original live directory
            var saveDir = !string.IsNullOrEmpty(contextPath) ? Path.GetDirectoryName(contextPath) : DefaultSaveDirectory;
            if (ProfOptsHelper.TryRead(saveDir, out bool isFemale, out int diff))
            {
                HeaderInfo.IsFemale = isFemale;
                HeaderInfo.DifficultyIndex = diff;
            }
            else
            {
                HeaderInfo.IsFemale = HeaderInfo.CharacterName.IndexOf("Sara", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Read Identity Code
            HeaderInfo.IdentityCode = GetIdentityCode();

            // Read Profile Ranks
            LoadProfileRanks();

            // Read Resources (Minerals, Tech, Bio)
            LoadResources();

            // Read Armory (Weapons, Mods, Armor, Fusion Mods, Consumables)
            LoadArmory();
        }

        private void ParseMetaHeader(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName.Contains("-"))
            {
                var parts = fileName.Split('-');
                var hexPart = parts[0].Replace("Career", "");
                if (uint.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out uint decId))
                {
                    HeaderInfo.CareerId = decId.ToString();
                }
                else
                {
                    HeaderInfo.CareerId = hexPart;
                }
            }
            else
            {
                HeaderInfo.CareerId = "Custom";
            }

            if (CurrentSave?.MetaHeader?.Values != null)
            {
                foreach (var item in CurrentSave.MetaHeader.Values)
                {
                    switch (item.Key)
                    {
                        case 313101859:
                            if (!string.IsNullOrWhiteSpace(item.Value?.ToString()))
                            {
                                HeaderInfo.CareerId = item.Value.ToString();
                            }
                            break;
                        case 1665193641:
                        case 1382092858:
                            var rawName = item.Value?.ToString() ?? "Scott Ryder";
                            HeaderInfo.CharacterName = CleanCharacterName(rawName);
                            break;
                        case 3074424332:
                            var loc = item.Value?.ToString() ?? "";
                            HeaderInfo.Location = CleanLocationName(loc);
                            break;
                        case 1464675614:
                            if (string.IsNullOrEmpty(HeaderInfo.Location) || HeaderInfo.Location == "Heleus Cluster" || HeaderInfo.Location.StartsWith("Heleus Cluster - UNC_"))
                            {
                                var mapLoc = CleanLocationName(item.Value?.ToString() ?? "");
                                if (!mapLoc.StartsWith("Heleus Cluster - UNC_") && mapLoc != "Heleus Cluster")
                                {
                                    HeaderInfo.Location = mapLoc;
                                }
                            }
                            break;
                        case 4113798944:
                            if (int.TryParse(item.Value?.ToString(), out int lvl) && lvl > 0)
                                HeaderInfo.Level = lvl;
                            break;
                        case 820093014:
                            if (double.TryParse(item.Value?.ToString(), out double pt))
                                HeaderInfo.PlayTimeSeconds = pt;
                            break;
                        case 1049366822:
                            if (bool.TryParse(item.Value?.ToString(), out bool isFem))
                                HeaderInfo.IsFemale = isFem;
                            break;
                    }
                }
            }
        }

        private void LoadProfileRanks()
        {
            ProfileRanks = new List<ProfileRankModel>();
            string[] names = { "Soldier", "Engineer", "Adept", "Sentinel", "Vanguard", "Infiltrator", "Explorer" };

            if (RyderData?.Progression?.Profiles != null)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    int rank = 1;
                    if (i < RyderData.Progression.Profiles.Count)
                    {
                        // In Frostbite MEA save data, profile Level is 0-indexed:
                        // -1 = locked (prologue), 0 = Rank 1, 1 = Rank 2, 2 = Rank 3, ..., 5 = Rank 6.
                        int rawLevel = RyderData.Progression.Profiles[i].Level;
                        rank = Math.Max(1, Math.Min(6, rawLevel + 1));
                    }
                    ProfileRanks.Add(new ProfileRankModel
                    {
                        Index = i,
                        Name = names[i],
                        Rank = rank
                    });
                }
            }
            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    ProfileRanks.Add(new ProfileRankModel { Index = i, Name = names[i], Rank = 1 });
                }
            }
        }

        private void LoadResources()
        {
            Minerals = KnownMinerals.Select(m => new ResourceItemModel
            {
                FriendlyName = m.FriendlyName,
                Category = m.Category,
                PartitionName = m.PartitionName,
                Rarity = m.Rarity,
                Description = m.Description,
                Quantity = 0,
                ExistingRawId = null
            }).ToList();

            TechMaterials = KnownTechMaterials.Select(m => new ResourceItemModel
            {
                FriendlyName = m.FriendlyName,
                Category = m.Category,
                PartitionName = m.PartitionName,
                Rarity = m.Rarity,
                Description = m.Description,
                Quantity = 0,
                ExistingRawId = null
            }).ToList();

            BioMaterials = KnownBioMaterials.Select(m => new ResourceItemModel
            {
                FriendlyName = m.FriendlyName,
                Category = m.Category,
                PartitionName = m.PartitionName,
                Rarity = m.Rarity,
                Description = m.Description,
                Quantity = 0,
                ExistingRawId = null
            }).ToList();

            SpecialItems = KnownSpecialItems.Select(m => new ResourceItemModel
            {
                FriendlyName = m.FriendlyName,
                Category = m.Category,
                PartitionName = m.PartitionName,
                Rarity = m.Rarity,
                Description = m.Description,
                Quantity = 0,
                ExistingRawId = null
            }).ToList();

            Salvage = KnownSalvage.Select(m => new ResourceItemModel
            {
                FriendlyName = m.FriendlyName,
                Category = m.Category,
                PartitionName = m.PartitionName,
                Rarity = m.Rarity,
                Description = m.Description,
                Quantity = 0,
                ExistingRawId = null
            }).ToList();

            Augmentations = KnownAugmentations.Select(m => new ResourceItemModel
            {
                FriendlyName = m.FriendlyName,
                Category = m.Category,
                PartitionName = m.PartitionName,
                Rarity = m.Rarity,
                Description = m.Description,
                Quantity = 0,
                ExistingRawId = null
            }).ToList();

            if (RyderData?.Inventory?.Items == null) return;

            var allRes = Minerals
                .Concat(TechMaterials)
                .Concat(BioMaterials)
                .Concat(SpecialItems)
                .Concat(Salvage)
                .Concat(Augmentations)
                .ToDictionary(r => r.PartitionName, r => r, StringComparer.OrdinalIgnoreCase);

            foreach (var raw in RyderData.Inventory.Items)
            {
                try
                {
                    var reader = new BitReader(raw.DataBytes);
                    reader.ReadUInt32();
                    var partName = reader.ReadString();

                    if (allRes.TryGetValue(partName, out var model))
                    {
                        model.Quantity = reader.ReadInt32();
                        model.ExistingRawId = raw.Id;
                    }
                    else if (partName.IndexOf("remdukoskip", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var keyItem = SpecialItems.FirstOrDefault();
                        if (keyItem != null)
                        {
                            keyItem.Quantity = reader.ReadInt32();
                            keyItem.ExistingRawId = raw.Id;
                        }
                    }
                }
                catch { }
            }
        }

        public bool Save()
        {
            if (CurrentSave == null || string.IsNullOrEmpty(WorkingFilePath)) return false;

            try
            {
                // 1. Ensure backup directory exists
                if (!Directory.Exists(BackupDirectory))
                {
                    Directory.CreateDirectory(BackupDirectory);
                }

                // 2. Create safety backup first in %USERPROFILE%\Documents\ME Andromeda Save Editor Backup
                string backupSource = File.Exists(OriginalLivePath) ? OriginalLivePath : WorkingFilePath;
                if (!string.IsNullOrEmpty(backupSource) && File.Exists(backupSource))
                {
                    CreateBackup(backupSource);
                }

                // 3. Commit all data back into save structures
                CommitAllData();

                // 4. Save directly to the live game save file
                if (!string.IsNullOrEmpty(OriginalLivePath))
                {
                    using (var outStream = File.Create(OriginalLivePath))
                    {
                        CurrentSave.Serialize(outStream);
                    }

                    // Write ProfOpts in live folder (Difficulty, Name, and Gender)
                    var liveDir = Path.GetDirectoryName(OriginalLivePath);
                    ProfOptsHelper.TryWrite(liveDir, HeaderInfo.CareerId, HeaderInfo.CharacterName, HeaderInfo.IsFemale, HeaderInfo.DifficultyIndex);
                }

                // 5. Also keep working copy updated
                if (!string.IsNullOrEmpty(WorkingFilePath) &&
                    !string.Equals(Path.GetFullPath(WorkingFilePath), Path.GetFullPath(OriginalLivePath ?? ""), StringComparison.OrdinalIgnoreCase))
                {
                    using (var outStream = File.Create(WorkingFilePath))
                    {
                        CurrentSave.Serialize(outStream);
                    }
                }

                OnLog?.Invoke($"Saved to game save: {Path.GetFileName(OriginalLivePath ?? WorkingFilePath)}");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Save failed: {ex.Message}");
                return false;
            }
        }

        private void CreateBackup(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath)) return;

            if (!Directory.Exists(BackupDirectory))
            {
                Directory.CreateDirectory(BackupDirectory);
            }

            var saveFileName = Path.GetFileName(sourceFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{saveFileName}_{timestamp}.bak";
            var backupFullPath = Path.Combine(BackupDirectory, backupFileName);

            File.Copy(sourceFilePath, backupFullPath, true);
            LastBackupPath = backupFullPath;
            OnLog?.Invoke($"Backup created: {backupFileName}");
        }

        public List<BackupItemModel> GetBackups()
        {
            var list = new List<BackupItemModel>();
            if (!Directory.Exists(BackupDirectory)) return list;

            var files = Directory.GetFiles(BackupDirectory, "*.bak");
            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                var fn = fi.Name;
                var origName = fn;
                var lastUnderscore = fn.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    origName = fn.Substring(0, lastUnderscore);
                }

                list.Add(new BackupItemModel
                {
                    FileName = fn,
                    FullPath = f,
                    OriginalSaveName = origName,
                    Timestamp = fi.LastWriteTime,
                    FileSizeBytes = fi.Length
                });
            }

            return list.OrderByDescending(b => b.Timestamp).ToList();
        }

        public List<LiveSaveItemModel> GetLiveSaves()
        {
            var list = new List<LiveSaveItemModel>();
            if (!Directory.Exists(DefaultSaveDirectory)) return list;

            var files = Directory.GetFiles(DefaultSaveDirectory, "*Save*")
                .Where(f => !f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                list.Add(new LiveSaveItemModel
                {
                    FileName = fi.Name,
                    FullPath = f,
                    LastModified = fi.LastWriteTime
                });
            }

            return list.OrderByDescending(s => s.LastModified).ToList();
        }

        public bool RestoreBackup(string backupFilePath, string activeSavePath)
        {
            if (!File.Exists(backupFilePath)) return false;
            if (string.IsNullOrEmpty(activeSavePath)) return false;

            try
            {
                File.Copy(backupFilePath, activeSavePath, true);
                return LoadSave(activeSavePath);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Restore failed: {ex.Message}");
                return false;
            }
        }

        private void CommitAllData()
        {
            // Character Name & Level in MetaHeader
            if (CurrentSave?.MetaHeader?.Values != null)
            {
                bool hasLevel = false;
                bool hasName1 = false;
                bool hasName2 = false;
                for (int i = 0; i < CurrentSave.MetaHeader.Values.Count; i++)
                {
                    var kv = CurrentSave.MetaHeader.Values[i];
                    if (kv.Key == 1665193641)
                    {
                        hasName1 = true;
                        if (!string.IsNullOrWhiteSpace(HeaderInfo.CharacterName))
                        {
                            CurrentSave.MetaHeader.Values[i] = new KeyValuePair<uint, string>(kv.Key, HeaderInfo.CharacterName);
                        }
                    }
                    else if (kv.Key == 1382092858)
                    {
                        hasName2 = true;
                        if (!string.IsNullOrWhiteSpace(HeaderInfo.CharacterName))
                        {
                            CurrentSave.MetaHeader.Values[i] = new KeyValuePair<uint, string>(kv.Key, HeaderInfo.CharacterName);
                        }
                    }
                    else if (kv.Key == 4113798944)
                    {
                        CurrentSave.MetaHeader.Values[i] = new KeyValuePair<uint, string>(kv.Key, HeaderInfo.Level.ToString());
                        hasLevel = true;
                    }
                }
                if (!hasName1 && !string.IsNullOrWhiteSpace(HeaderInfo.CharacterName))
                {
                    CurrentSave.MetaHeader.Values.Add(new KeyValuePair<uint, string>(1665193641, HeaderInfo.CharacterName));
                }
                if (!hasName2 && !string.IsNullOrWhiteSpace(HeaderInfo.CharacterName))
                {
                    CurrentSave.MetaHeader.Values.Add(new KeyValuePair<uint, string>(1382092858, HeaderInfo.CharacterName));
                }
                if (!hasLevel)
                {
                    CurrentSave.MetaHeader.Values.Add(new KeyValuePair<uint, string>(4113798944, HeaderInfo.Level.ToString()));
                }
            }

            // Profile Training ID
            if (RyderData?.Progression != null)
            {
                RyderData.Progression.CurrentProfileId = HeaderInfo.ProfileId;
            }

            // Ryder Level and XP (0-indexed in Frostbite)
            SetRyderXpAndLevel(HeaderInfo.Level, HeaderInfo.CurrentXp);

            // Skill Points & Snapshot Level
            if (PartyAgent?.Squad?.MemberSnapshots?.Count > 0)
            {
                PartyAgent.Squad.MemberSnapshots[0].SkillPointCount = HeaderInfo.SkillPoints;
                PartyAgent.Squad.MemberSnapshots[0].Level = Math.Max(0, HeaderInfo.Level - 1);
            }

            // Item Limit
            SetCurrentItemLimit(HeaderInfo.ItemLimit);

            // Nexus Level & AVP
            SetNexusLevel(HeaderInfo.NexusLevel);
            SetAvp(HeaderInfo.AvpPoints);

            // Credits
            SetCurrencyQuantity("stackables/credit", HeaderInfo.Credits);
            SetCurrencyQuantity("milkywayrdipoints", HeaderInfo.MilkyWayRD);
            SetCurrencyQuantity("alienrdipoints", HeaderInfo.HeleusRD);
            SetCurrencyQuantity("remnantrdipoints", HeaderInfo.RemnantRD);

            // Profile Ranks
            if (RyderData?.Progression?.Profiles != null)
            {
                for (int i = 0; i < Math.Min(ProfileRanks.Count, RyderData.Progression.Profiles.Count); i++)
                {
                    // Convert 1-based UI Rank (1..6) back to 0-based save Level (0..5)
                    RyderData.Progression.Profiles[i].Level = Math.Max(0, Math.Min(5, ProfileRanks[i].Rank - 1));
                }
            }

            // Resources
            CommitResources(Minerals);
            CommitResources(TechMaterials);
            CommitResources(BioMaterials);
            CommitResources(SpecialItems);
            CommitResources(Salvage);
            CommitResources(Augmentations);

            // Armory
            CommitArmory();

            // Write RyderData back into PartyAgent
            if (PartyAgent != null && PartyAgent.Squad.MemberRawData.Count > 0 && RyderData != null)
            {
                var writer = new BitWriter((RyderData.Inventory?.Items?.Count ?? 100) * 256 + 0x10000);
                RyderData.Write(writer);
                var oldRaw = PartyAgent.Squad.MemberRawData[0];
                var newRaw = new PartyMemberRawData
                {
                    Unknown1 = oldRaw.Unknown1,
                    Unknown2 = oldRaw.Unknown2,
                    DataBytes = writer.GetBytes()
                };
                PartyAgent.Squad.MemberRawData[0] = newRaw;
            }
        }

        private void CommitResources(IEnumerable<ResourceItemModel> resources)
        {
            if (RyderData?.Inventory?.Items == null || resources == null) return;

            foreach (var res in resources)
            {
                if (res.ExistingRawId.HasValue)
                {
                    int idx = RyderData.Inventory.Items.FindIndex(r => r.Id == res.ExistingRawId.Value);
                    if (idx >= 0)
                    {
                        var raw = RyderData.Inventory.Items[idx];
                        try
                        {
                            var reader = new BitReader(raw.DataBytes);
                            var unk1 = reader.ReadUInt32();
                            var partName = reader.ReadString();
                            var oldQty = reader.ReadInt32();
                            var isNew = reader.ReadBoolean();
                            var rarity = reader.ReadInt32();

                            if (oldQty != res.Quantity)
                            {
                                var writer = new BitWriter(raw.DataBytes.Length + 16);
                                writer.WriteUInt32(unk1);
                                writer.WriteString(partName);
                                writer.WriteInt32(res.Quantity);
                                writer.WriteBoolean(isNew);
                                writer.WriteInt32(rarity);

                                var newBytes = writer.GetBytes();
                                RyderData.Inventory.Items[idx] = new InventoryComponent.RawItemData
                                {
                                    Id = raw.Id,
                                    DataLength = newBytes.Length * 8,
                                    DataBytes = newBytes
                                };
                            }
                        }
                        catch { }
                    }
                }
                else if (res.Quantity > 0 && PartyAgent != null)
                {
                    uint newId = ++PartyAgent.NextItemId;
                    var writer = new BitWriter(256);
                    writer.WriteUInt32(1);
                    writer.WriteString(res.PartitionName);
                    writer.WriteInt32(res.Quantity);
                    writer.WriteBoolean(false);
                    writer.WriteInt32(res.Rarity);

                    var newBytes = writer.GetBytes();
                    RyderData.Inventory.Items.Add(new InventoryComponent.RawItemData
                    {
                        Id = newId,
                        DataLength = newBytes.Length * 8,
                        DataBytes = newBytes
                    });
                    res.ExistingRawId = newId;
                }
            }
        }

        public int GetCurrencyQuantity(string partitionMatch)
        {
            if (RyderData?.Inventory?.Items == null) return 0;
            foreach (var item in RyderData.Inventory.Items)
            {
                try
                {
                    var reader = new BitReader(item.DataBytes);
                    reader.ReadUInt32();
                    var partName = reader.ReadString();
                    if (partName.IndexOf(partitionMatch, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return reader.ReadInt32();
                    }
                }
                catch { }
            }
            return 0;
        }

        public bool SetCurrencyQuantity(string partitionMatch, int newQuantity)
        {
            if (RyderData?.Inventory?.Items == null) return false;

            for (int i = 0; i < RyderData.Inventory.Items.Count; i++)
            {
                var item = RyderData.Inventory.Items[i];
                try
                {
                    var reader = new BitReader(item.DataBytes);
                    var unk1 = reader.ReadUInt32();
                    var partName = reader.ReadString();
                    if (partName.IndexOf(partitionMatch, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        reader.ReadInt32();
                        var isNew = reader.ReadBoolean();
                        var rarity = reader.ReadInt32();

                        var writer = new BitWriter(item.DataBytes.Length + 64);
                        writer.WriteUInt32(unk1);
                        writer.WriteString(partName);
                        writer.WriteInt32(newQuantity);
                        writer.WriteBoolean(isNew);
                        writer.WriteInt32(rarity);

                        var newBytes = writer.GetBytes();
                        RyderData.Inventory.Items[i] = new InventoryComponent.RawItemData
                        {
                            Id = item.Id,
                            DataLength = newBytes.Length * 8,
                            DataBytes = newBytes
                        };
                        return true;
                    }
                }
                catch { }
            }

            // If item didn't exist in inventory yet, create it!
            if (newQuantity > 0 && PartyAgent != null)
            {
                string fullPart = partitionMatch;
                if (partitionMatch.IndexOf("credit", StringComparison.OrdinalIgnoreCase) >= 0)
                    fullPart = "Game/Items/Stackables/Credit";
                else if (partitionMatch.IndexOf("milkyway", StringComparison.OrdinalIgnoreCase) >= 0)
                    fullPart = "game/items/stackables/milkywayrdipoints";
                else if (partitionMatch.IndexOf("alien", StringComparison.OrdinalIgnoreCase) >= 0)
                    fullPart = "game/items/stackables/alienrdipoints";
                else if (partitionMatch.IndexOf("remnant", StringComparison.OrdinalIgnoreCase) >= 0)
                    fullPart = "game/items/stackables/remnantrdipoints";

                uint newId = ++PartyAgent.NextItemId;
                var writer = new BitWriter(256);
                writer.WriteUInt32(1);
                writer.WriteString(fullPart);
                writer.WriteInt32(newQuantity);
                writer.WriteBoolean(false);
                writer.WriteInt32(-1);

                var newBytes = writer.GetBytes();
                RyderData.Inventory.Items.Add(new InventoryComponent.RawItemData
                {
                    Id = newId,
                    DataLength = newBytes.Length * 8,
                    DataBytes = newBytes
                });
                return true;
            }

            return false;
        }

        public int GetCurrentItemLimit()
        {
            bool cap1 = GetPlotBool(Perk_TradeCapacity1) ?? false;
            bool cap2 = GetPlotBool(Perk_TradeCapacity2) ?? false;
            // Mass Effect Andromeda (Patch 1.05+): Base = 100, Trade Capacity I = 150, Trade Capacity II = 200
            if (cap1 && cap2) return 200;
            if (cap1 || cap2) return 150;
            return 100;
        }

        public void SetCurrentItemLimit(int limit)
        {
            HeaderInfo.ItemLimit = limit;
            // Mass Effect Andromeda (Patch 1.05+):
            // >= 151: unlocks both Trade Capacity I & II (200 capacity)
            // >= 101: unlocks Trade Capacity I (150 capacity)
            // <= 100: default baseline (100 capacity)
            if (limit >= 151)
            {
                SetPlotBool(Perk_TradeCapacity1, true);
                SetPlotBool(Perk_TradeCapacity2, true);
            }
            else if (limit >= 101)
            {
                SetPlotBool(Perk_TradeCapacity1, true);
                SetPlotBool(Perk_TradeCapacity2, false);
            }
            else
            {
                SetPlotBool(Perk_TradeCapacity1, false);
                SetPlotBool(Perk_TradeCapacity2, false);
            }
        }

        public int GetAvp()
        {
            return GetPlotInt(AVP_IntGuid) ?? 0;
        }

        public void SetAvp(int avp)
        {
            HeaderInfo.AvpPoints = Math.Max(0, avp);
            SetPlotInt(AVP_IntGuid, HeaderInfo.AvpPoints);
        }

        public int GetRemnantKeys()
        {
            var key = SpecialItems.FirstOrDefault();
            return key != null ? key.Quantity : GetCurrencyQuantity("remdukoskip");
        }

        public void SetRemnantKeys(int count)
        {
            var key = SpecialItems.FirstOrDefault();
            if (key != null) key.Quantity = Math.Max(0, count);
        }

        public int GetNexusLevel()
        {
            var avp = GetAvp();
            return Math.Max(1, Math.Min(29, 1 + (avp / 800)));
        }

        public void SetNexusLevel(int level)
        {
            level = Math.Max(1, Math.Min(29, level));
            HeaderInfo.NexusLevel = level;
            SetAvp((level - 1) * 800);
        }

        public int? GetPlotInt(Guid guid)
        {
            if (PlotAgent?.Plot?.Ints != null && PlotAgent.Plot.Ints.TryGetValue(guid, out int val))
                return val;
            return null;
        }

        public void SetPlotInt(Guid guid, int val)
        {
            if (PlotAgent?.Plot?.Ints != null)
                PlotAgent.Plot.Ints[guid] = val;
        }

        public bool? GetPlotBool(Guid guid)
        {
            if (PlotAgent?.Plot?.Bools != null && PlotAgent.Plot.Bools.TryGetValue(guid, out bool val))
                return val;
            return null;
        }

        public void SetPlotBool(Guid guid, bool val)
        {
            if (PlotAgent?.Plot?.Bools != null)
                PlotAgent.Plot.Bools[guid] = val;
        }

        public float? GetPlotFloat(Guid guid)
        {
            if (PlotAgent?.Plot?.Floats != null && PlotAgent.Plot.Floats.TryGetValue(guid, out float val))
                return val;
            return null;
        }

        public void SetPlotFloat(Guid guid, float val)
        {
            if (PlotAgent?.Plot?.Floats != null)
                PlotAgent.Plot.Floats[guid] = val;
        }

        public string GetIdentityCode()
        {
            try
            {
                var ca = CurrentSave?.Data?.GetComponentContainerAgent<CustomizationAgent>();
                if (ca != null && ca.ComponentContainers.TryGetValue(1, out var container))
                {
                    uint headId = 0;
                    uint headTexId = 0;
                    uint hairIndex = 0;
                    int sliderCount = 0;

                    foreach (var comp in container.Components)
                    {
                        if (comp is CustomizedHeadMorphParameter hmp)
                        {
                            headTexId = hmp.HeadTextureId;
                            sliderCount = hmp.SliderValues?.Count ?? 0;
                        }
                        else if (comp is CustomizedHairParameter hp)
                        {
                            headId = hp.HeadId;
                            hairIndex = hp.HairstyleIndex;
                        }
                    }

                    if (headId != 0 || headTexId != 0)
                    {
                        return $"7B/H{headId}/T{headTexId}/Hair{hairIndex}/SL{sliderCount}";
                    }
                }
            }
            catch { }

            return "7B/WAA/11.111/111";
        }

        public static string CleanCharacterName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Scott Ryder";
            var idx = raw.IndexOf(" (");
            if (idx > 0) return raw.Substring(0, idx).Trim();
            return raw.Trim();
        }

        public static int CalculateCurrentXp(int level, float unknown1)
        {
            int baseLevelXp = GetBaseXpForLevel(level);
            int nextLevelXp = GetXpForNextLevel(level);
            int range = nextLevelXp - baseLevelXp;
            if (range <= 0) return baseLevelXp;
            return baseLevelXp + (int)Math.Round(range * Math.Max(0.0f, Math.Min(1.0f, unknown1)));
        }

        public static int GetBaseXpForLevel(int level)
        {
            if (level <= 1) return 0;
            double xp = 0;
            for (int l = 1; l < level; l++)
            {
                xp += 1000 * Math.Pow(1.15, l - 1);
            }
            return (int)Math.Round(xp);
        }

        public static int GetXpForNextLevel(int level) => GetBaseXpForLevel(level + 1);

        public static int GetLevelForXp(int xp)
        {
            if (xp <= 0) return 1;
            int lvl = 1;
            while (lvl < 132 && GetBaseXpForLevel(lvl + 1) <= xp)
            {
                lvl++;
            }
            return lvl;
        }

        public void SetRyderXpAndLevel(int level, int currentXp)
        {
            if (RyderData?.Progression == null) return;
            level = Math.Max(1, Math.Min(132, level));
            // In Frostbite bit reader, RyderData.Progression.Level is 0-indexed (0 = Level 1)
            RyderData.Progression.Level = Math.Max(0, level - 1);

            int baseXp = GetBaseXpForLevel(level);
            int nextXp = GetXpForNextLevel(level);
            int diff = nextXp - baseXp;

            float unknown1 = 0f;
            if (diff > 0)
            {
                unknown1 = (float)Math.Max(0.0, Math.Min(1.0, (double)(currentXp - baseXp) / diff));
            }
            RyderData.Progression.Unknown1 = unknown1;
        }

        private string CleanLocationName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Heleus Cluster";
            string s = raw.ToLowerInvariant();

            // Tempest & Nexus
            if (s.Contains("tempest")) return "Tempest";
            if (s.Contains("nexus") || s.Contains("hyperion")) return "Nexus";

            // Landed on Planet: Heleus Cluster - System - Planet
            if (s.Contains("aya")) return "Heleus Cluster - Onaon - Aya";
            if (s.Contains("eos") || s.Contains("desert")) return "Heleus Cluster - Pytheas - Eos";
            if (s.Contains("havarl") || s.Contains("jungle")) return "Heleus Cluster - Faroang - Havarl";
            if (s.Contains("voeld") || s.Contains("ice") || s.Contains("fenhajan") || s.Contains("fen_hajan") || s.Contains("hajan")) return "Heleus Cluster - Nol - Voeld";
            if (s.Contains("kadara") || s.Contains("settlement")) return "Heleus Cluster - Govorkam - Kadara";
            if (s.Contains("elaaden") || s.Contains("wasteland")) return "Heleus Cluster - Zaubray - Elaaden";
            if (s.Contains("meridian")) return "Heleus Cluster - Meridian - Meridian";
            if (s.Contains("habitat") || s.Contains("hab7") || s.Contains("storm")) return "Heleus Cluster - Habitat 7 - Habitat 7";
            if (s.Contains("h-047c") || s.Contains("remav") || s.Contains("lowgrav")) return "Heleus Cluster - Remav - H-047c";
            if (s.Contains("archon") || s.Contains("flagship") || s.Contains("kett_ship")) return "Heleus Cluster - Kett Flagship - Flagship";
            if (s.Contains("khitasira") || s.Contains("khi_tasira") || s.Contains("civki")) return "Heleus Cluster - Civki - Khi Tasira";

            // Exploring Heleus System: Heleus Cluster - System
            if (s.Contains("onaon")) return "Heleus Cluster - Onaon";
            if (s.Contains("pytheas")) return "Heleus Cluster - Pytheas";
            if (s.Contains("faroang")) return "Heleus Cluster - Faroang";
            if (s.Contains("nol")) return "Heleus Cluster - Nol";
            if (s.Contains("govorkam")) return "Heleus Cluster - Govorkam";
            if (s.Contains("zaubray")) return "Heleus Cluster - Zaubray";
            if (s.Contains("remav")) return "Heleus Cluster - Remav";
            if (s.Contains("eriksson")) return "Heleus Cluster - Eriksson";
            if (s.Contains("sabris")) return "Heleus Cluster - Sabris";

            string clean = raw.Replace("SavedGame_", "").Replace("Game/Levels/", "").Replace("HUB_", "").Replace("UNC_", "").Trim();
            return $"Heleus Cluster - {clean}";
        }

        #region Armory Implementation

        public static void EnsureCatalogLoaded()
        {
            if (FullCatalog.Count > 0) return;

            try
            {
                if (InfoManager.Items != null)
                {
                    foreach (var kv in InfoManager.Items.Items)
                    {
                        var def = kv.Value;
                        if (def == null || string.IsNullOrWhiteSpace(def.Name) || def.IsHidden) continue;

                        string keyLower = kv.Key.ToLowerInvariant();
                        string nameUpper = def.Name.ToUpperInvariant();

                        // 1. Filter out developer debug, placeholders, unused prototypes, cutscene fallbacks, and Krogan melee
                        if (nameUpper.Contains("[BUG]") || nameUpper.Contains("NOT USED") || nameUpper.Contains("DEFAULT WEAPON NAME") || nameUpper == "HEADBUTT")
                            continue;

                        // 2. Filter out AI / NPC / Companion specific weapon assets and enemy beam launchers
                        if (keyLower.Contains("/ai/") || keyLower.Contains("beamlauncher"))
                            continue;

                        // 3. Filter out cinematic cutscene props and marketing trailer assets
                        if (keyLower.Contains("cine_") || keyLower.Contains("cine/") || keyLower.Contains("/marketing/"))
                            continue;

                        // 4. Filter out multiplayer specific gear presets, kits, and evade/color assets
                        if (keyLower.Contains("_mp") || keyLower.Contains("/mp/") || keyLower.Contains("multiplayer") || keyLower.Contains("gear_evade"))
                            continue;
                        if (keyLower.Contains("characters/kits/") && !keyLower.Contains("batariangauntlet"))
                            continue;
                        if (keyLower.Contains("characters/defense/") || keyLower.Contains("characters/offense/") || keyLower.Contains("characters/support/") || keyLower.Contains("characters/shared/"))
                            continue;

                        string cat = null;
                        if (def.Type == ItemType.RangedWeapon || def.Type == ItemType.MeleeWeapon)
                            cat = "Weapons";
                        else if (def.Type == ItemType.WeaponPart)
                            cat = "Weapon Mods";
                        else if (def.Type == ItemType.GearPart && (keyLower.Contains("gearmod") || nameUpper.StartsWith("FUSION MOD")))
                            cat = "Fusion Mods";
                        else if (def.Type == ItemType.Gear || def.Type == ItemType.GearPart)
                            cat = "Armor";
                        else if (def.Type == ItemType.Consumable)
                            cat = "Consumables";

                        if (cat != null)
                        {
                            FullCatalog.Add(new CatalogItemModel
                            {
                                Category = cat,
                                Name = def.Name,
                                PartitionName = kv.Key,
                                Tier = def.Tier,
                                ItemType = def.Type
                            });
                        }
                    }
                }
            }
            catch { }

            // Ensure all 12 fusion mods exist in catalog
            foreach (var fm in FusionModsList)
            {
                if (!FullCatalog.Any(c => c.PartitionName.Equals(fm.Partition, StringComparison.OrdinalIgnoreCase)))
                {
                    FullCatalog.Add(new CatalogItemModel
                    {
                        Category = "Fusion Mods",
                        Name = fm.Name,
                        PartitionName = fm.Partition,
                        Tier = 0,
                        ItemType = ItemType.GearPart
                    });
                }
            }

            // Ensure all core consumables exist in catalog
            foreach (var cc in CoreConsumablesList)
            {
                if (!FullCatalog.Any(c => c.PartitionName.Equals(cc.Partition, StringComparison.OrdinalIgnoreCase)))
                {
                    FullCatalog.Add(new CatalogItemModel
                    {
                        Category = "Consumables",
                        Name = cc.Name,
                        PartitionName = cc.Partition,
                        Tier = 0,
                        ItemType = ItemType.Consumable
                    });
                }
            }

            FullCatalog.Sort((a, b) => string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadArmory()
        {
            ArmoryItems.Clear();
            DeletedArmoryItemIds.Clear();

            if (RyderData?.Inventory?.Items == null) return;

            foreach (var rawItem in RyderData.Inventory.Items)
            {
                try
                {
                    var reader = new BitReader(rawItem.DataBytes);
                    var item = InventoryComponent.ReadItemData(reader, RyderData.Inventory.ReadVersion);
                    if (item?.Definition != null)
                    {
                        string cat = null;
                        if (item.Definition.Type == ItemType.RangedWeapon || item.Definition.Type == ItemType.MeleeWeapon)
                            cat = "Weapons";
                        else if (item.Definition.Type == ItemType.WeaponPart)
                            cat = "Weapon Mods";
                        else if (item.Definition.Type == ItemType.GearPart && (item.PartitionName.IndexOf("gearmod", StringComparison.OrdinalIgnoreCase) >= 0 || (item.Definition.Name != null && item.Definition.Name.StartsWith("FUSION MOD", StringComparison.OrdinalIgnoreCase))))
                            cat = "Fusion Mods";
                        else if (item.Definition.Type == ItemType.Gear || item.Definition.Type == ItemType.GearPart)
                            cat = "Armor";
                        else if (item.Definition.Type == ItemType.Consumable)
                            cat = "Consumables";

                        if (cat != null)
                        {
                            ArmoryItems.Add(new ArmoryItemModel
                            {
                                Id = rawItem.Id,
                                PartitionName = item.PartitionName,
                                DisplayName = !string.IsNullOrWhiteSpace(item.Definition.Name) ? item.Definition.Name : item.PartitionName,
                                Category = cat,
                                Tier = item.Definition.Tier,
                                Quantity = item.Quantity,
                                Rarity = item.Rarity,
                                IsNew = item.IsNew,
                                ItemData = item,
                                IsNewlyAdded = false
                            });
                        }
                    }
                }
                catch { }
            }
        }

        private void CommitArmory()
        {
            if (RyderData?.Inventory?.Items == null) return;

            // Remove deleted items
            if (DeletedArmoryItemIds.Count > 0)
            {
                RyderData.Inventory.Items.RemoveAll(r => DeletedArmoryItemIds.Contains(r.Id));
                DeletedArmoryItemIds.Clear();
            }

            // Update existing and append newly added items
            foreach (var armoryItem in ArmoryItems)
            {
                if (armoryItem.IsNewlyAdded)
                {
                    if (armoryItem.ItemData == null) continue;
                    armoryItem.ItemData.Quantity = armoryItem.Quantity;
                    armoryItem.ItemData.Rarity = armoryItem.Rarity;
                    armoryItem.ItemData.PartitionName = armoryItem.PartitionName;
                    var writer = new BitWriter(512);
                    InventoryComponent.WriteItemData(writer, armoryItem.ItemData);
                    var bytes = writer.GetBytes();
                    RyderData.Inventory.Items.Add(new InventoryComponent.RawItemData
                    {
                        Id = armoryItem.Id,
                        DataLength = bytes.Length * 8,
                        DataBytes = bytes
                    });
                    armoryItem.IsNewlyAdded = false;
                }
                else
                {
                    int idx = RyderData.Inventory.Items.FindIndex(r => r.Id == armoryItem.Id);
                    if (idx >= 0 && armoryItem.ItemData != null)
                    {
                        bool changed = armoryItem.ItemData.Quantity != armoryItem.Quantity ||
                                       armoryItem.ItemData.Rarity != armoryItem.Rarity ||
                                       !string.Equals(armoryItem.ItemData.PartitionName, armoryItem.PartitionName, StringComparison.OrdinalIgnoreCase);

                        if (changed)
                        {
                            armoryItem.ItemData.Quantity = armoryItem.Quantity;
                            armoryItem.ItemData.Rarity = armoryItem.Rarity;
                            armoryItem.ItemData.PartitionName = armoryItem.PartitionName;
                            var raw = RyderData.Inventory.Items[idx];
                            var writer = new BitWriter(raw.DataBytes.Length + 128);
                            InventoryComponent.WriteItemData(writer, armoryItem.ItemData);
                            var bytes = writer.GetBytes();
                            RyderData.Inventory.Items[idx] = new InventoryComponent.RawItemData
                            {
                                Id = raw.Id,
                                DataLength = bytes.Length * 8,
                                DataBytes = bytes
                            };
                        }
                    }
                }
            }
        }

        public static void SetItemDefinition(ItemData item, ItemDefinition def)
        {
            try
            {
                var prop = typeof(ItemData).GetProperty("Definition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                prop?.SetValue(item, def, null);
            }
            catch { }
        }

        public ArmoryItemModel AddItemToArmory(string partitionName, string category, int quantity)
        {
            if (PartyAgent == null || RyderData?.Inventory?.Items == null) return null;

            partitionName = partitionName.Trim().ToLowerInvariant();
            ItemDefinition def = null;
            if (InfoManager.Items != null)
            {
                InfoManager.Items.TryGetValue(partitionName, out def);
            }

            ItemType itemType = def != null ? def.Type : ItemType.Consumable;
            if (category == "Weapons" && def == null) itemType = ItemType.RangedWeapon;
            else if (category == "Weapon Mods" && def == null) itemType = ItemType.WeaponPart;
            else if (category == "Armor" && def == null) itemType = ItemType.Gear;
            else if (category == "Fusion Mods" && def == null) itemType = ItemType.GearPart;

            ItemData item;
            if (itemType == ItemType.RangedWeapon)
            {
                item = new RangedWeaponItemData();
            }
            else if (itemType == ItemType.Gear || itemType == ItemType.GearPart || itemType == ItemType.MeleeWeapon || itemType == ItemType.WeaponPart || itemType == ItemType.CasualOutfit)
            {
                item = new GearItemData();
            }
            else
            {
                item = new ItemData();
            }

            int actualQty = (category == "Consumables") ? Math.Max(1, quantity) : 1;
            item.Unknown1 = 0;
            item.PartitionName = partitionName;
            SetItemDefinition(item, def);
            item.Quantity = actualQty;
            item.IsNew = true;
            item.Rarity = -1;

            uint newId = ++PartyAgent.NextItemId;
            string displayName = (def != null && !string.IsNullOrWhiteSpace(def.Name)) ? def.Name : partitionName;

            var model = new ArmoryItemModel
            {
                Id = newId,
                PartitionName = partitionName,
                DisplayName = displayName,
                Category = category,
                Tier = def?.Tier ?? 0,
                Quantity = actualQty,
                Rarity = -1,
                IsNew = true,
                ItemData = item,
                IsNewlyAdded = true
            };

            ArmoryItems.Add(model);
            return model;
        }

        public void RemoveItemFromArmory(ArmoryItemModel item)
        {
            if (item == null) return;
            ArmoryItems.Remove(item);
            if (!item.IsNewlyAdded)
            {
                DeletedArmoryItemIds.Add(item.Id);
            }
        }

        public int AddAllFusionMods()
        {
            int added = 0;
            foreach (var fm in FusionModsList)
            {
                bool exists = ArmoryItems.Any(a => a.Category == "Fusion Mods" &&
                    a.PartitionName.Equals(fm.Partition, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    AddItemToArmory(fm.Partition, "Fusion Mods", 1);
                    added++;
                }
            }
            return added;
        }

        public int MaxConsumables(int targetQuantity = 100)
        {
            int modified = 0;
            foreach (var item in ArmoryItems.Where(a => a.Category == "Consumables"))
            {
                if (item.Quantity < targetQuantity)
                {
                    item.Quantity = targetQuantity;
                    modified++;
                }
            }

            foreach (var cc in CoreConsumablesList)
            {
                bool exists = ArmoryItems.Any(a => a.Category == "Consumables" &&
                    a.PartitionName.Equals(cc.Partition, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    AddItemToArmory(cc.Partition, "Consumables", targetQuantity);
                    modified++;
                }
            }
            return modified;
        }

        #endregion
    }

    public static class ProfOptsHelper
    {
        private static List<string> GetProfOptsFiles(string saveDir)
        {
            if (string.IsNullOrEmpty(saveDir) || !Directory.Exists(saveDir)) return new List<string>();
            return Directory.GetFiles(saveDir, "ProfOpts*")
                .Where(f => !f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith("_profile", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => string.Equals(Path.GetFileName(f), "ProfOpts", StringComparison.OrdinalIgnoreCase) ? 0 : 1) // prioritize primary ProfOpts
                .ToList();
        }

        public static bool TryRead(string saveDir, out bool isFemale, out int difficulty)
        {
            isFemale = false;
            difficulty = 2; // Normal
            try
            {
                var files = GetProfOptsFiles(saveDir);
                if (files.Count == 0) return false;

                var profPath = files[0];
                var bytes = File.ReadAllBytes(profPath);
                var str = Encoding.ASCII.GetString(bytes);

                // IsFemalePathfinder
                var idxF = str.IndexOf("IsFemalePathfinder\0");
                if (idxF >= 0)
                {
                    var valLenOffset = idxF + 19;
                    if (valLenOffset + 4 < bytes.Length)
                    {
                        var valChar = (char)bytes[valLenOffset + 4];
                        isFemale = (valChar == '1');
                    }
                }

                // GameDifficulty (SP)
                int cur = 0;
                int idxD = -1;
                while ((cur = str.IndexOf("GameDifficulty\0", cur)) >= 0)
                {
                    if (cur >= 2 && str.Substring(cur - 2, 2) != "MP")
                    {
                        idxD = cur;
                        break;
                    }
                    cur += 14;
                }

                if (idxD < 0)
                {
                    idxD = str.IndexOf("Difficulty\0");
                }

                if (idxD >= 0)
                {
                    var keyLen = str[idxD] == 'G' ? 15 : 11;
                    var valLenOffset = idxD + keyLen;
                    if (valLenOffset + 4 < bytes.Length)
                    {
                        var valChar = (char)bytes[valLenOffset + 4];
                        if (int.TryParse(valChar.ToString(), out int d))
                        {
                            difficulty = Math.Max(0, Math.Min(4, d));
                        }
                    }
                }

                return true;
            }
            catch { return false; }
        }

        public static bool TryWrite(string saveDir, string careerId, string characterName, bool isFemale, int difficulty)
        {
            try
            {
                var targetDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(saveDir) && Directory.Exists(saveDir))
                    targetDirs.Add(saveDir);
                if (Directory.Exists(SaveManager.DefaultSaveDirectory))
                    targetDirs.Add(SaveManager.DefaultSaveDirectory);

                bool anyUpdated = false;
                foreach (var dir in targetDirs)
                {
                    var files = GetProfOptsFiles(dir);
                    if (files.Count == 0) continue;

                    foreach (var profPath in files)
                    {
                        try
                        {
                            var bytes = File.ReadAllBytes(profPath);
                            var str = Encoding.ASCII.GetString(bytes);

                            // 1. IsFemalePathfinder
                            var idxF = str.IndexOf("IsFemalePathfinder\0");
                            if (idxF >= 0)
                            {
                                var valLenOffset = idxF + 19;
                                if (valLenOffset + 4 < bytes.Length)
                                {
                                    bytes[valLenOffset + 4] = isFemale ? (byte)'1' : (byte)'0';
                                }
                            }

                            // 2. GameDifficulty (SP)
                            int cur = 0;
                            int idxD = -1;
                            while ((cur = str.IndexOf("GameDifficulty\0", cur)) >= 0)
                            {
                                if (cur >= 2 && str.Substring(cur - 2, 2) != "MP")
                                {
                                    idxD = cur;
                                    break;
                                }
                                cur += 14;
                            }

                            if (idxD < 0)
                            {
                                idxD = str.IndexOf("Difficulty\0");
                            }

                            if (idxD >= 0)
                            {
                                var keyLen = str[idxD] == 'G' ? 15 : 11;
                                var valLenOffset = idxD + keyLen;
                                if (valLenOffset + 4 < bytes.Length)
                                {
                                    char diffChar = (char)('0' + Math.Max(0, Math.Min(4, difficulty)));
                                    bytes[valLenOffset + 4] = (byte)diffChar;
                                }
                            }

                            // 3. ProfileIdList (updates Career Name and Career Difficulty for Load Game menu)
                            var idxP = str.IndexOf("ProfileIdList\0");
                            if (idxP >= 0 && idxP + 18 <= bytes.Length)
                            {
                                int oldListLen = BitConverter.ToInt32(bytes, idxP + 14);
                                if (oldListLen > 0 && idxP + 18 + oldListLen <= bytes.Length)
                                {
                                    string listStr = Encoding.ASCII.GetString(bytes, idxP + 18, oldListLen);
                                    string nameToUse = !string.IsNullOrWhiteSpace(characterName) ? characterName.Trim() : "Ryder";
                                    int diffToUse = Math.Max(0, Math.Min(4, difficulty));

                                    List<string> candidates = new List<string>();
                                    if (!string.IsNullOrWhiteSpace(careerId))
                                    {
                                        candidates.Add(careerId.Trim());
                                        if (uint.TryParse(careerId, System.Globalization.NumberStyles.HexNumber, null, out uint pDec))
                                        {
                                            candidates.Add(pDec.ToString());
                                        }
                                        if (uint.TryParse(careerId, out uint decVal))
                                        {
                                            candidates.Add(decVal.ToString("x8"));
                                            candidates.Add(decVal.ToString("X8"));
                                        }
                                    }

                                    int cIdx = -1;
                                    string matchedPrefix = "";
                                    foreach (var cand in candidates)
                                    {
                                        int found = listStr.IndexOf(cand + ",");
                                        if (found >= 0)
                                        {
                                            cIdx = found;
                                            matchedPrefix = cand;
                                            break;
                                        }
                                    }

                                    // Fallback: if single career in list, match it
                                    if (cIdx < 0 && listStr.IndexOf(';') > 0 && listStr.IndexOf(';') == listStr.LastIndexOf(';'))
                                    {
                                        int firstComma = listStr.IndexOf(',');
                                        if (firstComma > 0)
                                        {
                                            cIdx = 0;
                                            matchedPrefix = listStr.Substring(0, firstComma);
                                        }
                                    }

                                    if (cIdx >= 0)
                                    {
                                        int semi = listStr.IndexOf(';', cIdx);
                                        if (semi > cIdx)
                                        {
                                            string newEntry = $"{matchedPrefix},{nameToUse},{nameToUse},{diffToUse};";
                                            string newListStr = listStr.Substring(0, cIdx) + newEntry + listStr.Substring(semi + 1);

                                            byte[] newListBytes = Encoding.ASCII.GetBytes(newListStr);
                                            byte[] newLenBytes = BitConverter.GetBytes(newListBytes.Length);

                                            int prefixLen = idxP + 14;
                                            int suffixStart = idxP + 18 + oldListLen;
                                            int suffixLen = bytes.Length - suffixStart;

                                            byte[] newFileBytes = new byte[prefixLen + 4 + newListBytes.Length + suffixLen];
                                            Buffer.BlockCopy(bytes, 0, newFileBytes, 0, prefixLen);
                                            Buffer.BlockCopy(newLenBytes, 0, newFileBytes, prefixLen, 4);
                                            Buffer.BlockCopy(newListBytes, 0, newFileBytes, prefixLen + 4, newListBytes.Length);
                                            Buffer.BlockCopy(bytes, suffixStart, newFileBytes, prefixLen + 4 + newListBytes.Length, suffixLen);

                                            bytes = newFileBytes;
                                            str = Encoding.ASCII.GetString(bytes);
                                        }
                                    }
                                }
                            }

                            File.WriteAllBytes(profPath, bytes);
                            anyUpdated = true;
                        }
                        catch { }
                    }
                }

                return anyUpdated;
            }
            catch { return false; }
        }
    }
}
