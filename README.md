Nombre del Juego:
Shooter Protocol 

Género del Juego:
Shooter de Acción Rápida - FPS

Objetivo del Juego:
Sobrevivir y destruir a todos los enemigos en el nivel. El nivel se completa únicamente al eliminar a TODOS los enemigos (el contador de enemigos de la pantalla debe llegar a cero).

Sistemas de IA Implementados:
-Torretas Inmoviles: Sistema de IA que rastrea la posición del jugador en tiempo real. 
Utiliza Raycasting para detectar la línea de visión: solo disparará proyectiles si tiene una visión clara del jugador (sin obstaculos). (Turret.cs)

-Robots: Persigue al jugador donde quiera que este hasta que lo alcanza y explota. (Robot.cs)

-Spawn de enemigos: Genera enemigos automaticamente en el escenario en puntos preestablecidos.

Controles Básicos:

-Movimiento: W A S D
-Mirar alrededor: Movimiento del Mouse
-Disparar: Clic Izquierdo del Mouse
-Apuntar / Zoom: Clic Derecho (Para armas que lo permiten)
-Saltar: Barra Espaciadora
