# Guía del Editor — Entrega 2 (Shooter Protocol)

Pasos exactos en Unity para completar la entrega, agrandar el nivel y verificar que todo funcione.

---

## 1. Antes de abrir Unity

1. Abrí el proyecto en **Unity 6** (versión del `ProjectSettings/ProjectVersion.txt`).
2. Esperá a que compile sin errores en la consola.

---

## 2. Escena correcta

1. Menú **File → Build Settings**.
2. Confirmá que **MainLevel** esté en la lista y marcada (enabled).
3. Abrí **Assets/Scenes/MainLevel.unity** (doble clic).

---

## 3. Verificar enemigos en la escena

### Torreta (`Turret`)

1. Seleccioná una torreta en la Hierarchy.
2. En el Inspector, componente **Turret**:
   - **Projectile Prefab** asignado.
   - **Turret Head**, **Player Target Point**, **Projectile Spawn Point** asignados (no deben estar en "None").
3. Probá en Play: escondete detrás de una pared → no debe disparar.

### Robot (`Robot`)

1. Seleccioná un Robot (o el prefab `Assets/Prefabs/Enemies/Robot.prefab`).
2. Componentes requeridos:
   - **Nav Mesh Agent** (Speed ~3, Stopping Distance puede quedar 0.5 por script).
   - **Robot** script.
   - **Enemy Health** script.
3. No hace falta **Steering Agent** (el steering va por `EnemyMovement` + `SteeringBehaviors`).
4. En Play, seleccioná el robot con Gizmos activos:
   - Línea **cyan** = ruta A* (NavMesh).
   - Rayo **verde** = vector de steering.

### Fleeing Robot (`FleeingRobot`)

1. Seleccioná un FleeingRobot en la escena.
2. Componentes:
   - **Nav Mesh Agent**
   - **Fleeing Robot** script
   - **Enemy Health**
3. En el hijo del modelo debe existir **"Projectile Spawn Fleeing Robot"** (la escena MainLevel lo agrega por instancia; si falta, el script lo busca solo).
4. Valores recomendados en Inspector:
   - **Flee Trigger Distance**: 10
   - **Resume Attack Distance**: 12
   - **Flee Speed**: 12
   - **Flee Acceleration**: 40
5. Gizmos al seleccionar:
   - Ruta **amarilla/naranja** = A*
   - Rayo **rojo** = steering de huida

---

## 4. Agrandar el nivel SIN romper el NavMesh

### Regla de oro

Cualquier cambio en pisos/paredes **obligatorio**: volver a **Bake** el NavMesh.

### Paso A — Duplicar / extender geometría

**Opción 1 — Duplicar partes existentes (más seguro)**

1. En Hierarchy, buscá el padre del nivel (suelo, muros, `Environment`, `Level`, etc.).
2. Seleccioná un tramo de piso o sala.
3. **Ctrl+D** para duplicar.
4. Mové el duplicado con la herramienta Move (**W**).
5. Alineá bordes para que no queden huecos entre piezas.

**Opción 2 — ProBuilder (si usás mallas ProBuilder)**

1. **Tools → ProBuilder → ProBuilder Window**.
2. Creá un nuevo **Plane** o **Cube** para piso extra.
3. Escalá y posicioná donde quieras extender el mapa.
4. Asegurate de que tenga **Collider** (Mesh Collider o Box Collider).

### Paso B — Marcar objetos como navegables

Para cada piso/muro que quieras que el NavMesh considere:

1. Seleccioná el objeto.
2. Inspector → icono **Static** (arriba a la derecha) → activá **Navigation Static**.
   - Si no aparece: **Window → AI → Navigation** (versión antigua) o usá solo Nav Mesh Surface (paso C).

En proyectos con **Nav Mesh Surface** (tu caso), lo importante es que el objeto tenga collider y esté en capas incluidas en el bake.

### Paso C — Rebake del Nav Mesh Surface

1. En Hierarchy, buscá el objeto **"Nav Mesh Surface"** (o similar con componente **Nav Mesh Surface**).
2. Seleccionarlo.
3. En Inspector, componente **Nav Mesh Surface**:
   - **Agent Type**: Humanoid (o el que uses).
   - **Include Layers**: incluí **Default** y las capas de tu suelo.
4. Clic en **Bake** (botón abajo del componente).
5. Deberías ver una capa **azul/violeta** sobre el suelo caminable en la Scene view.

### Paso D — Verificar que no se rompió nada

1. **Play**.
2. Los robots deben moverse solo sobre el área azul del NavMesh.
3. Si un enemigo queda quieto al inicio:
   - Movelo en la Scene view a una zona azul.
   - O subí **Sample Radius** en el script (ya hace Warp automático al iniciar).

### Paso E — Reposicionar gameplay

Después de agrandar:

1. Mové **Spawn Gates**, **Torretas**, **FleeingRobots** y **Player** a zonas con NavMesh.
2. Colocá al menos:
   - 2–3 Robots en pasillos con paredes (demuestra A* rodeando obstáculos).
   - 2 FleeingRobots en zona abierta.
   - 1–2 Torretas con línea de visión larga.
   - 1 Spawn Gate si querés variedad.

3. Guardá escena: **Ctrl+S**.

### Errores comunes al agrandar

| Problema | Solución |
|---|---|
| Enemigos no se mueven | Rebake NavMesh; colocar sobre zona azul |
| Caen al vacío | Falta collider en el piso nuevo |
| Ruta atraviesa paredes | El piso no tiene collider o no se incluyó en el bake |
| NavMesh viejo | Borrar datos: Nav Mesh Surface → Clear, luego Bake de nuevo |

---

## 5. Ajustes opcionales en Inspector (balance)

### Robot

| Campo | Valor sugerido |
|---|---|
| Vision Range | 15 |
| Patrol Radius | 10–15 (más mapa = más radio) |
| Arrival Slowing Radius | 3 |

### Fleeing Robot

| Campo | Valor sugerido |
|---|---|
| Vision Range | 20 |
| Flee Trigger Distance | 10 |
| Resume Attack Distance | 12 |
| Patrol Radius | 15–20 |

---

## 6. Demo para la defensa (Entrega 2)

1. Play en **MainLevel**.
2. Mostrá un **Robot** persiguiéndote rodeando una pared (ruta A* en Gizmos).
3. Mostrá un **FleeingRobot** disparando, acercate → huye rápido, alejate → vuelve a disparar.
4. Escondete de una **Torreta** detrás de un muro → deja de disparar.
5. Explicá verbalmente:
   - **FSM** decide el estado.
   - **Steering** elige dirección (Arrive, Pursue, Flee).
   - **NavMesh** calcula el camino A*.

---

## 7. Qué NO tocar (evitar romper)

- No elimines el componente **Nav Mesh Agent** de robots.
- No agregues de nuevo **Steering Agent** a los prefabs (ya no se usa; puede interferir).
- No muevas enemigos a zonas sin NavMesh bakeado.
- Después de editar geometría, **siempre Bake** de nuevo.

---

## 8. Checklist final antes de entregar

- [ ] MainLevel en Build Settings
- [ ] NavMesh bakeado tras agrandar el mapa
- [ ] 3 tipos de enemigos funcionando (Torreta, Robot, FleeingRobot)
- [ ] LoS con paredes funciona
- [ ] Robots patrullan y persiguen
- [ ] FleeingRobots patrullan, disparan y huyen
- [ ] README actualizado con sección Entrega 2
- [ ] Consola sin errores en Play
