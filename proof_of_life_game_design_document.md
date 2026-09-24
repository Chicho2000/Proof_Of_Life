# Game Design Document: PROOF OF LIFE
**Sigilo e infiltración en primera persona**

## Título del juego
**Proof of Life.**
El título conecta con la amenaza sobre la familia del protagonista y con la idea de chantaje, secuestro, negociación criminal y presión psicológica. También refuerza el tono de misiones donde la vida de seres queridos funciona como prueba, presión y motivo de obediencia.

## Equipo desarrollador y roles

| Integrante | Rol y responsabilidades |
| :--- | :--- |
| **Ciro Pregot** | Producer y Scripter. Organización de tareas, seguimiento de entregas, apoyo en diseño de sistemas e implementación de mecánicas, lógica de juego, UI y flujo de escenas. |
| **Felipe Isern** | Environment Artist y Level Designer. Diseño de escenarios, composición de niveles, rutas de infiltración, distribución de zonas y creación de entornos low poly. |
| **Sofia Koprcina** | Character Artist y Environment Artist. Diseño visual de personajes, disfraces, variaciones de NPCs, apoyo en props, ambientación y coherencia visual del mundo. |

## Motivación
La motivación del proyecto es construir un juego de sigilo donde la diversión no dependa sólo del combate, sino de observar, planificar y resolver una misión de múltiples maneras. La propuesta busca capturar la satisfacción de encontrar una ruta efectiva, usar el entorno a favor del jugador y completar un objetivo sin ser detectado.

El juego toma inspiración del estilo de infiltración social y resolución abierta de Hitman, pero adaptado a un alcance académico viable: escenarios más contenidos, estética low poly, sistemas claros y objetivos concretos. La intención es ofrecer una experiencia donde cada nivel funcione como un problema de infiltración, con rutas sociales, técnicas y agresivas.

## Proyecto
Proof of Life es un juego singleplayer de sigilo e infiltración en primera persona, ambientado en escenarios de alto perfil como mansiones, yates, bases militares, exposiciones o eventos privados.

El jugador controla a T.O.M. - Tactical Operative Mercenary -, un ex-operador de fuerzas especiales obligado a trabajar para una organización criminal que mantiene amenazadas a su esposa e hija. La organización no se muestra directamente: sólo se comunica por radio o mensajes, generando la sensación de estar controlado por una entidad invisible.

Cada misión plantea un objetivo principal, como robar información, extraer documentos o eliminar a una persona clave. El jugador debe reconocer el escenario, escuchar conversaciones, conseguir pistas, utilizar disfraces, evitar cámaras, manipular rutinas de NPCs, completar el objetivo y escapar del lugar.

El prototipo apunta a demostrar el núcleo jugable sin intentar recrear todos los sistemas de un juego comercial de sigilo. El objetivo es que el jugador pueda entrar a un escenario, observar NPCs, usar disfraces, evitar detección, completar un objetivo y extraerse.

### Alcance propuesto

| Elemento | Descripción |
| :--- | :--- |
| **Niveles** | 2 a 3 niveles jugables, cada uno con múltiples rutas de resolución. |
| **Personaje jugable** | Un protagonista: T.O.M. |
| **NPCs** | Guardias, civiles, personal del lugar, objetivos y NPCs importantes. |
| **Sigilo** | Sistema básico de visión, sospecha, alerta y combate. |
| **Disfraces** | Disfraces con acceso a distintas zonas y detección por comportamiento sospechoso. |
| **Cuerpos** | Noqueo, arrastre y ocultamiento en puntos específicos. |
| **Herramientas** | Ganzúa, objetos arrojables, arma limitada, herramienta de noqueo, llaves/tarjetas. |
| **UI** | Objetivo, disfraz, alerta, inventario rápido e interacción contextual. |
| **Escenas** | Menú principal, pausa, game over/victoria y transición simple entre misiones. |
| **Narrativa** | Cinemáticas simples con texto y audio/radio para contextualizar la amenaza a la familia. |

