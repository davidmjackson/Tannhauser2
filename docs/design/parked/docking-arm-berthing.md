> **Status: PARKED (future feature).** Not in scope for Rung 1 or the MVP.
> Earliest fit is Rung 3 ("varied docking procedures per station"), and only
> after the simple Rung 1 docking flow is locked in. Note that the walkable
> pressurised tunnel (crew traversing on foot) crosses the explicit MVP
> exclusion in CLAUDE.md ("no third-person, get-out-and-walk-around feature"),
> so adopting this is a deliberate redefinition of the MVP, not a normal rung
> step. Preserved here verbatim so the vision and open questions are not lost.
>
> Captured: 2026-06-28.

# Feature: Docking Arm (Berthing System)

## Concept
A docking arm is a station-side structure that captures and secures a player 
ship, then provides a walkable pressurised tunnel for crew and cargo transfer. 
This is "berthing" rather than "docking": the arm reaches out and grabs the 
ship, the ship does not fly itself into a fixed port.

Real-world analogy: an airport jet bridge combined with the ISS Canadarm2 
capture-and-berth process.

## Two Core Functions
1. **Capture and secure** — the arm detects a ship in range, extends/aligns, 
   and clamps onto a docking collar on the ship's hull, holding it rigid.
2. **Access** — once secured, the arm becomes a sealed walk-through tunnel. 
   Crew traverse it on foot; cargo/goods transfer through it.

## Components (suggested naming)
- **Docking arm / gantry** — the extending structural element.
- **Capture latches / docking clamps** — grab and lock the ship.
- **Docking collar** — the sealed ring where arm and ship hull mate.
- **Transfer tube / gangway** — the interior walkway crew move through.

## Behaviour Sequence
1. Ship enters detection range of the arm.
2. Arm signals readiness / requests alignment within tolerance.
3. Ship holds position within the capture envelope.
4. Arm extends and clamps onto the ship's docking collar.
5. Ship is held rigid (movement locked, physics constrained to the arm).
6. Tunnel seals and pressurises; gangway becomes walkable.
7. Crew and cargo transfer.
8. Reverse sequence on undock: depressurise, release latches, retract arm.

## Open Questions for Implementation
- Does the arm move to the ship, the ship to the arm, or both meet in a 
  tolerance zone?
- How does this interface with the existing socket-based ship architecture? 
  (The docking collar is likely itself a socket/attachment point.)
- Is the tunnel a fixed mesh that extends, or procedurally bridged between 
  two collar points?
- Player-controlled approach, or assisted/auto-align once in range?
