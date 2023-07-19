public class NetPkgDeployParachute : NetPackageVehicleSpawn
{

    // Package is only sent from client to the server
    public override void ProcessPackage(World _world, GameManager _callbacks)
    {
        // Code below is copied 1 to 1 from original
        // Only change: we keep a reference to player
        if (_world == null) return;
        if (EntityFactory.CreateEntity(entityType, pos, rot) is EntityVehicle entity)
        {
            entity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);
            entity.GetVehicle().SetItemValue(itemValue.Clone());
            var player = (GameManager.Instance.World.GetEntity(entityThatPlaced) as EntityPlayer);
            if (player != null)
            {
                entity.Spawned = true;
                ClientInfo clientInfo = ConnectionManager.Instance.Clients.ForEntityId(entityThatPlaced);
                entity.SetOwner(clientInfo.InternalId);
            }
            _world.SpawnEntityInWorld(entity);
            entity.bPlayerStatsChanged = true;
            // Following our custom additions
            if (player == null) return;
            if (entity == null) return;
            // Attach to parachute right away
            player.StartAttachToEntity(entity, 0);
        }
    }
}
