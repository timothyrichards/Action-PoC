use crate::modules::building_piece_variant::building_piece_variant_get;
use crate::modules::inventory::{inventory_add_item, inventory_get_item, inventory_remove_item};
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
    // Get the building piece variant to check its cost
    let variant = building_piece_variant_get(ctx, variant_id)?;

    // Check if player has all required materials
    for cost in &variant.build_cost {
        let inventory = inventory_get_item(ctx, cost.item_id)?;

        if inventory.quantity < cost.quantity {
            return Err("Not enough materials to build this piece".to_string());
        }
    }

    // Remove the materials from inventory
    for cost in &variant.build_cost {
        inventory_remove_item(ctx, cost.item_id, cost.quantity)?;
    }

    // Place the building piece
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
            // Get the building piece variant to refund materials
            let variant = building_piece_variant_get(ctx, piece.variant_id)?;

            // Refund the materials
            for cost in &variant.build_cost {
                inventory_add_item(ctx, cost.item_id, cost.quantity)?;
            }

            ctx.db.building_piece_placed().piece_id().delete(&piece_id);
            Ok(())
        } else {
            Err("Only the owner can remove their building pieces".to_string())
        }
    } else {
        Err("Building piece not found".to_string())
    }
}
