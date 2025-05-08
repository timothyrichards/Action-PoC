// External crate imports
use spacetimedb::{ReducerContext};

// Local module declarations
mod types;
mod modules;

// Local module imports
use crate::modules::player::player;
use crate::modules::creative_camera::creative_camera;

#[spacetimedb::reducer(init)]
pub fn init(ctx: &ReducerContext) -> Result<(), String> {
    modules::world_spawn::set_world_spawn(ctx, 0, 0.0, 2.0, 0.0, 0.0, 0.0, 0.0)?;
    Ok(())
}

#[spacetimedb::reducer(client_connected)]
pub fn connect(ctx: &ReducerContext) -> Result<(), String> {
    log::debug!("{} just connected.", ctx.sender);
    
    // Create or update player
    if ctx.db.player().identity().find(ctx.sender).is_none() {
        modules::player::create_player(ctx)?;
    } else {
        modules::player::set_player_online_status(ctx, true)?;
    }

    // Create or update creative camera
    if ctx.db.creative_camera().identity().find(ctx.sender).is_none() {
        modules::creative_camera::create_creative_camera(ctx)?;
    } else {
        modules::creative_camera::set_creative_camera_enabled(ctx, false)?;
    }
    
    Ok(())
}

#[spacetimedb::reducer(client_disconnected)]
pub fn disconnect(ctx: &ReducerContext) -> Result<(), String> {
    log::debug!("{} just disconnected.", ctx.sender);
    modules::player::set_player_online_status(ctx, false)
}