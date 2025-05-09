use crate::modules::world_spawn::world_spawn;
use crate::types::{DbVector2, DbVector3};
use spacetimedb::{Identity, ReducerContext, SpacetimeType, Table};

#[derive(SpacetimeType, Clone, Debug)]
pub struct DbAnimationState {
    pub horizontal_movement: f32,
    pub vertical_movement: f32,
    pub look_yaw: f32,
    pub is_moving: bool,
    pub is_turning: bool,
    pub is_jumping: bool,
    pub is_attacking: bool,
}

#[spacetimedb::table(name = player, public)]
pub struct Player {
    #[primary_key]
    pub identity: Identity,
    #[unique]
    #[auto_inc]
    pub player_id: u32,
    #[index(btree)]
    pub online: bool,
    pub position: DbVector3,
    pub rotation: DbVector3,
    pub look_direction: DbVector2,
    pub animation_state: DbAnimationState,
    pub health: f32,
    pub max_health: f32,
}

pub fn create_player(ctx: &ReducerContext) -> Result<(), String> {
    let (position, rotation) = if let Some(spawn) = ctx.db.world_spawn().id().find(&0) {
        (spawn.position, spawn.rotation)
    } else {
        (
            DbVector3 {
                x: 0.0,
                y: 0.0,
                z: 0.0,
            },
            DbVector3 {
                x: 0.0,
                y: 0.0,
                z: 0.0,
            },
        )
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
    Ok(())
}

pub fn set_player_online_status(ctx: &ReducerContext, online: bool) -> Result<(), String> {
    if let Some(mut player) = ctx.db.player().identity().find(ctx.sender) {
        player.online = online;
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
pub fn reset_player_health(ctx: &ReducerContext, target_identity: Identity) -> Result<(), String> {
    if let Some(mut player) = ctx.db.player().identity().find(&target_identity) {
        player.health = player.max_health;
        ctx.db.player().identity().update(player);
        Ok(())
    } else {
        Err("Player not found".to_string())
    }
}
