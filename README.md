# Shooter Protocol

Shooter en primera persona para Unity 6 (URP). El jugador recorre un laboratorio sci-fi, elimina agentes hostiles y gana cuando el contador **Enemies Left** llega a cero.

El proyecto integra dos materias:

- **Inteligencia Artificial:** FSM, Line of Sight, steering behaviors y pathfinding A* (NavMesh) en todos los enemigos móviles.
- **Programación Gráfica:** 13 Shader Graphs integrados en el nivel, postproceso Bloom y Render Textures en monitores del lab.

---

## Datos del proyecto

| Campo | Valor |
|---|---|
| Motor | Unity 6 (`6000.3.11f1`) |
| Escena principal | `Assets/Scenes/MainLevel.unity` |
| Render pipeline | Universal Render Pipeline (URP) |
| Input | Input System Package |
| Navegación | AI Navigation (NavMesh) |
| Objetivo | Eliminar a todos los enemigos y mantenerte con vida |

---

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
| Pausa | `ESC` → **Resume**, **Credits**, **Quit** |
| Créditos (examen PG) | `C` |

### Armas y supervivencia

- El jugador inicia con las tres armas (`1`–`3`). Cada arma tiene cargador de tamaño fijo; al vaciarlo aparece `RLD` y se recarga automáticamente.
- No hay pickups de munición en el nivel.
- Las cajas del mapa restauran vida (`HealthPickup`, +2 HP). Prefab: `Assets/Prefabs/Pickups/Health Pickup.prefab`.

### Victoria, pausa y créditos

- **Victoria:** cada enemigo con `EnemyHealth` suma `+1` al contador al aparecer y `-1` al morir. Con `0` enemigos se muestra **YOU WIN!** y se abren los créditos.
- **Pausa:** `ESC` congela el juego (`timeScale = 0`).
- **Créditos:** tecla `C`, botón **Credits** en pausa o pantalla de victoria. Configurados en `Exam Systems` → `ExamCreditsUI`.

---

## Inteligencia Artificial — Arquitectura

```
FSM (decisión)  →  SteeringBehaviors (velocidad deseada)  →  EnemyMovement (destino en NavMesh)  →  NavMesh A* (ruta)
```

| Archivo | Rol |
|---|---|
| `Assets/Scripts/AI/SteeringBehaviors.cs` | Seek, Flee, Arrive, Wander, Pursue, Evade |
| `Assets/Scripts/AI/EnemyMovement.cs` | Integra steering con NavMesh; evita recalcular destino cada frame |
| `Assets/Scripts/AI/EnemyVision.cs` | Line of Sight compartido (raycast) |
| `Assets/Scripts/Enemies/EnemyHealth.cs` | Vida, muerte y contador global |
| `Assets/Scripts/Misc/GameManager.cs` | Contador de enemigos, victoria, pausa, créditos |

**Steering en uso:** Arrive, Pursue, Flee, Wander, Seek, Evade.  
**Pathfinding:** A* vía `NavMeshAgent` (Unity AI Navigation).

> Si se modifica la geometría del nivel, rebakear NavMesh: **Window → AI → Navigation → Bake**.

---

## Agentes de IA

### Entrega 1 — Clase 7

| Agente | Script | FSM | Conductas (≥3) | LoS | Steering + A* |
|---|---|---|---|---|---|
| **Torreta** | `Turret.cs` | Apuntar / disparar | Apuntar, comprobar LoS, disparar, esperar | Sí (raycast propio) | — (estática) |
| **Robot** | `Robot.cs` | Patrol \| Chase | Waypoint, patrullar, esperar, perseguir, abandonar | Sí | Arrive, Pursue |
| **Fleeing Robot** | `FleeingRobot.cs` | Patrol \| Attack \| Flee | Patrullar, esperar, disparar, huir, reanudar ataque | Sí | Arrive, Flee |
| **Spawn Gate** | `SpawnGate.cs` | — | Genera robots con `Robot.cs` en intervalos | — | — |

### Entrega 2 — Clase 16 / 17

| Agente | Script | FSM | Steering | Rol |
|---|---|---|---|---|
| **Charger Robot** | `ChargerRobot.cs` | Patrol \| Charge \| Recover | Wander, Seek | Carga al jugador; daño por contacto |
| **Sniper Robot** | `SniperRobot.cs` | Hold \| Snipe \| Evade | Arrive, Evade | Disparo a distancia; esquiva si te acercás |

