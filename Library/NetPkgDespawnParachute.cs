public class NetPkgDespawnParachute : NetPackage
{
    private int entityId;

    public NetPkgDespawnParachute Setup(int _entityId)
    {
        entityId = _entityId;
        return this;
    }

    public override void read(PooledBinaryReader _br)
    {
        entityId = _br.ReadInt32();
    }

    public override void write(PooledBinaryWriter _bw)
    {
        base.write(_bw);
        _bw.Write(entityId);
    }

    // Package is only sent from client to the server
    public override void ProcessPackage(World _world, GameManager _callbacks)
    {
        if (_world == null) return;
        if (_world.IsRemote()) throw new System.Exception("Wrong context");
        _world.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);
        EntityVParachute.Deployer = null;
    }

    public override int GetLength() => 8;
}
