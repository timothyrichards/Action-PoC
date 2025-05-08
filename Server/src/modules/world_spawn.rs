use spacetimedb::{ReducerContext, Table};
use crate::types::DbVector3;

#[spacetimedb::table(name = world_spawn, public)]
pub struct WorldSpawn {
    #[primary_key]
    pub id: u32,
    pub position: DbVector3,
    pub rotation: DbVector3,
}

#[spacetimedb::reducer]
pub fn set_world_spawn(ctx: &ReducerContext, id: u32, x: f32, y: f32, z: f32, rx: f32, ry: f32, rz: f32) -> Result<(), String> {
    if let Some(mut spawn) = ctx.db.world_spawn().id().find(&id) {
        spawn.position = DbVector3 { x, y, z };
        spawn.rotation = DbVector3 { x: rx, y: ry, z: rz };
        ctx.db.world_spawn().id().update(spawn);
    }
    else {
        ctx.db.world_spawn().insert(WorldSpawn {
            id: 0,
            position: DbVector3 { x: 0.0, y: 2.0, z: 0.0 },
            rotation: DbVector3::default(),
        });
    }
    Ok(())
}
