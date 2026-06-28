# CLAUDE.md — Tannhauser

This file is the project handover. Claude Code reads it on every run. It defines what we are building, how we work together, what is locked, and what to build first. Read it fully before acting.

Note on naming: the game is **Tannhauser**. The on-disk project folder is `Tannhauser2` for historical reasons (the first folder was scrapped during a Unity version fix). The folder name does not matter and is not the game's title.

---

## 1. What this project is

A 3D space trading game, built in Unity, directed by a non-coder using Claude Code.

**Core fantasy:** fly a ship around a star system, trade cargo between stations for profit, take on missions, upgrade or buy ships, and eventually earn enough to fit a hyperdrive and jump to a new system.

**The long-term vision (NOT the near-term build):**
- One star system with multiple planets, each with bases on the surface and in orbit.
- Bases offer trade missions and combat or assassination missions.
- A market with prices that trend up and down.
- Cargo missions and shooter missions.
- Ship upgrades and ship purchasing.
- Different stations use different docking procedures.
- End goal: buy a hyperdrive, jump to a new system, earn from exploration data.

**Explicitly out of MVP:** no third-person, get-out-and-walk-around feature.

This is a learning project. The driver is learning game development with Claude and shipping a first playable game. Success is "finished something playable and learned a lot," not market size. Take the time it takes.

---

## 2. How we work together

I am the director, not the coder. I do not write C#. I read code at a "what does this do" level to sanity-check and steer. You write the code and explain it plainly.

**Operating rules:**
- **One step at a time.** Propose a step, let me confirm it is done, then move to the next. Do not run ahead.
- **Verdict first.** State the conclusion before the reasoning.
- **Plain language.** Explain what code does and why, briefly. No assuming C# knowledge.
- **No em dashes.** Use commas, periods, or parentheses.
- **Scannable output.** Clear headers, bold cues, tight bullets.
- **Flag the feel work.** When something can only be judged by playing it (flight feel, docking, combat weight), say so clearly and tell me what to test.
- **Challenge weak plans.** If I am scope-creeping or about to build the wrong thing, say so.

**Source control:** GitHub Flow. Feature branches, pull requests, squash-and-merge for clean history.

---

## 3. Locked technical decisions

Do not relitigate these without flagging a strong reason.

- **Engine:** Unity **6.3 LTS (6000.3.18f1)**. Stay on the LTS line. Do NOT move to the newest feature release (see Lessons, section 11: Unity 6.5 broke the community MCP tooling).
- **OS / environment:** Windows native, not WSL. Unity Editor and Claude Code both run on Windows.
- **Install layout:** Unity Editor installed on C (`C:\Program Files\Unity\Hub\...`). Project and asset libraries on D. Both drives are SSD, so this split is fine.
- **Project location:** `D:\Development\Unity\Tannhauser2` on the native Windows filesystem. Never under a WSL path.
- **Render pipeline:** Universal Render Pipeline (URP), Universal 3D template.
- **Code editor:** VS Code, set as Unity's External Script Editor. We do NOT use Visual Studio (it is not needed and is not installed).
- **AI control of the Editor:** **CoderGamester/mcp-unity** (free, fully local, no account), installed via git URL `https://github.com/CoderGamester/mcp-unity.git`. We deliberately did NOT use Unity's official AI Assistant MCP (see Lessons, section 11).
- **Asset tooling:** free packs first (Kenney, Quaternius, KayKit), procedural visuals coded by Claude, AI generators (Meshy/Tripo) and the Blender connector only when a specific gap appears.

---

## 4. Setup status

Done:
- [x] Unity Hub installed.
- [x] Unity 6.3 LTS (6000.3.18f1) installed (Windows module).
- [x] Project created: Tannhauser2, Universal 3D / URP, at `D:\Development\Unity\Tannhauser2`.
- [x] Git initialised, Unity `.gitignore` in place, pushed to GitHub (`https://github.com/davidmjackson/Tannhauser2.git`).
- [x] VS Code set as External Script Editor.
- [x] This CLAUDE.md committed to the project root.

Remaining to finish setup:
- [ ] Re-add the MCP package via git URL (`https://github.com/CoderGamester/mcp-unity.git`) and confirm the Console is clean (it should compile on 6.3 LTS).
- [ ] Open `Tools > MCP Unity > Server Window`, build the Node server if prompted, click Configure for Claude, then Start Server.
- [ ] **Cube test:** from a Claude Code session at the project root, ask to create a cube in the scene and confirm it appears in the Editor. This proves the loop and ends setup.

Stop after each step for my confirmation.

---

## 5. The build ladder (scope sequencing)

Build in this order. Do not start a rung until the one below feels good. This is the single most important discipline in the project.

- **Rung 1 — Vertical slice:** fly between two stations, dock at each, buy cargo at one and sell at the other for profit. Nothing else. (Full spec in section 6.)
- **Rung 2 — POC:** add 3 planets as backdrops with orbital stations, market price trends (numbers moving), and one cargo mission type.
- **Rung 3 — Toward MVP:** add basic combat and the shooter mission type, ship purchasing and upgrades, and varied docking procedures (only after one docking flow works end to end).
- **Rung 4 — v1:** hyperdrive and the jump to a second system. This is the natural "done" line.

