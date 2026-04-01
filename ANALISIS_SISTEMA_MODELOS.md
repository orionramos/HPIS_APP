# Análisis del Sistema de Gestión de Modelos Visuales - HPIS_APP

## 1. ARCHIVOS CLAVE IDENTIFICADOS

### Scripts Principales:
- **[FeedbackManager.cs](Assets/Scripts/Core/FeedbackManager.cs)** - Gestor central de todas las animaciones y medios
- **[CanvasAnchorPlacer.cs](Assets/Scripts/Core/CanvasAnchorPlacer.cs)** - Posicionador de canvas y anchor de animaciones
- **[UIReferenceHolder.cs](Assets/Scripts/Core/UIReferenceHolder.cs)** - Centralizador de referencias UI

### Recursos de Modelos:
- **[Assets/Models/Act1.fbx](Assets/Models/Act1.fbx)** - Modelo FBX para Actividad 1
- **[Assets/Models/Act3.fbx](Assets/Models/Act3.fbx)** - Modelo FBX para Actividad 3

### Prefabs Animados:
- **[Assets/Resources/AnimacionesPrefab/Prefab_Master_Actividad1.prefab](Assets/Resources/AnimacionesPrefab/Prefab_Master_Actividad1.prefab)**
  - Contiene la malla `MCH_Mesa` (línea 624)
  - Sistema de Animator controlado por `Act1_MasterController.controller`
  
- **[Assets/Resources/AnimacionesPrefab/Prefab_Master_Actividad3.prefab](Assets/Resources/AnimacionesPrefab/Prefab_Master_Actividad3.prefab)**
  - Contiene la malla `MCH_Mesa` (línea 807)
  - Sistema de Animator controlado por `Act3_MasterController.controller`

### Configuración JSON:
- **[Assets/StreamingAssets/feedback_database.json](Assets/StreamingAssets/feedback_database.json)**
  - Define todos los estados de animación (`animation_state`)
  - Formato: `"Prefab_Master_Actividad1|Act1_1"`
  - Incluye configuración de cámaras PiP específicas por paso

---

## 2. ARQUITECTURA DEL SISTEMA DE MODELOS

### Flujo de Carga de Animaciones:

```
FeedbackManager.ShowFeedback()
    ↓
ProcessStep() - Determina tipo de contenido
    ↓
    ├─ "animation_prefab" → ProcessAnimationPrefab()
    │   └─ Resources.Load<GameObject>("AnimacionesPrefab/NombrePrefab")
    │       └─ Instantiate en "animationContainer"
    │
    └─ "animation_state" → ProcessAnimationState()
        └─ Resources.Load<GameObject>("AnimacionesPrefab/Prefab_Master_ActivdadX")
            └─ Instantiate (solo si es diferente del cargado)
                └─ GetComponentInChildren<Animator>()
                    └─ Ejecuta clip específico del MasterController
```

### Gestión de Prefabs (Memoria Inteligente):

**Antes (Arquitectura antigua - animation_prefab):**
- Se cargaba UN prefab diferente por CADA estep
- Se destruía antes de cargar el siguiente
- Ineficiente para actividades con muchos pasos

**Ahora (Nueva arquitectura - animation_state):**
```csharp
// FeedbackManager.cs línea 497-509
// 1. Instancian SOLO si es diferente o no hay ninguno
if (_loadedMasterPrefabName != prefabName || currentAnimationPrefab == null)
{
    currentAnimationPrefab = Instantiate(prefabToLoad, animationContainer);
    _loadedMasterPrefabName = prefabName;  // Guardar nombre para comparar
}

// 2. Se mantiene en memoria durante toda la actividad
// 3. Solo cambia de CLIP de animación dentro del mismo Animator
rootAnimator.Play(clipName);  // Cambio eficiente
```

**Reutilización dentro de la misma Actividad (línea 254-256):**
```csharp
if (parts[0] == _loadedMasterPrefabName)
{
    keepMasterPrefab = true;  // Conservar el prefab
}
```

---

## 3. ANIMACIÓN DEL ANCHOR DE ANIMACIONES

### Propósito:
El **`animationAnchorVisualPrefab`** es un visualizador temporal que muestra dónde se colocará el anchor de animaciones mientras el usuario lo está posicionando.

### Código Relevante [CanvasAnchorPlacer.cs](Assets/Scripts/Core/CanvasAnchorPlacer.cs):

**Variables (líneas 46, 65-67):**
```csharp
[SerializeField] private GameObject animationAnchorVisualPrefab;
private GameObject _animationAnchorObject;
private GameObject _animationAnchorVisualInstance;
private bool _isAnimationAnchored = false;
```

**Instanciación (línea 482):**
```csharp
_animationAnchorVisualInstance = Instantiate(
    animationAnchorVisualPrefab, 
    _animationAnchorObject.transform
);
```

**Destrucción al Anclar (línea 411-413):**
```csharp
if (_animationAnchorVisualInstance != null)
{
    Destroy(_animationAnchorVisualInstance);
    _animationAnchorVisualInstance = null;
}
```