## Historia
El protagonista es un ex-operador de un equipo especial, con entrenamiento militar avanzado y experiencia en operaciones de alto riesgo. Tras retirarse del servicio, intenta dejar atrás la violencia, pero una organización criminal lo obliga a volver a actuar bajo amenaza directa contra su esposa e hija.

La organización le asigna el alias T.O.M. - Tactical Operative Mercenary -. No lo llaman por su nombre real, y su identidad personal permanece desconocida para el jugador. Esto refuerza la idea de que la organización lo despersonaliza y lo usa como herramienta.

El conflicto principal no es que el protagonista quiera ser un asesino o mercenario, sino que no tiene alternativa inmediata. Cada misión es una forma de ganar tiempo y mantener con vida a su familia. Todavía no existe un plan claro de escape: el objetivo narrativo inicial es sobrevivir misión tras misión y proteger a los suyos.

La organización criminal permanece en las sombras. No aparece físicamente, sino que da órdenes por radio, mensajes o intermediarios. A nivel de gameplay, las amenazas visibles son la seguridad de cada escenario: guardias, cámaras, personal, civiles, zonas restringidas y sistemas de vigilancia.

## Fuentes de inspiración
*   **Hitman:** infiltración social, disfraces, objetivos y múltiples formas de resolver una misión.
*   **Immersive sims:** libertad para combinar herramientas, rutas, información y decisiones dentro de un escenario abierto.
*   **Jason Bourne:** ex-agente atrapado en un sistema que lo persigue y lo utiliza.
*   **Donnie Yen en John Wick 4:** profesional letal que actúa por obligación y lealtad forzada, no por deseo propio.
*   **Payday / juegos de infiltración:** tensión progresiva, estados de alerta, cámaras, guardias y planificación antes del caos.
*   **One Armed Robber:** referencia visual low poly y producción viable para un equipo académico.
*   **Thief, Dishonored y Deus Ex:** referencias de sigilo en primera persona, lectura del entorno, rutas alternativas e interacción con herramientas.

## Diseño artístico
La dirección visual será low poly 3D. Este estilo permite representar escenarios variados, personajes, disfraces y objetos interactivos sin requerir un nivel de detalle demasiado costoso. También ayuda a que los cambios de vestimenta sean claros para el jugador y más fáciles de producir.

La estética general busca un tono serio, táctico y de infiltración, pero con una producción visual simple. Los escenarios deben diferenciarse por color, iluminación y props: una mansión puede verse cálida y lujosa; una base militar, fría y funcional; un yate, elegante y abierto; una exposición, moderna y social.

Los disfraces deben tener siluetas claras: invitado, mozo, guardia y seguridad de élite. La lectura visual rápida es clave porque el jugador necesita entender qué zonas puede atravesar y qué NPCs podrían sospechar.

## Desarrollo

### Género
Proof of Life pertenece al género de sigilo / infiltración, con elementos de stealth-action e immersive sim. El foco principal está en observar, planificar, evitar detección y completar objetivos mediante distintos enfoques.

La cámara será en primera persona, con movimiento libre de mirada. Esta perspectiva busca aumentar la inmersión, la tensión y la sensación de estar infiltrándose dentro del escenario, obligando al jugador a revisar esquinas, puertas, cámaras y rutas desde su propio campo visual. No se usará una cámara externa al personaje.

### Características Principales:
1. Niveles estructurados con múltiples rutas de resolución: social, técnica y agresiva.
2. Sistema de disfraces con zonas permitidas y niveles de sospecha.
3. IA con estados de comportamiento: idle, sospecha, alerta y combate.
4. Misiones de infiltración con objetivos como robar información, eliminar objetivos o extraer documentos.
5. Reglas morales: matar inocentes provoca derrota, pero noquearlos no.
6. Puzzles de investigación social basados en conversaciones, códigos, contraseñas y rutinas de NPCs.
7. Música y ambiente dinámicos según el estado de alerta del escenario.

### Audiencia y Tecnologías soportadas:

