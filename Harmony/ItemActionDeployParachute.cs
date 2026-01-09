using UnityEngine;

class ItemActionDeployParachute : ItemAction
{
    public bool ConsumeItem;
    public bool DisableHotKey;
    public override void ReadFrom(DynamicProperties props)
    {
        base.ReadFrom(props);
        ConsumeItem = props.Values.ContainsKey("ConsumeItem") ?
            bool.Parse(props.Values["ConsumeItem"]) : false;
        DisableHotKey = props.Values.ContainsKey("DisableHotKey") ?
            bool.Parse(props.Values["DisableHotKey"]) : false;
        // Not the nicest way to do it, but works for the POC
        if (DisableHotKey) OcbParachute.ParachuteHotKey = KeyCode.None;
    }

    public override void ExecuteAction(ItemActionData action, bool released)
    {
        if (!released || IsActionRunning(action)) return;
        if (!(action.invData.holdingEntity is EntityPlayerLocal player)) return;
        if (player.Buffs.HasBuff("buffOcbParachute")) DeployParachute(action, player);
        else if (player.PlayerUI?.xui?.CollectedItemList is XUiC_CollectedItemList list)
        {
            var iv = ItemClass.GetItem("modOcbArmorParachute");
            if (iv != null) list.AddItemStack(new ItemStack(iv, 0));
        }
    }

    private void DeployParachute(ItemActionData action, EntityPlayerLocal player)
    {
        // Don't deploy if on ground
        if (player.onGround) return;
        // Load the parachute vehicle only once
        if (EntityVParachute.Deployer != null) return;
        // Only deploy if we have a bit of speed to actually deploy it
        if (player.vp_FPController.m_FallSpeed > -0.15f) return;
        // Reduce item by one if option is set
        // if (ConsumeItem) action.invData.holdingEntity
        //         .inventory.DecHoldingItem(1);
        // Execute parachute deployment
        OcbParachute.DeployParachute(player);

        var xui = LocalPlayerUI.GetUIForPlayer(player).xui;
        var wm = xui.playerUI.windowManager;
        wm.Open("parachuteInfo", false, true/*, true, false*/);

    }

}
