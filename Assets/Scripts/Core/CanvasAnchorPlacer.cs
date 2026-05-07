using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace HPIS.Anchors
{
    /// <summary>
    /// Instancia un prefab de canvas, permite al usuario posicionarlo en tiempo real
    /// siguiendo un transform (como un controlador). Al presionar 'A' (OVRInput.Button.One), 
    /// crea un OVRSpatialAnchor, y conecta el UIReferenceHolder a los managers.
    /// Al presionar 'B' (OVRInput.Button.Two), destruye el ancla y reinicia el proceso.
    /// El joystick derecho ajusta la escala (izq/der) y la profundidad (arr/abj).
    /// </summary>
    public class CanvasAnchorPlacer : MonoBehaviour
    {
        [Header("Prefab del Canvas")]
        [Tooltip("Arrastra aquí tu prefab del Canvas que contiene el UIReferenceHolder.")]
        [SerializeField] private GameObject canvasPrefab;

        [Header("Controlador")]
        [Tooltip("El transform del controlador que guiará el posicionamiento del canvas.")]
        [SerializeField] private Transform controllerTransform;

        [Header("Posicionamiento Canvas")]
        [Tooltip("Desplazamiento posicional local del canvas respecto al origen del controlador.")]
        [SerializeField] private Vector3 canvasPositionOffset = new Vector3(0.0f, 0.1f, 0.5f);

        [Tooltip("Desplazamiento rotacional local del canvas respecto a la orientación del controlador.")]
        [SerializeField] private Vector3 canvasRotationOffset = new Vector3(15.0f, 0.0f, 0.0f);

        [Header("Ajustes en Tiempo Real")]
        [Tooltip("Velocidad con la que cambia la escala con el joystick.")]
        [SerializeField] private float scaleSpeed = 0.6f;
        [Tooltip("Velocidad con la que cambia la profundidad (eje Z) con el joystick.")]
        [SerializeField] private float depthSpeed = 0.6f;

        [Header("Animation Anchor")]
        [Tooltip("El transform que contendrá el anchor para las animaciones")]
        [SerializeField] private Transform animationAnchorContainer;
        [Header("Animation Anchor Visual")]
        [Tooltip("Prefab visual para mostrar la posición del anchor de animaciones mientras se posiciona.")]
        [SerializeField] private GameObject animationAnchorVisualPrefab;
        
        [Header("Mesa como Visualizador")]
        [Tooltip("Si está habilitado, usa solo la mesa (MCH_Mesa) de Act1.fbx como visualizador de animaciones.")]
        [SerializeField] private bool useMesaAsVisualizer = true;
        [Tooltip("Modelo FBX que contiene la mesa. Por defecto: Act1.fbx")]
        [SerializeField] private GameObject mesaModel;
        
        [Header("Animation Anchor Controls")]
        [Tooltip("Velocidad de rotación del anchor de animaciones con el joystick secundario")]
        [SerializeField] private float rotationSpeed = 100f;
        
        [Header("Animation Anchor Scale")]
        [Tooltip("Escala mínima para el visualizador del anchor de animaciones.")]
        [SerializeField] private float minAnimationScale = 0.001f;
        [Tooltip("Escala inicial para el visualizador del anchor de animaciones. Es un multiplicador, 1 es el tamaño normal.")]
        [SerializeField] private float initialAnimationScale = 0.01f;

        // Variables para el movimiento del anchor de animaciones
        [Header("Animation Anchor Movement")]
        [Tooltip("Velocidad de movimiento del anchor de animaciones con el joystick derecho.")]
        [SerializeField] private float moveSpeed = 0.5f;

        private GameObject _canvasInstance;
        private bool _isAnchored = false;
        
        private GameObject _animationAnchorObject;
        private bool _isAnimationAnchored = false;
        private GameObject _animationAnchorVisualInstance;

        // Variables para la escala
        private Vector3 _initialScale;
        private float _currentScaleMultiplier = 1.0f;
        // Variables para la escala del anchor de animaciones
        private Vector3 _animationInitialScale = Vector3.one;
        private float _animationCurrentScaleMultiplier = 1.0f;
        private Vector3 _currentAnimationRotation = Vector3.zero;
        private Vector3 _animationAnchorOffset = Vector3.zero;
        private Vector3 _editorAnimationAnchorPosition = Vector3.zero;
        private Vector3 _editorAnimationAnchorPositionOffset = Vector3.zero;

        void Start()
        {
            if (canvasPrefab == null || controllerTransform == null)
            {
                Debug.LogError("[CanvasAnchorPlacer] El prefab del Canvas o el Transform del controlador no están asignados. Desactivando script.");
                enabled = false;
                return;
            }
            
            StartPlacement();
        }

        private void ApplyOffset()
        {
            if (_canvasInstance == null || controllerTransform == null) return;

            Quaternion targetRotation = controllerTransform.rotation * Quaternion.Euler(canvasRotationOffset);
            Vector3 targetPosition = controllerTransform.position + controllerTransform.TransformDirection(canvasPositionOffset);

            _canvasInstance.transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        private void ApplyOffsetToAnimationAnchor()
        {
#if !UNITY_EDITOR
            if (_animationAnchorObject == null || controllerTransform == null) return;

            // En el dispositivo, la posición se basa en el controlador + offset del joystick
            _animationAnchorObject.transform.position = controllerTransform.position + _animationAnchorOffset;
#endif
        }

        private void LateUpdate()
        {
            if (_isAnimationAnchored || _animationAnchorObject == null) return;

            // Calcula la rotación de ajuste final a partir de la variable de estado
            Quaternion offsetRotation = Quaternion.Euler(_currentAnimationRotation);

#if UNITY_EDITOR
            // En el editor, la rotación base se calcula a partir de un raycast desde la cámara/mouse
            Quaternion baseRotation;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            // Solo actualiza la rotación base si no nos estamos moviendo con las teclas
            bool isMovingWithKeyboard = Keyboard.current.wKey.isPressed ||
                                        Keyboard.current.sKey.isPressed ||
                                        Keyboard.current.aKey.isPressed ||
                                        Keyboard.current.dKey.isPressed;

            if (!isMovingWithKeyboard && Physics.Raycast(ray, out RaycastHit hit))
            {
                baseRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 180, 0);
            }
            else
            {
                baseRotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
            _animationAnchorObject.transform.rotation = baseRotation * offsetRotation;
#else
            // En el dispositivo, la rotación base viene del controlador
            if (controllerTransform != null)
            {
                _animationAnchorObject.transform.rotation = controllerTransform.rotation * offsetRotation;
            }
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            // Lógica para el editor de Unity (sin gafas)
            if (Keyboard.current.spaceKey.wasPressedThisFrame) // Anclar con Barra Espaciadora
            {
                if (!_isAnchored)
                {
                    PlaceAndAnchorCanvas();
                }
            }

            if (Keyboard.current.rKey.wasPressedThisFrame) // Resetear con 'R'
            {
                ResetPlacement();
            }

            // Anclar animación con Enter
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                if (_isAnchored && !_isAnimationAnchored && _animationAnchorObject != null)
                {
                    PlaceAndAnchorAnimation();
                }
            }

            if (!_isAnchored && _canvasInstance != null)
            {
                EditorUpdateCanvasPosition();
            }
            else if (_isAnchored && !_isAnimationAnchored && _animationAnchorObject != null)
            {
                EditorUpdateAnimationAnchorPosition();
                HandleEditorAnimationAnchorInput();
            }
#else
            // Lógica para el dispositivo (Meta Quest)
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                if (!_isAnchored)
                {
                    PlaceAndAnchorCanvas();
                }
            }
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                if (_isAnchored)
                {
                    ResetPlacement();
                }
            }
            // Animación: usar el gatillo derecho para fijar el anchor
            if (!_isAnimationAnchored && _animationAnchorObject != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                PlaceAndAnchorAnimation();
            }
            if (!_isAnchored && _canvasInstance != null)
            {
                HandleCanvasJoystickInput();
                ApplyOffset();
            }
            else if (!_isAnimationAnchored && _animationAnchorObject != null)
            {
                HandleAnimationAnchorJoystickInput();
                ApplyOffsetToAnimationAnchor();
            }
#endif
        }

