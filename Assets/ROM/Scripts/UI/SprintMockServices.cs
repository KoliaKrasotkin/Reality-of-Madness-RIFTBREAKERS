using System;
using System.Collections.Generic;

namespace ROM.UI.Sprint01
{
    // Session-only presentation fixtures. Never write MultiFPS inventory, currency,
    // PlayerPrefs balances or authoritative ownership. Restart resets all previews.
    public sealed class SprintMockServices : IPlayerProfileService, ICurrencyBalanceService,
        IInventoryService, IMissionService, IMarketplaceService, IAuctionHouseService,
        ICurrencyExchangeService, IRewardService, IPartyService
    {
        public string Name => string.IsNullOrWhiteSpace(MultiFPS.UserSettings.UserNickname) ? "RiftWalker" : MultiFPS.UserSettings.UserNickname;
        public int Level => 42;
        public float Experience => .56f;
        public long AncientGold => 12840;
        public long RiftCrystals => 3650;
        public int Capacity => 4;
        public bool LocalReady { get; set; }
        public bool RulesConfigured => false;
        public string UnavailableReason => "Reglas de intercambio pendientes. Sin moneda de pago ni tasa definida.";
        public RewardState State { get; private set; } = RewardState.Available;
        public IReadOnlyList<ItemRecord> Items { get; } = new[] {
            new ItemRecord { Id="preview-rifle-01",DefinitionId="rifle",Name="Juramento de ceniza",Category="Arma",Rarity=Rarity.Legendary,OwnerId="mock-local",Description="Fusil recuperado del borde de la grieta.",Stats="Daño 72 · Estabilidad 64 · Alcance 80",Tradable=true },
            new ItemRecord { Id="preview-sidearm-01",DefinitionId="sidearm",Name="Eco del vacío",Category="Arma",Rarity=Rarity.Epic,OwnerId="mock-local",Stats="Daño 48 · Estabilidad 82",Tradable=true },
            new ItemRecord { Id="preview-armor-01",DefinitionId="armor",Name="Coraza del caminante",Category="Equipo",Rarity=Rarity.Rare,OwnerId="mock-local",Stats="Defensa 58 · Resistencia 44",Tradable=false },
            new ItemRecord { Id="preview-medkit-01",DefinitionId="medkit",Name="Reserva médica",Category="Consumible",Rarity=Rarity.Common,OwnerId="mock-local",Stats="Recuperación · Cantidad 3",Stackable=true,Quantity=3,Tradable=true }
        };
        public IReadOnlyList<MissionRecord> Missions { get; } = new[] {
            new MissionRecord { Id="horde",Name="Bosque Fracturado",Type="Incursión",Difficulty="Normal",Objectives="Contener las oleadas dimensionales\nDefender la fisura central\nExplorar las islas conectadas",Duration="Sesión abierta",Rewards="Recompensas pendientes de balance",Available=true },
            new MissionRecord { Id="recovery",Name="Señal entre las ruinas",Type="Recuperación",Difficulty="Difícil",Objectives="Localizar el archivo perdido\nExtraer los suministros",Duration="15–20 min",Rewards="Vista previa · Sin recompensas reales",Available=false },
            new MissionRecord { Id="investigation",Name="El silencio del Sol",Type="Investigación",Difficulty="Normal",Objectives="Rastrear la anomalía\nInvestigar el templo",Duration="20–25 min",Rewards="Vista previa · Sin recompensas reales",Available=false }
        };
        public IReadOnlyList<ProductRecord> Products { get; } = new[] {
            new ProductRecord { Id="skin-crimson",Name="Vigía carmesí",Category="Personaje",GoldPrice=1200,Season="Temporada 01",Description="Apariencia cosmética del caminante. Vista previa conceptual." },
            new ProductRecord { Id="skin-void",Name="Pulso del vacío",Category="Arma",GoldPrice=600,Season="Temporada 01",Description="Acabado cosmético de arma. Vista previa conceptual." },
            new ProductRecord { Id="bundle",Name="Hijos de la fisura",Category="Bundle",GoldPrice=1800,Season="Temporada 01",Description="Colección de cosméticos de demostración." }
        };
        public IReadOnlyList<ListingRecord> Listings { get; } = new[] {
            new ListingRecord { Id="listing-01",Seller="Nyx (ejemplo)",Item="Eco del vacío",Category="Arma",Rarity=Rarity.Epic,Price=650 },
            new ListingRecord { Id="listing-02",Seller="Kaelthar (ejemplo)",Item="Reserva médica",Category="Consumible",Rarity=Rarity.Common,Price=40 }
        };
        public ServiceResult PreviewPurchase(string id)
        {
            foreach(var p in Products) if(p.Id==id) { if(p.Owned)return new ServiceResult { Message="Este producto ya está marcado como adquirido en la demostración." }; p.Owned=true; return Result("Compra simulada. El saldo y el inventario reales no se modificaron."); }
            return new ServiceResult { Message="Producto no encontrado." };
        }
        public ServiceResult PreviewTransaction(string id) => Result("Operación simulada. Sin liquidación, reserva ni transferencia real.");
        public ServiceResult PreviewListing(string id,int price,int hours)
        {
            ItemRecord item=null;foreach(var candidate in Items)if(candidate.Id==id)item=candidate;
            if(item==null||!item.Tradable)return new ServiceResult { Message="Objeto no elegible para el anuncio de prueba." };
            if(price<=0||hours<=0)return new ServiceResult { Message="Ingresá un precio y duración válidos." };
            return Result($"Revisión simulada: {item.Name} · {price} RC · {hours} h. Tarifas pendientes de configuración. No se publicó un anuncio real.");
        }
        public ServiceResult PreviewClaim() { State=RewardState.PendingValidation; return Result("Solicitud de ejemplo pendiente de validación del servicio. No se emitió moneda."); }
        static ServiceResult Result(string message) => new ServiceResult { Success=true, Message=message, TransactionId="preview-"+Guid.NewGuid().ToString("N") };
    }
}
