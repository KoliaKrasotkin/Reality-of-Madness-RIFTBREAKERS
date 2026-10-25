using System;
using System.Collections.Generic;

namespace ROM.UI.Sprint01
{
    [Serializable] public class ItemRecord
    {
        public string Id, DefinitionId, Name, Description, Category, OwnerId, CosmeticReference;
        public Rarity Rarity;
        public int Quantity = 1;
        public bool Stackable, Tradable;
        public string Stats;
    }
    [Serializable] public class MissionRecord
    {
        public string Id, Name, Type, Difficulty, Objectives, Duration, Rewards;
        public int MinPlayers = 1, MaxPlayers = 4;
        public bool Available;
    }
    [Serializable] public class ProductRecord { public string Id, Name, Category, Description, Season, ModelKey; public int GoldPrice; public bool Owned; }
    [Serializable] public class ListingRecord { public string Id, Seller, Item, Category,ModelKey; public Rarity Rarity; public int Price; public string Status = "Activa"; }
    public struct ServiceResult { public bool Success; public string Message, TransactionId; }
    public interface IPlayerProfileService { string Name { get; } int Level { get; } float Experience { get; } }
    public interface ICurrencyBalanceService { long AncientGold { get; } long RiftCrystals { get; } }
    public interface IInventoryService { IReadOnlyList<ItemRecord> Items { get; } }
    public interface IMissionService { IReadOnlyList<MissionRecord> Missions { get; } }
    public interface IMarketplaceService { IReadOnlyList<ProductRecord> Products { get; } ServiceResult PreviewPurchase(string id); }
    public interface IAuctionHouseService { IReadOnlyList<ListingRecord> Listings { get; } ServiceResult PreviewTransaction(string id); ServiceResult PreviewListing(string itemId, int price, int hours); }
    // No payment mechanism or exchange rate is assumed. Production implementations must
    // reserve ownership/balances and settle atomically with idempotency and audit records.
    public interface ICurrencyExchangeService { bool RulesConfigured { get; } string UnavailableReason { get; } }
    public enum RewardState { Available, PendingValidation, Claimed, Failed }
    public interface IRewardService { RewardState State { get; } ServiceResult PreviewClaim(); }
    public interface IPartyService { int Capacity { get; } bool LocalReady { get; set; } }
    public sealed class SprintServices
    {
        public IPlayerProfileService Profile;
        public ICurrencyBalanceService Balances;
        public IInventoryService Inventory;
        public IMissionService Missions;
        public IMarketplaceService Marketplace;
        public IAuctionHouseService Auction;
        public ICurrencyExchangeService Exchange;
        public IRewardService Rewards;
        public IPartyService Party;
        public static SprintServices CreateMock(FrontEndCatalog catalog=null)
        {
            var mock=new SprintMockServices(catalog);
            return new SprintServices { Profile=mock,Balances=mock,Inventory=mock,Missions=mock,Marketplace=mock,Auction=mock,Exchange=mock,Rewards=mock,Party=mock };
        }
    }
}
