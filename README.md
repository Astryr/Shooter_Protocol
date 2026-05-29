# Shooter Protocol

## Género del Juego
Shooter de Acción Rápida - FPS

## Objetivo del Juego
Sobrevivir y destruir a todos los enemigos en el nivel. El nivel se completa únicamente al eliminar a TODOS los enemigos (el contador de enemigos de la pantalla debe llegar a cero).

---

## Entrega 1 — IA básica

### Line of Sight (LoS)
Raycast desde cada enemigo. Paredes y obstáculos bloquean la visión. Implementado en `EnemyVision.cs` y en `Turret.cs`.

### Toma de decisiones (FSM)
Máquina de estados finitos en cada agente móvil.

### Agentes (3 tipos)

| Agente | Script | Estados FSM | Comportamientos |
|---|---|---|---|
| **Torreta** | `Turret.cs` | Apunta / Dispara | Estática, solo dispara con LoS |
| **Robot** | `Robot.cs` | Patrol, Chase | Patrulla, persigue si te ve, vuelve a patrullar si pierde LoS |
| **Fleeing Robot** | `FleeingRobot.cs` | Patrol, Attack, Flee | Patrulla, dispara con LoS, huye si te acercás |

### Spawn Gates
Generan robots adicionales en puntos del mapa (`SpawnGate.cs`).

### Controles
- **Movimiento:** `W` `A` `S` `D`
- **Mirar:** Mouse
- **Disparar:** Clic izquierdo
- **Zoom:** Clic derecho (armas compatibles)
- **Saltar:** Espacio

---

## Entrega 2 — Steering + Pathfinding

### Pathfinding (A*)
Unity **NavMesh** con algoritmo **A\*** interno. Los agentes llaman `NavMeshAgent.SetDestination()` y el sistema calcula la ruta alrededor de obstáculos.

Archivos: `EnemyMovement.cs`, `NavMeshAgent` en cada robot.

### Steering Behaviors (≥ 3)

Biblioteca: `Assets/Scripts/AI/SteeringBehaviors.cs`

| Behavior | Usado por | Estado FSM |
|---|---|---|
| **Arrive** | Robot, FleeingRobot | Patrol |
| **Pursue** | Robot | Chase |
| **Flee** | FleeingRobot | Flee |
| Seek, Wander, Evade | Disponibles en biblioteca / `EnemyMovement` | — |

### Integración (FSM → Steering → A*)

```
FSM elige estado (Patrol / Chase / Flee / Attack)
    ↓
SteeringBehaviors calcula velocidad deseada (micromovimiento)
    ↓
EnemyMovement proyecta destino y valida con NavMesh.SamplePosition
    ↓
NavMeshAgent.SetDestination → ruta A* (visible en Gizmos al seleccionar enemigo)
```

### Mapa con obstáculos
`MainLevel` usa NavMesh bakeado. Las rutas rodean paredes y geometría.

### Diversidad de agentes
- **Torreta:** reacción solo por LoS, sin movimiento.
- **Robot:** patrulla + persecución melee.
- **Fleeing Robot:** patrulla + combate a distancia + huida por proximidad.
- **Spawn Gate:** refuerzos dinámicos.

### Guía del editor
Instrucciones para agrandar el nivel, rebakear NavMesh y configurar enemigos: **[EDITOR_ENTREGA2.md](EDITOR_ENTREGA2.md)**

---

## Estructura de scripts de IA

```
Assets/Scripts/AI/
  SteeringBehaviors.cs   — Seek, Flee, Arrive, Wander, Pursue, Evade
  EnemyMovement.cs       — Integración steering + NavMesh A*
  EnemyVision.cs         — Line of Sight compartido

Assets/Scripts/Enemies/
  Robot.cs
  FleeingRobot.cs
  Turret.cs
  SpawnGate.cs
```

---

## Escenas

| Escena | Uso |
|---|---|
| **MainLevel.unity** | Nivel principal (usar para entrega y demo) |
| Main.unity | Escena alternativa / pruebas |
