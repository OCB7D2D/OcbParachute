using UnityEngine;
using System.Linq;
public static class VehicleExtensions
{
    public static bool IsFlyingVehicle(this EntityVehicle vehicle)
    {
        if (vehicle == null)
        {
            return false;
        }

        if (vehicle is EntityVGyroCopter || vehicle is EntityVHelicopter || vehicle is EntityVBlimp)
        {
            return true;
        }

        var properties = vehicle.GetVehicle().Properties.Classes.Dict
            .Where(entry => entry.Key.Contains("force"))
            .Select(item => item.Value);

        foreach (DynamicProperties property in properties)
        {
            string trigger = property.GetString("trigger");
            if (trigger != null && (trigger.Contains("motor") || trigger.Contains("inputForward")))
            {
                Vector3 force = Vector3.zero;
                property.ParseVec("force", ref force);
                if (force.y > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static VPHeadlight GetHeadlight(this EntityVehicle vehicle)
    {
        if (vehicle == null)
        {
            return null;
        }

        return vehicle.GetVehicle().FindPart("headlight") as VPHeadlight;
    }
}

public class XUiC_ParachuteInfo : XUiController
{

    private EntityPlayerLocal localPlayer;
    private EntityVehicle vehicle;
    private VPHeadlight headlight;
    private bool isInFlyingVehicle;
    private bool isDriving;
    private bool isHeadlightOn;

    public EntityVehicle Vehicle
    {
        get => vehicle;
        internal set
        {
            if (vehicle != value)
            {
                vehicle = value;
                isInFlyingVehicle = vehicle.IsFlyingVehicle();
                headlight = vehicle.GetHeadlight();
                IsDirty = true;
            }
        }
    }

    public override void Init()
    {
        base.Init();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (localPlayer == null)
        {
            localPlayer = xui.playerUI.entityPlayer;
        }

        if (XUi.IsGameRunning() && localPlayer != null)
        {
            Vehicle = localPlayer.AttachedToEntity as EntityVehicle;
            if (isDriving != IsDriver())
            {
                IsDirty = true;
                isDriving = IsDriver();
            }
        }

        if (IsDirty || isHeadlightOn != IsHeadlightOn())
        {
            isHeadlightOn = IsHeadlightOn();
            IsDirty = false;
            RefreshBindings();
        }
        // IsDirty = false;
        RefreshBindings();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        IsDirty = true;
        RefreshBindings();
    }

    public override void OnClose()
    {
        base.OnClose();
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        var player = this.xui.playerUI.entityPlayer;
        if (player == null)
        {
            value = "NA";
            return true;
        }
        // if (wm == null) return false;
        var parachute = player.AttachedToEntity as EntityVParachute;
        // var parachute = vehicle.vehicle as EntityVParachute;
        if (parachute == null)
        {
            value = "NAP";
            return true;
        }
        // wm.Open("parachuteInfo", false, true);
        if (parachute.vehicleRB == null)
            {
                value = "NARB";
                return true;
            }

        if (parachute.vehicleRB.velocity == null)
            {
                value = "NAV";
                return true;
            }

        if (bindingName == "test")
        {
            value = "test";
            return true;
        }

        if (bindingName == "test-a")
        {
            value = "test-a";
            return true;
        }

        var wr = parachute.GetWorldRotation();
        var lr = parachute.GetLocalRotation();
        var wv = parachute.GetWorldVelocity();
        var lv = parachute.GetLocalVelocity();
        var aoa = parachute.GetAngleOfAttack();
        var cl = parachute.GetLiftCoefficient();
        var cd = parachute.GetDragCoefficient();

        var up = parachute.GetForceUplift();

        switch (bindingName)
        {
            case "world-speed-forward":
                value = wv.z.ToString();
                return true;
            case "world-speed-sideward":
                value = wv.x.ToString();
                return true;
            case "world-speed-downward":
                value = wv.y.ToString();
                return true;
                
            case "local-speed-forward":
                value = lv.z.ToString();
                return true;
            case "local-speed-sideward":
                value = lv.x.ToString();
                return true;
            case "local-speed-downward":
                value = lv.y.ToString();
                return true;

            case "local-rotation-x":
                value = lr.x.ToString();
                return true;
            case "local-rotation-y":
                value = lr.y.ToString();
                return true;
            case "local-rotation-z":
                value = lr.z.ToString();
                return true;
            case "world-rotation-x":
                value = wr.x.ToString();
                return true;
            case "world-rotation-y":
                value = wr.y.ToString();
                return true;
            case "world-rotation-z":
                value = wr.z.ToString();
                return true;

            case "force-uplift":
                value = up.ToString();
                return true;

            case "angle-of-attack":
                value = aoa.ToString();
                return true;

            case "lift-coeff":
                value = cl.ToString();
                return true;
            case "drag-coeff":
                value = cd.ToString();
                return true;

            case "input-strafe":
                value = parachute.vehicleRB.transform.TransformDirection(new Vector3(-0.15f, 0, 0)).ToString();
                return true;

        }

        if (bindingName == "local-speed-forward")
        {
            value = parachute.vehicleRB.velocity.x.ToString();
            return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);

        switch (bindingName)
        {
            case "test":
                value = "test";
                return true;
            case "invehicle":
                value = localPlayer != null
                    && !localPlayer.IsDead()
                    && vehicle != null
                    ? "true" : "false";
                return true;
            case "isdriver":
                value = IsDriver() ? "true" : "false";
                return true;
            case "isaflyingvehicle":
                value = isInFlyingVehicle ? "true" : "false";
                return true;
            case "hasengine":
            case "hasfuel":
                value = localPlayer != null
                    && !localPlayer.IsDead()
                    && vehicle != null
                    && vehicle.GetVehicle().HasEnginePart()
                    ? "true" : "false";
                return true;
            case "hasheadlight":
                value = localPlayer != null
                    && !localPlayer.IsDead()
                    && headlight != null
                    ? "true" : "false";
                return true;
            case "isheadlighton":
                value = localPlayer != null
                    && !localPlayer.IsDead()
                    && IsHeadlightOn()
                    ? "true" : "false";
                return true;
            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private bool IsDriver()
    {
        return localPlayer != null
            && !localPlayer.IsDead()
            && vehicle != null
            && vehicle.HasDriver
            && vehicle.AttachedMainEntity == localPlayer;
    }

    private bool IsHeadlightOn()
    {
        return headlight != null && headlight.IsOn();
    }

    /*

         }

    public override void Update(float _dt)
    {
        Log.Out("Update");
        base.Update(_dt);
        IsDirty = true;
        RefreshBindings(true);
    }

    public override bool GetBindingValueInternal(ref string _value, string _bindingName)
    {
        var player = this.xui.playerUI.entityPlayer;
        if (player == null) return false;
        var xui = LocalPlayerUI.GetUIForPlayer(player)?.xui;
        if (xui == null) return false;
        var wm = xui.playerUI.windowManager;
        if (wm == null) return false;
        var parachute = player.AttachedToEntity as EntityVParachute;
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        Log.Out("Parachute is {0} => {1}", player.AttachedToEntity, player.AttachedToEntity);
        // var parachute = vehicle.vehicle as EntityVParachute;
        if (parachute == null) return false;
        // wm.Open("parachuteInfo", false, true);
        Log.Out("FOOOOOOOOBARRR");
        if (parachute.physicsRB == null) return false;
        Log.Out("FOOOOOOOOBARRR");
        if (parachute.physicsRB.velocity == null) return false;
        Log.Out("FOOOOOOOOBARRR");

        if (_bindingName == "test")
        {
            _value = "test";
            return true;
        }

if (_bindingName == "test-a")
{
    _value = "test-a";
    return true;
}

if (_bindingName == "local-speed-forward")
{
    _value = parachute.physicsRB.velocity.x.ToString();
    return true;
}
return base.GetBindingValueInternal(ref _value, _bindingName);
    }

*/
}