| Campo | Descripción |
| :--- | :--- |
| **Audiencia** | Jugadores que disfrutan del sigilo, la planificación, la exploración de escenarios y la resolución creativa de objetivos. |
| **Plataforma** | PC. |
| **Motor** | Unity. |
| **Modo de juego** | Singleplayer. |
| **Controles** | Teclado y mouse. |
| **Estilo visual** | Low poly 3D. |

## Música
La música debe adaptarse al estado del escenario. En situación tranquila, el sonido será ambiental y contextual: música de gala en una mansión, ambiente lujoso en un yate o tono más tenso en una base militar. Cuando el jugador genera sospecha, la música aumenta la tensión. En alerta o combate, el ritmo sube y marca urgencia.

La radio también tiene un papel narrativo: la organización puede comunicarse con T.O.M. para dar órdenes, presionarlo o recordarle la amenaza contra su familia. La radio no ocupa espacio en el inventario; funciona como elemento constante de control narrativo.

## Core value
El valor central de Proof of Life es la tensión estratégica: la sensación de observar un entorno hostil, planificar una ruta, manipular sistemas y ejecutar un plan sin ser detectado. El placer principal no es ganar un combate, sino resolver una misión compleja con precisión.

La experiencia debe generar tensión, estrategia y satisfacción al resolver. El jugador debe sentir que cada decisión importa: qué disfraz usar, qué puerta forzar, qué conversación escuchar, cuándo moverse y cuándo esperar.

## Game feel
El game feel debe transmitir precisión, control y tensión. El movimiento debe sentirse táctico: caminar, agacharse, acercarse a una pared, asomarse por una esquina y actuar en el momento correcto. Las interacciones deben responder de forma clara, con feedback visual y sonoro: abrir puertas, forzar cerraduras, lanzar objetos, noquear enemigos, esconder cuerpos y cambiarse de ropa.

El sistema de sospecha debe dar feedback gradual para que el jugador entienda cuándo está en peligro. Las cámaras, guardias y NPCs deben comunicar claramente si están tranquilos, sospechando o en alerta.

Al estar en primera persona, el game feel debe apoyarse en manos visibles, animaciones de herramientas, sonidos de pasos, respiración, puertas, cerraduras y señales claras de alerta. La falta de una vista amplia obliga a que la información del entorno sea legible mediante audio, UI y comportamiento de NPCs.

## Gameplay
Durante una partida normal, el jugador explora un escenario, identifica rutas, escucha conversaciones, obtiene pistas, consigue disfraces o herramientas, se acerca al objetivo evitando detección, completa la misión y llega a un punto de extracción.

El juego permite distintos enfoques: sigilo puro, infiltración social mediante disfraces, resolución técnica mediante ganzúas/códigos/cámaras, o caos controlado mediante noqueos, distracciones y acciones más riesgosas.

### Objetivos:

| Tipo de objetivo | Descripción |
| :--- | :--- |
| **Corto plazo** | Evitar una cámara, esconderse de un guardia, escuchar una conversación, robar una llave, entrar a una zona sin levantar sospecha. |
| **Mediano plazo** | Acceder a una sala restringida, conseguir un disfraz útil, encontrar un código, neutralizar a un guardia clave o alcanzar la ubicación del objetivo. |
| **Largo plazo** | Completar el objetivo principal de la misión y escapar del escenario sin comprometer la identidad de T.O.M. |

### Condición de victoria y derrota
*   **Victoria:** completar el objetivo de la misión y llegar al punto de extracción sin haber sido comprometido de forma definitiva.
*   **Derrota:** morir en combate, activar una alarma total que bloquee la extracción, fallar una condición crítica de la misión o matar a un inocente.
*   *Nota:* Noquear civiles o NPCs no provoca derrota automática, pero puede aumentar la sospecha o activar alerta si el cuerpo es encontrado.

