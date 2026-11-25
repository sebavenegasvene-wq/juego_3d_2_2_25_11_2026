using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class movimiento : MonoBehaviour
{
    private Rigidbody rb;
    private int contador_coleccionables = 0;
    public Text puntaje;

    // Movimiento con aceleración
    public float velocidadCaminar = 8f;
    public float velocidadCorrer = 14f;
    public float aceleracion = 30f;
    public float frenado = 15f;
    public float controlEnAire = 0.5f;

    // Impulso de velocidad (poder "IMPULSO")
    public float multiplicadorBoost = 1.5f;   // 1.5 = 50% más rápido
    public float duracionBoost = 5f;          // segundos que dura
    public GameObject contenedorBarra;        // fondo de la barra (opcional, con Canvas)
    public Image barraRelleno;                // la barra que se vacía (opcional, con Canvas)
    private float tiempoBoost = 0f;

    // Indicador de impulso dibujado por código (sin Canvas)
    public bool mostrarIndicadorPantalla = true;
    private GUIStyle estiloTexto;
    private float tiempoDesdeActivacion = 99f;

    // ───────────── PODERES ALEATORIOS (Coleccionable2) ─────────────
    [Header("Poderes aleatorios")]
    public float duracionVuelo = 6f;           // segundos volando
    public float velocidadVuelo = 9f;          // velocidad vertical al volar (Espacio sube / Ctrl baja)
    public float duracionDashPoder = 15f;      // segundos que puedes usar el dash (doble W)
    public float velocidadDash = 35f;          // velocidad del impulso hacia adelante
    public float duracionImpulsoDash = 0.2f;   // lo que dura cada dash
    public float cooldownDash = 0.8f;          // espera entre dashes
    public float ventanaDobleToque = 0.3f;     // tiempo máximo entre las 2 pulsaciones de W
    public float duracionRuleta = 2.5f;        // segundos que gira la ruleta
    public float duracionResultadoRuleta = 2f; // segundos que se queda mostrando el resultado

    private const int TOTAL_PODERES = 4;
    private static readonly string[] nombresPoder = { "IMPULSO", "VUELO", "VIDA EXTRA", "DASH" };
    private static readonly Color[] coloresPoder =
    {
        new Color(1.00f, 0.90f, 0.20f),  // Impulso
        new Color(0.40f, 0.85f, 1.00f),  // Vuelo
        new Color(0.40f, 1.00f, 0.50f),  // Vida extra
        new Color(1.00f, 0.50f, 0.20f)   // Dash
    };

    private float tiempoVuelo = 0f;
    private float tiempoDashPoder = 0f;
    private bool tieneVidaExtra = false;

    private float ultimoToqueW = -99f;
    private float proximoDash = 0f;
    private float tiempoDashActual = 0f;
    private Vector3 dirDash;

    private Vector3 ultimaPosicionSegura;
    private float invulnerableHasta = 0f;
    private float tiempoMensajeVida = 99f;

    // Ruleta
    private bool ruletaActiva = false;
    private bool ruletaAplicada = false;
    private float tiempoRuleta = 0f;
    private int resultadoRuleta = 0;

    // Sistema de combo / estilo (tipo Devil May Cry), dibujado por código
    [Header("Combo de estilo")]
    public bool mostrarCombo = true;
    public float puntosColeccionable1 = 40f;
    public float puntosColeccionable2 = 60f;
    public float retrasoDecaimiento = 1.5f;
    public float decaimientoBase = 10f;
    public float decaimientoPorRango = 7f;
    private const float PUNTOS_POR_RANGO = 100f;
    private static readonly string[] letrasRango = { "D", "C", "B", "A", "S", "SS", "SSS" };
    private static readonly string[] nombresRango =
        { "DISMAL", "CRAZY", "BADASS", "APOCALYPTIC", "SAVAGE", "SICK STYLE", "SMOKIN' SEXY STYLE!" };
    private static readonly Color[] coloresRango =
    {
        new Color(0.65f, 0.65f, 0.70f),  // D
        new Color(0.40f, 0.75f, 1.00f),  // C
        new Color(0.40f, 0.95f, 0.55f),  // B
        new Color(1.00f, 0.88f, 0.30f),  // A
        new Color(1.00f, 0.55f, 0.15f),  // S
        new Color(1.00f, 0.28f, 0.22f),  // SS
        new Color(1.00f, 0.20f, 0.65f)   // SSS (arcoíris)
    };
    private float puntosEstilo = 0f;
    private int rangoActual = 0;
    private int combo = 0;
    private float tiempoUltimoPickup = -99f;
    private float tiempoDesdePickup = 99f;
    private float tiempoDesdeCambioRango = 99f;
    private float alfaCombo = 0f;
    private GUIStyle estiloLetra;

    // Salto
    public float fuerzaSalto = 8f;
    public int saltosMaximos = 2;
    private int saltosRestantes;
    private bool enSuelo = false;

    // ───────────── DOBLE SALTO EN PICADA ─────────────
    // El salto en el aire lanza al personaje hacia adelante, con la panza hacia el frente/suelo
    [Header("Doble salto en picada")]
    public bool dobleSaltoPicada = true;
    public float velocidadPicada = 16f;        // velocidad horizontal del lanzamiento
    public float duracionPicada = 0.5f;        // segundos que dura el empuje
    public float fuerzaSaltoPicada = 6f;       // impulso hacia arriba al lanzarse
    public float tiempoMaximoPicada = 4f;      // seguridad: la picada se cancela si no aterrizas en este tiempo
    public float duracionDeslizar = 0.8f;      // segundos que se desliza al aterrizar
    [Range(0f, 1f)] public float factorDeslizar = 1f;   // fracción de la velocidad que conserva al deslizarse
    private bool enPicada = false;             // true desde que te lanzas hasta que tocas el suelo
    private float inicioPicada = 0f;
    private float tiempoPicada = 0f;           // tiempo que dura el empuje hacia adelante
    private Vector3 dirPicada;
    private Vector3 velHorizPrevia;            // velocidad horizontal justo antes del impacto
    private float tiempoDeslizar = 0f;
    private float velDeslizar = 0f;
    private Vector3 dirDeslizar;
    private PersonajeRoblox personaje;

    // Sonidos
    public AudioClip sonidoDoh;
    [Range(0f, 1f)] public float volumen = 1f;
    public AudioClip sonidoColeccionable2;
    [Range(0f, 1f)] public float volumen2 = 1f;

    // Victoria
    public GameObject cilindroWin;
    public GameObject textoGanaste;
    public float segundosParaReiniciar = 5f;
    private int totalMonedas;
    private bool juegoGanado = false;

    // tp
    public GameObject portalTP;
    public int monedasParaTP = 5;
    private Vector3 destinoTP = new Vector3(-168.9687f, -1105.54f, 17.68513f);

    private bool reiniciando = false;

    // Pantallas (dibujadas por código, sin Canvas)
    [Header("Pantallas")]
    public float duracionCarga = 1f;
    public bool pantallaGanasteCodigo = true;
    private bool cargando = false;
    private float inicioCarga;
    private float tiempoDesdeGanar = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        personaje = GetComponent<PersonajeRoblox>();
        saltosRestantes = saltosMaximos;
        ultimaPosicionSegura = transform.position;

        foreach (GameObject zona in GameObject.FindGameObjectsWithTag("kill"))
        {
            Renderer r = zona.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        totalMonedas = GameObject.FindGameObjectsWithTag("Coleccionable").Length
                     + GameObject.FindGameObjectsWithTag("Coleccionable2").Length;
        Debug.Log("Monedas totales en el mapa: " + totalMonedas);

        if (cilindroWin != null) cilindroWin.SetActive(false);
        else Debug.LogWarning("Falta asignar Cilindro Win en el Jugador");

        if (textoGanaste != null) textoGanaste.SetActive(false);
        else Debug.LogWarning("Falta asignar Texto Ganaste en el Jugador");

        if (portalTP != null) portalTP.SetActive(false);
        else Debug.LogWarning("Falta asignar Portal TP en el Jugador");

        if (contenedorBarra != null) contenedorBarra.SetActive(false);
    }

    void Update()
    {
        // Salto y doble salto (no mientras vuelas)
        if (Input.GetKeyDown(KeyCode.Space) && saltosRestantes > 0 && !cargando && !juegoGanado && tiempoVuelo <= 0f)
        {
            bool saltoEnAire = !enSuelo;
            Vector3 v = rb.linearVelocity;

            if (dobleSaltoPicada && saltoEnAire)
            {
                // Doble salto: lanzamiento hacia adelante con la panza hacia adelante
                dirPicada = DireccionPicada();
                tiempoPicada = duracionPicada;
                tiempoDeslizar = 0f;
                enPicada = true;
                inicioPicada = Time.time;
                velHorizPrevia = dirPicada * velocidadPicada;
                v.x = dirPicada.x * velocidadPicada;
                v.z = dirPicada.z * velocidadPicada;
                v.y = fuerzaSaltoPicada;
                // La pose dura hasta que toques el suelo (tiempoMaximoPicada es solo un seguro)
                if (personaje != null) personaje.IniciarPicada(tiempoMaximoPicada);
            }
            else
            {
                CancelarPicada();   // saltar desde el suelo corta cualquier deslizamiento
                v.y = fuerzaSalto;
            }

            rb.linearVelocity = v;
            saltosRestantes--;
            enSuelo = false;
        }

        // Dash: doble toque de W (solo con el poder activo)
        if (Input.GetKeyDown(KeyCode.W) && !cargando && !juegoGanado)
        {
            if (tiempoDashPoder > 0f
                && Time.time - ultimoToqueW <= ventanaDobleToque
                && Time.time >= proximoDash)
            {
                IniciarDash();
                ultimoToqueW = -99f;
            }
            else
            {
                ultimoToqueW = Time.time;
            }
        }

        // Seguridad: la picada se cancela si pasa demasiado tiempo o si empiezas a volar
        if (enPicada && (Time.time - inicioPicada > tiempoMaximoPicada || tiempoVuelo > 0f)) CancelarPicada();

        if (juegoGanado) tiempoDesdeGanar += Time.unscaledDeltaTime;
        tiempoDesdeActivacion += Time.deltaTime;
        tiempoMensajeVida += Time.deltaTime;
        ActualizarBoost();
        ActualizarPoderes();
        ActualizarRuleta();
        ActualizarEstilo();
    }

    // Dirección de la picada: hacia donde empujas (relativo a la cámara) o, si no tocas nada, hacia donde mira la cámara
    private Vector3 DireccionPicada()
    {
        Camera camara = Camera.main;
        Vector3 adelante = (camara != null) ? camara.transform.forward : transform.forward;
        Vector3 derecha = (camara != null) ? camara.transform.right : transform.right;
        adelante.y = 0f;
        derecha.y = 0f;
        if (adelante.sqrMagnitude < 0.001f) adelante = transform.forward;
        adelante.Normalize();
        derecha.Normalize();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = adelante * v + derecha * h;

        if (dir.sqrMagnitude < 0.01f) dir = adelante;
        return dir.normalized;
    }

    // Cualquier superficie "de piso" (normal hacia arriba) termina la picada, tenga el tag que tenga
    private void IntentarAterrizarPicada(Collision collision)
    {
        if (!enPicada) return;
        if (Time.time - inicioPicada < 0.1f) return;   // margen para no cancelar al despegar

        foreach (ContactPoint contacto in collision.contacts)
        {
            if (contacto.normal.y > 0.5f)
            {
                AterrizarPicada();
                return;
            }
        }
    }

    // Al tocar el suelo: termina la picada y empieza un pequeño deslizamiento que frena solo
    private void AterrizarPicada()
    {
        enPicada = false;
        tiempoPicada = 0f;

        Vector3 vh = velHorizPrevia;
        vh.y = 0f;
        dirDeslizar = (vh.sqrMagnitude > 0.01f) ? vh.normalized : dirPicada;
        velDeslizar = Mathf.Min(vh.magnitude, velocidadPicada) * factorDeslizar;
        tiempoDeslizar = duracionDeslizar;

        if (personaje != null) personaje.IniciarDeslizamiento(duracionDeslizar);
    }

    // Corta la picada y el deslizamiento (salto desde el suelo, dash, vida extra, teletransporte...)
    private void CancelarPicada()
    {
        enPicada = false;
        tiempoPicada = 0f;
        tiempoDeslizar = 0f;
        if (personaje != null) personaje.TerminarPicada();
    }

    // ───────────── PODERES ─────────────

    private string DuracionPoderTexto(int poder)
    {
        switch (poder)
        {
            case 0: return duracionBoost.ToString("0.#") + " segundos";
            case 1: return duracionVuelo.ToString("0.#") + " segundos";
            case 2: return "Hasta que caigas al vacío";
            default: return duracionDashPoder.ToString("0.#") + " segundos";
        }
    }

    private string DescripcionPoder(int poder)
    {
        switch (poder)
        {
            case 0: return "Velocidad x" + multiplicadorBoost.ToString("0.#");
            case 1: return "Espacio sube · Ctrl baja";
            case 2: return "Al caer al vacío vuelves al último suelo";
            default: return "Pulsa W dos veces rápido";
        }
    }

    private void IniciarRuleta()
    {
        // Si ya había una ruleta girando, se aplica su premio antes de empezar otra
        if (ruletaActiva && !ruletaAplicada) AplicarPoder(resultadoRuleta);

        ruletaActiva = true;
        ruletaAplicada = false;
        tiempoRuleta = 0f;
        resultadoRuleta = Random.Range(0, TOTAL_PODERES);
    }

    private void ActualizarRuleta()
    {
        if (!ruletaActiva) return;

        tiempoRuleta += Time.deltaTime;

        if (!ruletaAplicada && tiempoRuleta >= duracionRuleta)
        {
            ruletaAplicada = true;
            AplicarPoder(resultadoRuleta);
        }

        if (tiempoRuleta >= duracionRuleta + duracionResultadoRuleta)
        {
            ruletaActiva = false;
        }
    }

    private void AplicarPoder(int poder)
    {
        switch (poder)
        {
            case 0:
                ActivarBoost();
                break;
            case 1:
                tiempoVuelo = duracionVuelo;
                rb.useGravity = false;
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                rb.linearVelocity = v;
                break;
            case 2:
                tieneVidaExtra = true;
                break;
            case 3:
                tiempoDashPoder = duracionDashPoder;
                break;
        }
        Debug.Log("Poder obtenido: " + nombresPoder[poder]);
    }

    private void ActualizarPoderes()
    {
        if (tiempoVuelo > 0f)
        {
            tiempoVuelo -= Time.deltaTime;
            if (tiempoVuelo <= 0f)
            {
                tiempoVuelo = 0f;
                rb.useGravity = true;
            }
        }

        if (tiempoDashPoder > 0f)
        {
            tiempoDashPoder -= Time.deltaTime;
            if (tiempoDashPoder < 0f) tiempoDashPoder = 0f;
        }

        // Guarda dónde estás pisando para la vida extra
        if (enSuelo && !cargando) ultimaPosicionSegura = transform.position;
    }

    private void IniciarDash()
    {
        Vector3 dir;
        Camera camara = Camera.main;
        if (camara != null) dir = camara.transform.forward;
        else dir = transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dirDash = dir.normalized;

        CancelarPicada();
        tiempoDashActual = duracionImpulsoDash;
        proximoDash = Time.time + cooldownDash;
    }

    // Cuando tocas "kill": si tienes vida extra te teletransporta, si no reinicia
    private void Morir(string origen)
    {
        if (reiniciando) return;
        if (Time.time < invulnerableHasta) return;

        if (tieneVidaExtra)
        {
            tieneVidaExtra = false;
            invulnerableHasta = Time.time + 1f;
            tiempoMensajeVida = 0f;
            tiempoDashActual = 0f;
            CancelarPicada();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = ultimaPosicionSegura + Vector3.up * 1.5f;
            Debug.Log("Vida extra usada (" + origen + ")");
            return;
        }

        Debug.Log("Reinicio por kill: " + origen);
        ReiniciarJuego();
    }

    // ───────────── ESTILO / BOOST ─────────────

    private void AgregarEstilo(float puntos)
    {
        combo++;
        puntosEstilo = Mathf.Min(puntosEstilo + puntos, PUNTOS_POR_RANGO * letrasRango.Length - 0.01f);
        tiempoUltimoPickup = Time.time;
        tiempoDesdePickup = 0f;

        int nuevoRango = Mathf.FloorToInt(puntosEstilo / PUNTOS_POR_RANGO);
        if (nuevoRango > rangoActual) tiempoDesdeCambioRango = 0f;
        rangoActual = nuevoRango;
    }

    private void ActualizarEstilo()
    {
        float dt = Time.deltaTime;
        tiempoDesdePickup += dt;
        tiempoDesdeCambioRango += dt;

        if (puntosEstilo > 0f && Time.time - tiempoUltimoPickup > retrasoDecaimiento)
        {
            puntosEstilo -= (decaimientoBase + rangoActual * decaimientoPorRango) * dt;
            if (puntosEstilo <= 0f)
            {
                puntosEstilo = 0f;
                combo = 0;
            }
            rangoActual = Mathf.Clamp(Mathf.FloorToInt(puntosEstilo / PUNTOS_POR_RANGO), 0, letrasRango.Length - 1);
        }

        alfaCombo = Mathf.MoveTowards(alfaCombo, puntosEstilo > 0f ? 1f : 0f, dt * 4f);
    }

    private void ActualizarBoost()
    {
        if (tiempoBoost <= 0f) return;

        tiempoBoost -= Time.deltaTime;

        if (barraRelleno != null)
        {
            barraRelleno.fillAmount = Mathf.Clamp01(tiempoBoost / duracionBoost);
        }

        if (tiempoBoost <= 0f && contenedorBarra != null)
        {
            contenedorBarra.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (cargando || juegoGanado)
        {
            if (!rb.isKinematic)
            {
                Vector3 vq = rb.linearVelocity;
                vq.x = 0f;
                vq.z = 0f;
                rb.linearVelocity = vq;
            }
            return;
        }

        bool volando = tiempoVuelo > 0f;

        // Guarda la velocidad horizontal antes del impacto (para calcular el deslizamiento al aterrizar)
        if (enPicada) velHorizPrevia = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Velocidad vertical al volar
        float vyVuelo = 0f;
        if (volando)
        {
            if (Input.GetKey(KeyCode.Space)) vyVuelo += velocidadVuelo;
            if (Input.GetKey(KeyCode.LeftControl)) vyVuelo -= velocidadVuelo;
        }

        // Dash en curso: ignora el movimiento normal
        if (tiempoDashActual > 0f)
        {
            tiempoDashActual -= Time.fixedDeltaTime;
            Vector3 vd = dirDash * velocidadDash;
            vd.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = vd;
            return;
        }

        // Picada del doble salto en curso: dirección fija, frena un poco al final
        if (tiempoPicada > 0f)
        {
            tiempoPicada -= Time.fixedDeltaTime;
            float t = Mathf.Clamp01(tiempoPicada / Mathf.Max(0.01f, duracionPicada));
            Vector3 vp = dirPicada * (velocidadPicada * Mathf.Lerp(0.6f, 1f, t));
            vp.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = vp;
            return;
        }

        // Deslizamiento al aterrizar de la picada: arranca rápido y frena hasta detenerse
        if (tiempoDeslizar > 0f)
        {
            tiempoDeslizar -= Time.fixedDeltaTime;
            float ts = Mathf.Clamp01(tiempoDeslizar / Mathf.Max(0.01f, duracionDeslizar));
            Vector3 vs = dirDeslizar * (velDeslizar * ts);
            vs.y = volando ? vyVuelo : rb.linearVelocity.y;
            rb.linearVelocity = vs;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Camera camara = Camera.main;
        if (camara == null) return;

        Transform cam = camara.transform;
        Vector3 adelante = cam.forward;
        Vector3 derecha = cam.right;
        adelante.y = 0f;
        derecha.y = 0f;
        adelante.Normalize();
        derecha.Normalize();

        Vector3 dir = Vector3.ClampMagnitude(adelante * v + derecha * h, 1f);

        float velocidadMax = Input.GetKey(KeyCode.LeftShift) ? velocidadCorrer : velocidadCaminar;

        float multiplicador = (tiempoBoost > 0f) ? multiplicadorBoost : 1f;
        velocidadMax *= multiplicador;

        Vector3 velActual = rb.linearVelocity;
        velActual.y = 0f;

        Vector3 velObjetivo = dir * velocidadMax;

        float cambio = (dir.sqrMagnitude > 0.01f) ? aceleracion * multiplicador : frenado;
        if (!enSuelo && !volando) cambio *= controlEnAire;

        Vector3 velNueva = Vector3.MoveTowards(velActual, velObjetivo, cambio * Time.fixedDeltaTime);

        velNueva.y = volando ? vyVuelo : rb.linearVelocity.y;
        rb.linearVelocity = velNueva;
    }

    private void OnCollisionStay(Collision collision)
    {
        IntentarAterrizarPicada(collision);

        if (!collision.gameObject.CompareTag("pizo")) return;

        foreach (ContactPoint contacto in collision.contacts)
        {
            if (contacto.normal.y > 0.5f)
            {
                enSuelo = true;
                if (rb.linearVelocity.y <= 0.1f) saltosRestantes = saltosMaximos;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("pizo"))
        {
            if (enSuelo && saltosRestantes == saltosMaximos && rb.linearVelocity.y <= 0.1f)
            {
                saltosRestantes = saltosMaximos - 1;
            }
            enSuelo = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        IntentarAterrizarPicada(collision);

        if (collision.gameObject.CompareTag("win")) Ganar();
        else if (collision.gameObject.CompareTag("kill"))
        {
            Morir(collision.gameObject.name);
        }
        else if (collision.gameObject.CompareTag("tp"))
        {
            Debug.Log("Teletransporte por tp: " + collision.gameObject.name);
            Teletransportar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Coleccionable"))
        {
            if (sonidoDoh != null) AudioSource.PlayClipAtPoint(sonidoDoh, transform.position, volumen);
            RecogerMoneda(other.gameObject);
            AgregarEstilo(puntosColeccionable1);
        }
        else if (other.gameObject.CompareTag("Coleccionable2"))
        {
            if (sonidoColeccionable2 != null) AudioSource.PlayClipAtPoint(sonidoColeccionable2, transform.position, volumen2);
            RecogerMoneda(other.gameObject);
            AgregarEstilo(puntosColeccionable2);
            IniciarRuleta();   // poder aleatorio
        }
        else if (other.gameObject.CompareTag("win")) Ganar();
        else if (other.gameObject.CompareTag("kill"))
        {
            Morir(other.gameObject.name);
        }
        else if (other.gameObject.CompareTag("tp"))
        {
            Debug.Log("Teletransporte por tp: " + other.gameObject.name);
            Teletransportar();
        }
    }

    private void ActivarBoost()
    {
        tiempoBoost = duracionBoost;
        tiempoDesdeActivacion = 0f;

        if (contenedorBarra != null) contenedorBarra.SetActive(true);
        if (barraRelleno != null) barraRelleno.fillAmount = 1f;
    }

    private void RecogerMoneda(GameObject moneda)
    {
        moneda.SetActive(false);
        contador_coleccionables++;
        Debug.Log("Coleccionables recogidos: " + contador_coleccionables + " de " + totalMonedas);
        if (puntaje != null) puntaje.text = contador_coleccionables + "";

        if (contador_coleccionables >= monedasParaTP && portalTP != null)
        {
            portalTP.SetActive(true);
        }

        if (contador_coleccionables >= totalMonedas && cilindroWin != null)
        {
            cilindroWin.SetActive(true);
        }
    }

    private void ReiniciarJuego()
    {
        if (reiniciando) return;
        reiniciando = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Teletransportar()
    {
        if (cargando) return;
        StartCoroutine(CargaTP());
    }

    private IEnumerator CargaTP()
    {
        cargando = true;
        inicioCarga = Time.unscaledTime;

        CancelarPicada();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.position = destinoTP;
        ultimaPosicionSegura = destinoTP;

        yield return new WaitForSecondsRealtime(duracionCarga);

        rb.isKinematic = false;
        cargando = false;
    }

    private void Ganar()
    {
        if (juegoGanado) return;
        juegoGanado = true;

        // Termina el vuelo para que no quede flotando
        tiempoVuelo = 0f;
        rb.useGravity = true;

        if (textoGanaste != null && !pantallaGanasteCodigo) textoGanaste.SetActive(true);
        tiempoDesdeGanar = 0f;
        Debug.Log("¡Ganaste!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Invoke(nameof(ReiniciarJuego), segundosParaReiniciar);
    }

    // ───────────── DIBUJO (OnGUI) ─────────────

    private void OnGUI()
    {
        if (estiloTexto == null)
        {
            estiloTexto = new GUIStyle(GUI.skin.label);
            estiloTexto.alignment = TextAnchor.MiddleCenter;
            estiloTexto.fontStyle = FontStyle.Bold;
        }
        if (estiloLetra == null)
        {
            estiloLetra = new GUIStyle(GUI.skin.label);
            estiloLetra.alignment = TextAnchor.MiddleCenter;
            estiloLetra.fontStyle = FontStyle.BoldAndItalic;
        }

        DibujarCombo();
        DibujarIndicadorImpulso();
        DibujarPoderesActivos();
        DibujarRuleta();
        DibujarPantallaGanaste();
        DibujarPantallaCarga();   // siempre al final para que quede encima de todo
    }

    // Lista de poderes activos (abajo a la izquierda) + aviso de vida extra usada
    private void DibujarPoderesActivos()
    {
        float e = Screen.height / 1080f;
        float x = 30f * e;
        float y = Screen.height - 60f * e;
        float alto = 40f * e;
        float ancho = 420f * e;

        estiloTexto.fontSize = Mathf.RoundToInt(28f * e);

        if (tieneVidaExtra)
        {
            TextoConSombra(new Rect(x, y, ancho, alto), "♥ VIDA EXTRA", estiloTexto, coloresPoder[2], 1f, 2f * e);
            y -= alto;
        }
        if (tiempoDashPoder > 0f)
        {
            TextoConSombra(new Rect(x, y, ancho, alto),
                           "DASH (W W)  " + tiempoDashPoder.ToString("0.0") + "s",
                           estiloTexto, coloresPoder[3], 1f, 2f * e);
            y -= alto;
        }
        if (tiempoVuelo > 0f)
        {
            TextoConSombra(new Rect(x, y, ancho, alto),
                           "VUELO  " + tiempoVuelo.ToString("0.0") + "s",
                           estiloTexto, coloresPoder[1], 1f, 2f * e);
        }

        // Aviso al gastar la vida extra
        if (tiempoMensajeVida < 2f)
        {
            float a = Mathf.Clamp01((2f - tiempoMensajeVida) / 0.5f);
            estiloTexto.fontSize = Mathf.RoundToInt(48f * e);
            TextoConSombra(new Rect(0f, Screen.height * 0.25f, Screen.width, 70f * e),
                           "¡VIDA EXTRA USADA!", estiloTexto, coloresPoder[2], a, 3f * e);
        }
        GUI.color = Color.white;
    }

    // Ruleta: gira por los poderes, frena en el premio y muestra su duración
    private void DibujarRuleta()
    {
        if (!ruletaActiva) return;

        float e = Screen.height / 1080f;
        float total = duracionRuleta + duracionResultadoRuleta;
        float alfa = Mathf.Clamp01((total - tiempoRuleta) / 0.4f) * Mathf.Clamp01(tiempoRuleta / 0.2f);

        float p = Mathf.Clamp01(tiempoRuleta / duracionRuleta);
        float suave = 1f - Mathf.Pow(1f - p, 3f);
        int pasos = Mathf.FloorToInt(suave * (5 * TOTAL_PODERES + resultadoRuleta));
        int actual = pasos % TOTAL_PODERES;
        int anterior = (actual + TOTAL_PODERES - 1) % TOTAL_PODERES;
        int siguiente = (actual + 1) % TOTAL_PODERES;
        bool termino = tiempoRuleta >= duracionRuleta;

        float anchoP = 700f * e;
        float altoP = 330f * e;
        Rect panel = new Rect((Screen.width - anchoP) / 2f, Screen.height * 0.16f, anchoP, altoP);

        // Fondo
        GUI.color = new Color(0f, 0f, 0f, 0.78f * alfa);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        // Título
        estiloTexto.fontSize = Mathf.RoundToInt(34f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 10f * e, panel.width, 45f * e),
                       "¡RULETA DE PODERES!", estiloTexto, Color.white, alfa, 2f * e);

        // Franja de selección
        Color colorActual = coloresPoder[actual];
        Rect franja = new Rect(panel.x + 30f * e, panel.y + 118f * e, panel.width - 60f * e, 90f * e);
        GUI.color = new Color(colorActual.r, colorActual.g, colorActual.b, 0.25f * alfa);
        GUI.DrawTexture(franja, Texture2D.whiteTexture);

        // Anterior / actual / siguiente
        estiloTexto.fontSize = Mathf.RoundToInt(32f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 68f * e, panel.width, 44f * e),
                       nombresPoder[anterior], estiloTexto, new Color(1f, 1f, 1f, 0.45f), alfa, 2f * e);
        TextoConSombra(new Rect(panel.x, panel.y + 214f * e, panel.width, 44f * e),
                       nombresPoder[siguiente], estiloTexto, new Color(1f, 1f, 1f, 0.45f), alfa, 2f * e);

        float latido = termino ? 1f + 0.08f * Mathf.Sin(Time.time * 12f) : 1f;
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(latido, latido), franja.center);
        estiloTexto.fontSize = Mathf.RoundToInt(66f * e);
        TextoConSombra(franja, nombresPoder[actual], estiloTexto, colorActual, alfa, 4f * e);
        GUI.matrix = matrizOriginal;

        // Resultado: descripción y duración
        if (termino)
        {
            estiloTexto.fontSize = Mathf.RoundToInt(24f * e);
            TextoConSombra(new Rect(panel.x, panel.y + 262f * e, panel.width, 30f * e),
                           DescripcionPoder(actual), estiloTexto, Color.white, alfa, 2f * e);
            TextoConSombra(new Rect(panel.x, panel.y + 292f * e, panel.width, 30f * e),
                           "Duración: " + DuracionPoderTexto(actual), estiloTexto, colorActual, alfa, 2f * e);
        }

        GUI.color = Color.white;
    }

    private void DibujarPantallaGanaste()
    {
        if (!juegoGanado || !pantallaGanasteCodigo) return;

        float e = Screen.height / 1080f;
        float fade = Mathf.Clamp01(tiempoDesdeGanar / 0.5f);

        GUI.color = new Color(0f, 0f, 0f, 0.8f * fade);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        float pop = Mathf.Clamp01((tiempoDesdeGanar - 0.2f) / 0.35f);
        float suave = 1f - (1f - pop) * (1f - pop);
        float escala = Mathf.Lerp(2.2f, 1f, suave);
        float alfaTitulo = Mathf.Clamp01((tiempoDesdeGanar - 0.2f) / 0.2f);

        Vector2 centro = new Vector2(Screen.width / 2f, Screen.height * 0.40f);
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(escala, escala), centro);

        estiloLetra.fontSize = Mathf.RoundToInt(170f * e);
        TextoConSombra(new Rect(centro.x - 700f * e, centro.y - 110f * e, 1400f * e, 220f * e),
                       "¡GANASTE!", estiloLetra, new Color(1f, 0.88f, 0.25f), alfaTitulo, 7f * e);

        GUI.matrix = matrizOriginal;

        float alfaSub = Mathf.Clamp01((tiempoDesdeGanar - 0.7f) / 0.3f);
        estiloTexto.fontSize = Mathf.RoundToInt(42f * e);
        TextoConSombra(new Rect(0f, Screen.height * 0.58f, Screen.width, 60f * e),
                       "Monedas: " + contador_coleccionables + " / " + totalMonedas,
                       estiloTexto, Color.white, alfaSub, 2f * e);

        int restante = Mathf.CeilToInt(Mathf.Max(0f, segundosParaReiniciar - tiempoDesdeGanar));
        estiloTexto.fontSize = Mathf.RoundToInt(28f * e);
        TextoConSombra(new Rect(0f, Screen.height * 0.67f, Screen.width, 40f * e),
                       "Reiniciando en " + restante + "...",
                       estiloTexto, new Color(1f, 1f, 1f, 0.8f), alfaSub, 2f * e);

        GUI.color = Color.white;
    }

    private void DibujarPantallaCarga()
    {
        if (!cargando) return;

        float e = Screen.height / 1080f;
        float transcurrido = Time.unscaledTime - inicioCarga;
        float progreso = Mathf.Clamp01(transcurrido / duracionCarga);
        float alfa = Mathf.Clamp01((duracionCarga - transcurrido) / 0.2f);

        GUI.color = new Color(0.04f, 0.04f, 0.08f, alfa);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        string puntos = new string('.', (int)(transcurrido * 4f) % 4);
        estiloTexto.fontSize = Mathf.RoundToInt(64f * e);
        GUI.color = new Color(1f, 1f, 1f, alfa);
        GUI.Label(new Rect(Screen.width / 2f - 400f * e, Screen.height / 2f - 70f * e, 800f * e, 90f * e),
                  "CARGANDO" + puntos, estiloTexto);

        Rect fondo = new Rect(Screen.width / 2f - 250f * e, Screen.height / 2f + 50f * e, 500f * e, 10f * e);
        GUI.color = new Color(1f, 1f, 1f, 0.2f * alfa);
        GUI.DrawTexture(fondo, Texture2D.whiteTexture);
        Rect relleno = fondo;
        relleno.width *= progreso;
        GUI.color = new Color(1f, 0.9f, 0.2f, alfa);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void TextoConSombra(Rect r, string texto, GUIStyle estilo, Color color, float alfa, float desplazamiento)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.85f * alfa);
        GUI.Label(new Rect(r.x + desplazamiento, r.y + desplazamiento, r.width, r.height), texto, estilo);
        GUI.color = new Color(color.r, color.g, color.b, alfa);
        GUI.Label(r, texto, estilo);
    }

    private void DibujarCombo()
    {
        if (!mostrarCombo || alfaCombo <= 0.01f) return;

        float e = Screen.height / 1080f;
        float cx = Screen.width - 220f * e;
        float cy = 300f * e;

        Color color = (rangoActual >= letrasRango.Length - 1)
            ? Color.HSVToRGB(Mathf.Repeat(Time.time * 0.7f, 1f), 0.75f, 1f)
            : coloresRango[rangoActual];

        float pop = 1f + 0.30f * (1f - Mathf.Clamp01(tiempoDesdePickup / 0.15f));
        float t = Mathf.Clamp01(tiempoDesdeCambioRango / 0.4f);
        float escalaRango = 1f + 0.55f * (1f - t);
        float giro = Mathf.Sin(tiempoDesdeCambioRango * 45f) * 7f * (1f - t);

        Vector2 centro = new Vector2(cx, cy);
        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(pop * escalaRango, pop * escalaRango), centro);
        GUIUtility.RotateAroundPivot(giro, centro);

        estiloLetra.fontSize = Mathf.RoundToInt((rangoActual >= 5 ? 150f : 190f) * e);
        TextoConSombra(new Rect(cx - 190f * e, cy - 120f * e, 380f * e, 200f * e),
                       letrasRango[rangoActual], estiloLetra, color, alfaCombo, 6f * e);

        estiloTexto.fontSize = Mathf.RoundToInt(30f * e);
        TextoConSombra(new Rect(cx - 200f * e, cy + 80f * e, 400f * e, 40f * e),
                       nombresRango[rangoActual], estiloTexto, color, alfaCombo, 2f * e);

        float progreso = (puntosEstilo - rangoActual * PUNTOS_POR_RANGO) / PUNTOS_POR_RANGO;
        Rect fondoBarra = new Rect(cx - 130f * e, cy + 128f * e, 260f * e, 12f * e);
        GUI.color = new Color(0f, 0f, 0f, 0.6f * alfaCombo);
        GUI.DrawTexture(new Rect(fondoBarra.x - 2f * e, fondoBarra.y - 2f * e, fondoBarra.width + 4f * e, fondoBarra.height + 4f * e), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.2f * alfaCombo);
        GUI.DrawTexture(fondoBarra, Texture2D.whiteTexture);
        Rect relleno = fondoBarra;
        relleno.width *= Mathf.Clamp01(progreso);
        GUI.color = new Color(color.r, color.g, color.b, alfaCombo);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        estiloTexto.fontSize = Mathf.RoundToInt(26f * e);
        TextoConSombra(new Rect(cx - 150f * e, cy + 148f * e, 300f * e, 34f * e),
                       "COMBO x" + combo, estiloTexto, Color.white, alfaCombo, 2f * e);

        GUI.color = Color.white;
        GUI.matrix = matrizOriginal;
    }

    private void DibujarIndicadorImpulso()
    {
        if (!mostrarIndicadorPantalla || tiempoBoost <= 0f) return;

        float escala = Screen.height / 1080f;
        float ancho = 460f * escala;
        float alto = 130f * escala;
        Rect panel = new Rect((Screen.width - ancho) / 2f, 40f * escala, ancho, alto);

        float pop = Mathf.Clamp01(tiempoDesdeActivacion / 0.25f);
        float rebote = 1f + 0.4f * (1f - pop);
        float alfa = Mathf.Clamp01(tiempoBoost / 1f);

        Matrix4x4 matrizOriginal = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(rebote, rebote), panel.center);

        GUI.color = new Color(0f, 0f, 0f, 0.6f * alfa);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        estiloTexto.fontSize = Mathf.RoundToInt(44f * escala);
        GUI.color = new Color(1f, 0.9f, 0.2f, alfa);
        GUI.Label(new Rect(panel.x, panel.y + 8f * escala, panel.width, 55f * escala),
                  "¡IMPULSO!", estiloTexto);

        estiloTexto.fontSize = Mathf.RoundToInt(24f * escala);
        GUI.color = new Color(1f, 1f, 1f, alfa);
        GUI.Label(new Rect(panel.x, panel.y + 58f * escala, panel.width, 30f * escala),
                  "Velocidad x" + multiplicadorBoost.ToString("0.#") + "  ·  " + tiempoBoost.ToString("0.0") + "s",
                  estiloTexto);

        float margen = 24f * escala;
        Rect fondoBarra = new Rect(panel.x + margen, panel.y + 98f * escala, panel.width - margen * 2f, 14f * escala);
        GUI.color = new Color(1f, 1f, 1f, 0.25f * alfa);
        GUI.DrawTexture(fondoBarra, Texture2D.whiteTexture);

        Rect relleno = fondoBarra;
        relleno.width *= Mathf.Clamp01(tiempoBoost / duracionBoost);
        GUI.color = new Color(1f, 0.9f, 0.2f, alfa);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.matrix = matrizOriginal;
    }
}