use crate::modules::building_piece_placed::DbBuildingPieceType;
use spacetimedb::{ReducerContext, SpacetimeType, Table};

#[derive(SpacetimeType, Clone, Debug)]
pub struct DbBuildingCost {
    pub item_id: u32,
    pub quantity: u32,
}

#[spacetimedb::table(name = building_piece_variant, public)]
pub struct DbBuildingPieceVariant {
    #[primary_key]
    pub variant_id: u32,
    pub piece_type: DbBuildingPieceType,
    pub variant_name: String,
    pub build_cost: Vec<DbBuildingCost>,
    pub max_health: f32,
}

pub fn building_piece_variant_get(
    ctx: &ReducerContext,
    variant_id: u32,
) -> Result<DbBuildingPieceVariant, String> {
    let variant = ctx
        .db
        .building_piece_variant()
        .variant_id()
        .find(&variant_id)
        .ok_or("Building piece variant not found")?;

    Ok(variant)
}

pub fn building_piece_variant_init(ctx: &ReducerContext) -> Result<(), String> {
    foundation_variants(ctx)?;
    floor_variants(ctx)?;
    wall_variants(ctx)?;
    stair_variants(ctx)?;
    Ok(())
}

fn foundation_variants(ctx: &ReducerContext) -> Result<(), String> {
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 0,
            piece_type: DbBuildingPieceType::Foundation,
            variant_name: "Square".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 1,
            piece_type: DbBuildingPieceType::Foundation,
            variant_name: "Triangle".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    Ok(())
}

fn floor_variants(ctx: &ReducerContext) -> Result<(), String> {
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 2,
            piece_type: DbBuildingPieceType::Floor,
            variant_name: "Floor".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 3,
            piece_type: DbBuildingPieceType::Floor,
            variant_name: "Half Floor".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 4,
            piece_type: DbBuildingPieceType::Floor,
            variant_name: "Quarter Floor".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 5,
            piece_type: DbBuildingPieceType::Floor,
            variant_name: "Triangle".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    Ok(())
}

fn wall_variants(ctx: &ReducerContext) -> Result<(), String> {
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 6,
            piece_type: DbBuildingPieceType::Wall,
            variant_name: "Wall".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 7,
            piece_type: DbBuildingPieceType::Wall,
            variant_name: "Half Wall".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 8,
            piece_type: DbBuildingPieceType::Wall,
            variant_name: "Quarter Wall".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 9,
            piece_type: DbBuildingPieceType::Wall,
            variant_name: "Door".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 10,
            piece_type: DbBuildingPieceType::Wall,
            variant_name: "Window".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    Ok(())
}

fn stair_variants(ctx: &ReducerContext) -> Result<(), String> {
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 11,
            piece_type: DbBuildingPieceType::Stair,
            variant_name: "Stair".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    ctx.db
        .building_piece_variant()
        .insert(DbBuildingPieceVariant {
            variant_id: 12,
            piece_type: DbBuildingPieceType::Stair,
            variant_name: "Half Stair".to_string(),
            build_cost: vec![DbBuildingCost {
                item_id: 0,
                quantity: 5,
            }],
            max_health: 100.0,
        });
    Ok(())
}
