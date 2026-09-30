# ND SPORT RACING

Juego de carreras en Unity. Equipo: **Hernán + Darío**.

**Objetivo de la beta:** 6–8 semanas. Juego jugable de principio a fin con 1 vehículo, 1 pista y las mecánicas fundamentales funcionando.

> **Regla fundamental:** No se agrega una nueva mecánica importante hasta que la anterior funcione de principio a fin.

Estrategia: construir primero un **vertical slice** (auto → pista → física → carrera → mecánica básica → menú) y recién después escalar a los tres modos. Los tres modos comparten sistemas, no son tres juegos distintos.

---

# 🔴 AHORA (Semana 0 y 1)

## Semana 0: preparación

- [ ] Repositorio en GitHub creado (`ND-SPORT-RACING`)
- [ ] Darío agregado como colaborador
- [ ] Proyecto Unity creado dentro del repo
- [ ] `.gitignore` de Unity
- [ ] README
- [ ] Ramas: `main`, `develop`, `hernan`, `dario`
- [ ] Protección de `main` y `develop`
- [ ] Tablero Kanban (GitHub Projects)
- [ ] Estructura de carpetas de Unity
- [ ] Reunión inicial hecha

## Reunión inicial (1–2 horas)

Definir y anotar aquí:

- [ ] Nombre provisional
- [ ] Estilo visual
- [ ] Cámara (tercera persona, cabina, ambas)
- [ ] Plataforma objetivo
- [ ] Tipo de conducción (arcade, simulación o intermedio)
- [ ] Vehículo inicial
- [ ] Primera pista
- [ ] Qué mecánicas entran en la beta
- [ ] Qué queda fuera

## Semana 1: auto básico

Solo esto, nada de F1, tuning ni soldadura:

```
Input → VehicleController → Física → Ruedas → Rigidbody
```

El auto debe poder:

- [ ] Acelerar
- [ ] Frenar
- [ ] Girar
- [ ] Retroceder
- [ ] Derrapar mínimamente
- [ ] Tener cámara

Sobre una **TestTrack** muy simple (plano con muros). **Si esto no funciona, no se avanza.**

## Quién hace qué ahora

| | Darío (sistemas) | Hernán (juego / contenido) |
|---|---|---|
| Semana 0 | Arquitectura y Git | Diseño y documentación |
| Semana 1 | VehicleController | Auto y pista básica |

## Reglas de trabajo

**Git**

```
main
  │
  └── develop
       │
       ├── dario
       └── hernan
```

- Nadie trabaja directo sobre `main`.
- Cada uno trabaja en su rama.
- Funcionalidad terminada → Pull Request a `develop`.
- Solo versiones estables llegan a `main`.

**Reunión semanal (30–60 min)**

1. ¿Qué hicimos?
2. ¿Qué funciona?
3. ¿Qué está roto?
4. ¿Qué haremos esta semana?
5. ¿Qué debemos eliminar del alcance?

Si una característica no entra en la beta, se manda al backlog.

**Tablero**

```
BACKLOG → POR HACER → EN DESARROLLO → TESTING → TERMINADO
```

---

# 🟡 PRÓXIMAS SEMANAS (2 a 8)

## Roadmap

| Semana | Darío | Hernán | Resultado |
|---|---|---|---|
| 2 | Física / ruedas | Test Track | Física funcional |
| 3 | Motor / transmisión | Modelos / UI inicial | Auto completo |
| 4 | Mecánica / daño | Garage | Sistema mecánico |
| 5 | RaceManager / IA | Pista / carrera | Carrera completa |
| 6 | Integración | UI / Garage | Vertical Slice |
| 7 | Testing / bugs | Balance / contenido | Beta candidata |
| 8 | Optimización | Pulido | BETA |

## Semana 2: física de ruedas y terreno

La física **no** va dentro del terreno. Cada superficie es un objeto de datos (`SurfaceData`) y la rueda lo consulta:

```
Raycast → ¿Qué superficie golpeé? → SurfaceData → Modifica el neumático
```

Valores de ejemplo (no finales):

| Superficie | Grip | RollingResistance |
|---|---|---|
| Asfalto | 1.00 | 0.015 |
| Tierra | 0.65 | 0.08 |
| Pasto | 0.45 | 0.12 |
| Arena | 0.30 | 0.20 |

Cada rueda:

```
Wheel
├── Suspension
├── Tire
├── SurfaceDetector
├── Grip
├── Slip
└── Temperature
```

## Pruebas obligatorias en la TestTrack

1. 0 → 100 km/h
2. Frenado
3. Curva
4. Asfalto → tierra
5. Tierra → pasto
6. Salto
7. Colisión
8. Diferentes neumáticos

Cada semana se revisa una lista, no un "creo que funciona":

```
FÍSICA
[ ] Acelera
[ ] Frena
[ ] Gira
[ ] Suspensión
[ ] Asfalto
[ ] Tierra
```

