use spacetimedb::{ReducerContext, Table, Identity, SpacetimeType};

#[spacetimedb::table(name = player, public)]
pub struct Player {
    #[primary_key]
    identity: Identity,
    #[unique]
    #[auto_inc]
    player_id: u32,
    position: DbVector3,
    rotation: DbVector3,
}

#[spacetimedb::reducer(client_connected)]
pub fn connect(ctx: &ReducerContext) -> Result<(), String> {
    log::debug!("{} just connected.", ctx.sender);
    ctx.db.player().insert(Player {
        identity: ctx.sender,
        player_id: 0,
        position: DbVector3 { x: 0.0, y: 0.0, z: 0.0 },
        rotation: DbVector3 { x: 0.0, y: 0.0, z: 0.0 },
    });
    Ok(())
}

#[spacetimedb::reducer(client_disconnected)]
pub fn disconnect(ctx: &ReducerContext) -> Result<(), String> {
    log::debug!("{} just disconnected.", ctx.sender);
    ctx.db.player().identity().delete(&ctx.sender);
    Ok(())
}

#[spacetimedb::reducer]
pub fn move_player(ctx: &ReducerContext, position: DbVector3, rotation: DbVector3) -> Result<(), String> {
    if let Some(mut player) = ctx.db.player().identity().find(ctx.sender) {
        player.position = position;
        player.rotation = rotation;
        ctx.db.player().identity().update(player);
        Ok(())
    } else {
        Err("Player not found".to_string())
    }
}

#[derive(SpacetimeType, Clone, Debug)]
pub struct DbVector3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}