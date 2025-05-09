---

kanban-plugin: board

---

## Backlog

- [ ] Add more animations to movement (like diagonal strafing) using these: https://quaternius.com/packs/universalanimationlibrary.html
- [ ] Update building pieces with real assets instead of prototype assets: https://quaternius.com/packs/medievalvillagemegakit.html
- [ ] Setup server authoritative multiplayer, options are Rapier physics simulation on STDB, or a sidecar unity server client.


## Next

- [ ] Create a better way to track online vs offline players since making queries from the CLI or accessing the web portal keeps creating ghost players.
- [ ] Add multiple attacks so you can have attack chains


## In Progress

- [ ] Go through classes and decouple them as much as possible


## Done

- [ ] Improve the way building pieces are stored in the database so we're not just using an arbitrary index that can change at any moment on the client
- [ ] Add multiple building piece options for each category
- [ ] Add UI to the building UI that shows the current anchor state
- [ ] Add auto as an option in the anchor cycling rotation and disable logic that tries to determine when to allow manual switching vs auto anchor switching
- [ ] Add automatic anchor detection to build mode preview piece


***

## Archive

- [ ] Basic 3rd person movement implementation
- [ ] Basic combat implementation
- [ ] Basic building implementation
- [ ] Network enabled movement
- [ ] Network enabled combat
- [ ] Network enabled building

%% kanban:settings
```
{"kanban-plugin":"board","list-collapse":[false,false,false,false]}
```
%%