# Shooter Protocol

FPS de acción — proyecto Unity 6 (URP). Nivel principal: **MainLevel**.

## Objetivo

Eliminar a todos los enemigos del nivel. El contador **Enemies Left** debe llegar a **0** para ganar.

## Controles

| Acción | Tecla |
|---|---|
| Movimiento | `W` `A` `S` `D` |
| Mirar | Mouse |
| Disparar | Clic izquierdo |
| Zoom | Clic derecho |
| Saltar | Espacio |

---

# Arquitectura de IA del proyecto

## Visión general

Cada agente hostil usa una **FSM** (máquina de estados finitos) para decidir qué hacer. La **percepción** usa **Line of Sight** (raycast). El **movimiento** combina:

1. **Steering Behaviors** — dirección y velocidad deseada (micromovimiento).
2. **Pathfinding A\*** — Unity NavMesh calcula la ruta válida alrededor de obstáculos.
3. **NavMeshAgent** — ejecuta la ruta en el mapa.

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────────┐     ┌──────────────┐
│  FSM        │ ──► │ SteeringBehaviors │ ──► │ EnemyMovement       │ ──► │ NavMesh A*   │
│  (decisión) │     │ (Arrive/Pursue/   │     │ (proyecta destino)  │     │ SetDestination│
│             │     │  Flee, etc.)      │     │                     │     │              │
└─────────────┘     └──────────────────┘     └─────────────────────┘     └──────────────┘
```

Archivos centrales:

| Archivo | Rol |
|---|---|
| `Assets/Scripts/AI/SteeringBehaviors.cs` | Biblioteca: Seek, Flee, Arrive, Wander, Pursue, Evade |
| `Assets/Scripts/AI/EnemyMovement.cs` | Integración steering → destino NavMesh |
| `Assets/Scripts/AI/EnemyVision.cs` | Line of Sight compartido (raycast) |
| `Assets/Scripts/AI/SteeringAgent.cs` | Componente legado/auxiliar (no requerido en prefabs actuales) |

---

## Por tipo de agente

### 1. Torreta (`Turret.cs`)

| Aspecto | Detalle |
|---|---|
| **Movimiento** | Ninguno (agente estático) |
| **Pathfinding** | No aplica |
| **Steering** | No aplica |
| **FSM** | Apuntar al jugador → disparar si hay LoS |
| **LoS** | Raycast desde el cañón; paredes bloquean |
| **Comportamientos** | Rastrear (LookAt), disparar, esperar entre disparos |

### 2. Robot (`Robot.cs`)

| Aspecto | Detalle |
|---|---|
| **FSM** | `Patrol` \| `Chase` |
| **Steering en Patrol** | **Arrive** (desacelera al llegar al waypoint) |
| **Steering en Chase** | **Pursue** (predice posición del jugador) |
| **Pathfinding** | NavMesh **A\*** hacia waypoint o jugador |
| **LoS** | `EnemyVision` — entra en Chase solo con visión |
| **Comportamientos** | Patrullar, esperar en punto, perseguir, volver a patrullar al perder LoS |
| **Spawn** | Colocado en escena o generado por **Spawn Gate** |
| **Auto-config** | `ApplyLargeMapPatrolProfile` en Awake (mapa grande) |

### 3. Fleeing Robot (`FleeingRobot.cs`)

| Aspecto | Detalle |
|---|---|
| **FSM** | `Patrol` \| `Attack` \| `Flee` |
| **Steering en Patrol** | **Arrive** |
| **Steering en Flee** | **Flee** (huida rápida, sin depender de LoS) |
| **Pathfinding** | NavMesh **A\*** |
| **LoS** | Solo para disparar (Attack); Flee por proximidad |
| **Comportamientos** | Patrullar, disparar mientras te ve, huir si te acercás |

### 4. Spawn Gate (`SpawnGate.cs`)

| Aspecto | Detalle |
|---|---|
| **Rol** | Diversidad en el mapa — genera **Robots** en el tiempo |
| **Prefab** | `Robot.prefab` |
| **Parámetros** | `maxSpawns`, `spawnTime`, `spawnPoint` |

---

## Steering Behaviors implementados

| Behavior | En código | Usado en juego por |
|---|---|---|
| Seek | Sí | Disponible vía `EnemyMovement` |
| Flee | Sí | FleeingRobot (Flee) |
| Arrive | Sí | Robot y FleeingRobot (Patrol) |
| Wander | Sí | Disponible vía `EnemyMovement` |
| Pursue | Sí | Robot (Chase) |
| Evade | Sí | Disponible vía `EnemyMovement` |

**Mínimo Entrega 2 (≥3 en uso):** Arrive, Pursue, Flee.

---

## Pathfinding

- **Algoritmo:** **A\*** (Unity `NavMeshAgent`, paquete AI Navigation).
- **Mapa:** `MainLevel` con NavMesh bakeado; rutas rodean obstáculos.
- **Visualización:** seleccionar un Robot o FleeingRobot en Play → Gizmos de ruta (cyan/naranja) y steering (verde/rojo).

---

# Cumplimiento Entrega 1 y 2

| Requisito | Estado |
|---|---|
| Escena jugable + jugador | OK — `MainLevel` en Build Settings |
| ≥3 agentes con IA | OK — Torreta, Robot, FleeingRobot (+ Spawn Gate) |
| Line of Sight | OK — `EnemyVision`, `Turret` |
| FSM | OK — cada enemigo móvil |
| ≥3 comportamientos por agente | OK — ver tablas arriba |
| Estética coherente | OK — assets GDTV / tema sci-fi |
| ≥3 Steering Behaviors | OK — Arrive, Pursue, Flee (+ biblioteca completa) |
| Pathfinding A* | OK — NavMesh |
| Integración FSM + steering + path | OK — `EnemyMovement` |
| Mapa con obstáculos / NavMesh | OK — requiere **Bake** tras editar geometría |
| Agentes distintos entre sí | OK — estático / melee / ranged+flee |
| README con arquitectura IA | OK — este documento |

---

# Entregables

## 1. Link de Git

Subir el proyecto completo a GitHub/GitLab y entregar la URL del repositorio.

**No subir:** carpetas `Library/`, `Temp/`, `Logs/`, `obj/`, builds locales (ya están en `.gitignore`).

## 2. Documento / README

Este archivo cumple el documento de arquitectura de IA. Complemento de editor: **[EDITOR_ENTREGA2.md](EDITOR_ENTREGA2.md)**.

## 3. Build de entrega

- Escena de entrega: **Assets/Scenes/MainLevel.unity**
- Verificar en **File → Build Settings** que MainLevel esté habilitada.

---

# Estructura de scripts

```
Assets/Scripts/
├── AI/
│   ├── SteeringBehaviors.cs
│   ├── EnemyMovement.cs
│   ├── EnemyVision.cs
│   └── SteeringAgent.cs
├── Enemies/
│   ├── Robot.cs
│   ├── FleeingRobot.cs
│   ├── Turret.cs
│   ├── SpawnGate.cs
│   ├── EnemyHealth.cs
│   ├── Projectile.cs
│   └── Explosion.cs
├── Player/
├── Pickups/
└── Misc/
    └── GameManager.cs
```

---

# Guías

- **Editor (agregar enemigos, agrandar mapa, NavMesh):** [EDITOR_ENTREGA2.md](EDITOR_ENTREGA2.md)
