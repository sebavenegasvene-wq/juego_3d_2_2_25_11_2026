using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Personaje PUG (perrito cartoon de cabeza enorme y ojos grandes) generado por código,
/// con animación procedural estilo STOP MOTION: poses a pocos cuadros por segundo con un leve temblor,
/// ciclo de caminar con "waddle", salto con estirón, aterrizaje con aplastamiento, picada y deslizamiento,
/// orejas que se agitan, parpadeo, mirada a los lados y polvo.
///
/// (La clase se sigue llamando PersonajeRoblox para no romper la referencia en tu Jugador
///  ni en el script "movimiento".)
///
/// USO: arrastra este script al objeto "Jugador" (el que tiene el Rigidbody).
/// No necesita modelos ni Animator.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PersonajeRoblox : MonoBehaviour
{
    [Header("Tamaño")]
    [Tooltip("Altura total del personaje (con la cabeza) en unidades de Unity")]
    public float altura = 2f;

    [Header("Colores del pug")]
    public Color colorPelaje  = new Color(0.82f, 0.58f, 0.40f);   // cabeza y orejas
    public Color colorCuerpo  = new Color(0.87f, 0.65f, 0.52f);   // cuerpo, brazos y piernas
    public Color colorVientre = new Color(0.93f, 0.74f, 0.62f);
    public Color colorHocico  = new Color(0.20f, 0.11f, 0.08f);   // hocico, aros de los ojos, bigotes
    public Color colorMejilla = new Color(0.32f, 0.19f, 0.14f);
    public Color colorOscuro  = new Color(0.38f, 0.22f, 0.15f);   // puntas de las orejas y mechón
    public Color colorOjo     = new Color(0.93f, 0.80f, 0.96f);
    public Color colorPupila  = new Color(0.03f, 0.03f, 0.03f);
    public Color colorNariz   = new Color(0.06f, 0.04f, 0.04f);

    [Header("Estilo stop motion")]
    [Tooltip("Si está activo, la pose se actualiza por cuadros como en stop motion")]
    public bool stopMotion = true;
    [Range(4, 30)] public int fpsStopMotion = 12;
    [Tooltip("Cuánto tiembla la pose en cada cuadro (0 = nada)")]
    [Range(0f, 3f)] public float temblor = 1f;
    [Tooltip("Si está activo, también la posición del muñeco se mueve por cuadros (más cortado)")]
    public bool posicionEscalonada = false;

    [Header("Animación")]
    public float velocidadParaCaminar = 0.3f;
    public float velocidadParaCorrer = 6f;
    public float anguloCaminar = 45f;
    public float anguloCorrer = 70f;
    [Tooltip("Solo se usa con stopMotion desactivado")]
    public float suavizado = 14f;
    public float velocidadGiro = 14f;

    [Header("Polvo")]
    public bool polvo = true;
    public Color colorPolvo = new Color(0.88f, 0.84f, 0.76f);

    [Header("Suelo")]
    public LayerMask capasSuelo = ~0;
    public float margenSuelo = 0.15f;

    // Rotación extra que le pone el script "movimiento" al modelo durante un emote (identidad = nada)
    [HideInInspector] public Quaternion rotacionEmote = Quaternion.identity;

    // ---- rig ----
    Rigidbody rb;
    Collider col;
    Material matBase;
    Transform raiz;           // modelo visual (no rueda con la esfera)
    Transform cuerpo;         // torso + brazos + cuello (origen en la cadera)
    Transform cuello;         // pivote de la cabeza (todo lo de la cara cuelga de aquí)
    Transform brazoI, brazoD, piernaI, piernaD;
    Transform orejaI, orejaD, ojoI, ojoD;
    float cintura;            // altura de la cadera sobre el piso

    const float OREJA_BASE = 28f;

    // ---- estado físico ----
    bool enSuelo, enSueloAntes;
    float vy, vyAntes, velHoriz;
    Vector3 vHoriz;
    float tiempoAterrizaje, impactoAterrizaje, tiempoDespegue;
    const float DURACION_ATERRIZAJE = 0.28f;

    // ---- picada / deslizamiento ----
    float tiempoPicada;
    float tDesdePicada = 99f, tDesdeDeslizar = 99f;
    bool deslizando;
    int rafagaPolvo;

    // ---- animación ----
    float fase, tiempoPaso, ampRamp;
    Quaternion giroObjetivo = Quaternion.identity;
    Quaternion yaw = Quaternion.identity;
    float inclinacion;   // 0 = de pie, 90 = horizontal con la panza hacia abajo
    float desliz;        // 0..1: cuánto se pega el cuerpo al piso al deslizarse
    float miraY, miraYObj, tiempoMirar;
    float tiempoParpadeo = 2f, parpadeoRestante;
    Vector3 piesActual, posVisual;
    bool pasoNuevo, posInit;

    // ---- polvo ----
    class Polvo { public Transform t; public Vector3 vel; public float vida, vidaMax; }
    readonly List<Polvo> polvoPool = new List<Polvo>();
    Transform polvoRaiz;
    float tamPolvo;

    // ---------------------------------------------------------------- API pública
    /// <summary>Lo llama el script de movimiento al hacer el doble salto (dura hasta que aterrices).</summary>
    public void IniciarPicada(float duracion)
    {
        tiempoPicada = duracion;
        deslizando = false;
        tDesdePicada = 0f;
    }

    /// <summary>Lo llama el script de movimiento al aterrizar: sigue boca abajo mientras se desliza.</summary>
    public void IniciarDeslizamiento(float duracion)
    {
        tiempoPicada = duracion;
        deslizando = true;
        tDesdeDeslizar = 0f;
        rafagaPolvo = 10;
    }

    /// <summary>Corta la picada o el deslizamiento y vuelve a la pose normal.</summary>
    public void TerminarPicada()
    {
        tiempoPicada = 0f;
        deslizando = false;
    }

    // ---------------------------------------------------------------- construcción
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        cintura = 0.17f * altura;
        tamPolvo = altura * 0.07f;

        // Ocultar la esfera original (se queda el collider/rigidbody para la física)
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) { matBase = mr.sharedMaterial; mr.enabled = false; }

        ConstruirPug();
        CrearPolvo();
    }

    Material CrearMaterial(Color color, float brillo)
    {
        Material m = matBase != null ? new Material(matBase) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    /// <summary>Pieza primitiva: posición y tamaño en fracciones de la altura total, sin colisionador.</summary>
    Transform Crear(PrimitiveType tipo, string nombre, Transform padre, Vector3 posH, Vector3 escH, Color color, float brillo = 0.2f)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Destroy(go.GetComponent<Collider>());   // la física la maneja el Jugador
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posH * altura;
        go.transform.localScale = escH * altura;
        go.GetComponent<MeshRenderer>().sharedMaterial = CrearMaterial(color, brillo);
        return go.transform;
    }

    Transform Pivote(string nombre, Transform padre, Vector3 posH)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = posH * altura;
        return t;
    }

    void ConstruirPug()
    {
        raiz = new GameObject("PersonajePug_Visual").transform;
        raiz.position = transform.position;

        // Piernas cortitas, pivote en la cadera
        piernaI = CrearPierna("PiernaI", -1f);
        piernaD = CrearPierna("PiernaD", 1f);

        // Cuerpo: origen en la cadera
        cuerpo = Pivote("Cuerpo", raiz, new Vector3(0f, 0.17f, 0f));
        Crear(PrimitiveType.Sphere, "Torso", cuerpo, new Vector3(0f, 0.14f, 0f), new Vector3(0.30f, 0.34f, 0.27f), colorCuerpo);
        Crear(PrimitiveType.Sphere, "Barriga", cuerpo, new Vector3(0f, 0.12f, 0.06f), new Vector3(0.25f, 0.28f, 0.20f), colorVientre);

        // Brazos finitos que cuelgan a los lados
        brazoI = CrearBrazo("BrazoI", -1f);
        brazoD = CrearBrazo("BrazoD", 1f);

        // Cabeza gigante sobre el cuello
        cuello = Pivote("Cuello", cuerpo, new Vector3(0f, 0.30f, 0f));
        Crear(PrimitiveType.Sphere, "Cabeza", cuello, new Vector3(0f, 0.25f, 0f), new Vector3(0.62f, 0.50f, 0.54f), colorPelaje);

        // Hocico oscuro de pug, nariz y mejillas
        Crear(PrimitiveType.Sphere, "Hocico", cuello, new Vector3(0f, 0.14f, 0.19f), new Vector3(0.40f, 0.28f, 0.22f), colorHocico);
        Crear(PrimitiveType.Sphere, "Nariz", cuello, new Vector3(0f, 0.21f, 0.30f), new Vector3(0.075f, 0.05f, 0.05f), colorNariz, 0.9f);
        Crear(PrimitiveType.Sphere, "MejillaI", cuello, new Vector3(-0.07f, 0.11f, 0.285f), new Vector3(0.11f, 0.08f, 0.06f), colorMejilla);
        Crear(PrimitiveType.Sphere, "MejillaD", cuello, new Vector3( 0.07f, 0.11f, 0.285f), new Vector3(0.11f, 0.08f, 0.06f), colorMejilla);

        // Ojos enormes con pupilas mirando hacia afuera
        ojoI = CrearOjo("OjoI", -1f);
        ojoD = CrearOjo("OjoD", 1f);

        // Orejas caídas
        orejaI = CrearOreja("OrejaI", -1f);
        orejaD = CrearOreja("OrejaD", 1f);

        // Mechón de pelo en la coronilla
        var m1 = Crear(PrimitiveType.Capsule, "Mechon1", cuello, new Vector3(0.00f, 0.51f, 0.03f), new Vector3(0.028f, 0.045f, 0.028f), colorOscuro);
        m1.localRotation = Quaternion.Euler(0f, 0f, 25f);
        var m2 = Crear(PrimitiveType.Capsule, "Mechon2", cuello, new Vector3(0.03f, 0.505f, 0.02f), new Vector3(0.028f, 0.040f, 0.028f), colorOscuro);
        m2.localRotation = Quaternion.Euler(0f, 0f, -20f);

        // Bigotes (3 por lado)
        for (int lado = -1; lado <= 1; lado += 2)
        {
            for (int i = 0; i < 3; i++)
            {
                var w = Crear(PrimitiveType.Cube, "Bigote", cuello,
                              new Vector3(lado * 0.215f, 0.10f + i * 0.025f, 0.22f),
                              new Vector3(0.10f, 0.004f, 0.004f), colorHocico);
                w.localRotation = Quaternion.Euler(0f, 0f, lado * (i - 1) * 14f);
            }
        }
    }

    Transform CrearPierna(string nombre, float lado)
    {
        var p = Pivote(nombre + "_Pivote", raiz, new Vector3(lado * 0.075f, 0.17f, 0f));
        Crear(PrimitiveType.Capsule, nombre, p, new Vector3(0f, -0.085f, 0f), new Vector3(0.085f, 0.085f, 0.085f), colorCuerpo);
        Crear(PrimitiveType.Sphere, nombre + "_Pata", p, new Vector3(0f, -0.145f, 0.025f), new Vector3(0.10f, 0.055f, 0.13f), colorPelaje);
        return p;
    }

    Transform CrearBrazo(string nombre, float lado)
    {
        var p = Pivote(nombre + "_Pivote", cuerpo, new Vector3(lado * 0.165f, 0.26f, 0.015f));
        Crear(PrimitiveType.Capsule, nombre, p, new Vector3(0f, -0.10f, 0f), new Vector3(0.055f, 0.10f, 0.055f), colorCuerpo);
        Crear(PrimitiveType.Sphere, nombre + "_Mano", p, new Vector3(0f, -0.20f, 0.005f), new Vector3(0.07f, 0.065f, 0.075f), colorCuerpo);
        return p;
    }

    Transform CrearOjo(string nombre, float lado)
    {
        // El pivote no tiene escala: así el parpadeo (escala Y) afecta a todas las piezas del ojo
        var p = Pivote(nombre, cuello, new Vector3(lado * 0.13f, 0.30f, 0.20f));
        Crear(PrimitiveType.Sphere, nombre + "_Aro", p, new Vector3(0f, 0f, -0.012f), new Vector3(0.25f, 0.23f, 0.10f), colorHocico);
        Crear(PrimitiveType.Sphere, nombre + "_Globo", p, Vector3.zero, Vector3.one * 0.20f, colorOjo, 0.9f);
        Crear(PrimitiveType.Sphere, nombre + "_Pupila", p, new Vector3(lado * 0.03f, 0f, 0.09f), new Vector3(0.075f, 0.085f, 0.05f), colorPupila, 0.95f);
        return p;
    }

    Transform CrearOreja(string nombre, float lado)
    {
        var p = Pivote(nombre + "_Pivote", cuello, new Vector3(lado * 0.22f, 0.43f, 0f));
        Crear(PrimitiveType.Sphere, nombre, p, new Vector3(lado * 0.02f, -0.08f, 0f), new Vector3(0.20f, 0.22f, 0.07f), colorPelaje);
        Crear(PrimitiveType.Sphere, nombre + "_Punta", p, new Vector3(lado * 0.03f, -0.13f, 0.02f), new Vector3(0.12f, 0.10f, 0.05f), colorOscuro);
        return p;
    }

    void CrearPolvo()
    {
        polvoRaiz = new GameObject("PersonajePug_Polvo").transform;
        for (int i = 0; i < 32; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Polvo";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(polvoRaiz, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = CrearMaterial(colorPolvo, 0.1f);
            go.SetActive(false);
            polvoPool.Add(new Polvo { t = go.transform });
        }
    }

    // ---------------------------------------------------------------- bucle principal
    void Update()
    {
        if (raiz == null) return;
        float dt = Time.deltaTime;

        // --- estado físico (cada frame real) ---
        Vector3 v = rb.linearVelocity;
        vy = v.y;
        vHoriz = new Vector3(v.x, 0f, v.z);
        velHoriz = vHoriz.magnitude;

        enSueloAntes = enSuelo;
        enSuelo = DetectarSuelo();

        bool picando = tiempoPicada > 0f;
        if (enSuelo && !enSueloAntes && vyAntes < -3f && !picando)
        {
            tiempoAterrizaje = DURACION_ATERRIZAJE;
            impactoAterrizaje = Mathf.Clamp01(-vyAntes / 12f);
            rafagaPolvo = Mathf.Max(rafagaPolvo, 3 + Mathf.RoundToInt(impactoAterrizaje * 5f));
        }
        if (!enSuelo && enSueloAntes && vy > 2f && !picando)
            tiempoDespegue = 0.25f;
        vyAntes = vy;

        tiempoAterrizaje = Mathf.Max(0f, tiempoAterrizaje - dt);
        tiempoDespegue   = Mathf.Max(0f, tiempoDespegue - dt);
        if (tiempoPicada > 0f) tiempoPicada -= dt;
        if (tiempoPicada <= 0f) deslizando = false;
        tDesdePicada += dt;
        tDesdeDeslizar += dt;

        if (velHoriz > 0.5f)
            giroObjetivo = Quaternion.LookRotation(vHoriz.normalized, Vector3.up);

        // --- reloj de animación ---
        if (!stopMotion)
        {
            Animar(dt, true);
            return;
        }

        tiempoPaso += dt;
        float paso = 1f / Mathf.Max(1, fpsStopMotion);
        if (tiempoPaso >= paso)
        {
            int n = Mathf.FloorToInt(tiempoPaso / paso);
            float dtAnim = n * paso;
            tiempoPaso -= dtAnim;
            Animar(dtAnim, false);   // un "cuadro" nuevo de stop motion
        }
    }

    /// <summary>
    /// Calcula y aplica una pose. Con suave=true se interpola cada frame; con suave=false
    /// es un cuadro de stop motion: la pose salta directamente (o casi) a la nueva.
    /// </summary>
    void Animar(float dt, bool suave)
    {
        bool picando = tiempoPicada > 0f;

        float kSuave = 1f - Mathf.Exp(-suavizado * dt);
        float kSnap  = suave ? kSuave : 1f;      // poses de ciclo (caminar/correr): cuadro limpio
        float kTrans = suave ? kSuave : 0.65f;   // transiciones: llegan en 2-3 cuadros, como "intermedios"
        float kUsar  = kTrans;

        // Valores objetivo (ángulos en grados; X negativo = hacia adelante)
        float bI = 0f, bD = 0f, pI = 0f, pD = 0f;
        float bIz = -2f, bDz = 2f;
        float tX = 0f, tY = 0f, tZ = 0f;      // torso
        float cX = 0f, cY = 0f, cZ = 0f;      // cabeza
        float oreja = 0f;                     // + = orejas más abiertas hacia afuera
        float ojoEsc = 1f, ojoAbierto = 1f;
        float rebote = 0f;
        float inclObj = 0f;
        Vector3 esc = Vector3.one;
        float jit = suave ? 0f : temblor;
        bool idle = false;

        if (picando)
        {
            // ---------- PICADA / DESLIZAMIENTO ----------
            inclObj = 90f;
            ampRamp = 0f;
            bI = bD = -175f;
            bIz = -8f; bDz = 8f;
            ojoAbierto = 0.75f;

            if (!deslizando)
            {
                float aleteo = Mathf.Sin(Time.time * 20f) * 6f;
                pI = 8f + aleteo; pD = 12f - aleteo;
                tX = -10f;            // arqueado, estilo Superman
                cX = -60f;            // levanta la cabeza para mirar hacia adelante
                oreja = 45f;          // orejas al viento
                // estirón de lanzamiento (smear)
                if (tDesdePicada < 0.12f) esc = new Vector3(0.82f, 1.28f, 0.82f);
                else if (tDesdePicada < 0.30f) esc = new Vector3(0.96f, 1.07f, 0.96f);
            }
            else
            {
                pI = 4f; pD = 7f;
                tX = -4f;
                cX = -75f;            // cabeza bien arriba mirando hacia donde se desliza
                bIz = -16f; bDz = 16f;
                oreja = 30f;
                jit *= 2f;            // vibración de velocidad
                if (tDesdeDeslizar < 0.12f) { esc = new Vector3(1.18f, 0.78f, 1.18f); ojoAbierto = 0.5f; }   // impacto
                else if (tDesdeDeslizar < 0.25f) esc = new Vector3(0.98f, 1.05f, 0.98f);                       // rebote
            }
        }
        else if (!enSuelo)
        {
            // ---------- AIRE ----------
            ampRamp = 0f;
            float flot = Mathf.Sin(Time.time * 25f) * 8f;

            if (vy > 0.5f)
            {
                // subiendo: brazos arriba, piernas recogidas, estirón de despegue
                bI = bD = -150f; bIz = -25f; bDz = 25f;
                pI = -25f; pD = 18f;
                tX = -4f; cX = -10f;
                oreja = -25f;         // las orejas se pegan hacia abajo
                esc = tiempoDespegue > 0.12f ? new Vector3(0.86f, 1.20f, 0.86f)
                                             : new Vector3(0.95f, 1.06f, 0.95f);
            }
            else if (vy > -1.5f)
            {
                // cima del salto: pose "congelada"
                bI = bD = -160f; bIz = -30f; bDz = 30f;
                pI = -10f; pD = 10f;
                cX = -4f;
                oreja = 10f;
            }
            else
            {
                // cayendo: brazos al aire que aletean, orejas abiertas como alas, ojos muy abiertos
                bI = -125f + flot; bD = -125f - flot;
                bIz = -18f; bDz = 18f;
                pI = -12f + flot * 0.5f; pD = 14f - flot * 0.5f;
                tX = 6f; cX = 8f;
                oreja = 55f + flot * 0.4f;
                ojoEsc = vy < -8f ? 1.2f : 1.1f;
                if (vy < -8f) esc = new Vector3(0.94f, 1.08f, 0.94f);
            }
        }
        else if (velHoriz > velocidadParaCaminar)
        {
            // ---------- CAMINAR / CORRER (waddle de pug) ----------
            float t = Mathf.InverseLerp(velocidadParaCaminar, velocidadParaCorrer, velHoriz);
            float ang = Mathf.Lerp(anguloCaminar, anguloCorrer, t);
            ampRamp = Mathf.MoveTowards(ampRamp, 1f, dt * 5f);

            // Con pocos cuadros por segundo hay que limitar la frecuencia para evitar el efecto "rueda de carreta"
            float maxFase = suave ? 24f : 2f * Mathf.PI * Mathf.Max(1, fpsStopMotion) / 5f;
            float rate = Mathf.Lerp(7f, 12f, t) * Mathf.Clamp(velHoriz / 4f, 0.6f, 1.3f);
            fase += dt * Mathf.Min(rate, maxFase);

            float s = Mathf.Sin(fase), c = Mathf.Cos(fase);
            float a = ang * ampRamp;

            pI = -s * a;        pD =  s * a;
            bI =  s * a * 0.9f; bD = -s * a * 0.9f;
            bIz = -4f - 6f * t; bDz = 4f + 6f * t;

            tX = Mathf.Lerp(3f, 14f, t);                 // se inclina hacia adelante al correr
            tY = s * Mathf.Lerp(5f, 12f, t);             // torsión del torso contra las caderas
            tZ = c * Mathf.Lerp(4f, 8f, t);              // balanceo lateral exagerado (waddle)
            cX = -tX * 0.6f + c * 3f; cY = -tY * 0.6f; cZ = -tZ * 0.7f;   // la cabezota compensa y rebota
            oreja = Mathf.Sin(2f * fase) * Mathf.Lerp(8f, 16f, t) + Mathf.Lerp(0f, 15f, t);
            rebote = Mathf.Abs(c) * 0.06f * altura * Mathf.Lerp(0.6f, 1.2f, t);
            kUsar = kSnap;
        }
        else
        {
            // ---------- IDLE ----------
            idle = true;
            ampRamp = 0f;
            fase += dt * 1.8f;
            float s = Mathf.Sin(fase);

            bI = s * 3f; bD = -s * 3f;
            bIz = -3f - s * 1.5f; bDz = 3f + s * 1.5f;
            tX = 1.5f * s; tZ = 1.2f * s;
            cX = -2f * s;
            oreja = s * 2f;
            rebote = (s * 0.5f + 0.5f) * 0.014f * altura;

            // De vez en cuando mira a los lados
            tiempoMirar -= dt;
            if (tiempoMirar <= 0f)
            {
                miraYObj = Random.value < 0.35f ? 0f : Random.Range(-28f, 28f);
                tiempoMirar = Random.Range(1.2f, 3.5f);
            }
        }

        miraY = Mathf.Lerp(miraY, idle ? miraYObj : 0f, kTrans);
        if (idle) cY = miraY;

        // ---------- Aterrizaje normal: aplastamiento con rebote ----------
        if (!picando && enSuelo && tiempoAterrizaje > 0f)
        {
            float tt = DURACION_ATERRIZAJE - tiempoAterrizaje;
            float f = Mathf.Lerp(0.5f, 1f, impactoAterrizaje);
            if (tt < 0.10f) { esc = new Vector3(1f + 0.18f * f, 1f - 0.24f * f, 1f + 0.18f * f); ojoAbierto = 0.5f; }
            else if (tt < 0.20f) esc = new Vector3(1f - 0.05f * f, 1f + 0.08f * f, 1f - 0.05f * f);
            tX += 8f * f;
            oreja += 20f * f;
        }

        // ---------- Parpadeo ----------
        tiempoParpadeo -= dt;
        if (tiempoParpadeo <= 0f)
        {
            parpadeoRestante = 0.1f;
            tiempoParpadeo = Random.Range(1.8f, 4.5f);
        }
        else if (parpadeoRestante > 0f)
        {
            parpadeoRestante -= dt;
        }

        // ---------- Aplicar la pose (con temblor de stop motion) ----------
        System.Func<float, float> J = g => jit > 0f ? (Random.value * 2f - 1f) * g * jit : 0f;

        brazoI.localRotation  = Quaternion.Slerp(brazoI.localRotation,  Quaternion.Euler(bI + J(1.5f), J(1f), bIz + J(1f)), kUsar);
        brazoD.localRotation  = Quaternion.Slerp(brazoD.localRotation,  Quaternion.Euler(bD + J(1.5f), J(1f), bDz + J(1f)), kUsar);
        piernaI.localRotation = Quaternion.Slerp(piernaI.localRotation, Quaternion.Euler(pI + J(1.5f), 0f, J(0.8f)), kUsar);
        piernaD.localRotation = Quaternion.Slerp(piernaD.localRotation, Quaternion.Euler(pD + J(1.5f), 0f, J(0.8f)), kUsar);

        cuerpo.localRotation = Quaternion.Slerp(cuerpo.localRotation, Quaternion.Euler(tX + J(0.8f), tY, tZ + J(0.8f)), kUsar);
        cuello.localRotation = Quaternion.Slerp(cuello.localRotation, Quaternion.Euler(cX + J(1f), cY, cZ), kUsar);

        // Orejas (izquierda = lado negativo): se abren hacia afuera según "oreja"
        orejaI.localRotation = Quaternion.Slerp(orejaI.localRotation, Quaternion.Euler(J(2f), 0f, -(OREJA_BASE + oreja + J(2f))), kTrans);
        orejaD.localRotation = Quaternion.Slerp(orejaD.localRotation, Quaternion.Euler(J(2f), 0f,  (OREJA_BASE + oreja + J(2f))), kTrans);

        // Ojos: parpadeo y expresión
        float ojoY = (parpadeoRestante > 0f ? 0.12f : 1f) * ojoAbierto;
        Vector3 escOjo = new Vector3(ojoEsc, ojoY * ojoEsc, ojoEsc);
        float kOjo = suave ? kSuave : 0.85f;
        ojoI.localScale = Vector3.Lerp(ojoI.localScale, escOjo, kOjo);
        ojoD.localScale = Vector3.Lerp(ojoD.localScale, escOjo, kOjo);

        Vector3 posCuerpo = new Vector3(J(0.01f), cintura + rebote, J(0.01f));
        cuerpo.localPosition = Vector3.Lerp(cuerpo.localPosition, posCuerpo, kUsar);

        raiz.localScale = Vector3.Lerp(raiz.localScale, esc, suave ? kSuave : 0.8f);

        // Giro e inclinación del muñeco completo
        yaw = Quaternion.Slerp(yaw, giroObjetivo, 1f - Mathf.Exp(-velocidadGiro * dt));
        inclinacion = Mathf.Lerp(inclinacion, inclObj,
                                 suave ? 1f - Mathf.Exp(-(picando ? 20f : 10f) * dt) : 0.65f);
        desliz = Mathf.Lerp(desliz, (picando && deslizando) ? 1f : 0f,
                            suave ? 1f - Mathf.Exp(-16f * dt) : 0.7f);
        raiz.rotation = yaw * Quaternion.Euler(inclinacion, 0f, 0f) * rotacionEmote;

        // ---------- Polvo ----------
        if (polvo)
        {
            if (rafagaPolvo > 0) SoltarPolvo(rafagaPolvo, 1.6f);
            if (picando && deslizando && velHoriz > 1f) SoltarPolvo(2, 0.8f);
            ActualizarPolvo(dt);
        }
        rafagaPolvo = 0;

        pasoNuevo = true;
    }

    void LateUpdate()
    {
        if (raiz == null) return;

        // El emote (si hay uno) se aplica CADA frame, no solo en los cuadros de stop motion
        raiz.rotation = yaw * Quaternion.Euler(inclinacion, 0f, 0f) * rotacionEmote;

        // El modelo sigue al Jugador pero NO hereda su rotación (la esfera puede rodar)
        float radioPies = col != null ? col.bounds.extents.y : 0.5f;
        Vector3 c = col != null ? col.bounds.center : transform.position;
        Vector3 pies = new Vector3(c.x, c.y - radioPies, c.z);
        piesActual = pies;

        // Al inclinarse, el cuerpo gira alrededor de su centro y baja hacia el centro del collider;
        // al deslizarse baja aún más, pegado al piso (la cabezota necesita espacio)
        float w = inclinacion / 90f;
        Vector3 centroPicada = Vector3.Lerp(c, pies + Vector3.up * altura * 0.24f, desliz);
        Vector3 centroCuerpo = Vector3.Lerp(pies + Vector3.up * altura * 0.5f, centroPicada, w);
        Vector3 mitad = Vector3.Scale(Vector3.up * altura * 0.5f, raiz.localScale);
        Vector3 pos = centroCuerpo - raiz.rotation * mitad;

        if (!(posicionEscalonada && stopMotion) || pasoNuevo || !posInit)
        {
            posVisual = pos;
            posInit = true;
            pasoNuevo = false;
        }
        raiz.position = posVisual;
    }

    // ---------------------------------------------------------------- polvo
    void SoltarPolvo(int n, float fuerza)
    {
        Vector3 atras = -(yaw * Vector3.forward);
        for (int i = 0; i < n; i++)
        {
            Polvo p = TomarPolvo();
            if (p == null) return;

            Vector3 rnd = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            p.t.position = piesActual + Vector3.up * altura * 0.03f + rnd * altura * 0.12f + atras * altura * 0.15f;
            p.vel = rnd * fuerza + atras * fuerza * 0.6f + Vector3.up * Random.Range(0.8f, 1.8f) * fuerza;
            p.vidaMax = Random.Range(0.35f, 0.55f);
            p.vida = p.vidaMax;
            p.t.localScale = Vector3.one * tamPolvo;
            p.t.gameObject.SetActive(true);
        }
    }

    Polvo TomarPolvo()
    {
        for (int i = 0; i < polvoPool.Count; i++)
            if (polvoPool[i].vida <= 0f) return polvoPool[i];
        return null;
    }

    void ActualizarPolvo(float dt)
    {
        for (int i = 0; i < polvoPool.Count; i++)
        {
            Polvo p = polvoPool[i];
            if (p.vida <= 0f) continue;

            p.vida -= dt;
            if (p.vida <= 0f) { p.t.gameObject.SetActive(false); continue; }

            p.vel.y -= 5f * dt;
            p.vel *= Mathf.Max(0f, 1f - 2.5f * dt);
            p.t.position += p.vel * dt;
            p.t.localScale = Vector3.one * (tamPolvo * Mathf.Clamp01(p.vida / p.vidaMax) * 1.4f);
            p.t.rotation = Random.rotation;   // giran a saltos, como en stop motion
        }
    }

    // ---------------------------------------------------------------- suelo
    bool DetectarSuelo()
    {
        float radio = col != null ? col.bounds.extents.y : 0.5f;
        Vector3 origen = col != null ? col.bounds.center : transform.position;
        return Physics.SphereCast(origen, radio * 0.6f, Vector3.down, out _, radio * 0.4f + margenSuelo,
                                  capasSuelo, QueryTriggerInteraction.Ignore);
    }

    void OnDestroy()
    {
        if (raiz != null) Destroy(raiz.gameObject);
        if (polvoRaiz != null) Destroy(polvoRaiz.gameObject);
    }
}