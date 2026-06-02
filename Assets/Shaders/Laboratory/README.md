# Shaders de laboratorio (pilares) — PROTOTIPO HLSL

> **Examen 2do Parcial PG:** la consigna exige **Shader Graph**. Estos `.shader` HLSL sirven como prototipo visual; el entregable debe estar en `Assets/ShaderGraph/Exam/` siguiendo `Assets/ShaderGraph/EXAMEN_PARCIAL_PG.md`.

Cuatro shaders **custom** para URP, pensados para cilindros/pilares del nivel (`Cylinder` en ProBuilder). Son **procedurales** (no necesitan texturas).

## Shaders incluidos

| Shader (menú) | Efecto | Uso sugerido en pilares |
|---|---|---|
| `Laboratory/Grid Pillar` | Rejilla sci-fi con emisión y scroll vertical | Sala principal, pasillos “clean room” |
| `Laboratory/Hazard Stripes` | Franjas diagonales tipo advertencia | Zonas peligrosas / límites del mapa |
| `Laboratory/Containment Pulse` | Bandas horizontales pulsantes | Cápsulas de contención, núcleo del lab |
| `Laboratory/Hologram Glass` | Cristal transparente con rim + scanlines | Pilares “campo de fuerza” o barandillas |

## Cómo implementarlos en Unity

1. Abrí el proyecto y esperá a que Unity importe `Assets/Shaders/Laboratory/`.
2. Verificá en la consola que **no haya errores de compilación** en los shaders (deben aparecer bajo `Laboratory/` en el Project).
3. En `Assets/Materials/Laboratory/` hay **materiales de ejemplo** listos para arrastrar:
   - `Mat_LabGridPillar`
   - `Mat_LabHazardStripes`
   - `Mat_LabContainmentPulse`
   - `Mat_LabHologramGlass`
4. Seleccioná cada **pilar** (cilindro) en `MainLevel` → en el **Mesh Renderer** asigná un material distinto por grupo de pilares para variedad visual.
5. Ajustá colores y escalas en el Inspector (cada shader expone sus propiedades).

### Tips para cilindros

- Los tres shaders opacos usan **UV cilíndrica en espacio de objeto** (eje Y = altura del pilar). Funciona bien si el cilindro está **vertical** y centrado en el origen del mesh.
- Si un pilar se ve “torcido”, rotá el objeto padre, no el material.
- **Hologram Glass**: requiere objetos detrás para notar transparencia; conviene ponerlo en pilares delgados o duplicar un cilindro ligeramente más grande.

### Para la entrega (documentación)

En el README del repo o informe, podés describir cada shader como un **tipo distinto**:

- **Grid**: patrón de rejilla + líneas emisivas animadas.
- **Hazard**: mezcla procedural de dos colores con bandas diagonales.
- **Containment**: bandas + pulso temporal (`_Time`).
- **Hologram**: superficie transparente con Fresnel y scanlines.

## Archivos

```
Assets/Shaders/Laboratory/
  LabCommon.hlsl          (funciones compartidas)
  LabGridPillar.shader
  LabHazardStripes.shader
  LabContainmentPulse.shader
  LabHologramGlass.shader

Assets/Materials/Laboratory/
  Mat_*.mat               (presets)
```