---

## 6. Rung 1 spec (the first real target)

Goal: prove the core loop is fun and that flying and docking feel good. Everything here is placeholder-art quality on purpose.

**Scene contents:**
- One controllable ship.
- Two space stations, A and B, placed some distance apart in empty space.
- A skybox or starfield so space looks like space (you code this procedurally, no art asset needed).

**Mechanics, in build order:**
1. **Flight.** The ship moves and turns under player control in 3D. Tune until it feels good to fly. This is feel work; I will play and judge.
2. **Docking.** Approach a station, trigger a dock, and "arrive." One simple docking flow. Feel work again.
3. **Trade loop.** Docked at A, buy cargo at a price. Fly to B. Docked at B, sell that cargo at a higher price. Credits go up. Repeat.
4. **Minimal UI.** Show credits, current cargo, and a buy/sell panel when docked.

**Deliberately excluded from Rung 1:** planets, combat, missions, market trends, ship upgrades, multiple cargo types, varied docking. All parked.

---

## 7. Asset strategy

- **Rung 1:** Kenney space and UI kits as placeholder ship, stations, and UI. Procedural skybox/starfield coded by you. Zero Blender, zero AI generation.
- **Later rungs:** AI-generate hero pieces with Meshy or Tripo only when a free pack does not have what we need. Use the official Anthropic Blender connector to fix or rescale a model when required (treat it as "describe the fix," not "learn Blender").
- **Licensing:** keep a one-line note next to each asset folder recording source and licence. Prefer CC0 (Kenney, Quaternius, Poly Haven). Check each licence before anything ships.

---

## 8. Rung 1 acceptance tests (play-test checks)

Rung 1 is done only when all of these pass. The first two are judged by me playing, not by code review.

- [ ] Flying the ship feels good and responsive (my call after play-testing).
- [ ] Docking feels clear and satisfying (my call after play-testing).
- [ ] I can buy cargo at A, sell at B, and see credits increase.
- [ ] The buy/sell UI shows credits and cargo correctly and updates after a trade.
- [ ] A full loop (buy, travel, sell) can be done without errors.
- [ ] The project builds and runs as a Windows build, not just in the Editor.

---

## 9. Parked (do not build yet)

Kept safe here so the vision is not lost, but explicitly out of scope until their rung:
planets as landable or detailed bodies, market price trends, cargo mission system, combat and shooter missions, assassination missions, ship purchasing, ship upgrades, varied docking procedures per station, hyperdrive, second system, exploration data income.

If I ask for any of these during Rung 1, remind me they are parked and ask if I want to change scope on purpose.

---

## 10. Open items to resolve

- None blocking. Name (Tannhauser), engine (6.3 LTS), pipeline (URP), and MCP (CoderGamester) are all settled.

---

## 11. Lessons learned (do not repeat these)

- **Stick to Unity LTS. Avoid the newest feature release.** We started on Unity 6.5 (6000.5.1f1) and it cost hours. 6.5 replaced the 32-bit InstanceID with a 64-bit EntityId struct and turned the old InstanceID APIs into hard compile errors (CS0619), which broke every community Unity MCP package. Dropping to 6.3 LTS fixed it.
- **We rejected Unity's official AI Assistant MCP (`com.unity.ai.assistant`).** On a free Unity licence it showed "Up to 0 direct connections allowed" (a known 2.7.0 bug) and kept dropping external clients with "Connection revoked," appearing to require a paid AI seat. The free, fully-local CoderGamester MCP avoids all of that.
- **When installing a Unity Editor via Hub, untick Visual Studio Community.** It is not needed (we use VS Code), and its installer crashed the machine once. Only install the Windows build support modules.
- **Git auth note:** pushes authenticate via a stored token credential (`x-access-token`). If a future push suddenly asks to re-authenticate, the token likely expired; just re-auth then.
- **MCP Unity Server Window must stay OPEN.** Closing it stops the server and all Claude commands time out. It cannot be minimised (it is an Editor window, the minimise button is ghosted). To hide it while keeping it running, dock it as a tab behind the Console. Reopen via `Tools > MCP Unity > Server Window`.
- **Unity must be the foreground window to process Claude's commands.** The MCP package runs each command on Unity's next editor update tick (`EditorApplication.delayCall`), and a backgrounded Unity barely ticks, so commands queue and time out. Clicking into the terminal to message Claude sends Unity to the background, which is why commands stall. `Preferences > General > Interaction Mode > No Throttling` does NOT fix this (it only governs the foreground window). Working protocol: Claude announces "Running now" before any Editor command, and the director wiggles the mouse over the Unity window to wake it. Also note replies lag, so a "request timed out" error often means the command actually ran; Claude verifies by reading scene state rather than retrying blindly (which can create duplicates).
