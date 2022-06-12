using System.Reflection;
using HarmonyLib;
using UnityEngine;

public class OcbParachute : IModApi
{

    public void InitMod(Mod mod)
    {
        Debug.Log("Loading OCB Parachute Patch: " + GetType().ToString());
        var harmony = new Harmony(GetType().ToString());
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
                __instance.Detach();
                parachute.Detach();
            }
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
            // Register KeyPress on every frame update
            HasKeyPress |= Input.GetKeyDown(KeyCode.G);
        }
        static void Postfix(vp_FPController __instance)
        {
            // Fixes jitter when not driving (e.g. when falling in 3rd person view)
            // Note: You may still see a little jitter, but that is from motion blur ;)
            __instance.CharacterController.transform.position = __instance.SmoothPosition;
        }
    }

    [HarmonyPatch(typeof(vp_FPController))]
    [HarmonyPatch("UpdateForces")]
    public class vp_FPController_UpdateForces
    {

        static bool TooltipShown = false;

        static EntityVehicle parachute;

        public static void Postfix(
            bool ___m_Grounded,
            ref float ___m_FallSpeed,
            ref float ___m_FallImpact,
            ref float ___m_NonRetardedFallSpeed,
            ref Vector3 ___m_ExternalForce,
            ref Vector3 ___m_MoveDirection,
            vp_FPController __instance)
        {

            var player = GameManager.Instance.World.GetPrimaryPlayer();
            bool ParachuteDeployed = parachute != null && parachute.IsSpawned();

            // Reset flag when grounded
            if (___m_Grounded)
            {
                TooltipShown = false;
            }
            // Check if Tooltip should be shown when falling too fast
            else if (ParachuteDeployed == false)
            {
                if (___m_FallSpeed < -0.2f && TooltipShown == false)
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
                if (ParachuteDeployed)
                {
                    TooltipShown = false;
                }
            }
            else if (!ParachuteDeployed)
            {
                if (IsPressed && !WasPressed && ___m_FallSpeed < -0.15f)
                {
                    // Load the parachute vehicle only once
                    if (parachute == null) parachute = EntityFactory.CreateEntity(
                        EntityClass.FromString("OcbParachute"), player.position,
                        new Vector3(0f, player.rotation.y, 0f)) as EntityVehicle;

                    GameManager.Instance.World.SpawnEntityInWorld(parachute);
                    player.StartAttachToEntity(parachute);
                }
            }
        }
    }
    
}