#if UNITY_EDITOR
        private void EditorUpdateCanvasPosition()
        {
            if (_canvasInstance == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Posiciona el canvas en el punto de colisión
                _canvasInstance.transform.position = hit.point;
                // Orienta el canvas para que mire hacia la cámara, alineado con la normal de la superficie
                _canvasInstance.transform.rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Si no hay colisión, posiciona el canvas a una distancia fija de la cámara
                _canvasInstance.transform.position = ray.GetPoint(10.0f);
                _canvasInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
        }

        private void EditorUpdateAnimationAnchorPosition()
        {
            if (_animationAnchorObject == null) return;

            // Solo actualiza con raycast si ninguna tecla WASD está presionada
            bool isMovingWithKeyboard = Keyboard.current.wKey.isPressed ||
                                        Keyboard.current.sKey.isPressed ||
                                        Keyboard.current.aKey.isPressed ||
                                        Keyboard.current.dKey.isPressed;

            if (!isMovingWithKeyboard)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _editorAnimationAnchorPosition = hit.point;
                }
                else
                {
                    _editorAnimationAnchorPosition = ray.GetPoint(10.0f);
                }
            }

            // Aplica posición base + offset generado por WASD
            _animationAnchorObject.transform.position = _editorAnimationAnchorPosition + _editorAnimationAnchorPositionOffset;
        }

        private void HandleEditorAnimationAnchorInput()
        {
            if (_animationAnchorObject == null) return;

            // W/S: Movimiento en Z (adelante/atrás)
            if (Keyboard.current.wKey.isPressed)
            {
                _editorAnimationAnchorPositionOffset += _animationAnchorObject.transform.forward * moveSpeed * Time.deltaTime;
            }
            if (Keyboard.current.sKey.isPressed)
            {
                _editorAnimationAnchorPositionOffset -= _animationAnchorObject.transform.forward * moveSpeed * Time.deltaTime;
            }

            // A/D: Movimiento en X (izquierda/derecha)
            if (Keyboard.current.aKey.isPressed)
            {
                _editorAnimationAnchorPositionOffset -= _animationAnchorObject.transform.right * moveSpeed * Time.deltaTime;
            }
            if (Keyboard.current.dKey.isPressed)
            {
                _editorAnimationAnchorPositionOffset += _animationAnchorObject.transform.right * moveSpeed * Time.deltaTime;
            }

            // Q/E: Rotación en Y (izquierda/derecha)
            if (Keyboard.current.qKey.isPressed)
            {
                _currentAnimationRotation.y -= rotationSpeed * Time.deltaTime;
            }
            if (Keyboard.current.eKey.isPressed)
            {
                _currentAnimationRotation.y += rotationSpeed * Time.deltaTime;
            }

            // Z/X: Rotación en X (arriba/abajo)
            if (Keyboard.current.zKey.isPressed)
            {
                _currentAnimationRotation.x += rotationSpeed * Time.deltaTime;
            }
            if (Keyboard.current.xKey.isPressed)
            {
                _currentAnimationRotation.x -= rotationSpeed * Time.deltaTime;
            }

            // Scroll del mouse para escala
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                float scrollDirection = Mathf.Sign(scrollDelta);
                _animationCurrentScaleMultiplier += scrollDirection * scaleSpeed * Time.deltaTime;
                _animationCurrentScaleMultiplier = Mathf.Max(minAnimationScale, _animationCurrentScaleMultiplier);
                _animationAnchorObject.transform.localScale = _animationInitialScale * _animationCurrentScaleMultiplier;
            }
        }
