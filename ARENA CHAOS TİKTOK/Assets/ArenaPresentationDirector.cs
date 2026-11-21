using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Visual-only presentation layer for the arena. It never owns fighter physics
/// or gameplay state and is deliberately capped to keep the arena lightweight.
/// </summary>
public sealed class ArenaPresentationDirector : MonoBehaviour
{
    private static readonly Dictionary<int, Material> materials = new Dictionary<int, Material>();
    private readonly Dictionary<ArenaFighter, int> lastAttackCycles = new Dictionary<ArenaFighter, int>();
    private Transform visualsRoot;
    private Camera arenaCamera;
    private Transform cameraRig;
    private Transform CameraRig => cameraRig!=null?cameraRig:arenaCamera.transform;
    private Vector3 cameraHomePosition;
    private Quaternion cameraHomeRotation;
    private Vector3 cameraBasePosition;
    private Quaternion cameraBaseRotation;
    bool portraitCameraLayout;
    private Coroutine cameraRoutine;
    private float nextCameraFocus;
    private Material redNeon;
    private Material blueNeon;
    private BattleArenaManager manager;
    [Header("Entrance Queue")]
    [SerializeField, Min(1)] private int maximumEntranceQueue = 12;
    [SerializeField, Min(0)] private float entranceInterval = .25f;
    [SerializeField, Min(.1f)] private float gateLiftDuration = .35f;
    [SerializeField, Min(.1f)] private float doorSwingDuration = .4f;
    [SerializeField, Min(.1f)] private float entranceWalkDuration = 1.3f;
    [SerializeField, Min(.1f)] private float cameraTransitionDuration = .8f;
    [SerializeField, Range(0,.4f)] private float cameraApproach = .18f;
    readonly Queue<KeyValuePair<ArenaFighter,Vector3>> entranceQueue = new Queue<KeyValuePair<ArenaFighter,Vector3>>();
    bool processingEntrance;
    public int PendingEntrances => entranceQueue.Count;
    public bool CanAcceptEntrance => entranceQueue.Count < maximumEntranceQueue;
    public int ActiveEntrances { get; private set; }
    readonly Transform[] hingedDoors = new Transform[2];
    readonly Quaternion[] closedDoorRotations = new Quaternion[2];
    private readonly Transform[] gateDoors = new Transform[2];
    private readonly List<Transform> audience = new List<Transform>();
    private readonly List<Vector3> audiencePositions = new List<Vector3>();
    private float nextCrowdUpdate;
    private float cheerUntil;
    [SerializeField] private AudioClip crowdAmbience;
    [SerializeField] private AudioClip crowdCheer;
    private AudioSource cheerSource;
    [SerializeField, Range(0,1)] private float ambienceVolume=.18f;
    [SerializeField, Range(0,1)] private float cheerVolume=.4f;
    private AudioSource ambienceSource;
    private float Radius => manager != null ? manager.ArenaRadius : 8f;

    private void Start()
    {
        manager = GetComponent<BattleArenaManager>();
        arenaCamera = Camera.main;
        if (arenaCamera != null)
        {
            cameraBasePosition=arenaCamera.transform.position;cameraBaseRotation=arenaCamera.transform.rotation;portraitCameraLayout=Screen.width<Screen.height;
            ArenaPortraitCameraFit.Fit(arenaCamera,manager.ArenaRadius);
            cameraHomePosition = CameraRig.position;
            cameraHomeRotation = CameraRig.rotation;
            var brain=arenaCamera.GetComponent<CinemachineBrain>();
            if(brain==null)brain=arenaCamera.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,cameraTransitionDuration);
            var rig=new GameObject("Arena Cinemachine Camera");rig.transform.SetParent(transform,false);
            rig.transform.SetPositionAndRotation(cameraHomePosition,cameraHomeRotation);
            var virtualCamera=rig.AddComponent<CinemachineCamera>();
            virtualCamera.Lens.FieldOfView=arenaCamera.fieldOfView;
            virtualCamera.Lens.NearClipPlane=arenaCamera.nearClipPlane;
            virtualCamera.Lens.FarClipPlane=arenaCamera.farClipPlane;
            cameraRig=rig.transform;
        }

