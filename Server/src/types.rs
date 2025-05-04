// External crate imports
use spacetimedb::{Identity, SpacetimeType};

#[derive(SpacetimeType, Clone, Debug)]
pub struct DbVector3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(SpacetimeType, Clone, Debug)]
pub struct DbVector2 {
    pub x: f32,
    pub y: f32,
}

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

#[spacetimedb::table(name = world_spawn, public)]
pub struct WorldSpawn {
    #[primary_key]
    pub id: u32,
    pub position: DbVector3,
    pub rotation: DbVector3,
}

#[spacetimedb::table(name = player, public)]
pub struct Player {
    #[primary_key]
    pub identity: Identity,
    #[unique]
    #[auto_inc]
    pub player_id: u32,
    pub position: DbVector3,
    pub rotation: DbVector3,
    pub look_direction: DbVector2,
    pub animation_state: DbAnimationState,
}
