# Shooter Protocol

Videojuego FPS de acción desarrollado en Unity 6 (URP). El jugador recorre un nivel sci-fi, elimina a los agentes hostiles y gana al vaciar el contador de enemigos. La inteligencia artificial está integrada al gameplay: percepción, decisiones, steering behaviors y pathfinding forman parte del comportamiento observable de cada enemigo.

## Datos del proyecto

| Campo | Descripción |
|---|---|
| Nombre | Shooter Protocol |
| Género | FPS / shooter en primera persona |
| Motor | Unity 6 |
| Escena de entrega | `Assets/Scenes/MainLevel.unity` |
| Objetivo | Reducir **Enemies Left** a cero eliminando todos los agentes hostiles |

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

### Armas y munición

El jugador inicia con las tres armas en el inventario. Cada arma usa cargadores de capacidad fija: al agotar las balas aparece el estado de recarga en pantalla (`RLD`), tras un breve intervalo el cargador se repone automáticamente. No hay pickups de munición en el nivel. Las cajas de suministro restauran vida (+2 por defecto).

---

## Sistemas de IA — Entrega 1

Requisitos cubiertos en esta instancia: escena jugable, percepción por línea de visión, toma de decisiones y al menos tres conductas diferenciables por agente.

### Percepción

**Line of Sight (LoS):** raycast desde el agente (o desde el cañón en torretas) hacia el jugador. Paredes y obstáculos bloquean la visión. Implementación compartida en `EnemyVision.cs` y lógica específica en `Turret.cs`.

### Toma de decisiones

**FSM (máquina de estados finitos)** en cada agente: estados discretos, transiciones por distancia, LoS y condiciones de combate. No se usa árbol de comportamiento ni sistema por puntaje; la FSM cumple el requisito de la consigna.

### Agentes de la Entrega 1

#### Torreta (`Turret.cs`)

- **Movimiento:** ninguno (agente estático).
- **FSM:** apuntar al jugador; disparar solo con LoS.
- **Conductas:** rastrear objetivo, disparar, esperar entre disparos.
- **Pathfinding / steering:** no aplica.

#### Robot (`Robot.cs`)

- **FSM:** `Patrol` | `Chase`.
- **LoS:** entra en persecución solo si ve al jugador.
- **Conductas:** patrullar entre waypoints, esperar en punto, perseguir, volver a patrulla al perder visión.
- **Entrega 2:** en patrulla usa **Arrive**; en persecución **Pursue**; desplazamiento con NavMesh **A\***.

#### Fleeing Robot (`FleeingRobot.cs`)

- **FSM:** `Patrol` | `Attack` | `Flee`.
- **LoS:** necesaria para disparar; la huida se activa por proximidad.
- **Conductas:** patrullar, atacar a distancia, huir si el jugador se acerca.
- **Entrega 2:** **Arrive** en patrulla, **Flee** en huida, NavMesh **A\***.

#### Spawn Gate (`SpawnGate.cs`)

Genera robots adicionales en el tiempo para aumentar la presión en el mapa. No es un agente de combate independiente; reutiliza la IA del `Robot`.

---

## Sistemas de IA — Entrega 2

La segunda entrega mantiene la base de la primera y agrega **steering behaviors**, **pathfinding** y agentes complementarios con roles distintos.

### Arquitectura general

```
FSM (decisión) → SteeringBehaviors (micromovimiento) → EnemyMovement (proyección al NavMesh) → NavMesh A* (ruta)
```

| Archivo | Función |
|---|---|
| `Assets/Scripts/AI/SteeringBehaviors.cs` | Seek, Flee, Arrive, Wander, Pursue, Evade |
| `Assets/Scripts/AI/EnemyMovement.cs` | Integra steering con `NavMeshAgent.SetDestination` |
| `Assets/Scripts/AI/EnemyVision.cs` | LoS compartido para agentes móviles |

**Pathfinding:** algoritmo **A\*** mediante `NavMeshAgent` (Unity AI Navigation). El mapa incluye obstáculos y zonas no caminables; las rutas evitan paredes.

**Steering en uso:** los seis comportamientos de la biblioteca aparecen en runtime: Arrive, Pursue, Flee (agentes E1 ampliados en E2), Wander y Seek (`ChargerRobot`), Evade (`SniperRobot`).