        BuildAudienceAndLighting();
        var content = ArenaContent.Load();
        if (content != null) { crowdAmbience = content.ambience; crowdCheer = content.cheer; }
        // Optional licensed recordings; no synthetic stand-in for real cheering.
        if (crowdAmbience == null) crowdAmbience = Resources.Load<AudioClip>("ArenaAudio/CrowdAmbience");
        if (crowdCheer == null) crowdCheer = Resources.Load<AudioClip>("ArenaAudio/CrowdCheer");
        if (crowdAmbience != null)
        {
            AudioSource ambience = gameObject.AddComponent<AudioSource>();
            ambienceSource=ambience;
            ambience.clip = crowdAmbience;
            ambience.loop = true;
            ambience.volume = ambienceVolume;
            ambience.Play();
        }
        cheerSource = gameObject.AddComponent<AudioSource>();
        cheerSource.playOnAwake = false;
        cheerSource.volume = cheerVolume;
    }

    private void Update()
    {
        if(arenaCamera!=null&&portraitCameraLayout!=(Screen.width<Screen.height))
        {
            portraitCameraLayout=Screen.width<Screen.height;
            if(cameraRoutine!=null){StopCoroutine(cameraRoutine);cameraRoutine=null;}
            arenaCamera.rect=new Rect(0,0,1,1);arenaCamera.transform.SetPositionAndRotation(cameraBasePosition,cameraBaseRotation);
            ArenaPortraitCameraFit.Fit(arenaCamera,manager.ArenaRadius);
            cameraHomePosition=arenaCamera.transform.position;cameraHomeRotation=arenaCamera.transform.rotation;
            CameraRig.SetPositionAndRotation(cameraHomePosition,cameraHomeRotation);
        }
        if(ambienceSource!=null)ambienceSource.volume=ambienceVolume * ArenaAnnouncer.BackgroundGain;
        if(cheerSource!=null)cheerSource.volume=cheerVolume * ArenaAnnouncer.BackgroundGain;
        AnimateAudience();
        if (ArenaContent.Load() != null) return; // New impact effects are emitted only on actual limb contact.
        if (manager == null) return;

        foreach (ArenaFighter fighter in manager.Fighters)
        {
            if (fighter == null || !fighter.IsAlive) continue;
            if (!lastAttackCycles.TryGetValue(fighter, out int previous)) { lastAttackCycles[fighter] = fighter.AttackCycle; continue; }
            if (fighter.AttackCycle != previous)
            {
                lastAttackCycles[fighter] = fighter.AttackCycle;
                PlayPunchImpact(fighter.transform.position + fighter.transform.forward * 0.75f + Vector3.up * 0.72f, fighter.IsHeadbutting);
            }
        }
    }

    public void PlayEntrance(ArenaFighter fighter, Vector3 entryPosition)
    {
        if (fighter == null) return;
        fighter.IsEntering = true;
        fighter.enabled = false;
        fighter.gameObject.SetActive(false);
        entranceQueue.Enqueue(new KeyValuePair<ArenaFighter,Vector3>(fighter,entryPosition));
        if (!processingEntrance) StartCoroutine(ProcessEntrances());
    }

    public void PlayDuelEntrance(ArenaFighter fighter, int side)
    {
        fighter.IsEntering = true;
        fighter.enabled = false;
        StartCoroutine(EntranceRoutine(fighter, new Vector3(side, 0, 0), tournament: true));
    }

    private IEnumerator ProcessEntrances()
    {
        processingEntrance = true;
        while(entranceQueue.Count > 0)
        {
            var request = entranceQueue.Dequeue();
            if(request.Key == null || !request.Key.IsAlive) continue;
            ActiveEntrances = 1;
            yield return EntranceRoutine(request.Key,request.Value);
            ActiveEntrances = 0;
            // Camera return finishes during the short queue interval.
            yield return new WaitForSeconds(entranceInterval);
        }
        processingEntrance = false;
    }

    private IEnumerator SwingDoor(int gate, bool opening)
    {
        var door = hingedDoors[gate];
        if(door == null) yield break;
        ArenaAudioEffects.Door();
        var closed = closedDoorRotations[gate];
        var opened = closed * Quaternion.Euler(0,105,0);
        float elapsed=0;
        while(elapsed<doorSwingDuration)
        {
            elapsed += Time.deltaTime;
            door.localRotation=Quaternion.Slerp(opening?closed:opened,opening?opened:closed,
                Mathf.SmoothStep(0,1,elapsed/doorSwingDuration));
            yield return null;
        }
        door.localRotation=opening?opened:closed;
    }

    public void PlayMegaBlast()
    {
        if (ArenaContent.Load() != null) { ArenaAudioEffects.Blast(); return; }
        StartCoroutine(MegaBlastRoutine());
    }

    private IEnumerator EntranceRoutine(ArenaFighter fighter, Vector3 entryPosition, bool tournament = false)
    {
        int side = entryPosition.x < 0f ? -1 : 1;
        int gate = side < 0 ? 0 : 1;
        if (fighter == null || !fighter.IsAlive) yield break;
        Rigidbody body = fighter.GetComponent<Rigidbody>();
        bool wasKinematic = body.isKinematic;
        bool hadCollisions = body.detectCollisions;
        body.isKinematic = true;
        body.detectCollisions = false;
        Vector3 start = new Vector3(side * (Radius + 3.2f), 1f, 0f);
        Vector3 finish = new Vector3(side * (Radius - 2.2f), 1f, 0f);
        fighter.transform.SetPositionAndRotation(start, Quaternion.LookRotation(Vector3.left * side));
        // Instantiate is hidden behind the opaque door and roof.
        fighter.gameObject.SetActive(true);
        if (!tournament) FocusCameraBriefly(fighter);
        ArenaAudioEffects.Door();
        cheerUntil = Time.time + 4f;
        if (cheerSource != null && crowdCheer != null && !cheerSource.isPlaying) cheerSource.PlayOneShot(crowdCheer);
        Transform door = gateDoors[gate];
        Vector3 closed = door != null ? door.position : Vector3.zero;
        float elapsed = 0f;
        while (elapsed < gateLiftDuration)
        {
            elapsed += Time.deltaTime;
            if (door != null) door.position = closed + Vector3.up * (3.5f * Mathf.SmoothStep(0f, 1f, elapsed / gateLiftDuration));
            yield return null;
        }
        elapsed = 0f;
        yield return SwingDoor(gate, true);
        float duration = entranceWalkDuration;
        while (elapsed < duration && fighter != null)
        {
            elapsed += Time.deltaTime;
            fighter.transform.position = Vector3.Lerp(start, finish, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (fighter != null && fighter.IsAlive)
        {
            Vector3 clear = FindClearEntrance(fighter, side, finish);
            Vector3 approach = fighter.transform.position;
            elapsed = 0f;
            while (elapsed < .15f)
            {
                elapsed += Time.deltaTime;
                fighter.transform.position = Vector3.Lerp(approach, clear, Mathf.SmoothStep(0f, 1f, elapsed/.15f));
                yield return null;
            }
            finish = clear;
            fighter.transform.position = finish;
            body.position = finish;
            body.isKinematic = wasKinematic;
            body.detectCollisions = hadCollisions;
            fighter.IsEntering = false;
            fighter.enabled = true;
            if (!tournament) ArenaAnnouncer.PlayerEntered(fighter);
        }
        yield return SwingDoor(gate, false);
        elapsed = 0f;
        while (elapsed < gateLiftDuration)
        {
            elapsed += Time.deltaTime;
            if (door != null) door.position = closed + Vector3.up * (3.5f * (1f - Mathf.SmoothStep(0f, 1f, elapsed / gateLiftDuration)));
            yield return null;
        }
        if (door != null) door.position = closed;
    }

    private Vector3 FindClearEntrance(ArenaFighter incoming, int side, Vector3 fallback)
    {
        for(int depth=0;depth<6;depth++)
        for(int lane=0;lane<5;lane++)
        {
            Vector3 candidate = new Vector3(side*(Radius-2.2f-depth*.7f),1f,(lane-2)*.65f);
            bool free=true;
            foreach(var other in manager.Fighters)
            {
                if(other==null||other==incoming||!other.IsAlive||other.IsEntering)continue;
                Vector3 delta=other.transform.position-candidate;delta.y=0;
                if(delta.sqrMagnitude<1.44f){free=false;break;}
            }
            if(free)return candidate;
        }
        return fallback;
    }

    private void FocusCameraBriefly(ArenaFighter fighter)
    {
        if (arenaCamera == null || fighter == null || Screen.width<Screen.height) return;
        if (cameraRoutine != null || Time.time < nextCameraFocus) return;
        nextCameraFocus = Time.time + 5f;
        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        cameraRoutine = StartCoroutine(CameraFocusRoutine(fighter.transform));
    }

    private IEnumerator CameraFocusRoutine(Transform target)
    {
        Vector3 startPosition = CameraRig.position;
        Quaternion startRotation = CameraRig.rotation;
        Vector3 focusPosition = Vector3.Lerp(cameraHomePosition, target.position + Vector3.up * 7f, cameraApproach);
        Quaternion focusRotation = Quaternion.LookRotation(target.position + Vector3.up * 0.7f - focusPosition, Vector3.up);
        float elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / cameraTransitionDuration));
            CameraRig.SetPositionAndRotation(Vector3.Lerp(startPosition, focusPosition, t), Quaternion.Slerp(startRotation, focusRotation, t));
            yield return null;
        }

        elapsed=0f;
        var entering = target.GetComponent<ArenaFighter>();
        while(target!=null && entering!=null && entering.IsEntering && elapsed<gateLiftDuration+doorSwingDuration+entranceWalkDuration+1f)
        {
            elapsed+=Time.deltaTime;
            focusPosition=Vector3.Lerp(CameraRig.position,Vector3.Lerp(cameraHomePosition,target.position+Vector3.up*7f,cameraApproach),1f-Mathf.Exp(-2f*Time.deltaTime));
            focusRotation=Quaternion.LookRotation(target.position+Vector3.up*.5f-focusPosition);
            CameraRig.SetPositionAndRotation(focusPosition,focusRotation);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / cameraTransitionDuration));
            CameraRig.SetPositionAndRotation(Vector3.Lerp(focusPosition, cameraHomePosition, t), Quaternion.Slerp(focusRotation, cameraHomeRotation, t));
            yield return null;
        }
        CameraRig.SetPositionAndRotation(cameraHomePosition, cameraHomeRotation);
        cameraRoutine = null;
    }

    private void BuildAudienceAndLighting()
    {
        visualsRoot = new GameObject("Audience And Neon Presentation").transform;
        Transform arenaVisuals = GameObject.Find("ArenaVisuals")?.transform;
        visualsRoot.SetParent(arenaVisuals, false);
        redNeon = GetMaterial(new Color(1f, 0.015f, 0.04f), true);
        blueNeon = GetMaterial(new Color(0.01f, 0.35f, 1f), true);
        var content = ArenaContent.Load();
        if (content != null)
        {
            redNeon = content.red; blueNeon = content.blue;
            BuildTunnel(-1, content.stone, content.red);
            BuildTunnel(1, content.stone, content.blue);
            CreateText("ARENA CHAOS", new Vector3(0, 4.4f, Radius + .8f), Quaternion.identity, content.gold, .5f);
            for (int side = 0; side < 4; side++)
            {
                Quaternion rotation = Quaternion.Euler(0, side * 90, 0);
                Vector3 forward = rotation * Vector3.forward, right = rotation * Vector3.right;
                for (int row = 0; row < 2; row++)
                for (int bank = -1; bank <= 1; bank += 2)
                {
                    float height = .45f + row * .85f;
                    Vector3 center = forward * (Radius + 1.9f + row * 1.4f) + right * bank * 4.9f;
                    KayKitArenaEnvironment.Place(content.Find("floor_wood_large"), visualsRoot, center + Vector3.up * (height*.5f), new Vector3(5.4f,height,1.3f),rotation);
                    for(int i=0;i<3;i++)
                    {
                        Vector3 p = center + right * ((i-1)*1.65f) + Vector3.up * height;
                        int index=(side*6+row*3+i)%content.spectators.Length;
                        var spectator=Instantiate(content.spectators[index],p,Quaternion.LookRotation(-forward),visualsRoot);
                        spectator.transform.localScale=Vector3.one*.65f;
                        audience.Add(spectator.transform); audiencePositions.Add(p);
                    }
                }
            }
            return;
        }
        Material dark = GetMaterial(new Color(0.018f, 0.025f, 0.05f));
        Material seat = GetMaterial(new Color(0.12f, 0.14f, 0.2f));

        CreateText("ARENA CHAOS", new Vector3(0f, 3.15f, 8.05f), Quaternion.Euler(0f, 180f, 0f), redNeon, 0.32f);
        CreateText("LIVE BATTLE", new Vector3(0f, 3.15f, -8.05f), Quaternion.identity, blueNeon, 0.25f);

        BuildTunnel(-1, dark, redNeon);
        BuildTunnel(1, dark, blueNeon);
        for (int side = 0; side < 4; side++)
        {
            float angle = side * 90f;
            Vector3 forward = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
            for (int row = 0; row < 3; row++)
            {
                float height = 0.55f + row * 0.65f;
                Vector3 center = forward * (Radius + 1.4f + row * 0.9f);
                // Two banks leave the central aisle and entrance tunnels clear.
                for (int bank = -1; bank <= 1; bank += 2)
                {
                    Vector3 bankCenter = center + right * bank * 4.7f;
                    CreateBox("Tiered Tribune", bankCenter + Vector3.up * height * 0.5f,
                        new Vector3(5.3f, height, 0.88f), seat, Quaternion.Euler(0f, angle, 0f));
                    CreateBox("Tribune Accent", bankCenter + Vector3.up * (height + 0.03f) - forward * 0.44f,
                        new Vector3(5.3f, 0.045f, 0.045f), side % 2 == 0 ? blueNeon : redNeon, Quaternion.Euler(0f, angle, 0f));
                    // 72 spectators total, with shared materials and no Animators.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = bankCenter + right * ((i - 1) * 1.6f) + Vector3.up * (height + 0.12f);
                        Material shirt = GetMaterial(Color.HSVToRGB(((side * 17 + row * 7 + i * 13 + bank + 2) % 12) / 12f, 0.7f, 0.9f));
                        CreateAudienceFigure(p, Quaternion.LookRotation(-forward), shirt, dark);
                    }
                }
            }
        }

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 p = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 7.8f;
            CreateLight(p, i % 2 == 0 ? redNeon.color : blueNeon.color);
        }
    }

    private void CreateAudienceFigure(Vector3 position, Quaternion rotation, Material accent, Material dark)
    {
        Transform person = new GameObject("Cheering Spectator").transform;
        person.SetParent(visualsRoot, false);
        person.SetPositionAndRotation(position, rotation);
        audience.Add(person);
        audiencePositions.Add(position);
        Material skin = GetMaterial(new Color(1f, 0.72f, 0.44f));
        Material white = GetMaterial(Color.white);
        AudiencePart(person, PrimitiveType.Capsule, "Shirt", new Vector3(0f, 0.32f, 0f), new Vector3(0.42f, 0.3f, 0.28f), accent);
        AudiencePart(person, PrimitiveType.Sphere, "Big Funny Head", new Vector3(0f, 0.87f, 0f), new Vector3(0.44f, 0.46f, 0.4f), skin);
        AudiencePart(person, PrimitiveType.Sphere, "Left Wide Eye", new Vector3(-0.1f, 0.92f, 0.18f), new Vector3(0.13f, 0.15f, 0.065f), white);
        AudiencePart(person, PrimitiveType.Sphere, "Right Wide Eye", new Vector3(0.1f, 0.92f, 0.18f), new Vector3(0.13f, 0.15f, 0.065f), white);
        AudiencePart(person, PrimitiveType.Sphere, "Left Pupil", new Vector3(-0.085f, 0.92f, 0.216f), Vector3.one * 0.05f, dark);
        AudiencePart(person, PrimitiveType.Sphere, "Right Pupil", new Vector3(0.085f, 0.94f, 0.216f), Vector3.one * 0.05f, dark);
        AudiencePart(person, PrimitiveType.Sphere, "Cheering Mouth", new Vector3(0f, 0.77f, 0.19f), new Vector3(0.14f, 0.12f, 0.065f), dark);
        AudiencePart(person, PrimitiveType.Capsule, "Raised Left Arm", new Vector3(-0.29f, 0.7f, 0f), new Vector3(0.1f, 0.3f, 0.1f), skin);
        AudiencePart(person, PrimitiveType.Capsule, "Raised Right Arm", new Vector3(0.29f, 0.7f, 0f), new Vector3(0.1f, 0.3f, 0.1f), skin);
    }

    private void AudiencePart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Destroy(part.GetComponent<Collider>());
    }

    private void AnimateAudience()
    {
        if (Time.time < nextCrowdUpdate) return;
        nextCrowdUpdate = Time.time + 1f / 15f;
        float energy = Time.time < cheerUntil ? 0.18f : 0.055f;
        for (int i = 0; i < audience.Count; i++)
        {
            if (audience[i] == null) continue;
            float wave = Mathf.Sin(Time.time * 6f + i * 0.7f);
            audience[i].position = audiencePositions[i] + Vector3.up * (Mathf.Max(0f, wave) * energy);
            audience[i].localRotation = Quaternion.LookRotation(-new Vector3(audiencePositions[i].x, 0f, audiencePositions[i].z))
                * Quaternion.Euler(0f, 0f, wave * 5f);
        }
    }

    private void BuildTunnel(int side, Material dark, Material neon)
    {
        float x = side * (Radius + 1.7f);
        var content = ArenaContent.Load();
        if(content!=null)
        {
            KayKitArenaEnvironment.Place(content.Find("floor_tile_large"),visualsRoot,new Vector3(x,-.06f,0),new Vector3(5,.12f,3.2f),Quaternion.identity);
            KayKitArenaEnvironment.Place(content.Find("floor_tile_large"),visualsRoot,new Vector3(x,3.3f,0),new Vector3(5,.28f,3.6f),Quaternion.identity);
            foreach(int sign in new[]{-1,1})
            {
                KayKitArenaEnvironment.Place(content.Find("wall"),visualsRoot,new Vector3(x,1.6f,sign*1.7f),new Vector3(5,3.2f,.3f),Quaternion.identity);
                KayKitArenaEnvironment.Place(content.Find("column"),visualsRoot,new Vector3(side*(Radius-.2f),1.7f,sign*1.8f),new Vector3(.65f,3.4f,.65f),Quaternion.identity);
            }
            KayKitArenaEnvironment.Place(content.Find("wall"),visualsRoot,new Vector3(side*(Radius+4.1f),1.6f,0),new Vector3(3.6f,3.2f,.3f),Quaternion.Euler(0,90,0));
            var doorway = KayKitArenaEnvironment.Place(content.Find("wall_doorway"),visualsRoot,new Vector3(side*(Radius-.15f),1.9f,0),new Vector3(3.6f,3.8f,.55f),Quaternion.Euler(0,90,0));
            int gateIndex=side<0?0:1;
            foreach(Transform part in doorway.GetComponentsInChildren<Transform>())
                if(part.name == "wall_doorway_door")
                { hingedDoors[gateIndex]=part;closedDoorRotations[gateIndex]=part.localRotation;break; }
            if(hingedDoors[gateIndex]==null) Debug.LogError("Entrance door hinge not found",doorway);
            var gate = KayKitArenaEnvironment.Place(content.Find("wall_gated"),visualsRoot,new Vector3(side*(Radius-.10f),1.6f,0),new Vector3(3.15f,3.2f,.18f),Quaternion.Euler(0,90,0));
            gateDoors[side<0?0:1]=gate.transform;
            CreateText(side<0?"KIZIL KAPI":"MAVİ KAPI",new Vector3(side*(Radius-.5f),4f,0),Quaternion.Euler(0,side<0?90:-90,0),content.gold,.16f);
            return;
        }
        CreateBox("Entrance Floor", new Vector3(x, -0.06f, 0f), new Vector3(5f, 0.12f, 3.2f), dark, Quaternion.identity);
        CreateBox("Opaque Tunnel Roof", new Vector3(x, 3.3f, 0f), new Vector3(5f, 0.25f, 3.6f), dark, Quaternion.identity);
        CreateBox("Tunnel Side A", new Vector3(x, 1.6f, -1.7f), new Vector3(5f, 3.2f, 0.22f), dark, Quaternion.identity);
        CreateBox("Tunnel Side B", new Vector3(x, 1.6f, 1.7f), new Vector3(5f, 3.2f, 0.22f), dark, Quaternion.identity);
        CreateBox("Opaque Backstage", new Vector3(side * (Radius + 4.1f), 1.6f, 0f), new Vector3(0.25f, 3.2f, 3.6f), dark, Quaternion.identity);
        for (int i = -1; i <= 1; i += 2)
            CreateBox("Gate Neon Post", new Vector3(side * (Radius - 0.3f), 1.65f, i * 1.65f),
                new Vector3(0.22f, 3.4f, 0.16f), neon, Quaternion.identity);
        CreateBox("Gate Neon Header", new Vector3(side * (Radius - 0.3f), 3.4f, 0f),
            new Vector3(0.22f, 0.16f, 3.5f), neon, Quaternion.identity);
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = side < 0 ? "Red Sliding Entrance Door" : "Blue Sliding Entrance Door";
        door.transform.SetParent(visualsRoot, false);
        door.transform.position = new Vector3(side * (Radius - 0.15f), 1.6f, 0f);
        door.transform.localScale = new Vector3(0.18f, 3.2f, 3.15f);
        door.GetComponent<Renderer>().sharedMaterial = dark;
        Destroy(door.GetComponent<Collider>());
        gateDoors[side < 0 ? 0 : 1] = door.transform;
        CreateText(side < 0 ? "RED CORNER" : "BLUE CORNER",
            new Vector3(side * (Radius - 0.55f), 3.8f, 0f), Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f), neon, 0.15f);
    }

    private void CreateLight(Vector3 position, Color color)
    {
        GameObject lightObject = new GameObject("Neon Arena Light");
        lightObject.transform.SetParent(visualsRoot, false);
        lightObject.transform.position = position + Vector3.up * 3f;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 2.2f;
        light.range = 5.5f;
        light.shadows = LightShadows.None;
    }

    private void PlayPunchImpact(Vector3 position, bool headbutt)
    {
        GameObject root = new GameObject(headbutt ? "Headbutt Impact VFX" : "Punch Impact VFX");
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 0.18f;
        main.startLifetime = 0.18f;
        main.startSpeed = headbutt ? 3.5f : 4.5f;
        main.startSize = headbutt ? 0.12f : 0.08f;
        main.startColor = headbutt ? new Color(1f, 0.7f, 0.08f) : new Color(0.1f, 0.7f, 1f);
        main.maxParticles = 18;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;
        particles.Play();
        Destroy(root, 0.35f);
    }

    private IEnumerator MegaBlastRoutine()
    {
        GameObject root = new GameObject("Epic Gift Explosion VFX");
        root.transform.position = Vector3.up * 0.25f;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 1.2f;
        main.startLifetime = 0.9f;
        main.startSpeed = 8f;
        main.startSize = 0.16f;
        main.startColor = new Color(1f, 0.18f, 0.02f);
        main.maxParticles = 160;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 110) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        particles.Play();
        yield return new WaitForSeconds(1.25f);
        if (root != null) Destroy(root);
    }

    private void CreateText(string text, Vector3 position, Quaternion rotation, Material material, float size)
    {
        GameObject sign = new GameObject(text + " Sign");
        sign.transform.SetParent(visualsRoot, false);
        sign.transform.SetPositionAndRotation(position, rotation);
        TextMesh mesh = sign.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 64;
        mesh.characterSize = size * 0.25f;
        mesh.color = material != null ? material.color : Color.white;
    }

    private void CreateBox(string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(visualsRoot, false);
        box.transform.SetPositionAndRotation(position, rotation);
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(box.GetComponent<Collider>());
    }

    private void CreateSphere(string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(visualsRoot, false);
        sphere.transform.SetPositionAndRotation(position, rotation);
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(sphere.GetComponent<Collider>());
    }

    private void CreateCapsule(string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = name;
        capsule.transform.SetParent(visualsRoot, false);
        capsule.transform.SetPositionAndRotation(position, rotation);
        capsule.transform.localScale = scale;
        capsule.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(capsule.GetComponent<Collider>());
    }

    private static Material GetMaterial(Color color, bool emission = false)
    {
        Color32 c = color;
        int key = c.r | (c.g << 8) | (c.b << 16) | (c.a << 24) | (emission ? 1 << 30 : 0);
        if (materials.TryGetValue(key, out Material cached) && cached != null) return cached;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 4f);
        }
        material.enableInstancing = true;
        materials[key] = material;
        return material;
    }
}
