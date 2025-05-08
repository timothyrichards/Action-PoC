use spacetimedb::{SpacetimeType, ReducerContext, Table, Identity};
use crate::types::{DbVector3};

#[derive(SpacetimeType, Clone, Debug)]
pub enum DbBuildingPieceType {
    Foundation,
    Wall,
    Floor,
    Stair,
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