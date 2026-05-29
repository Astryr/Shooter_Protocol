# Shooter Protocol

Shooter en primera persona para Unity 6 (URP). El jugador recorre un nivel sci-fi, elimina a los agentes hostiles y gana cuando el contador **Enemies Left** llega a cero. La inteligencia artificial forma parte del gameplay: cada enemigo percibe al jugador, decide en una FSM y se desplaza con steering behaviors y pathfinding sobre NavMesh.

## Datos del proyecto

| Campo | Valor |
|---|---|
| Motor | Unity 6 (`6000.3.11f1` — ver `ProjectSettings/ProjectVersion.txt`) |
| Escena de entrega | `Assets/Scenes/MainLevel.unity` |
| Render pipeline | Universal Render Pipeline |
| Input | Input System Package (ver `Edit → Project Settings → Player`) |
| Objetivo | Reducir **Enemies Left** a `0` eliminando todos los enemigos con `EnemyHealth` |

## Controles

| Acción | Entrada |
|---|---|
| Movimiento | `W` `A` `S` `D` |
| Mirar | Mouse |
| Disparar | Clic izquierdo |
| Zoom (francotirador) | Clic derecho |
| Saltar | Espacio |
| Pistola | `1` |
| Ametralladora | `2` |
| Francotirador | `3` |
| Pausa | `ESC` (botones **Resume** y **Quit**) |

### Armas y supervivencia

- El jugador inicia con las tres armas (teclas `1`–`3`). Cada arma tiene cargador de tamaño fijo; al vaciarlo aparece `RLD` y tras un breve tiempo se recarga solo.
- No hay pickups de munición en el nivel.
- Las cajas del mapa restauran vida (`HealthPickup`, +2 HP por defecto). Prefab: `Assets/Prefabs/Pickups/Health Pickup.prefab`.

### Victoria y pausa

- **Victoria:** cada enemigo con `EnemyHealth` suma `+1` al contador al aparecer y `-1` al morir. Con `0` enemigos se muestra **YOU WIN!**
- **Pausa:** `ESC` congela el juego (`timeScale = 0`) y muestra overlay con **Resume** y **Quit**. En el Editor, **Quit** detiene Play Mode.

---

## Cumplimiento de consignas

### Entrega 1 — Clase 7

| Requisito | Estado | Implementación |
|---|---|---|
| Escena jugable con jugador controlable | Cumplido | `MainLevel.unity`, Starter Assets FPS, armas y vida |
| Al menos 3 agentes con IA integrada | Cumplido | Torreta, Robot, Fleeing Robot (+ Spawn Gate y agentes E2) |
| Line of Sight que influya en el comportamiento | Cumplido | `EnemyVision.cs`, LoS en torreta y transiciones FSM móviles |
| Sistema de decisión (FSM u otro) | Cumplido | FSM explícita en cada agente móvil; torreta con ciclo apuntar/disparar |
| Al menos 3 conductas por agente | Cumplido | Ver tabla por agente más abajo |
| Identidad visual coherente | Cumplido | Pack GDTV + esferas de color por tipo de enemigo |

### Entrega 2 — Clase 16 / 17

| Requisito | Estado | Implementación |
|---|---|---|
| Entrega 1 sigue funcionando | Cumplido | Comportamientos E1 preservados en scripts base |
| Al menos 3 steering behaviors | Cumplido | 6 en `SteeringBehaviors.cs`; 5+ en uso en runtime |
| Pathfinding (A*, Dijkstra o Theta*) | Cumplido | **A\*** vía `NavMeshAgent` (Unity AI Navigation) |
| Integración decisión + steering + path | Cumplido | FSM → `SteeringBehaviors` → `EnemyMovement` → NavMesh |
| Mapa con obstáculos y navegación | Cumplido | NavMesh en `MainLevel` (requiere bake si se edita geometría) |
| Al menos 3 agentes complementarios | Cumplido | Charger Robot y Sniper Robot (más diversidad en escena) |
| README con arquitectura por agente | Cumplido | Este documento |

**Steering en uso en runtime:** Arrive, Pursue, Flee, Wander, Seek, Evade.

---

## Arquitectura de IA

```
FSM (decisión)  →  SteeringBehaviors (velocidad deseada)  →  EnemyMovement (destino en NavMesh)  →  NavMesh A* (ruta)
```