### Agentes complementarios (Entrega 2)

#### Robot Cargador (`ChargerRobot.cs`)

- **FSM:** `Patrol` | `Charge` | `Recover`.
- **Steering:** **Wander** en patrulla, **Seek** al detectar jugador con LoS.
- **Combate:** sin proyectiles; daño por contacto y autodestrucción.
- **Identificación visual:** esfera naranja/roja.

#### Robot Francotirador (`SniperRobot.cs`)

- **FSM:** `Hold` | `Snipe` | `Evade`.
- **Steering:** **Arrive** al puesto de guardia, **Evade** si el jugador se acerca (predicción de movimiento).
- **Combate:** disparo a media y larga distancia; no dispara por debajo del rango mínimo.
- **Identificación visual:** esfera violeta.

Diferencia respecto al Fleeing Robot: el francotirador mantiene distancia y esquiva con **Evade**; el fleeing robot huye con **Flee** sin predecir la trayectoria del jugador.

---

## Tabla resumen por agente

| Agente | Entrega | FSM | Steering | Pathfinding |
|---|---|---|---|---|
| Torreta | 1 | Apuntar / disparar | — | — |
| Robot | 1 + 2 | Patrol, Chase | Arrive, Pursue | A* |
| Fleeing Robot | 1 + 2 | Patrol, Attack, Flee | Arrive, Flee | A* |
| Spawn Gate | 1 | Spawn temporal | — | — |
| Charger Robot | 2 | Patrol, Charge, Recover | Wander, Seek | A* |
| Sniper Robot | 2 | Hold, Snipe, Evade | Arrive, Evade | A* |

---

## Estética y presentación

El proyecto usa assets del pack GDTV Sharp Shooter con dirección sci-fi coherente: entorno futurista, robots flotantes, torretas y código de color en las esferas de los enemigos (cyan/verde robot estándar, amarillo fleeing, naranja cargador, violeta francotirador).

---

## Estructura de scripts

```
Assets/Scripts/
├── AI/
│   ├── SteeringBehaviors.cs
│   ├── EnemyMovement.cs
│   ├── EnemyVision.cs
├── Enemies/
│   ├── Turret.cs
│   ├── Robot.cs
│   ├── FleeingRobot.cs
│   ├── SpawnGate.cs
│   ├── ChargerRobot.cs
│   ├── SniperRobot.cs
│   ├── EnemyHealth.cs
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
```

---

## Ejecución del proyecto

1. Abrir el repositorio en **Unity 6** (versión indicada en `ProjectSettings/ProjectVersion.txt`).
2. Abrir la escena `Assets/Scenes/MainLevel.unity`.
3. Confirmar en **File → Build Settings** que `MainLevel` está habilitada.
4. Si se modificó la geometría del nivel, ejecutar **Bake** del NavMesh (Window → AI → Navigation).
5. Entrar en Play Mode y verificar que no haya errores en la consola.

Las armas se obtienen por el inventario inicial (teclas 1–3). Las cajas del mapa otorgan vida.

---

## Entregables

| Entregable | Ubicación |
|---|---|
| Proyecto Unity completo | Este repositorio |
| Documentación de IA | Este `README.md` |
| Link público Git | _(completar con la URL del repositorio al subir el proyecto)_ |

Al publicar el repositorio, no incluir `Library/`, `Temp/`, `Logs/` ni builds locales (ya contemplados en `.gitignore`).

---

## Cumplimiento de consignas

| Requisito | Estado |
|---|---|
| Jugador controlable y objetivo claro | Cumplido |
| Al menos 3 agentes con IA distinta | Cumplido (5 tipos de combate + spawn gate) |
| LoS que influye en el comportamiento | Cumplido |
| FSM (u otro sistema de decisión) | Cumplido (FSM) |
| Al menos 3 conductas por agente E1 | Cumplido |
| Al menos 3 steering behaviors | Cumplido (6 en uso) |
| Pathfinding A*, Dijkstra o Theta* | Cumplido (A* / NavMesh) |
| Integración FSM + steering + path | Cumplido |
| Mapa con obstáculos y navegación | Cumplido (requiere NavMesh bakeado) |
| Identidad visual coherente | Cumplido |
| README con arquitectura por agente | Cumplido |
