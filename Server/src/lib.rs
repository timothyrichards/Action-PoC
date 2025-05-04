// Standard library imports (if any)

// External crate imports
use spacetimedb::{ReducerContext, Table};

// Local module declarations
mod types;

// Local module imports
use types::{DbVector3, DbVector2, DbAnimationState, WorldSpawn, world_spawn, Player, player};

#[spacetimedb::reducer(init)]
pub fn init(ctx: &ReducerContext) -> Result<(), String> {
    // Set up initial world spawn point
    ctx.db.world_spawn().insert(WorldSpawn {
        id: 0,
        position: DbVector3 { x: 0.0, y: 2.0, z: 0.0 },
        rotation: DbVector3 { x: 0.0, y: 0.0, z: 0.0 },
    });
    Ok(())
}

#[spacetimedb::reducer]
pub fn set_spawn_point(ctx: &ReducerContext, id: u32, x: f32, y: f32, z: f32, rx: f32, ry: f32, rz: f32) -> Result<(), String> {
    // Update the spawn point
    if let Some(mut spawn) = ctx.db.world_spawn().id().find(&id) {
        spawn.position = DbVector3 { x: x, y: y, z: z };
        spawn.rotation = DbVector3 { x: rx, y: ry, z: rz };
        ctx.db.world_spawn().id().update(spawn);
    }
    Ok(())
}

#[spacetimedb::reducer(client_connected)]
pub fn connect(ctx: &ReducerContext) -> Result<(), String> {
    log::debug!("{} just connected.", ctx.sender);
    ctx.db.player().insert(Player {
        identity: ctx.sender,
        player_id: 0,
        position: DbVector3 { x: 0.0, y: 0.0, z: 0.0 },
        rotation: DbVector3 { x: 0.0, y: 0.0, z: 0.0 },
        look_direction: DbVector2 { x: 0.0, y: 0.0 },
        animation_state: DbAnimationState {
            horizontal_movement: 0.0,
            vertical_movement: 0.0,
            look_yaw: 0.0,
            is_moving: false,
            is_turning: false,
            is_jumping: false,
            is_attacking: false,
        },
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
pub fn move_player(
    ctx: &ReducerContext, 
    position: DbVector3, 
    rotation: DbVector3,
    look_direction: DbVector2,
    animation_state: DbAnimationState,
) -> Result<(), String> {
    if let Some(mut player) = ctx.db.player().identity().find(ctx.sender) {
        player.position = position;
        player.rotation = rotation;
        player.look_direction = look_direction;
        player.animation_state = animation_state;
        ctx.db.player().identity().update(player);
        Ok(())
    } else {
        Err("Player not found".to_string())
    }
}