### Mecánicas
*   Movimiento en primera persona: caminar, correr, agacharse y moverse con cuidado por el escenario.
*   Sigilo: evitar líneas de visión, cámaras, zonas restringidas y comportamientos sospechosos.
*   Cobertura y esquinas: apoyarse contra paredes o asomarse para observar patrullas.
*   Disfraces: cambiar de ropa para acceder a zonas específicas.
*   Noqueo silencioso cuerpo a cuerpo.
*   Arrastrar y esconder cuerpos en puntos determinados.
*   Distracciones: lanzar objetos para generar ruido y modificar rutas de NPCs.
*   Conversaciones/pistas: escuchar NPCs para obtener códigos, contraseñas, rutinas o información útil.
*   Sistema de sospecha/alerta de IA.
*   Interacción con puertas, cerraduras y zonas restringidas.
*   Inventario básico con ganzúa, objetos arrojables, arma limitada, herramienta de noqueo, tarjetas o llaves encontradas.
*   Radio narrativa: comunicación constante con la organización, sin ocupar espacio de inventario.

### Dinámicas
*   El jugador puede observar una ruta de patrulla, esperar el momento correcto y cruzar sin ser visto.
*   Puede conseguir un disfraz para transformar una zona prohibida en una zona transitable.
*   Puede escuchar una conversación para descubrir una contraseña y evitar forzar una puerta.
*   Puede distraer a un guardia con ruido, noquearlo, esconder el cuerpo y abrir una ruta alternativa.
*   Puede optar por no matar inocentes para mantener la misión bajo control y evitar derrota inmediata.
*   Puede resolver un objetivo por ruta social, técnica o agresiva, según el riesgo que esté dispuesto a asumir.

### Estéticas
*   **Tensión:** el jugador se siente observado y vulnerable si actúa mal.
*   **Estrategia:** cada ruta, disfraz y herramienta modifica el plan.
*   **Satisfacción:** completar la misión sin ser detectado genera sensación de dominio.
*   **Misterio:** la organización permanece oculta y controla al protagonista desde las sombras.
*   **Presión moral:** el protagonista actúa por obligación para proteger a su familia, no por placer.

## Elementos más importantes del Gameplay: personajes, enemigos, puzles.

### Personajes
El jugador controla únicamente a T.O.M. Los demás personajes serán bots: guardias, civiles, invitados, personal de servicio, objetivos, NPCs importantes y miembros del entorno de cada misión.

T.O.M. es letal, entrenado y capaz, pero la estructura del juego no lo premia por matar indiscriminadamente. La presión narrativa y las reglas del sistema obligan al jugador a actuar con precisión.

### Enemigos y amenazas
*   Guardias armados con rutas de patrulla fijas o dinámicas.
*   Oficiales o seguridad de élite con mayor rango de detección y acceso a zonas críticas.
*   NPCs civiles o personal del lugar que no atacan, pero pueden delatar al jugador si detectan una conducta sospechosa.
*   Cámaras de seguridad evitables, hackeables o destruibles según el nivel.
*   Objetivos de alto perfil más protegidos, vigilados o difíciles de alcanzar.

Los guardias pueden pasar de idle a sospecha y luego a combate. Los civiles o NPCs no armados pueden pasar de idle a sospecha y luego a alerta, avisando a la seguridad.

### Puzzles
Los puzles no serán acertijos de lógica clásicos, sino puzles de investigación social y espacial. El jugador deberá escuchar conversaciones, encontrar documentos, combinar pistas, descubrir contraseñas, obtener llaves o identificar rutinas de NPCs para abrir rutas hacia el objetivo.

*Ejemplo:* un mozo comenta que la caja fuerte usa la fecha de inauguración de la galería; el jugador puede buscar esa fecha en una placa del escenario, entrar a la oficina con el disfraz correcto y desbloquear la caja sin forzarla.

