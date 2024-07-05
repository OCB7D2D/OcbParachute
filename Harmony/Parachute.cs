using Audio;
using HarmonyLib;
using Platform;
using System.Reflection;
using UnityEngine;

public class OcbParachute : IModApi
{

    static public KeyCode ParachuteHotKey = KeyCode.LeftControl;

    public void InitMod(Mod mod)
    {
        Log.Out("OCB Harmony Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    [HarmonyPatch(typeof(EntityPlayerLocal))]
    [HarmonyPatch("FallImpact")]
    public class EntityPlayerLocal_FallImpact
    {
        static void Prefix(EntityPlayerLocal __instance)
        {
            if (__instance.AttachedToEntity is EntityVParachute parachute)
            {
                if (!ConnectionManager.Instance.IsServer)
                {
                    ConnectionManager.Instance.SendToServer(
                        NetPackageManager.GetPackage<NetPkgDespawnParachute>()
                            .Setup(parachute.entityId));
                }
                else
                {
                    EntityVParachute.Deployer = null;
                    __instance.Detach();
                    // parachute.Detach();
                }
            }
        }
    }

    // Fix bug with vanilla when we remove parachute
    // Game wants to save waypoints for all vehicles
    // But the parachute already instantly despawned
    [HarmonyPatch(typeof(XUiC_MapArea))]
    [HarmonyPatch("CreateVehicleLastKnownWaypoint")]
    public class XUiC_MapArea_CreateVehicleLastKnownWaypoint
    {
        static bool Prefix(EntityVehicle _vehicle)
        {
            // Skip this for the parachute vehicle
            return !(_vehicle is EntityVParachute);
        }
    }

    // Hide some stuff from UI for parachutes
    [HarmonyPatch(typeof(XUiC_HUDStatBar))]
    [HarmonyPatch("GetBindingValue")]
    public class XUiC_HUDStatBar_GetBindingValue
    {
        static bool Prefix(
            ref string value,
            string bindingName,
            XUiC_HUDStatBar __instance,
            HUDStatTypes ___statType,
            ref bool __result)
        {
            if (bindingName == "statvisible")
            {
                if (___statType != HUDStatTypes.VehicleFuel)
                {
                    if (__instance.Vehicle is EntityVParachute)
                    {
                        value = "false";
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }
    }

    static bool IsPressed = false;
    static bool WasPressed = false;
    static bool HasKeyPress = false;

    [HarmonyPatch(typeof(vp_FPController))]
    [HarmonyPatch("Update")]
    public class vp_FPController_Update
    {
        static void Prefix(vp_FPController __instance)
        {
            if (ParachuteHotKey == KeyCode.None) return;
            // Register KeyPress on every frame update
            HasKeyPress |= Input.GetKeyDown(ParachuteHotKey);
        }
        /*
        static void Postfix(vp_FPController __instance)
        {
            // Fixes jitter when not driving (e.g. when falling in 3rd person view)
            // Note: You may still see a little jitter, but that is from motion blur ;)
            __instance.CharacterController.transform.position = __instance.SmoothPosition;
        }
        */
    }

    [HarmonyPatch(typeof(vp_FPController))]
    [HarmonyPatch("UpdateForces")]
    public class vp_FPController_UpdateForces
    {

        static bool TooltipShown = false;

        public static void Postfix(
            bool ___m_Grounded,
            ref float ___m_FallSpeed,
            ref float ___m_FallImpact,
            ref Vector3 ___m_ExternalForce,
            ref Vector3 ___m_MoveDirection,
            vp_FPController __instance)
        {

            // Skip if HotKey was disabled from item action
            if (ParachuteHotKey == KeyCode.None) return;

            var player = GameManager.Instance.World.GetPrimaryPlayer();

            // Reset flag when grounded
            if (___m_Grounded)
            {
                TooltipShown = false;
            }
            // Check if Tooltip should be shown when falling too fast
            else if (EntityVParachute.Deployer == null)
            {
                if (___m_FallSpeed < -0.25f && TooltipShown == false)
                {
                    GameManager.ShowTooltip(
                        XUiM_Player.GetPlayer() as EntityPlayerLocal,
                        Localization.Get("ttDeployParachute"));
                    TooltipShown = true;
                }
            }

            if (HasKeyPress)
            {
                HasKeyPress = false;
                WasPressed = IsPressed;
                IsPressed = true;
            }
            else
            {
                WasPressed = IsPressed;
                IsPressed = false;
            }
            // Check if parachute was toggled mid air
            if (___m_Grounded)
            {
                if (EntityVParachute.Deployer != null)
                {
                    TooltipShown = false;
                }
            }
            else if (EntityVParachute.Deployer == null)
            {
                if (IsPressed && !WasPressed && ___m_FallSpeed < -0.15f)
                {
                    DeployParachute(player);
                }
            }
        }

    }

    public static void DeployParachute(EntityPlayerLocal player)
    {
        // Load the parachute vehicle only once
        if (EntityVParachute.Deployer == null)
        {
            EntityVParachute.Deployer = player;
            int id = EntityClass.FromString("OcbParachute");
            var item = ItemClass.GetItem("OcbParachutePlaceable");
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            {
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(
                    NetPackageManager.GetPackage<NetPkgDeployParachute>()
                        .Setup(id, player.position + Vector3.up,
                            new Vector3(0f, player.rotation.y, 0f),
                            item, player.entityId),
                        true);
            }
            else
            {
                var vehicle = EntityFactory.CreateEntity(
                    id, player.position + Vector3.up,
                    new Vector3(0f, player.rotation.y, 0f)) as EntityVehicle;
                vehicle.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);
                vehicle.SetOwner(PlatformManager.InternalLocalUserIdentifier);
                vehicle.GetVehicle().SetItemValue(item);
                GameManager.Instance.World.SpawnEntityInWorld(vehicle);
                player.StartAttachToEntity(vehicle, 0);
                if (vehicle is EntityVParachute parachute)
                {
                    Manager.Play(vehicle, parachute.SoundOpen);
                    Manager.Play(vehicle, parachute.SoundFlutter);
                }
            }
        }
    }

}
