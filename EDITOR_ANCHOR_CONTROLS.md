# Controles del Editor para Posicionamiento de Anchors

## Descripción General

El script `CanvasAnchorPlacer` permite posicionar el canvas y el anchor de animaciones tanto en **Meta Quest (con gafas VR)** como en el **editor de Unity (PC sin gafas)**.

Esta guía explica cómo utilizar los controles del editor para posicionar y configurar los anchors en tiempo de desarrollo.

## Flujo de Uso en el Editor

### 1. Posicionamiento del Canvas

Cuando inicias el script en el editor, primero debes posicionar el **canvas UI**.

| Control | Acción |
|---------|--------|
| **Mouse** | Mueve el canvas siguiendo el raycast de la cámara |
| **Barra Espaciadora** | Fija el canvas en su posición actual |
| **R** | Reinicia el proceso desde el principio |

**Notas:**
- El canvas se posiciona raycasteando desde la cámara hacia la escena
- Si no hay colisión, se coloca a una distancia fija (10 unidades)

### 2. Posicionamiento del Anchor de Animaciones

Una vez que fijas el canvas con **Barra Espaciadora**, automáticamente comienza el posicionamiento del **anchor de animaciones**.

#### Controles de Movimiento

| Control | Acción |
|---------|--------|
| **Mouse** | Establece la posición base del anchor (raycast) |
| **W** | Mueve adelante (eje Z positivo) |
| **S** | Mueve atrás (eje Z negativo) |
| **A** | Mueve izquierda (eje X negativo) |
| **D** | Mueve derecha (eje X positivo) |

**Comportamiento del Movimiento:**
- El mouse establece la **posición base**
- WASD se suma como un **offset** a la posición del mouse
- Al soltar WASD, el anchor mantiene la posición alcanzada
- Si mueves el mouse nuevamente, ese punto se convierte en la nueva posición base

#### Controles de Rotación

| Control | Acción |
|---------|--------|
| **Q** | Rota izquierda alrededor del eje Y |
| **E** | Rota derecha alrededor del eje Y |
| **Z** | Rota arriba alrededor del eje X |
| **X** | Rota abajo alrededor del eje X |

**Notas:**
- La rotación se aplica en el espacio local del objeto
- Ambos ejes pueden combinarse para rotaciones complejas

#### Controles de Escala

| Control | Acción |
|---------|--------|
| **Scroll del Mouse ↑** | Aumenta la escala |
| **Scroll del Mouse ↓** | Disminuye la escala |

**Notas:**
- La escala mínima es 0.1x
- Funciona en cualquier momento durante el posicionamiento

#### Control de Confirmación

| Control | Acción |
|---------|--------|
| **Enter** | Fija el anchor de animaciones en su posición final |

**Después de presionar Enter:**
- El anchor se convierte en un `OVRSpatialAnchor`
- Se conecta automáticamente al `FeedbackManager`
- Se termina el flujo de posicionamiento

## Resumen de Controles Rápido

```
┌─────────────────────────────────────────────────────────┐
│ FASE 1: Posicionamiento del Canvas                      │
├─────────────────────────────────────────────────────────┤
│ Mouse          → Posicionar canvas                      │
│ Espacio        → Fijar canvas y pasar a Fase 2         │
│ R              → Reiniciar desde el principio           │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ FASE 2: Posicionamiento del Anchor de Animaciones       │
├─────────────────────────────────────────────────────────┤
│ Mouse          → Posición base (raycast)               │
│ W/A/S/D        → Mover (offset a la posición base)     │
│ Q/E            → Rotar eje Y                            │
│ Z/X            → Rotar eje X                            │
│ Scroll         → Cambiar escala                         │
│ Enter          → Fijar anchor de animaciones           │
│ R              → Reiniciar todo el proceso             │
└─────────────────────────────────────────────────────────┘
```

## Ejemplo de Uso

1. **Inicia la escena** en el editor
2. **Posiciona el canvas**:
   - Mueve el mouse sobre la escena
   - Presiona **Espacio** cuando esté en el lugar correcto
3. **Posiciona el anchor**:
   - Mueve el mouse para establecer la posición base
   - Usa **WASD** para hacer ajustes finos
   - Usa **Q/E** para girar horizontalmente
   - Usa **Z/X** para girar verticalmente
   - Usa **Scroll** si necesitas cambiar el tamaño
4. **Confirma con Enter** para fijar la posición
5. **Presiona R** en cualquier momento si quieres reiniciar

## Notas Técnicas

- **Sistema de Offset**: El movimiento con WASD se suma a la posición del mouse, permitiendo una precisión sin perder la referencia visual
- **Raycast**: Se realiza desde la cámara principal hacia la escena
- **Rotación Local**: Todas las rotaciones se aplican en el espacio local del objeto
- **OVRSpatialAnchors**: Se crean automáticamente al confirmar la posición (solo en dispositivos VR)

## Troubleshooting

### El anchor no se mueve con WASD
- Verifica que el archivo esté compilando correctamente
- Asegúrate de que no hay errores en la consola de Unity
- Intenta presionar **R** para reiniciar

### El canvas desaparece después de presionar Espacio
- Es normal, se oculta cuando comienza el posicionamiento del anchor
- Solo el anchor de animaciones debe ser visible en esta fase

### El anchor retorna a la posición del mouse
- Estás presionando WASD mientras mueves el mouse
- Suelta WASD primero, luego mueve el mouse si deseas cambiar la posición base

## Integración en Proyecto

Este sistema solo funciona en el **editor de Unity** (cuando se ejecuta dentro de Unity).

Para dispositivos **Meta Quest**, se utiliza:
- **OVRInput** para los controles de gafas
- **Joysticks** para el movimiento
- **Gatillos** para confirmar posiciones

El código está condicionado con `#if UNITY_EDITOR` para separar ambos sistemas.