### Vinculación con FeedbackManager (línea 419-422):
```csharp
var feedbackManager = FindFirstObjectByType<FeedbackManager>();
if (feedbackManager != null)
{
    feedbackManager.SetAnimationAnchor(_animationAnchorObject.transform);
}
```

---

## 4. ESTRUCTURA DEL MASTER PREFAB (Actividad 1)

```
Prefab_Master_Actividad1
├─ Act1 (GameObject raíz con Animator)
│  ├─ Skeletons (Rig/Armadura importada de Act1.fbx)
│  │  ├─ Armature
│  │  │  ├─ MCH_Mesa (MALLA PRINCIPAL)
│  │  │  ├─ Pelvis
│  │  │  ├─ spine.001
│  │  │  ├─ spine.002
│  │  │  └─ ... (resto del esqueleto)
│  │  └─ Act1 (Mesh Renderer)
│  │
│  ├─ [Animators]
│  ├─ [PiPDisplayController] - Sistema de cámaras multicámara
│  │  ├─ Camara_Protesis
│  │  ├─ Camara_ManoSana
│  │  └─ Camara_General
│  │
│  └─ [Act1_MasterController] (Animator Controller)
│     ├─ Act1_1 (clip)
│     ├─ Act1_2 (clip)
│     ├─ Act1_3 (clip)
│     ├─ Act1_4 (clip)
│     ├─ Act1_5 (clip)
│     └─ Act1_6 (clip)
```

### Malla MCH_Mesa:
- **Localización**: Dentro de la estructura de armadura de Act1.fbx
- **Propósito**: Renderización de la mesa (objeto de contexto visual)
- **Animación**: Está vinculada al esqueleto, permite animaciones de mesa si es necesario
- **Persistencia**: Permanece en el prefab durante toda la actividad

---

## 5. CONTROLADORES DE ANIMACIÓN

### Act1_MasterController.controller
```
Estados de Animación:
- Act1_1 (clip de animación específico)
- Act1_2 (clip de animación específico)
- Act1_3 (clip de animación específico)
- Act1_4 (clip de animación específico)
- Act1_5 (clip de animación específico)
- Act1_6 (clip de animación específico)
```

**Cargado en FeedbackManager línea 518:**
```csharp
Animator rootAnimator = currentAnimationPrefab.GetComponentInChildren<Animator>();
```

**Reproducción de clips (línea 548):**
```csharp
rootAnimator.Play(clipName);  // Ej: "Act1_1"
```

### Sistema PiP (Picture-in-Picture)
**Localización**: [FeedbackManager.cs](Assets/Scripts/Core/FeedbackManager.cs) línea 527-531

```csharp
var pipController = currentAnimationPrefab.GetComponentInChildren<HPIS.Core.Visuals.PiPDisplayController>();
if (pipController != null)
{
    pipController.ActivatePiP(pipCameraName);  // Ej: "Camara_Protesis"
}
```

---

## 6. BASE DE DATOS DE FEEDBACK (feedback_database.json)

**Estructura de animación_state:**
```json
{
  "id": 1,
  "contentType": "animation_state",
  "contentValue": "Prefab_Master_Actividad1|Act1_1",
  "pip_camera": "Camara_Protesis"
}
```

- **Prefab_Master_Actividad1**: Nombre del prefab Master a cargar
- **Act1_1**: Nombre del clip/estado del Animator a reproducir
- **Camara_Protesis**: Cámara PiP específica para este paso

**Ejemplos en archivo:**
- Líneas 71-76: Actividad 1 (6 pasos)
- Líneas 383-395: Actividad 3 (13 pasos)

---

## 7. FLUJO DE POSICIONAMIENTO DEL ANCHOR

```
CanvasAnchorPlacer.Start()
    ↓
StartPlacement()
    ├─ Instantiate(canvasPrefab)
    ├─ Usuario posiciona canvas con controles
    └─ [Presiona 'A'/Button_One] → PlaceAndAnchorCanvas()
        ├─ AddComponent<OVRSpatialAnchor>()
        ├─ LinkManagers() - Conecta CanvasAnchorPlacer con:
        │  ├─ DataManager
        │  ├─ WebSocketClient
        │  ├─ UIManager
        │  └─ FeedbackManager
        └─ StartAnimationAnchorPlacement()
            ├─ Instantiate(animationAnchorVisualPrefab)
            ├─ Usuario posiciona anchor de animaciones
            └─ [Presiona 'Enter'] → AnchorAnimationAnchor()
                ├─ AddComponent<OVRSpatialAnchor>()
                ├─ Destroy(animationAnchorVisualPrefab)
                └─ feedbackManager.SetAnimationAnchor(_animationAnchorObject)
                    └─ Ahora FeedbackManager instancia animaciones EN ESTE LUGAR
```

---

## 8. CARGA Y INSTANCIACIÓN DE MODELOS

