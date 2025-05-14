---

kanban-plugin: board

---

## Backlog

- [ ] Add more animations to movement (like diagonal strafing) using these: https://quaternius.com/packs/universalanimationlibrary.html
- [ ] Update building pieces with real assets instead of prototype assets: https://quaternius.com/packs/medievalvillagemegakit.html
- [ ] Setup server authoritative multiplayer, options are Rapier physics simulation on STDB, or a sidecar unity server client.


## Next

- [ ] Make gathering materials server authoritative
- [ ] Make gathering nodes respawn


## In Progress



## Done

**Complete**
- [x] Add material requirements to building
- [x] Add gatherable items that can be picked up into the inventory (start with sticks so that can be used as a building material)
- [x] Create a character inventory system


***

## Archive

- [x] Add multiple attacks so you can have attack chains
- [x] Create a better way to track online vs offline players since making queries from the CLI or accessing the web portal keeps creating ghost players.
- [x] Go through classes and decouple them as much as possible
- [x] Improve the way building pieces are stored in the database so we're not just using an arbitrary index that can change at any moment on the client
- [x] Add multiple building piece options for each category
- [x] Add UI to the building UI that shows the current anchor state
- [x] Add auto as an option in the anchor cycling rotation and disable logic that tries to determine when to allow manual switching vs auto anchor switching
- [x] Add automatic anchor detection to build mode preview piece
- [x] Basic 3rd person movement implementation
- [x] Basic combat implementation
- [x] Basic building implementation
- [x] Network enabled movement
- [x] Network enabled combat
- [x] Network enabled building

%% kanban:settings
```
{"kanban-plugin":"board","list-collapse":[false,false,false,false],"new-card-insertion-method":"prepend","show-checkboxes":true}
```
%%