| Archivo | Rol |
|---|---|
| `Assets/Scripts/AI/SteeringBehaviors.cs` | Seek, Flee, Arrive, Wander, Pursue, Evade |
| `Assets/Scripts/AI/EnemyMovement.cs` | Proyecta steering al NavMesh y llama `NavMeshAgent.SetDestination` |
| `Assets/Scripts/AI/EnemyVision.cs` | Line of Sight compartido (raycast, capas configurables) |
| `Assets/Scripts/Enemies/EnemyHealth.cs` | Vida, muerte y contador global vía `GameManager` |
| `Assets/Scripts/Misc/GameManager.cs` | Contador de enemigos, victoria, pausa |

---

## Agentes — Entrega 1

### Torreta (`Turret.cs`)

- **Movimiento:** ninguno (agente estático).
- **Decisión:** apunta al jugador; dispara solo si el raycast desde el cañón impacta al jugador sin obstáculos intermedios.
- **Conductas (≥3):** apuntar, comprobar LoS, disparar, esperar entre disparos (`fireRate`).
- **Steering / pathfinding:** no aplica.

### Robot (`Robot.cs`)

- **FSM:** `Patrol` | `Chase`.
- **Percepción:** entra en persecución solo con jugador visible (`visionRange` + LoS). Si pierde visión o distancia, vuelve a patrulla.
- **Conductas (≥3):** elegir waypoint en NavMesh, patrullar, esperar en punto, perseguir, abandonar persecución.
- **Entrega 2:** patrulla con **Arrive**; persecución con **Pursue**; ruta **A\***.

### Fleeing Robot (`FleeingRobot.cs`)

- **FSM:** `Patrol` | `Attack` | `Flee`.
- **Percepción:** ataca con LoS a distancia; si el jugador se acerca por debajo de `fleeTriggerDistance`, huye aunque aún lo vea.
- **Conductas (≥3):** patrullar, esperar, disparar, huir rápido, reanudar patrulla/ataque según distancia y visión.
- **Entrega 2:** **Arrive** en patrulla, **Flee** en huida, **A\***.

### Spawn Gate (`SpawnGate.cs`)

- Genera robots adicionales en intervalos (`maxSpawns` por puerta). No es un agente de combate: no lleva lógica FSM propia; los robots spawneados usan `Robot.cs` + `EnemyHealth`.

---

## Agentes — Entrega 2

### Robot cargador (`ChargerRobot.cs`)

- **FSM:** `Patrol` | `Charge` | `Recover`.
- **Steering:** **Wander** en patrulla, **Seek** al detectar jugador con LoS.
- **Combate:** daño por contacto; sin proyectiles.
- **Visual:** esfera naranja/roja (`EnemyGlowVisual` / material).

### Robot francotirador (`SniperRobot.cs`)

- **FSM:** `Hold` | `Snipe` | `Evade`.
- **Steering:** **Arrive** al puesto, **Evade** si el jugador se acerca (con predicción de movimiento).
- **Combate:** disparo a media/larga distancia; no dispara por debajo del rango mínimo.
- **Visual:** esfera violeta.

---

## Tabla resumen

| Agente | Entrega | FSM | Steering (E2) | Pathfinding |
|---|---|---|---|---|
| Torreta | 1 | Apuntar / disparar con LoS | — | — |
| Robot | 1 + 2 | Patrol, Chase | Arrive, Pursue | A* (NavMesh) |
| Fleeing Robot | 1 + 2 | Patrol, Attack, Flee | Arrive, Flee | A* (NavMesh) |
| Spawn Gate | 1 | Spawn temporal | — | — |
| Charger Robot | 2 | Patrol, Charge, Recover | Wander, Seek | A* (NavMesh) |
| Sniper Robot | 2 | Hold, Snipe, Evade | Arrive, Evade | A* (NavMesh) |

---

## Estructura de scripts (código del juego)

```
Assets/Scripts/
├── AI/
│   ├── SteeringBehaviors.cs
│   ├── EnemyMovement.cs
│   └── EnemyVision.cs
├── Enemies/
│   ├── Turret.cs
│   ├── Robot.cs
│   ├── FleeingRobot.cs
│   ├── SpawnGate.cs
│   ├── ChargerRobot.cs
│   ├── SniperRobot.cs
│   ├── EnemyHealth.cs
│   ├── EnemyGlowVisual.cs
│   ├── Projectile.cs
│   └── Explosion.cs
├── Player/
│   ├── ActiveWeapon.cs
│   ├── Weapon.cs
│   ├── WeaponSO.cs
│   └── PlayerHealth.cs
├── Pickups/
│   ├── Pickup.cs
│   └── HealthPickup.cs
└── Misc/
    └── GameManager.cs
