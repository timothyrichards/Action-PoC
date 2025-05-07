// Standard library imports (if any)

// External crate imports
use spacetimedb::{ReducerContext, Table, Identity};

// Local module declarations
mod types;
mod tables;

// Local module imports
use types::{DbVector3, DbVector2, DbAnimationState, DbBuildingPieceType};
use tables::{WorldSpawn, world_spawn, Player, player, DbBuildingPiece, building_piece};

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
pub fn set_world_spawn(ctx: &ReducerContext, id: u32, x: f32, y: f32, z: f32, rx: f32, ry: f32, rz: f32) -> Result<(), String> {
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
    if ctx.db.player().identity().find(ctx.sender).is_none() {
        let (position, rotation) = if let Some(spawn) = ctx.db.world_spawn().id().find(&0) {
            (spawn.position, spawn.rotation)
        } else {
            (DbVector3 { x: 0.0, y: 0.0, z: 0.0 }, DbVector3 { x: 0.0, y: 0.0, z: 0.0 })
        };

        ctx.db.player().insert(Player {
            identity: ctx.sender,
            player_id: 0,
            online: true,
            position,
            rotation,
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
            health: 100.0,
            max_health: 100.0,
        });
    } else {
        if let Some(mut player) = ctx.db.player().identity().find(ctx.sender) {
            player.online = true;
            ctx.db.player().identity().update(player);
        }
    }
    Ok(())
}

#[spacetimedb::reducer(client_disconnected)]
pub fn disconnect(ctx: &ReducerContext) -> Result<(), String> {
    log::debug!("{} just disconnected.", ctx.sender);
    if let Some(mut player) = ctx.db.player().identity().find(ctx.sender) {
        player.online = false;
        ctx.db.player().identity().update(player);
    }
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

#[spacetimedb::reducer]
pub fn apply_damage(
    ctx: &ReducerContext,
    target_identity: Identity,
    damage: f32,
) -> Result<(), String> {
    if let Some(mut player) = ctx.db.player().identity().find(&target_identity) {
        player.health -= damage;
        if player.health < 0.0 {
            player.health = 0.0;
        }
        ctx.db.player().identity().update(player);
        Ok(())
    } else {
        Err("Player not found".to_string())
    }
}

#[spacetimedb::reducer]
pub fn reset_player_health(
    ctx: &ReducerContext,
    target_identity: Identity,
) -> Result<(), String> {
    if let Some(mut player) = ctx.db.player().identity().find(&target_identity) {
        player.health = player.max_health;
        ctx.db.player().identity().update(player);
        Ok(())
    } else {
        Err("Player not found".to_string())
    }
}

#[spacetimedb::reducer]
pub fn place_building_piece(
    ctx: &ReducerContext,
    index: u32,
    piece_type: DbBuildingPieceType,
    position: DbVector3,
    rotation: DbVector3,
) -> Result<(), String> {
    ctx.db.building_piece().insert(DbBuildingPiece {
        piece_id: 0,
        owner: ctx.sender,
        piece_type,
        index,
        position,
        rotation,
    });
    Ok(())
}

#[spacetimedb::reducer]
pub fn remove_building_piece(
    ctx: &ReducerContext,
    piece_id: u32,
) -> Result<(), String> {
    // Only allow removal if the sender is the owner
    if let Some(piece) = ctx.db.building_piece().piece_id().find(&piece_id) {
        if piece.owner == ctx.sender {
            ctx.db.building_piece().piece_id().delete(&piece_id);
            Ok(())
        } else {
            Err("Only the owner can remove their building pieces".to_string())
        }
    } else {
        Err("Building piece not found".to_string())
    }
}
