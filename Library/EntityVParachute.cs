using JetBrains.Annotations;
using UnityEngine;


public static class TransformExtensions
{
    public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var localToWorldMatrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one);
        return localToWorldMatrix.MultiplyPoint3x4(position);
    }

    public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var worldToLocalMatrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one).inverse;
        return worldToLocalMatrix.MultiplyPoint3x4(position);
    }
}

public class EntityVParachute : EntityDriveable
{

    public static EntityPlayerLocal Deployer = null;

    public string SoundOpen = string.Empty;
    public string SoundFlutter = string.Empty;

    public override void CopyPropertiesFromEntityClass()
    {
        base.CopyPropertiesFromEntityClass();
        EntityClass ec = EntityClass.list[entityClass];
        ec.Properties.ParseString("SoundParachuteOpen", ref SoundOpen);
        ec.Properties.ParseString("SoundParachuteFlutter", ref SoundFlutter);
    }

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        Log.Out("Init {0}", vehicleRB?.mass);
        vehicleRB.drag = 0f;
        vehicleRB.angularDrag = 0f;
        vehicleRB.useGravity = true;
        vehicleRB.WakeUp();
    }

    public override AttachedToEntitySlotInfo GetAttachedToInfo(int _slotIdx)
    {
        var info = base.GetAttachedToInfo(_slotIdx);
        info.bKeep3rdPersonModelVisible = false;
        return info;
    }

    public override void HandleNavObject()
    {
        // Don't show any nav object
    }

    public override void DetachEntity(Entity _other)
    {
        Deployer = null;
        base.DetachEntity(_other);
        ForceDespawn();
    }

    public override void Update()
    {
        base.Update();

        // Check if we have an attached player to update physics?
        if (GetAttachedPlayerLocal() is EntityPlayerLocal player)
        {

            var xui = LocalPlayerUI.GetUIForPlayer(player).xui;
            var wm = xui.playerUI.windowManager;
            wm.Open("parachuteInfo", false, true/*, true, false*/);

            vp_FPCamera cam = player.vp_FPCamera;
            vp_FPController ctr = player.vp_FPController;
            transform.position = cam.DrivingPosition + Vector3.down * 2;
            ctr.m_FallSpeed = vehicleRB.velocity.y * 0.5f;
            // Log.Out("ctr.m_FallSpeed {0}", ctr.m_FallSpeed);
        }
        else if (ConnectionManager.Instance.IsServer && !IsDriven())
        {
            // Make sure to clean-up all parachutes that are not driven
            // And hope this never applies to parachutes you actually want
            world.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);
        }

    }

    public Vector3 GetLocalRotation()
    {
        // North is y == 0, East == 90, South == 180, West == 270
        return transform.localEulerAngles;
    }

    public Vector3 GetWorldRotation()
    {
        return transform.eulerAngles;
    }

    public Vector3 GetLocalVelocity()
    {
        return vehicleRB.velocity;
    }

    public Vector3 GetWorldVelocity()
    {
        Vector3 wv = GetLocalVelocity();
        Quaternion quat = transform.rotation;
        return transform.TransformVector(wv);
    }

    // Uplift force of the canopy
    // Incrases with forward speed
    // ToDo: implement attack angle
    public float GetForceUplift()
    {
        Vector3 wv = GetLocalVelocity();
        return Mathf.Abs(wv.x); // More when faster
    }

    public float GetAngleOfAttack()
    {
        var aoa = - GetLocalRotation().x;
        if (aoa < -180) aoa += 360;
        return aoa;
    }

    float DEG2RAD = Mathf.PI / 180f;
    float RAD2DEG = 180f / Mathf.PI;

    public float GetLiftCoefficient()
    {
        var aoa = GetAngleOfAttack();
        // No uplift below a certain angle
        if (aoa < -5f) return 0f;
        // Plot via sin curve from -5 to 10 (result 0 to 1)
        // Increases nearly linear til reaching max uplift
        if (aoa < 10f) return Mathf.Sin((aoa + 5f) / 15f * 0.5f * Mathf.PI);
        // After that it rapidly decreases to zero again
        var rv = Mathf.Cos(aoa * 0.2f * Mathf.PI) * 2f - 1f;
        // Always return positive result
        return rv < 0 ? 0f : rv;
    }

    public float GetDragCoefficient()
    {
        var aoa = GetAngleOfAttack();
        // No uplift below a certain angle
        return 1f - Mathf.Abs(Mathf.Cos(2f * aoa * DEG2RAD));
    }

    // protected override void onSpawnStateChanged() { base.onSpawnStateChanged(); }

    // public override void OnAddedToWorld() { base.OnAddedToWorld(); }

    // public override void PostInit() { base.PostInit(); }

    // protected override void Awake() { base.Awake(); }
    // int count = 0;
    public override void PhysicsInputMove()
    {

        if (vehicleRB == null) return;
        if (movementInput == null) return;

        float parachuteDrag = 1f;
        float canopyHeight = 2f;

        vehicleRB.useGravity = false;
        vehicleRB.WakeUp();


        // vehicleRB.velocity = new Vector3(
        //     vehicleRB.velocity.x * 0.95f,
        //     vehicleRB.velocity.y * 0.75f,
        //     vehicleRB.velocity.z * 0.95f
        // );
        // 
        // vehicleRB.angularVelocity = new Vector3(
        //     vehicleRB.angularVelocity.x * 0.98f,
        //     vehicleRB.angularVelocity.y * 0.98f,
        //     vehicleRB.angularVelocity.z * 0.98f
        // );

        var rot = Quaternion.FromToRotation(transform.up, Vector3.up);

        float angleDiffFromVertical = Vector3.Angle(Vector3.up, transform.up);

        float parchuteSurfaceArea;

        if (angleDiffFromVertical <= 90) //bottom down
        {
            parchuteSurfaceArea = Mathf.Cos(angleDiffFromVertical * Mathf.PI / 180f) * 1; //doesn't matter how wide the parachute is
        }
        else
        {
            parchuteSurfaceArea = Mathf.Abs(Mathf.Cos(Mathf.PI - angleDiffFromVertical * Mathf.PI / 180f));
            parchuteSurfaceArea /= 5; //way less lift if upside down
        }

        Vector3 appliedForceAngle = Vector3.Slerp(transform.up, Vector3.up, 0.33f);

        parachuteDrag *= Mathf.Abs(vehicleRB.velocity.y) * parchuteSurfaceArea;

        // 180 when fully falling down
        float AngleDiffFromMotion = Vector3.Angle(transform.up, vehicleRB.velocity);
        
        if (++count > 20)
        {
            // Log.Out("Angle diff {0}", angleDiffFromVertical);
            // Log.Out("Mass {0}", vehicleRB.mass);
            // Log.Out("Speed {0}", vehicleRB.velocity);
            // Log.Out("Player Up {0}", transform.up);
            // Log.Out("RB Up {0}", vehicleRB.transform.up);
            // Log.Out("Surface {0}", parchuteSurfaceArea);
            // Log.Out("Up Drag {0}", parachuteDrag);
            count = 0;
        }

        // Uplift can be heigher than gravity if speed is high!?

        parachuteDrag *= 4.81f;

        // Completely stop gravity at some point
        // vehicleRB.AddForce(Vector3.up * 9.81f);

        // Add air drag from the canopy
        // vehicleRB.AddForceAtPosition(appliedForceAngle * parachuteDrag,
        //     transform.TransformPoint(transform.up * canopyHeight));

        // Add air drag against the velocity motion (Cd)
        // Scales quadratic with speed of actual motion
        Vector3 airResistanceBody = new Vector3(-0.95f, -0.65f, -0.8f);
        Vector3 cdVector = transform.TransformPointUnscaled(airResistanceBody);
        // Scale drag by squared velocity
        cdVector.Scale(vehicleRB.velocity); // Scale by velocity
        cdVector.Scale(vehicleRB.velocity); // Scale by square
        // float velocitySquare = vehicleRB.velocity.sqrMagnitude;
        Vector3 cbForce = cdVector * GetLiftCoefficient();

        Vector3 airUpliftVec = new Vector3(1.0f, 0f, 0f);

        // What does this do?
        airUpliftVec = transform.TransformPointUnscaled(airUpliftVec);

        // Scale drag by squared velocity
        // airUpliftVec.Scale(vehicleRB.velocity); // Scale by velocity
        // airUpliftVec.Scale(vehicleRB.velocity); // Scale by square

        if (count == 0)
        {
            //Log.Out("Add force {0}", cbForce);
            //Log.Out("Add uplift {0}", airUpliftVec);
        }
        // vehicleRB.AddForce(0.002f * cbForce);

        // vehicleRB.AddForceAtPosition(transform.up * parachuteDrag, transform.TransformPoint(Vector3.up * canopyHeight));

        // vehicleRB.AddForceAtPosition(Vector3.up);

        float factor = 0.8f / Vector3.Dot(vehicleRB.transform.up,
            Quaternion.Euler(-30f, transform.rotation.eulerAngles.y, 0f) * Vector3.up);
        
        // AddForce(
        //     new Vector3(0f, -factor, 0f),
        //     ForceMode.VelocityChange);

        var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);


        if (!isEntityRemote)
        {
            // Dampen forward speed by air drag
            // Air drag is quadratic to existing speed
            //vehicleRB.AddForce(
            //    forward, // * factor * factor
            //    ForceMode.VelocityChange);
            // vehicleRB.AddRelativeTorque(
            //     movementInput.moveForward * 10.1f,
            //     movementInput.moveStrafe * 10.03f,
            //     movementInput.moveStrafe * -2.02f);
            if (movementInput.moveStrafe != 0) {
                vehicleRB.AddForceAtPosition(
                    vehicleRB.transform.TransformDirection(new Vector3(0, 0, 10.75f)),
                    vehicleRB.transform.TransformPoint(new Vector3(movementInput.moveStrafe * 4f, 8, 0))
                );
                // vehicleRB.AddForceAtPosition(
                //     new Vector3(0, 0, 10.75f),
                //     vehicleRB.transform.TransformPoint(new Vector3(movementInput.moveStrafe * -4f, 8f, 0))
                // );
            }
            Log.Out("FORCES {0}", vehicleRB.GetAccumulatedForce());
            Log.Out("TORQUES {0}", vehicleRB.GetAccumulatedTorque());
            // Dampen the torque to get upright again
            // vehicleRB.AddTorque(new Vector3(rot.x, rot.y, rot.z) * 100f);
        }

        // vehicleRB.velocity = new Vector3(vehicleRB.velocity.x, 0, vehicleRB.velocity.z);
        // Physics.SyncTransforms();
    }

    public override void SetWheelsForces(float motorTorque, float motorTorqueBase, float brakeTorque, float _frictionPercent)
    {
    }

    public override void UpdateWheelsSteering()
    {
    }

}