### Método 1: Resources.Load (Actual)
```csharp
// FeedbackManager.cs línea 387, 497
string resourcePath = $"AnimacionesPrefab/{prefabName}";
var prefabToLoad = Resources.Load<GameObject>(resourcePath);
currentAnimationPrefab = Instantiate(prefabToLoad, animationContainer);
```

**Ventajas:**
- Rápido para acceso frecuente
- Los prefabs se mantienen en memoria caché
- No requiere rutas complejas

**Ubicación esperada:**
- `Assets/Resources/AnimacionesPrefab/*.prefab`

### Método 2: Integración de Modelos FBX
Los archivos `.fbx` (Act1.fbx, Act3.fbx) se importan en los prefabs Master:

1. **Importación directa en prefab:**
   - Los modelos se arrastran al Scene
   - Se crean prefabs que contienen estos modelos
   - Los Animators se vinculan a los controladores (Act1_MasterController, Act3_MasterController)

2. **Puntos clave:**
   - El modelo importado contiene el `rig` con MCH_Mesa
   - El Animator busca automáticamente el `Animator` component
   - Los clips se generan a partir de las animaciones del FBX

---

## 9. GESTIÓN DE ESTADO DE OBJETOS

**Método SaveStatefulObjects (línea 134-137):**
```csharp
SaveStatefulObjects(currentAnimationPrefab);  // Antes de destruir
Destroy(currentAnimationPrefab);
```

**Propósito:**
- Guardar posiciones y rotaciones de objetos animados
- Restaurar estos estados si se recarga el mismo prefab
- Evitar "saltos" visuales al cambiar entre clips

---

## 10. DIAGRAMA DE DEPENDENCIAS

```
feedback_database.json
    ↓ (Define qué animar)
FeedbackManager.ShowFeedback()
    ↓ (Procesa steps)
ProcessAnimationState()
    ↓ (Carga prefab)
Resources.Load("AnimacionesPrefab/Prefab_Master_Actividad1")
    ↓ (Obtiene animator)
GetComponentInChildren<Animator>()
    ↓ (Ejecuta clip)
rootAnimator.Play("Act1_1")
    ↓ (Aplica transformaciones)
MCH_Mesa + esqueleto se animan
    ↓ (Activa cámara PiP)
PiPDisplayController.ActivatePiP("Camara_Protesis")
```

---

## 11. RESUMEN DE COMPONENTES VISUALES

| Componente | Ubicación | Propósito | Ciclo de Vida |
|-----------|----------|----------|--------------|
| **animationAnchorVisualPrefab** | Campo en CanvasAnchorPlacer | Visualizador temporal del anchor | Se instancia → Se posiciona → Se destruye |
| **Prefab_Master_Actividad1** | Resources/AnimacionesPrefab/ | Contenedor de modelo + animator | Se carga 1x por actividad |
| **Act1.fbx** | Assets/Models/ | Modelo 3D con rig y MCH_Mesa | Se importa en el prefab Master |
| **MCH_Mesa** | Dentro del rig de Act1.fbx | Malla visible de mesa | Permanece durante actividad |
| **Act1_MasterController** | Assets/Animations/ | Controlador de estados de anim. | Controla qué clip se reproduce |

---

## 12. PUNTOS DE EXTENSIÓN

Si necesitas modificar el sistema de modelos:

1. **Añadir nuevo prefab animado:**
   - Crear `Prefab_Master_ActividadX.prefab` en `Resources/AnimacionesPrefab/`
   - Asignar `ActX_MasterController`
   - Agregar entrada en `feedback_database.json`

2. **Cambiar posición del anchor de animaciones:**
   - En `CanvasAnchorPlacer`: ajustar `animationAnchorContainer` o usar los offset del editor

3. **Agregar más cámaras PiP:**
   - Crear nuevos GameObjects en el prefab Master
   - Registrarlos en `PiPDisplayController`
   - Referenciarlos en `feedback_database.json`

4. **Optimizar memoria:**
   - La arquitectura `animation_state` ya reutiliza prefabs
   - Para múltiples modelos simultáneos, considerar object pooling

---

## 13. REFERENCIAS A CÓDIGO RELACIONADO

### Todas las búsquedas realizadas encontraron 20+ coincidencias:
- **animationAnchor**: FeedbackManager.cs, CanvasAnchorPlacer.cs
- **animationAnchorVisualPrefab**: CanvasAnchorPlacer.cs (línea 46, 480-482)
- **MCH_Mesa**: Prefab_Master_Actividad1.prefab, Prefab_Master_Actividad3.prefab
- **Act1_1, Act1_2, ... Act1_6**: En feedback_database.json y Act1_MasterController
- **Act3_1 ... Act3_13**: En feedback_database.json y Act3_MasterController

---

**Fecha de análisis:** 31 de marzo de 2026  
**Versión del proyecto:** HPIS_APP (Unity)
**Estado:** Sistema completo de animación con arquitectura Master Prefab implementada
