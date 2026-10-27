using System.Linq;
using Mirror;
using MultiFPS;
using UnityEngine;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;

namespace ROM.Horde
{
    public sealed class RiftVillagePlayer : NetworkBehaviour
    {
        PlayerInstance Player => GetComponent<PlayerInstance>();
        bool BOT => Player.BOT;
        CharacterInstance MyCharacter => Player.MyCharacter;
        [SyncVar] public double PotionReadyAt;
        double nextVillageTrade;
        // The existing earned-gold wallet is local PlayerPrefs. Never allow a remote
        // client to spend the host's wallet; a backend wallet will replace this gate.
        bool LocalVillageWallet => connectionToClient != null && connectionToClient == NetworkServer.localConnection;

        [Command] public void CmdVillageTrade(string npcId, int product, bool sell)
        {
            ServerVillageTrade(npcId, product, sell, out var message);
            TargetVillageNotice(connectionToClient, message);
        }
        [Server] public bool ServerVillageTrade(string npcId, int product, bool sell, out string message)
        {
            message = "Comercio disponible en la partida local; falta conectar la cartera al backend para clientes remotos.";
            if (!LocalVillageWallet) return false;
            message = "Acercate al comerciante para comprar o vender.";
            if (BOT || !MyCharacter || MyCharacter.Health.CurrentHealth <= 0 || !RiftVillageSupplies.Valid(product)) return false;
            var npc = Object.FindObjectsByType<RiftVillageNpc>(FindObjectsSortMode.None).FirstOrDefault(n => n.Id == npcId && n.Trades);
            if (!npc || Vector3.Distance(npc.transform.position, MyCharacter.transform.position) > npc.InteractionRadius ||
                !GameManager.Gamemode.IsProtectedPosition(MyCharacter.transform.position)) return false;
            if (NetworkTime.time < nextVillageTrade) { message = "Esperá un instante."; return false; }
            nextVillageTrade = NetworkTime.time + .15;
            bool result=RiftVillageSupplies.Trade(product, sell, out message);if(result&&!sell)ROM.Progression.RiftJournalPlayer.Record(Player,RiftProgressEvent.PotionBought,product.ToString());return result;
        }
        [Command] public void CmdDrinkPotion(int product){ServerDrinkPotion(product,out var message);TargetVillageNotice(connectionToClient,message);}
        [Command] public void CmdDrinkVillagePotion()
        {
            ServerDrinkVillagePotion(out var message);
            TargetVillageNotice(connectionToClient, message);
        }
        [Server] public bool ServerDrinkVillagePotion(out string message)
        {return ServerDrinkPotion(RiftVillageSupplies.Count(0)>0?0:1,out message);}
        [Server] public bool ServerDrinkPotion(int id,out string message)
        {
            message="Las provisiones de El Rift se conservan durante el prólogo.";if(ROM.Tutorial.Crilium.CriliumSession.Active)return false;
            message="Poción desconocida.";if(!RiftVillageSupplies.Valid(id))return false;
            message = "Las pociones de esta cartera local no están habilitadas para clientes remotos.";
            if (!LocalVillageWallet) return false;
            message = "No podés beber una poción ahora.";
            if (BOT || !MyCharacter || MyCharacter.Health.CurrentHealth <= 0) return false;
            if (MyCharacter.Health.CurrentHealth >= MyCharacter.Health.MaxHealth) { message = "Ya tenés la vida completa."; return false; }
            if (NetworkTime.time < PotionReadyAt) { message = "La poción todavía está en recarga."; return false; }
            if (!RiftVillageSupplies.Consume(id)) { message = "No tenés pociones. Visitá a la boticaria."; return false; }
            int heal = Mathf.CeilToInt(MyCharacter.Health.MaxHealth * RiftVillageSupplies.Healing(id));
            MyCharacter.Health.ServerHeal(heal, MyCharacter.DNID);
            PotionReadyAt = NetworkTime.time + 5;
            ROM.Progression.RiftJournalPlayer.Record(Player,RiftProgressEvent.PotionUsed,id.ToString());
            message = "Bebiste " + RiftVillageSupplies.Name(id) + ".";
            return true;
        }
        [TargetRpc] void TargetVillageNotice(NetworkConnectionToClient target, string message)
        {
            RiftVillageUI.Notify(message);
        }
    }
}