## Semana 3: motor y transmisión

No se simula cada explosión del cilindro, solo un modelo suficientemente bueno para gameplay.

Cada motor es un objeto de datos (`EngineData`): nombre, peso, curva de torque (RPM → Torque), IdleRPM, Redline, potencia, cilindrada, confiabilidad, vehículos compatibles. Nada de `if (engine == "V8")`.

```
Engine → Clutch → Transmission → Differential → Wheels
```

La transmisión: marchas 1–6, reversa, `GearRatio` y `FinalDrive`.

## Semana 4: mecánica

No es un simulador de taller. Primera versión:

```
Engine
├── Block
├── Head
├── Pistons
├── Crankshaft
├── Camshaft
├── SparkPlugs
├── FuelSystem
└── Cooling
```

Cada pieza tiene `Condition: 0–100%`.

Reparación:

```
Seleccionar daño → Diagnóstico → ¿Reparable? → Reparación
→ Minijuego / barra de precisión → Resultado
```

## Semana 5: carrera e IA

`RaceManager` controla: countdown, starting grid, checkpoints, vueltas, posición, timer, finish y resultados.

IA de la beta (nada hiperinteligente):

- `RacingLine`: puntos P1 → P2 → P3 que la IA sigue
- `TargetSpeed` por sección (recta 180, curva 90, curva cerrada 60 km/h)

## Semana 6: garage

Pantalla con motor, transmisión, suspensión y neumáticos, y botones Modificar / Reparar / Probar.

## Alcance real de la beta

- **Vehículos:** 1 completo, 2–3 motores, 2 configuraciones de transmisión
- **Pista:** 1 principal con asfalto, tierra y pasto
- **Física:** aceleración, frenado, dirección, suspensión, tracción, derrape, grip por superficie, transferencia de peso básica
- **Carrera:** grid, cuenta regresiva, vueltas, checkpoints, posiciones, cronómetro, final
- **Mecánica:** desmontar motor, cambiar piezas, reparar pieza, instalar otro motor, condición de componentes
- **UI:** menú principal, selección de carrera, garage, HUD, resultados

## Modos en la beta

| Modo | Estado en la beta |
|---|---|
| 🏁 Carrera clandestina | Completo |
| 🏆 Carreras oficiales | Versión básica |
| 🏎️ F1 | Prototipo experimental |

## Flujo del jugador al final de la beta

```
MENÚ → GARAGE → Selecciona vehículo → Revisa motor → Cambia motor
→ Repara una pieza → Configura neumáticos → Carrera clandestina
→ Pista (asfalto, tierra, curva, derrape) → Meta → Resultados → Garage
```

---

# 🟢 DESPUÉS DE LA BETA (backlog)

No se construye ahora, pero la arquitectura debe quedar preparada.

- Humedad y clima: `FinalGrip = BaseGrip × TireCondition × WeatherModifier × TemperatureModifier`
- Más vehículos y más motores
- Turbo y supercharger
- AWD / RWD / FWD y diferenciales
- Temperatura de neumáticos
- Lluvia y neumáticos especializados
- F1 completo
- Carreras oficiales completas
- Economía, NPCs, delivery y trabajos
- Tuning avanzado

---

# 📚 REFERENCIA

## Arquitectura general

```
GAME
│
├── Core
│   ├── GameManager
│   ├── SaveSystem
│   ├── Input
│   └── Audio
│
├── Vehicles
│   ├── VehicleController
│   ├── VehiclePhysics
│   ├── Engine
│   ├── Transmission
│   ├── Differential
│   ├── Suspension
│   ├── Wheel
│   ├── Brakes
│   └── Damage
│
├── Mechanics
│   ├── EngineParts
│   ├── Assembly
│   ├── Repair
│   ├── Welding
│   └── Diagnostics
│
├── Racing
│   ├── RaceManager
│   ├── Checkpoints
│   ├── LapSystem
│   ├── StartingGrid
│   ├── AI
│   └── Results
│
├── Tracks
│   ├── Asphalt
│   ├── Dirt
│   ├── Grass
│   └── Sand
│
├── UI
│   ├── MainMenu
│   ├── Garage
│   ├── RaceHUD
│   └── MechanicalUI
│
└── Scenes
    ├── Bootstrap
    ├── MainMenu
    ├── Garage
    ├── TestTrack
    └── Race01
```

## Separación del vehículo

Nada de un `VehicleController.cs` de 2.000 líneas:

```
VehicleController
├── Input
├── Engine
├── Transmission
├── Suspension
├── Wheels
├── Differential
└── Aerodynamics
```

## Convenciones

- Scripts en PascalCase (`VehicleController.cs`)
- Un script = una responsabilidad
- Nada de `autoFINAL2` 😂lidad
- Nada de `autoFINAL2` 😂