### Detalle por agente

**Torreta** — Estática. Rota la cabeza hacia el jugador y dispara solo con línea de visión libre desde el cañón.

**Robot** — Patrulla waypoints aleatorios en NavMesh; si ve al jugador (rango + LoS), persigue con Pursue. Al tocar al jugador se autodestruye.

**Fleeing Robot** — Patrulla, ataca a distancia con proyectiles si tiene LoS, y huye si el jugador entra en `fleeTriggerDistance`.

**Charger Robot** — Vaga con Wander; al detectar jugador con LoS carga con Seek. Tras perderlo, entra en Recover.

**Sniper Robot** — Vuelve a su puesto (Hold + Arrive), dispara en Snipe si el jugador está lejos y visible, y usa Evade si se acerca demasiado.

**Spawn Gate** — Instancia robots en `spawnPoint` y los coloca en NavMesh con `EnemyMovement.EnsureOnNavMesh`.

### Identificación visual de enemigos

| Agente | Color de esfera (glow) |
|---|---|
| Robot / Torreta | Cian (`EnemyGlowVisual.TealGlow`) |
| Fleeing Robot | Amarillo |
| Charger Robot | Naranja / rojo |
| Sniper Robot | Violeta |

---

## Cumplimiento — Inteligencia Artificial

### Entrega 1

| Requisito | Estado |
|---|---|
| Escena jugable con jugador | Cumplido |
| ≥ 3 agentes con IA integrada | Cumplido (Torreta, Robot, Fleeing + más en E2) |
| Line of Sight que influya | Cumplido |
| FSM / árbol / puntaje | Cumplido (FSM explícita) |
| ≥ 3 conductas por agente | Cumplido |
| Estética coherente | Cumplido |

### Entrega 2

| Requisito | Estado |
|---|---|
| Entrega 1 sigue funcionando | Cumplido |
| ≥ 3 steering behaviors | Cumplido (6 implementados, 6 en uso) |
| Pathfinding A* / Dijkstra / Theta* | Cumplido (A* NavMesh) |
| Integración FSM + steering + path | Cumplido |
| Mapa con obstáculos y navegación | Cumplido |
| ≥ 3 agentes que actúen distinto | Cumplido |
| README con arquitectura | Cumplido (este documento) |

---

## Programación Gráfica — Shader Graph

Guía detallada: [`Assets/ShaderGraph/EXAMEN_PARCIAL_PG.md`](Assets/ShaderGraph/EXAMEN_PARCIAL_PG.md)

**Carpetas:**

| Recurso | Ruta |
|---|---|
| Shader Graphs (13) | `Assets/ShaderGraph/Exam/` |
| Materiales | `Assets/Materials/Exam/` |
| Postproceso | `Assets/Settings/LabExamVolumeProfile.asset` |
| Render Textures | `Assets/RenderTextures/SecurityCamera_RT`, `Exam_Secondary_RT` |

**Menú editor:** `PG → Examen → …`

### Inventario de shaders

| # | Shader | Tipo | Uso en MainLevel | Conceptos clave |
|---|---|---|---|---|
| 1 | `SG_Lit_LabWall` | Lit + textura | Paredes / estructura | Lit + textura |
| 2 | `SG_Lit_LabCeiling` | Lit + textura | Techos | Lit, Seno, Emission, Bloom |
| 3 | `SG_Lit_PillarPulse` | Lit + textura | Pilares (Cylinder) | Lit, Seno, Emission, Bloom |
| 4 | `SG_Lit_CratePanel` | Lit + textura | Cajas decorativas | Lit, Coseno |
| 5 | `SG_Lit_ReactorCore` | Lit + emisión | Núcleos / focos de energía | Seno, Emission, Bloom |
| 6 | `SG_Unlit_WarningPulse` | Unlit | Pilares de advertencia | Unlit, Seno |
| 7 | `SG_Unlit_StatusLED` | Unlit | Esferas LED junto a luces | Unlit, Seno, Step |
| 8 | `SG_Unlit_SecurityMonitor` | Unlit + RT | Monitor CCTV en pared | Unlit, Render Texture, Seno |
| 9 | `SG_Unlit_RenderTextureView` | Unlit + RT | Segundo monitor | Unlit, Render Texture |
| 10 | `SG_Trans_HoloGlass` | Transparente | Cilindros holográficos | Scene Color, Dot |
| 11 | `SG_Trans_LabFluid` | Transparente | Charcos | Depth Fade, Seno |
| 12 | `SG_Trans_SmokeVent` | Transparente | Respiraderos (humo) | Distancia, Seno |
| 13 | `SG_Trans_ShieldDot` | Transparente | Esferas de contención | Dot, Distancia, Coseno |

