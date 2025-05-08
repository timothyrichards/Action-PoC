use spacetimedb::{Identity};
use crate::types::{DbVector3, DbVector2, DbAnimationState, DbBuildingPieceType};

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
    pub online: bool,
    pub position: DbVector3,
    pub rotation: DbVector3,
    pub look_direction: DbVector2,
    pub animation_state: DbAnimationState,
    pub health: f32,
    pub max_health: f32,
}

#[spacetimedb::table(name = creative_camera, public)]
pub struct CreativeCamera {
    #[primary_key]
    pub identity: Identity,
    pub enabled: bool,
    pub position: DbVector3,
    pub rotation: DbVector3,
}

#[spacetimedb::table(name = building_piece, public)]
pub struct DbBuildingPiece {
    #[primary_key]
    #[auto_inc]
    pub piece_id: u32,
    pub owner: Identity,
    pub index: u32,
    pub piece_type: DbBuildingPieceType,
    pub position: DbVector3,
    pub rotation: DbVector3,
} 