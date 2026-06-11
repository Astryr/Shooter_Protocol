# 2do Examen Parcial — Programación Gráfica (Shooter Protocol)

Guía de cumplimiento para el trabajo grupal. **Todos los shaders del examen son Shader Graph** en `Assets/ShaderGraph/Exam/` (13 archivos `.shadergraph`).

---

## Checklist rápido (mínimo + equipo de 4)

| Requisito | Mínimo | En este proyecto (13 SG) |
|-----------|--------|--------------------------|
| Lit + texturas | 3 | 5 (`LabWall`, `LabCeiling`, `CratePanel`, `PillarPulse`, `ReactorCore`) |
| Unlit | 2 | 4 (`WarningPulse`, `StatusLED`, `SecurityMonitor`, `RenderTextureView`) |
| Transparentes | 3 | 4 (`HoloGlass`, `LabFluid`, `SmokeVent`, `ShieldDot`) |
| Postproceso | 1 | Bloom (`LabExamVolumeProfile`) + Global Volume en MainLevel |
| Seno (2 usos) | 2 | `LabCeiling`, `PillarPulse`, `ReactorCore`, `WarningPulse`, `StatusLED`, `SmokeVent` |
| Coseno (2 usos) | 2 | `CratePanel`, `ShieldDot` |
| Distancia (2) | 2 | `SmokeVent`, `ShieldDot` |
| Dot (2) | 2 | `HoloGlass`, `ShieldDot` |
| Depth Fade | 1 | `LabFluid` |
| Scene Color | 1 | `HoloGlass` |
| Emission HDR + Bloom | 2 | `ReactorCore`, `PillarPulse`, `LabCeiling` |
| Render Texture | 1 | `SecurityCamera_RT` + `Exam_Secondary_RT` (2 monitores) |
| Créditos | 1 | `ExamCreditsUI` — tecla **C** o botón **Credits** en pausa |

---

## Reparto sugerido (4 integrantes)

| Integrante | Shader Graphs a crear | Conceptos que cubre |
|------------|----------------------|---------------------|
| **1** | `SG_Lit_LabWall`, `SG_Lit_LabCeiling`, `SG_Lit_CratePanel` | Lit + texturas + seno/coseno |
| **2** | `SG_Unlit_WarningPulse`, `SG_Unlit_StatusLED`, `SG_Lit_PillarPulse`, `SG_Lit_ReactorCore` | Unlit + Lit emisión HDR (bloom) |
| **3** | `SG_Trans_LabFluid`, `SG_Trans_SmokeVent`, `SG_Trans_ShieldDot` | Transparentes + Depth Fade + distancia |
| **4** | `SG_Trans_HoloGlass`, `SG_Unlit_SecurityMonitor`, `SG_Unlit_RenderTextureView`, postproceso, créditos | Scene Color + Render Texture + integración |

---

## Carpeta y convención de nombres

```
Assets/ShaderGraph/Exam/          ← todos los .shadergraph
Assets/Materials/Exam/            ← materiales que usan esos graphs
Assets/Textures/Exam/             ← (opcional) copias de texturas GDTV/Starter
```

Crear cada graph: **Click derecho → Create → Shader Graph → URP → Lit / Unlit**.

---

## Dónde colocar cada efecto en MainLevel

