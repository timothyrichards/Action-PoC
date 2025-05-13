use crate::types::DbVector3;
use spacetimedb::{Identity, ReducerContext, SpacetimeType, Table};

#[derive(SpacetimeType, Clone, Debug)]
pub enum DbBuildingPieceType {
    Foundation,
    Wall,
    Floor,
    Stair,
}

#[spacetimedb::table(name = building_piece_placed, public)]
pub struct DbBuildingPiecePlaced {
    #[primary_key]
    #[auto_inc]
    pub piece_id: u32,
    pub owner: Identity,
    pub variant_id: u32,
    pub position: DbVector3,
    pub rotation: DbVector3,
}

#[spacetimedb::reducer]
pub fn building_piece_place(
    ctx: &ReducerContext,
    variant_id: u32,
    position: DbVector3,
    rotation: DbVector3,
) -> Result<(), String> {
    let piece = DbBuildingPiecePlaced {
        piece_id: 0,
        owner: ctx.sender,
        variant_id,
        position,
        rotation,
    };
    ctx.db.building_piece_placed().insert(piece);
    Ok(())
}

#[spacetimedb::reducer]
pub fn building_piece_remove(ctx: &ReducerContext, piece_id: u32) -> Result<(), String> {
    // Only allow removal if the sender is the owner
    if let Some(piece) = ctx.db.building_piece_placed().piece_id().find(&piece_id) {
        if piece.owner == ctx.sender {
            ctx.db.building_piece_placed().piece_id().delete(&piece_id);
            Ok(())
        } else {
            Err("Only the owner can remove their building pieces".to_string())
        }
    } else {
        Err("Building piece not found".to_string())
    }
}