### Cumplimiento — Programación Gráfica

| Requisito | Mínimo | Estado |
|---|---|---|
| Lit + texturas | 3 | Cumplido (4+) |
| Unlit | 2 | Cumplido (4) |
| Transparentes | 3 | Cumplido (4) |
| Postproceso | 1 | Cumplido (Bloom + Global Volume) |
| Seno (2 usos) | 2 | Cumplido |
| Coseno (2 usos) | 2 | Cumplido |
| Distancia (2 usos) | 2 | Cumplido |
| Dot Product (2 usos) | 2 | Cumplido |
| Depth Fade | 1 | Cumplido |
| Scene Color | 1 | Cumplido |
| Emission HDR + Bloom (2 shaders) | 2 | Cumplido |
| Render Texture | 1 | Cumplido (2 RT en escena) |
| Créditos con apellidos | 1 | Cumplido |
| Solo Shader Graph | Todos | Cumplido |

### Monitores y cámaras (Render Texture)

| Objeto | Cámara | Render Texture | Material |
|---|---|---|---|
| `SecurityMonitor_CCTV` | `Security Camera` | `SecurityCamera_RT` | `Mat_SG_Unlit_SecurityMonitor` |
| `SecondaryMonitor` | `SecondaryCamera` | `Exam_Secondary_RT` | `Mat_SG_Unlit_RenderTextureView` |

Scripts: `LabSecurityCamera.cs`, `LabMonitorDisplay.cs`.

### Postproceso

- Perfil: `LabExamVolumeProfile.asset` (Bloom activo).
- Escena: GameObject **Global Volume** con Weight = 1 e Is Global activado.
- También referenciado en `PC_RPAsset` y `Mobile_RPAsset`.

---

## Estructura de scripts

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
│   ├── PlayerHealth.cs
│   └── CinemachineLensHelper.cs
├── Pickups/
│   ├── Pickup.cs
│   └── HealthPickup.cs
├── Misc/
│   ├── GameManager.cs
│   ├── ExamCreditsUI.cs
│   ├── ExamSystemsBootstrap.cs
│   ├── LabSecurityCamera.cs
│   └── LabMonitorDisplay.cs
└── Editor/
    └── PGExamMenu.cs
```

---

## Cómo abrir y probar

1. Clonar el repositorio y abrir la carpeta en **Unity 6**.
2. Abrir la escena `Assets/Scenes/MainLevel.unity`.
3. Verificar que el **NavMesh** esté bakeado (zona azul en Scene view con Navigation visible).
4. **Play:** moverte, disparar, observar IA y efectos de shaders.
5. Probar créditos con `C` o pausa → **Credits**.

### Build ejecutable (entrega PG)

1. **File → Build Settings**
2. Escena incluida: `MainLevel`
3. **Build** (Windows x64 u otra plataforma requerida)

### Entrega GitHub

Subir al repositorio:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

---

## Notas técnicas

- Los robots usan **NavMeshAgent** (no Rigidbody) para movimiento. La navegación se actualiza con destinos estables para evitar trabas y oscilaciones.
- Las animaciones de los robots pueden permanecer en **Idle** mientras se desplazan (el foco del proyecto es IA + shaders, no animación locomotion).
- El objeto **Exam Systems** en la escena contiene `ExamCreditsUI` enlazado a `GameManager`.

---

## Créditos del equipo

Configurados en el objeto `ExamSystems` de MainLevel (`ExamCreditsUI`):

- Herrera, Oriana.
- Lima, Thiago.
- Muñoz, Guadalupe.
- Jorge, Santino.

Línea de materia en pantalla: *Inteligencia Artificial & Programación Gráfica*.

> Si la consigna de PG limita el grupo a 3 integrantes, dejá solo los apellidos requeridos en el Inspector de `ExamCreditsUI`.

---

*Shooter Protocol — Inteligencia Artificial + Programación Gráfica — Unity 6 URP*