| Material | Objetos de la escena | Zona |
|----------|---------------------|------|
| `Mat_SG_Lit_LabWall` | Paredes grandes, ProBuilder boxes | Estructura general |
| `Mat_SG_Lit_LabCeiling` | Techos / paneles superiores | Estructura general |
| `Mat_SG_Lit_CratePanel` | Cajas decorativas | Cobertura |
| `Mat_SG_Lit_PillarPulse` | Pilares bajo **Misc → Cylinder** (mayoría) | Pasillos |
| `Mat_SG_Unlit_WarningPulse` | Pilares en accesos / Spawn Gate | Advertencia |
| `Mat_SG_Unlit_StatusLED` | Esferas pequeñas junto a luces Point | LEDs |
| `Mat_SG_Trans_HoloGlass` | 1–2 cilindros finos | Barrera holográfica |
| `Mat_SG_Trans_LabFluid` | Disco/plano en charcos (ProBuilder) | Charcos tóxicos |
| `Mat_SG_Trans_SmokeVent` | Quad vertical en respiraderos | Vapor |
| `Mat_SG_Trans_ShieldDot` | (Opcional) esfera alrededor del jugador al dañarse | Escudo |
| `Mat_SG_Lit_ReactorCore` | 1 pilar o esfera central brillante | Núcleo del lab |
| `Mat_SG_Unlit_SecurityMonitor` | Monitor CCTV en pared | `SecurityCamera_RT` |
| `Mat_SG_Unlit_RenderTextureView` | Segundo monitor | `Exam_Secondary_RT` |

---

## Postproceso (ya configurado en código)

1. Perfil: `Assets/Settings/LabExamVolumeProfile.asset` (Bloom activo).
2. Referenciado en `PC_RPAsset` y `Mobile_RPAsset`.
3. En **MainLevel**, agregar si no existe:
   - GameObject `Global Volume`
   - Componente **Volume** → Profile = `LabExamVolumeProfile` → Weight = **1** → Is Global = **on**

---

## Render Texture (integrante 4)

1. RT: `Assets/RenderTextures/SecurityCamera_RT.renderTexture` (512×512).
2. Crear GameObject `Security Camera` con:
   - **Camera** (desactivar Audio Listener si hay conflicto)
   - **LabSecurityCamera** → asignar el RT
3. Crear monitor (ProBuilder Quad) + **LabMonitorDisplay** o **RawImage** en UI world space.
4. Material del monitor: shader **`SG_Unlit_SecurityMonitor`** (Unlit, solo muestra textura del RT).

---

## Créditos (obligatorio)

1. GameObject vacío `Exam Systems` en la escena.
2. Añadir **ExamCreditsUI** y completar **Participant Surnames** con los 4 apellidos.
3. En **GameManager**, arrastrar la referencia a `Exam Credits UI` (o dejar que lo busque solo).
4. Probar: tecla **C**, menú pausa **Credits**, o victoria (YOU WIN).

---

## Recetas Shader Graph (nodos)

### 1) `SG_Lit_LabWall` — Lit + textura (Integrante 1)

- **Target:** URP Lit, Surface Opaque.
- **Propiedades:** `_BaseMap` (Texture2D), `_NormalMap`, `_Smoothness`.
- **Grafos:** Sample Texture 2D (Base) → Base Color. Sample Normal → Normal (Tangent Space).
- **Textura:** `Assets/Imported Assets/StarterAssets/Environment/Art/Textures/Grid_01_BaseMap.png`
- **Normal:** `Grid_01_Normal.png`

### 2) `SG_Lit_LabCeiling` — Lit + textura + seno + emisión (Integrante 1)

- Sample Texture 2D (techo) + **Sin**(**Time** × Speed) en emisión HDR para bloom en techos.

### 3) `SG_Lit_CratePanel` — Lit + coseno (Integrante 1 — **Coseno A**)

- Sample Texture 2D × **Cos**(**Time** × Speed + UV) mezclado suave (Lerp) → Base Color.

### 4) `SG_Lit_PillarPulse` — Lit + emisión HDR + seno (Integrante 2 — **Seno A**, **Bloom**)

- Base Color oscuro azul.
- **Emission:** Color HDR (×4) × **Sin**(**Time** × PulseSpeed + Position.y × Bands).
- Fragment: Position (Object) Y → sin → One Minus → Power → × Emission Color (HDR).
- Aplicar a pilares Misc.

### 5) `SG_Unlit_WarningPulse` — Unlit + seno (Integrante 2 — **Seno B**)

- Color amarillo HDR alternando: **Sin(Time × Speed)** → Remap (-1,1 → 0,1) → Lerp negro/amarillo.
- Sin iluminación; ideal LEDs de advertencia.

