# Shooter Protocol 

## 🎮 Género del Juego
Shooter de Acción Rápida - FPS

## 🎯 Objetivo del Juego
Sobrevivir y destruir a todos los enemigos en el nivel. El nivel se completa únicamente al eliminar a TODOS los enemigos (el contador de enemigos de la pantalla debe llegar a cero).

## 🧠 Sistemas de IA Implementados (Entrega 1)
- **Implementación de Line of Sight (LoS):** Implementado con Raycast en todos los enemigos para que evalúen la línea de visión directa.
- **Toma de Decisiones (FSM):** Se usa Máquina de Estados Finitos para controlar el comportamiento de los agentes hostiles.
- **Agentes (3 tipos):**
  1. **Torretas Inmóviles (`Turret.cs`):** Sistema estacionario que rastrea la posición del jugador. Utiliza Raycasting para detectar la línea de visión (LoS); solo dispara proyectiles si tiene visión clara del jugador sin obstáculos.
  2. **Robots Cuerpo a Cuerpo (`Robot.cs`):** Utilizan un sistema FSM para patrullar (`Patrol`), esperar (`Idle`) y perseguir al jugador (`Chase`) en el momento en que obtienen línea de visión cruzando un NavMesh.
  3. **Robots Tácticos a Distancia (`FleeingRobot.cs`):** Utilizan un FSM para patrullar (`Patrol`), atacar a media distancia disparando proyectiles de energía (`Attack`) y escapar buscando una distancia segura en caso de que el jugador se acerque demasiado (`Flee`).
- **Spawn Gates:** Genera un volumen controlado y limitado de enemigos dinámicamente en el escenario en puntos preestablecidos.

## ⌨️ Controles Básicos
- **Movimiento:** `W` `A` `S` `D`
- **Mirar alrededor:** Movimiento del Mouse
- **Disparar:** Clic Izquierdo del Mouse
- **Apuntar / Zoom:** Clic Derecho (Para armas que lo permiten)
- **Saltar:** Barra Espaciadora

## 🔗 Link del Repositorio
**GitHub:** [https://github.com/Astryr/Shooter_Protocol](https://github.com/Astryr/Shooter_Protocol)