### Reglas
*   Matar inocentes provoca derrota inmediata de la misión.
*   Noquear inocentes no provoca derrota, pero puede aumentar la alerta si el cuerpo es encontrado.
*   Cada disfraz permite acceder a ciertas zonas, pero no garantiza impunidad total.
*   Algunos NPCs pueden sospechar si el jugador se acerca demasiado, permanece mucho tiempo en una zona restringida o actúa de forma extraña.
*   Las cámaras detectan comportamientos o presencia en zonas no autorizadas.
*   Arrastrar cuerpos, llevar armas visibles o forzar cerraduras genera sospecha si es visto.
*   Una alarma total puede bloquear rutas, aumentar guardias o provocar derrota según la misión.
*   El jugador debe completar el objetivo y extraerse para ganar.

## Sistema de disfraces
La siguiente tabla funciona como ejemplo general para explicar la mecánica de disfraces. No significa que todos los niveles vayan a usar exactamente los mismos trajes ni las mismas zonas: cada escenario definirá sus propios disfraces, permisos, restricciones y NPCs capaces de sospechar.

| Disfraz | Zonas permitidas / función |
| :--- | :--- |
| **Invitado** | Zonas públicas, salones, áreas sociales y eventos. |
| **Mozo** | Cocina, áreas de servicio, pasillos secundarios y zonas de preparación. |
| **Guardia** | Pasillos restringidos, accesos de seguridad y zonas de vigilancia general. |
| **Seguridad de élite** | Zonas cercanas al objetivo, oficinas privadas y áreas de máxima protección. |

## Estados de IA

| Estado | Descripción |
| :--- | :--- |
| **Idle** | El NPC está en su rutina normal: patrulla, conversa, trabaja o permanece en su puesto. |
| **Sospecha** | El NPC detecta algo raro: presencia extraña, comportamiento incorrecto, arma visible, zona indebida, ruido o cuerpo sospechoso. |
| **Alerta** | NPCs civiles, invitados o personal no armado avisan a seguridad o activan una alarma. |
| **Combate** | Guardias u oficiales identifican al jugador como amenaza y lo atacan. |

## Rutas de resolución

| Ruta | Descripción |
| :--- | :--- |
| **Ruta social** | Usar disfraces, escuchar conversaciones y moverse entre NPCs para llegar al objetivo sin violencia. |
| **Ruta técnica** | Usar ganzúa, cámaras, puertas, llaves, códigos y documentos para abrir accesos alternativos. |
| **Ruta agresiva** | Noquear guardias, esconder cuerpos y avanzar con mayor riesgo de detección y alerta. |

## Gameloop
Reconocer el escenario -> Recolectar información/pistas -> Elegir enfoque (sigilo / disfraz / técnica / caos controlado / no-letal) -> Acercarse al objetivo evitando detección -> Completar el objetivo -> Extraerse -> Siguiente nivel.

## Visualización
La visualización de Proof of Life será en primera persona, con movimiento libre de cámara desde la mirada de T.O.M. Esta perspectiva busca aumentar la inmersión y la tensión del jugador, haciendo que cada esquina, puerta, pasillo o cámara de seguridad se perciba desde dentro del escenario.

Al no contar con una cámara externa al personaje, el jugador tendrá que observar cuidadosamente el entorno, escuchar sonidos, revisar rutas, mirar por esquinas y prestar atención a las señales visuales de los NPCs. Esto refuerza la sensación de infiltración y vulnerabilidad, ya que el jugador no puede ver todo el escenario al mismo tiempo.

La estética visual será low poly 3D, con escenarios claros y fáciles de leer. Cada zona deberá diferenciarse visualmente según su nivel de acceso: zonas públicas más abiertas e iluminadas, áreas de servicio más cerradas, pasillos restringidos con mayor presencia de guardias, salas de seguridad con cámaras y oficinas privadas donde se encuentran los objetivos o información importante.

La interfaz deberá acompañar esta visualización sin llenar demasiado la pantalla. Al tratarse de un juego de sigilo, la información visual debe ser clara pero discreta: estado de alerta, disfraz actual, objetivo activo, interacción disponible e inventario rápido. La prioridad es que el jugador pueda entender la situación sin romper la inmersión.