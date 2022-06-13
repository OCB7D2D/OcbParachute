using UnityEngine;

public class EntityVParachute : EntityDriveable
{

    public static EntityPlayerLocal Deployer = null;

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        vehicleRB.drag = 0f;
        vehicleRB.angularDrag = 0f;
        vehicleRB.useGravity = false;
        vehicleRB.WakeUp();
    }

    public override AttachedToEntitySlotInfo GetAttachedToInfo(int _slotIdx)
    {
        var info = base.GetAttachedToInfo(_slotIdx);
        info.bKeep3rdPersonModelVisible = false;
        return info;
    }

    protected override void HandleNavObject()
    {
        // Don't show any nav object
    }

    protected override void DetachEntity(Entity _other)
    {
        Deployer = null;
        base.DetachEntity(_other);
        ForceDespawn();
    }

    protected override void Update()
    {
        base.Update();
        // Check if we have an attached player to update physics?
        if (GetAttachedPlayerLocal() is EntityPlayerLocal player)
        {
            vp_FPCamera cam = player.vp_FPCamera;
            vp_FPController ctr = player.vp_FPController;
            transform.position = cam.DrivingPosition + Vector3.down * 2;
            ctr.m_NonRetardedFallSpeed = vehicleRB.velocity.y * 2f;
        }
        else if (ConnectionManager.Instance.IsServer && !IsDriven())
        {
            // Make sure to clean-up all parachutes that are not driven
            // And hope this never applies to parachutes you actually want
            world.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);
        }

    }

    // protected override void onSpawnStateChanged() { base.onSpawnStateChanged(); }

    // public override void OnAddedToWorld() { base.OnAddedToWorld(); }

    // public override void PostInit() { base.PostInit(); }

    // protected override void Awake() { base.Awake(); }

    protected override void PhysicsInputMove()
    {

        if (vehicleRB == null) return;
        if (movementInput == null) return;

        vehicleRB.WakeUp();

        vehicleRB.velocity = new Vector3(
            vehicleRB.velocity.x * 0.95f,
            vehicleRB.velocity.y * 0.75f,
            vehicleRB.velocity.z * 0.95f
        );

        vehicleRB.angularVelocity = new Vector3(
            vehicleRB.angularVelocity.x * 0.98f,
            vehicleRB.angularVelocity.y * 0.98f,
            vehicleRB.angularVelocity.z * 0.98f
        );

        var rot = Quaternion.FromToRotation(transform.up, Vector3.up);

        float factor = 0.8f / Vector3.Dot(vehicleRB.transform.up,
            Quaternion.Euler(-30f, transform.rotation.eulerAngles.y, 0f) * Vector3.up);

        AddForce(
            new Vector3(0f, -factor, 0f),
            ForceMode.VelocityChange);

        var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        AddForce(
            forward * factor * factor * 0.15f,
            ForceMode.VelocityChange);

        vehicleRB.AddRelativeTorque(
            movementInput.moveForward * 0.01f,
            movementInput.moveStrafe * 0.03f,
            movementInput.moveStrafe * -0.02f,
            ForceMode.VelocityChange);

        // Dampen the torque to get upright again
        vehicleRB.AddTorque(new Vector3(rot.x, rot.y, rot.z) * 100f);

        // Physics.SyncTransforms();
    }

    protected override void SetWheelsForces(float motorTorque, float brakeTorque)
    {
    }

    protected override void UpdateWheelsSteering()
    {
    }

}