### 6) `SG_Unlit_StatusLED` — Unlit (Integrante 2)

- Lerp entre dos colores con **Step** sobre **Sin(Time)**.
- Esferas pequeñas de estado (verde/rojo).

### 7) `SG_Trans_LabFluid` — Transparente + Depth Fade (Integrante 3)

- **Surface:** Transparent, Blend Alpha.
- Nodo **Depth Fade** (o Fog of war pattern): `Scene Depth` − `Pixel Depth` → Saturate → Fade.
- Color turquesa semitransparente; usar en charcos.

### 8) `SG_Trans_SmokeVent` — Transparente + distancia (Integrante 3 — **Distancia A**)

- **Distance**(worldPos, _Origin) → One Minus → Saturate → Alpha.
- **Sin** en UV.y para scroll de ruido (textura ruido opcional o Voronoi del SG).

### 9) `SG_Trans_ShieldDot` — Transparente + Dot + Distancia (Integrante 3 — **Dot A**, **Distancia B**)

- **Dot Product**(normalWS, viewDirWS) → rim.
- **Distance** al jugador para atenuar alpha lejos.
- Fresnel + alpha; opcional en daño.

### 10) `SG_Trans_HoloGlass` — Transparente + Scene Color (Integrante 4 — **Scene Color**)

- **Scene Color** node → mezclar con tinte cyan.
- **Dot** (normal · view) para borde brillante (Fresnel).
- Alpha 0.25–0.4. Reemplaza HLSL `LabHologramGlass` en 1–2 pilares.

### 11) `SG_Lit_ReactorCore` — Lit emisión HDR (Integrante 4 — **Bloom B**)

- Emission HDR verde/cyan muy alta (Intensity 3–6 en color HDR).
- Esfera o pilar central del mapa.

### 12) `SG_Unlit_SecurityMonitor` — Unlit + RT (Integrante 4)

- Propiedad `_MonitorTex` (Texture2D) ← asignar **SecurityCamera_RT** en runtime o material.
- Sample Texture → Base Color (Unlit).

### 13) `SG_Unlit_RenderTextureView` — Unlit + RT secundaria (Integrante 4)

- Igual que SecurityMonitor pero con **Exam_Secondary_RT** en el segundo monitor del lab.

---

## Conceptos obligatorios — mapa

| Concepto | Shader(s) |
|----------|-------------|
| Seno | `SG_Lit_PillarPulse`, `SG_Unlit_WarningPulse`, `SG_Trans_SmokeVent` |
| Coseno | `SG_Lit_CratePanel`, `SG_Trans_ShieldDot` |
| Distancia | `SG_Trans_SmokeVent`, `SG_Trans_ShieldDot` |
| Dot | `SG_Trans_HoloGlass`, `SG_Trans_ShieldDot` |
| Depth Fade | `SG_Trans_LabFluid` |
| Scene Color | `SG_Trans_HoloGlass` |
| Emission + Bloom | `SG_Lit_PillarPulse`, `SG_Lit_ReactorCore` |
| Render Texture | `SG_Unlit_SecurityMonitor`, `SG_Unlit_RenderTextureView` + `LabSecurityCamera` |

---

## Entrega GitHub + Build

- Subir `Assets`, `Packages`, `ProjectSettings`.
- Build Windows en `/Build` o Releases.
- README raíz: enlace al repo + tabla de shaders + apellidos.
- Video corto (recomendado): mostrar bloom, transparencias, monitor RT y créditos.

---

## Menú Unity del proyecto

**PG → Examen → Abrir guía de consignas** (si instalaste el script editor).

Verificación manual en Play Mode:

- [ ] Bloom visible en emisivos
- [ ] Al menos 3 tipos transparentes visibles
- [ ] Monitor muestra imagen de la cámara secundaria
- [ ] Créditos con 4 apellidos
- [ ] 13 archivos `.shadergraph` en `Assets/ShaderGraph/Exam/`