#endif

        private void HandleCanvasJoystickInput()
        {
            Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            // Izquierda/Derecha: Cambiar la escala del canvas
            if (Mathf.Abs(thumbstick.x) > 0.1f)
            {
                _currentScaleMultiplier += thumbstick.x * scaleSpeed * Time.deltaTime;
                _currentScaleMultiplier = Mathf.Max(0.1f, _currentScaleMultiplier);
                _canvasInstance.transform.localScale = _initialScale * _currentScaleMultiplier;
            }

            // Arriba/Abajo: Cambiar la profundidad (offset en Z) del canvas
            if (Mathf.Abs(thumbstick.y) > 0.1f)
            {
                canvasPositionOffset.z += thumbstick.y * depthSpeed * Time.deltaTime;
            }
        }

        private void HandleAnimationAnchorJoystickInput()
        {
            // Joystick derecho para mover y escalar
            Vector2 rightThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            // Si el usuario mantiene presionado el gatillo derecho, X controla la escala
            if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                if (Mathf.Abs(rightThumbstick.x) > 0.1f)
                {
                    _animationCurrentScaleMultiplier += rightThumbstick.x * scaleSpeed * Time.deltaTime;
                    _animationCurrentScaleMultiplier = Mathf.Max(minAnimationScale, _animationCurrentScaleMultiplier);
                    if (_animationAnchorObject != null)
                    {
                        _animationAnchorObject.transform.localScale = _animationInitialScale * _animationCurrentScaleMultiplier;
                    }
                }
            }
            else
            {
                // Movimiento en el espacio (X: derecha/izquierda, Y: adelante/atrás)
                if (Mathf.Abs(rightThumbstick.x) > 0.1f)
                {
                    _animationAnchorOffset += controllerTransform.right * rightThumbstick.x * moveSpeed * Time.deltaTime;
                }
                if (Mathf.Abs(rightThumbstick.y) > 0.1f)
                {
                    _animationAnchorOffset += controllerTransform.forward * rightThumbstick.y * moveSpeed * Time.deltaTime;
                }
            }

            // Joystick izquierdo para rotación
            Vector2 leftThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
            if (leftThumbstick.magnitude > 0.1f)
            {
                _currentAnimationRotation.y += leftThumbstick.x * rotationSpeed * Time.deltaTime;
                _currentAnimationRotation.x -= leftThumbstick.y * rotationSpeed * Time.deltaTime;
                _currentAnimationRotation.x = Mathf.Repeat(_currentAnimationRotation.x, 360f);
                _currentAnimationRotation.y = Mathf.Repeat(_currentAnimationRotation.y, 360f);
            }
        }

        private void StartPlacement()
        {
            _isAnchored = false;
            _canvasInstance = Instantiate(canvasPrefab, Vector3.zero, Quaternion.identity);
            _canvasInstance.name = "CanvasPreview";
            // Guardar la escala inicial y resetear el multiplicador
            _initialScale = _canvasInstance.transform.localScale;
            _currentScaleMultiplier = 1.0f;
            // Resetear escala de animación
            _animationInitialScale = Vector3.one;
            _animationCurrentScaleMultiplier = 1.0f;
#if !UNITY_EDITOR
            ApplyOffset();
#endif
            Debug.Log("[CanvasAnchorPlacer] Mueve el mouse para posicionar el canvas. 'Espacio' para anclar. 'R' para reiniciar.");
        }

        private void PlaceAndAnchorAnimation()
        {
            if (_animationAnchorObject == null || _isAnimationAnchored) return;

            _isAnimationAnchored = true;
            Debug.Log("[CanvasAnchorPlacer] Posición del anchor de animaciones fijada.");

            var anchor = _animationAnchorObject.AddComponent<OVRSpatialAnchor>();
            _animationAnchorObject.name = "AnimationAnchor_HPIS";

            // Destruir el visualizador
            if (_animationAnchorVisualInstance != null)
            {
                Destroy(_animationAnchorVisualInstance);
                _animationAnchorVisualInstance = null;
            }

            // Notificar al FeedbackManager sobre el nuevo anchor
            var feedbackManager = FindFirstObjectByType<FeedbackManager>();
            if (feedbackManager != null)
            {
                feedbackManager.SetAnimationAnchor(_animationAnchorObject.transform);
            }
        }

        private void PlaceAndAnchorCanvas()
        {
            if (_canvasInstance == null || _isAnchored) return;

            _isAnchored = true;
            Debug.Log("[CanvasAnchorPlacer] Posición fijada. Creando ancla y conectando managers...");
            var anchor = _canvasInstance.AddComponent<OVRSpatialAnchor>();
            _canvasInstance.name = "AnchoredCanvas_HPIS";
            
            var uiHolder = _canvasInstance.GetComponent<UIReferenceHolder>();
            if (uiHolder == null)
            {
                Debug.LogError("[CanvasAnchorPlacer] ¡Crítico! El prefab del canvas no tiene UIReferenceHolder. No se pueden conectar los managers.");
                return;
            }

            LinkManagers(uiHolder);

            // After anchoring the canvas, automatically start the animation anchor placement
            StartAnimationAnchorPlacement();
        }

        public void ResetPlacement()
        {
            if (_canvasInstance != null)
            {
                Destroy(_canvasInstance);
            }
            
            Debug.Log("[CanvasAnchorPlacer] Ancla anterior destruida. Reiniciando posicionamiento.");
            StartPlacement();
        }

        public void StartAnimationAnchorPlacement()
        {
            if (_animationAnchorObject != null)
            {
                Destroy(_animationAnchorObject);
            }
            if (_animationAnchorVisualInstance != null)
            {
                Destroy(_animationAnchorVisualInstance);
            }

            _isAnimationAnchored = false;
            _currentAnimationRotation = new Vector3(0, 180, 0); // Resetear la rotación a 180 grados en Y
            _animationAnchorOffset = Vector3.zero; // Resetear el offset
            _editorAnimationAnchorPosition = Vector3.zero; // Resetear posición del editor
            _editorAnimationAnchorPositionOffset = Vector3.zero; // Resetear offset de posición del editor
            _animationAnchorObject = new GameObject("AnimationAnchorPreview");
            _animationAnchorObject.transform.SetParent(animationAnchorContainer, false);
            
            // Establecer la escala inicial del anchor de la animación
            _animationCurrentScaleMultiplier = initialAnimationScale;
            _animationAnchorObject.transform.localScale = _animationInitialScale * _animationCurrentScaleMultiplier;

            if (animationAnchorVisualPrefab != null)
            {
                _animationAnchorVisualInstance = Instantiate(animationAnchorVisualPrefab, _animationAnchorObject.transform);
            }

            // Si useMesaAsVisualizer está habilitado, carga y muestra solo la mesa
            if (useMesaAsVisualizer)
            {
                InstanciarMesaComoVisualidor();
            }

#if !UNITY_EDITOR
            ApplyOffsetToAnimationAnchor();
#endif
            Debug.Log("[CanvasAnchorPlacer] Mueve el mouse para posicionar el anchor. WASD: Mover, Q/E: Rotar Y, Z/X: Rotar X, Scroll: Escala. 'Enter' para anclar.");
        }

        private void LinkManagers(UIReferenceHolder uiHolder)
        {
            var dataManager = FindFirstObjectByType<DataManager>();
            if (dataManager != null) { dataManager.uiHolder = uiHolder; }

            var wsClient = FindFirstObjectByType<WebSocketClient>();
            if (wsClient != null) { wsClient.uiHolder = uiHolder; }

            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null) { uiManager.uiHolder = uiHolder; }

            var feedbackManager = FindFirstObjectByType<FeedbackManager>();
            if (feedbackManager != null) { feedbackManager.uiHolder = uiHolder; }
            
            Debug.Log("Todos los managers han sido conectados al nuevo canvas.");
        }

        /// <summary>
        /// Carga y instancia la mesa (MCH_Mesa) del modelo FBX como visualizador del anchor de animaciones.
        /// Este método reemplaza el prefab visual estándar con solo la mesa para facilitar el ajuste.
        /// </summary>
        private void InstanciarMesaComoVisualidor()
        {
            if (_animationAnchorObject == null) return;

            // Si ya hay una instancia visual, la destruye
            if (_animationAnchorVisualInstance != null)
            {
                Destroy(_animationAnchorVisualInstance);
                _animationAnchorVisualInstance = null;
            }

            // Intenta cargar el modelo desde la ruta asignada o desde Assets/Models
            GameObject modeloAct1 = mesaModel;
            if (modeloAct1 == null)
            {
#if UNITY_EDITOR
                // En el editor, carga desde la ruta directa de Assets
                modeloAct1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Act1.fbx");
#else
                // En runtime, intenta desde Resources
                modeloAct1 = Resources.Load<GameObject>("Models/Act1");
#endif
            }

            if (modeloAct1 == null)
            {
                Debug.LogWarning("[CanvasAnchorPlacer] No se pudo cargar el modelo Act1.fbx. Asigna el modelo en el inspector o verifica la ruta Assets/Models/Act1.fbx");
                return;
            }

            // Instancia el modelo completo
            GameObject instanciaModelo = Instantiate(modeloAct1, _animationAnchorObject.transform);
            instanciaModelo.name = "Act1_Model";

            // Desactiva TODO primero
            DesactivarRecursivo(instanciaModelo.transform);

            // Busca la mesa y el plato
            Transform mesaTransform = BuscarPorNombre(instanciaModelo.transform, "MCH_Mesa");
            Transform platoTransform = BuscarPorNombre(instanciaModelo.transform, "MCH_plato");
            Transform cuerpoTransform = BuscarPorNombre(instanciaModelo.transform, "MCH_cuerpo");

            if (mesaTransform != null)
            {
                // Activa la mesa y todos sus ancestros y descendientes
                ActivarConAncestros(mesaTransform);
                _animationAnchorVisualInstance = instanciaModelo;
                Debug.Log("[CanvasAnchorPlacer] Mesa (MCH_Mesa) cargada como visualizador.");
            }

            if (platoTransform != null)
            {
                // Activa el plato y todos sus ancestros y descendientes
                ActivarConAncestros(platoTransform);
                Debug.Log("[CanvasAnchorPlacer] Plato (MC_Plato) agregado como referencia de orientación.");
            }

            if (cuerpoTransform != null)
            {
                // Activa el cuerpo y todos sus ancestros y descendientes
                ActivarConAncestros(cuerpoTransform);
                Debug.Log("[CanvasAnchorPlacer] Cuerpo (MCH_cuerpo) agregado como referencia de orientación.");
            }

            if (mesaTransform == null && platoTransform == null && cuerpoTransform == null)
            {
                Debug.LogWarning("[CanvasAnchorPlacer] No se encontraron MCH_Mesa, MC_Plato ni MCH_cuerpo. Mostrando modelo completo.");
                ActivarRecursivo(instanciaModelo.transform);
            }

            if (_animationAnchorVisualInstance == null)
            {
                _animationAnchorVisualInstance = instanciaModelo;
            }
        }

        /// <summary>
        /// Desactiva recursivamente todos los GameObjects de un árbol.
        /// </summary>
        private void DesactivarRecursivo(Transform root)
        {
            root.gameObject.SetActive(false);
            foreach (Transform child in root)
            {
                DesactivarRecursivo(child);
            }
        }

        /// <summary>
        /// Busca recursivamente un GameObject con el nombre específico.
        /// </summary>
        private Transform BuscarPorNombre(Transform root, string nombre)
        {
            if (root.name == nombre)
                return root;

            foreach (Transform child in root)
            {
                Transform resultado = BuscarPorNombre(child, nombre);
                if (resultado != null)
                    return resultado;
            }
            return null;
        }

        /// <summary>
        /// Activa un transform, todos sus ancestros hasta la raíz, todos sus hijos.
        /// Esto asegura que sea visible en la jerarquía.
        /// </summary>
        private void ActivarConAncestros(Transform target)
        {
            // Activa el target y todos sus hijos
            ActivarRecursivo(target);

            // Activa todos los ancestros hasta la raíz
            Transform actual = target.parent;
            while (actual != null)
            {
                actual.gameObject.SetActive(true);
                actual = actual.parent;
            }
        }

        /// <summary>
        /// Activa recursivamente un GameObject y todos sus hijos.
        /// </summary>
        private void ActivarRecursivo(Transform root)
        {
            root.gameObject.SetActive(true);
            foreach (Transform child in root)
            {
                ActivarRecursivo(child);
            }
        }
    }
}