using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
public class ArenaFighter : MonoBehaviour
{
    private static int nextVisualStyle;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 16f;
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 0.9f;

    private BattleArenaManager arena;
    private ArenaFighter target;
    private Rigidbody body;
    private Renderer bodyRenderer;
    private Transform fist;
    private Transform healthFill;
    private Renderer healthFillRenderer;
    private TextMesh nameLabel;
    private VisualMotionDriver visualMotion;
    private FighterVisualRig visualRig;
    private ImportedFighterAnimationDriver contactAnimation;
    private KayKitFighterDriver kayKitAnimation;
    private float health;
    private float nextTargetSearch;
    private float nextAttack;
    private bool isPunching;
    private bool isEliminated;
    private float shieldHealth;
    private bool visualMoving;
    private float attackProgress;
    private int attackCycle;
    private Color fighterColor = Color.red;
    private readonly System.Collections.Generic.List<Material> ownedMaterials = new System.Collections.Generic.List<Material>();

    ArenaTacticalBrain tacticalBrain;
    public ArenaFighterRole Role { get; private set; }
    public bool IsBot { get; set; }
    public float LastDamageTime { get; private set; }
    public ArenaTacticalBrain TacticalBrain=>tacticalBrain;
    public void ConfigureRole(ArenaGiftRoster.Tier tier)
    {
        Role=tier.role;maxHealth=tier.health;health=maxHealth;attackDamage=tier.damage;moveSpeed=tier.speed;attackRange=tier.range;attackCooldown=tier.cooldown;shieldHealth=tier.shield;
        var loadout=gameObject.AddComponent<ArenaClassEquipment>();loadout.Build(this);UpdateHealthBar();
    }
    public bool TryRangedHit(ArenaFighter victim)
    {
        if(!IsAlive||arena.IsFinished||victim==null||!victim.IsAlive||victim.IsEntering)return false;
        float before=victim.Health;victim.TakeDamage(attackDamage*PowerUps.DamageMultiplier+PowerUps.FireDamage,this);PowerUps.OnContact(victim,before-victim.Health);return true;
    }
    public ArenaPowerUps PowerUps { get; private set; }
    public BattleArenaManager Arena => arena;
    public void EliminateFromMeteor(ArenaFighter attacker)
    {
        if(!IsAlive||this==attacker)return;
        float lost=health;arena.RecordRoundDamage(attacker,lost,this);health=0;shieldHealth=0;isEliminated=true;UpdateHealthBar();

        ArenaBroadcastHUD.RecordElimination(this,attacker);
        arena.NotifyElimination(this,attacker);
        if(kayKitAnimation!=null&&gameObject.activeInHierarchy)
        {kayKitAnimation.React(true);StartCoroutine(RemoveAfterDeath());}
        else gameObject.SetActive(false);
    }
    public int VisualIndex { get; private set; }
    public string ViewerId { get; private set; }
    public string AvatarUrl { get; private set; }
    public void SetViewerProfile(string viewerId,string avatarUrl)
    {ViewerId=viewerId;AvatarUrl=avatarUrl;}
    public void Heal(float amount){if(!IsAlive)return;health=Mathf.Min(maxHealth,health+Mathf.Max(0,amount));UpdateHealthBar();}
    public void SetNameplateVisible(bool visible){if(nameLabel!=null)nameLabel.GetComponent<Renderer>().enabled=visible;}
    public bool IsAlive => !isEliminated && health > 0f;
    public bool IsEntering { get; set; }
    public float Health => health;
    public float MaxHealth => maxHealth;
    public float ShieldHealth => shieldHealth;
    public string DisplayName { get; private set; }
    public bool IsMoving => visualMoving;
    public bool IsPunching => isPunching;
    public bool IsHeadbutting => isPunching && attackCycle % 3 == 0;
    public float AttackProgress => attackProgress;
    public int AttackCycle => attackCycle;

    public void GiftIncreaseMaxHealth(float amount)
    {
        if (!IsAlive) return;
        amount = Mathf.Max(0f, amount);
        maxHealth += amount;
        health += amount;
        UpdateHealthBar();
    }

    public void GiftIncreaseAttack(float amount)
    {
        attackDamage += Mathf.Max(0f, amount);
    }

    public void GiftIncreaseSpeed(float amount)
    {
        moveSpeed += Mathf.Max(0f, amount);
    }

    public void GiftShield(float amount)
    {
        if (!IsAlive) return;
        shieldHealth += Mathf.Max(0f, amount);
    }

    public void AttachExternalVisual(Object visualPrefab, Vector3 localPosition, Vector3 localScale, RuntimeAnimatorController animatorController = null)
    {
        if (visualPrefab == null) return;
        var content = ArenaContent.Load();
        if (content != null && visualPrefab is GameObject kaykit && System.Array.IndexOf(content.fighters, kaykit) >= 0)
        {
            VisualIndex=System.Array.IndexOf(content.fighters,kaykit);
            var model = Instantiate(kaykit, transform);
            model.name = "KayKit Fighter Visual";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            Bounds bounds = new Bounds(); bool found = false;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            { if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds); }
            if (found && bounds.size.y > .01f)
            {
                float factor = 2.15f * transform.lossyScale.y / bounds.size.y;
                model.transform.localScale *= factor;
                model.transform.localPosition = new Vector3(0f, -1f - (bounds.min.y - transform.position.y) / transform.lossyScale.y * factor, 0f);
            }
            DisableVisualPhysics(model);
            HideBuiltInVisuals();
            visualRig?.SetVisible(false);
            if (fist != null) fist.GetComponent<Renderer>().enabled = false;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            var modelAnimator = model.GetComponent<Animator>();
            modelAnimator.runtimeAnimatorController = content.controller;
            modelAnimator.applyRootMotion = false;
            kayKitAnimation = model.AddComponent<KayKitFighterDriver>();
            attackRange = 1.2f;
            return;
        }

        int childCountBeforeInstantiate = transform.childCount;
        GameObject visualInstance = null;

#if UNITY_EDITOR
        visualInstance = PrefabUtility.InstantiatePrefab(visualPrefab, transform) as GameObject;
#else
        visualInstance = Instantiate(visualPrefab, transform) as GameObject;
#endif

        if (visualInstance == null && visualPrefab is GameObject sourceGameObject)
        {
            visualInstance = Instantiate(sourceGameObject, transform);
        }

#if UNITY_EDITOR
        if (visualInstance == null)
        {
            string fallbackAssetPath = AssetDatabase.GetAssetPath(visualPrefab);
            GameObject resolvedModel = string.IsNullOrEmpty(fallbackAssetPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(fallbackAssetPath);
            if (resolvedModel != null) visualInstance = Instantiate(resolvedModel, transform);
        }
#endif

        if (visualInstance == null && transform.childCount > childCountBeforeInstantiate)
        {
            for (int i = childCountBeforeInstantiate; i < transform.childCount; i++)
            {
                Animator childAnimator = transform.GetChild(i).GetComponentInChildren<Animator>(true);
                if (childAnimator != null)
                {
                    visualInstance = transform.GetChild(i).gameObject;
                    break;
                }
            }
        }

        if (visualInstance == null)
        {
            Debug.LogError("ArenaFighter: Harici karakter modeli oluşturulamadı. Atanan asset: " + visualPrefab.name);
            return;
        }

        bool isNormalgonVisual = visualPrefab.name.Contains("Character_MedievalBasic");
        bool isImportedAnimationVisual = visualPrefab.name.Contains("EEJANAIbot") || visualPrefab.name.Contains("Male Athlate") || visualPrefab.name.Contains("MaleAthlate");
#if UNITY_EDITOR
        string visualAssetPath = AssetDatabase.GetAssetPath(visualPrefab);
        isImportedAnimationVisual |= visualAssetPath.Contains("Assets/EEJANAI_Team/") || visualAssetPath.Contains("CharCrafter");
#endif
        visualInstance.name = isNormalgonVisual ? "Normalgon Player Visual (Test Only)" :
            (visualPrefab.name.Contains("Male") ? "Male Athlate Visual (PLAYER)" : "EEJANAI Fighter Visual (PLAYER Only)");
        visualInstance.transform.localPosition = localPosition;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = localScale;
        visualInstance.SetActive(true);

        if (isNormalgonVisual)
        {
            var assembler = visualInstance.GetComponent<NormalgonRuntimeAssembler>() ?? visualInstance.AddComponent<NormalgonRuntimeAssembler>();
            assembler.Assemble();
            var motionDriver = visualInstance.GetComponent<NormalgonMotionDriver>() ?? visualInstance.AddComponent<NormalgonMotionDriver>();
            motionDriver.enabled = true;
        }

        Renderer[] importedRenderers = visualInstance.GetComponentsInChildren<Renderer>(true);
        if (importedRenderers.Length == 0)
        {
            Debug.LogError("ArenaFighter: Atanan model render edilebilir bir mesh icermiyor: " + visualPrefab.name, this);
            Destroy(visualInstance);
            return;
        }

        DisableVisualPhysics(visualInstance);
        HideBuiltInVisuals();
        if (fist != null)
        {
            Renderer legacyFistRenderer = fist.GetComponent<Renderer>();
            if (legacyFistRenderer != null) legacyFistRenderer.enabled = false;
        }
        visualRig?.SetVisible(false);
        SetProceduralRigVisible(false);

        if (!isNormalgonVisual && !isImportedAnimationVisual) HideExternalVisualRenderers(visualInstance);
        else ShowExternalVisualRenderers(visualInstance);

        Animator animator = visualInstance.GetComponentInChildren<Animator>(true);
        if (animator == null && isImportedAnimationVisual) animator = visualInstance.AddComponent<Animator>();
        if (animator == null)
        {
            if (!isNormalgonVisual && !isImportedAnimationVisual)
                Debug.LogWarning("ArenaFighter: Harici prefabda Animator bulunamadı; model animasyon oynatamayacak.");
            return;
        }
        if (animatorController != null) animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false;
        if (isNormalgonVisual && animator.runtimeAnimatorController == null) animator.enabled = false;
        else animator.enabled = true;

        if (isImportedAnimationVisual)
        {
            var animationDriver = visualInstance.GetComponent<ImportedFighterAnimationDriver>() ?? visualInstance.AddComponent<ImportedFighterAnimationDriver>();
            animationDriver.enabled = true;
            contactAnimation = animationDriver;
            var dressing = visualInstance.GetComponent<CartoonFighterDressing>() ?? visualInstance.AddComponent<CartoonFighterDressing>();
            dressing.Apply(fighterColor, gameObject.name == "PLAYER");
        }
        else if (!isNormalgonVisual)
        {
            visualMotion = visualInstance.GetComponent<VisualMotionDriver>() ?? visualInstance.AddComponent<VisualMotionDriver>();
            visualMotion.enabled = true;
        }
    }

    private void HideBuiltInVisuals()
    {
        if (bodyRenderer != null) bodyRenderer.enabled = false;
        Transform cartoonRig = transform.Find("Procedural Fighter Rig");
        if (cartoonRig != null) cartoonRig.gameObject.SetActive(false);
        string[] visualPartNames = { "Head", "Arm", "Leg", "Eye", "Belt", "Boot", "Glove" };
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == bodyRenderer || renderer.transform == transform) continue;
            foreach (string partName in visualPartNames)
                if (renderer.gameObject.name.Contains(partName))
                { renderer.enabled = false; break; }
        }
    }

    private static void HideExternalVisualRenderers(GameObject visualInstance)
    {
        foreach (Renderer r in visualInstance.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
    }

    private static void ShowExternalVisualRenderers(GameObject visualInstance)
    {
        foreach (Renderer r in visualInstance.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
    }

    private void SetProceduralRigVisible(bool isVisible)
    {
        Transform proceduralRig = transform.Find("Procedural Fighter Rig");
        if (proceduralRig != null) proceduralRig.gameObject.SetActive(isVisible);
    }

    private static void DisableVisualPhysics(GameObject visualInstance)
    {
        foreach (Collider c in visualInstance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Rigidbody rb in visualInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }

    public void Initialize(BattleArenaManager manager, string displayName, Color color)
    {
        arena = manager;
        tacticalBrain=gameObject.AddComponent<ArenaTacticalBrain>();
        PowerUps=GetComponent<ArenaPowerUps>();if(PowerUps==null)PowerUps=gameObject.AddComponent<ArenaPowerUps>();
        DisplayName = displayName;
        ViewerId=displayName.TrimStart('@');
        health = maxHealth;
        fighterColor = color;
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.mass = 1f;
        body.linearDamping = 5f;
        if (ArenaContent.Load() == null)
        {
        visualRig = gameObject.AddComponent<FighterVisualRig>();
        int visualStyle = (nextVisualStyle++ + Random.Range(0, 4)) % 4;
        visualRig.Build(color, visualStyle);
        visualRig.SetVisible(true);
        SetProceduralRigVisible(true);
        }
        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null) bodyRenderer.material = CreateMaterial(color);
        if (ArenaContent.Load() == null) CreateCharacterVisuals(color);
        CreateNameLabel();
        CreateHealthBar();
        CreateFist(color);
        UpdateHealthBar();
    }

    private void FixedUpdate()
    {
        if (!IsAlive || IsEntering || arena == null || arena.IsFinished) return;
        if (Time.time >= nextTargetSearch || target == null || !target.IsAlive)
        {
            target = tacticalBrain.SelectTarget();
            nextTargetSearch = Time.time + 0.35f;
        }
        if (target == null)
        {
            visualMoving = false;
            visualMotion?.SetMoving(false);
            return;
        }
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (direction.sqrMagnitude > 0.01f) body.MoveRotation(Quaternion.LookRotation(direction.normalized));
        bool attack;
        Vector3 steering=tacticalBrain.Decide(target,attackRange,out attack);
        bool moving=steering.sqrMagnitude>.015f;
        visualMoving=moving;visualMotion?.SetMoving(moving);
        if(moving)
        {
            Vector3 nextPos=body.position+steering*moveSpeed*PowerUps.MovementMultiplier*Time.fixedDeltaTime;
            nextPos.x=Mathf.Clamp(nextPos.x,-arena.ArenaRadius+.8f,arena.ArenaRadius-.8f);
            nextPos.z=Mathf.Clamp(nextPos.z,-arena.ArenaRadius+.8f,arena.ArenaRadius-.8f);
            body.MovePosition(nextPos);
        }
        if(attack&&Time.time>=nextAttack&&!isPunching)
        {
            nextAttack=Time.time+attackCooldown/PowerUps.AttackRate;
            if(Role==ArenaFighterRole.Archer)
            {
                attackCycle++;ArenaArrowPool.Fire(this,target);
            }
            else StartCoroutine(PunchRoutine());
        }
    }

    private IEnumerator PunchRoutine()
    {
        isPunching = true;
        attackCycle++;
        attackProgress = 0f;
        Vector3 start = new Vector3(0.62f, 0.3f, 0.18f);
        Vector3 reach = new Vector3(0.62f, 0.3f, 0.98f);
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            attackProgress = Mathf.Clamp01(elapsed / 0.24f);
            fist.localPosition = Vector3.Lerp(start, reach, elapsed / 0.12f);
            yield return null;
        }
        fist.localPosition = reach;
        if ((contactAnimation == null || !contactAnimation.UsesLimbContacts) && (kayKitAnimation == null || !kayKitAnimation.UsesLimbContacts))
        foreach (Collider hit in Physics.OverlapSphere(fist.position, 1.05f))
        {
            var victim = hit.GetComponentInParent<ArenaFighter>();
            if (victim != null && victim != this && victim.IsAlive) victim.TakeDamage(attackDamage, this);
        }
        elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            attackProgress = 0.5f + Mathf.Clamp01(elapsed / 0.24f);
            fist.localPosition = Vector3.Lerp(reach, start, elapsed / 0.12f);
            yield return null;
        }
        fist.localPosition = start;
        attackProgress = 0f;
        isPunching = false;
    }

    // Called only by the animated limb contact window. Damage remains owned
    // by the combat component, including gift attack upgrades and shields.
    public bool ApplyLimbContactDamage(ArenaFighter victim)
    {
        if (!IsAlive || arena == null || arena.IsFinished || victim == null || victim == this || !victim.IsAlive || victim.IsEntering) return false;
        float before=victim.Health;
        float bonus=PowerUps.FireDamage;
        victim.TakeDamage(attackDamage*PowerUps.DamageMultiplier+bonus, this);
        PowerUps.OnContact(victim,before-victim.Health);
        return true;
    }

    public void TakeDamage(float damage, ArenaFighter attacker)
    {
        if (!IsAlive || IsEntering) return;
        LastDamageTime=Time.time;
        if(Role==ArenaFighterRole.Guardian&&attacker!=null)
        {
            var facing=attacker.transform.position-transform.position;facing.y=0;
            if(Vector3.Dot(transform.forward,facing.normalized)>.25f)
            {
                damage*=arena.MatchSettings!=null?arena.MatchSettings.guardianFrontalDamageMultiplier:.65f;
                ArenaAudioEffects.PowerHit(transform.position+transform.forward*.45f+Vector3.up*.5f,ArenaPower.HolyShield);
            }
        }
        damage=Mathf.Max(0,damage)*PowerUps.DamageReduction;
        float absorbed = Mathf.Min(shieldHealth, damage);
        shieldHealth -= absorbed;
        float previousHealth = health;
        health = Mathf.Max(0f, health - (damage - absorbed));

        arena.RecordRoundDamage(attacker,previousHealth-health,this);
        UpdateHealthBar();
        CreateHitEffect();
        if (health > 0f) kayKitAnimation?.React(false);
        if (health <= 0f)
        {
            isEliminated = true;
            ArenaBroadcastHUD.RecordElimination(this, attacker);
            arena.NotifyElimination(this, attacker);
            if (kayKitAnimation != null)
            { kayKitAnimation.React(true); StartCoroutine(RemoveAfterDeath()); }
            else gameObject.SetActive(false);
        }
    }

    private IEnumerator RemoveAfterDeath()
    {
        if(nameLabel!=null)nameLabel.gameObject.SetActive(false);
        if(healthFill!=null)healthFill.parent.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.25f);
        gameObject.SetActive(false);
    }

    private void CreateNameLabel()
    {
        GameObject go = new GameObject("Username");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 1.48f, 0f);
        nameLabel = go.AddComponent<TextMesh>();
        nameLabel.text = ArenaUiText.Short(DisplayName,18);
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.characterSize = 0.075f;
        nameLabel.fontSize = 28;
        nameLabel.color = Color.white;
        go.AddComponent<BillboardLabel>();
    }

    private void CreateCharacterVisuals(Color color)
    {
        CreateVisualPart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.15f, 0f), new Vector3(0.35f, 0.35f, 0.35f), color);
        CreateVisualPart(PrimitiveType.Capsule, "Left Arm", new Vector3(-0.58f, 0.28f, 0f), new Vector3(0.18f, 0.45f, 0.18f), color, new Vector3(0f, 0f, -18f));
        CreateVisualPart(PrimitiveType.Capsule, "Right Arm", new Vector3(0.58f, 0.28f, 0f), new Vector3(0.18f, 0.45f, 0.18f), color, new Vector3(0f, 0f, 18f));
        CreateVisualPart(PrimitiveType.Capsule, "Left Leg", new Vector3(-0.25f, -0.85f, 0f), new Vector3(0.25f, 0.55f, 0.25f), Color.Lerp(color, Color.black, 0.25f));
        CreateVisualPart(PrimitiveType.Capsule, "Right Leg", new Vector3(0.25f, -0.85f, 0f), new Vector3(0.25f, 0.55f, 0.25f), Color.Lerp(color, Color.black, 0.25f));
        CreateVisualPart(PrimitiveType.Sphere, "Left Eye", new Vector3(-0.14f, 1.22f, 0.31f), new Vector3(0.08f, 0.11f, 0.04f), Color.black);
        CreateVisualPart(PrimitiveType.Sphere, "Right Eye", new Vector3(0.14f, 1.22f, 0.31f), new Vector3(0.08f, 0.11f, 0.04f), Color.black);
        CreateVisualPart(PrimitiveType.Cube, "Belt", new Vector3(0f, -0.25f, 0f), new Vector3(0.82f, 0.14f, 0.52f), Color.Lerp(color, Color.black, 0.45f));
        CreateVisualPart(PrimitiveType.Sphere, "Left Boot", new Vector3(-0.25f, -1.3f, 0.12f), new Vector3(0.28f, 0.18f, 0.42f), Color.black);
        CreateVisualPart(PrimitiveType.Sphere, "Right Boot", new Vector3(0.25f, -1.3f, 0.12f), new Vector3(0.28f, 0.18f, 0.42f), Color.black);
        CreateVisualPart(PrimitiveType.Sphere, "Left Glove", new Vector3(-0.67f, 0.2f, 0.12f), new Vector3(0.28f, 0.28f, 0.28f), Color.Lerp(color, Color.white, 0.35f));
    }

    private void CreateVisualPart(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(transform);
        part.transform.localPosition = pos;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = CreateMaterial(color);
        Destroy(part.GetComponent<Collider>());
    }

    private void CreateHealthBar()
    {
        GameObject root = new GameObject("Health Bar");
        root.transform.SetParent(transform);
        root.transform.localPosition = new Vector3(0f, 1.28f, 0f);
        root.AddComponent<BillboardLabel>();
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "Health Background";
        bg.transform.SetParent(root.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(0.85f, 0.08f, 0.02f);
        bg.GetComponent<Renderer>().material = CreateMaterial(new Color(0.08f, 0.02f, 0.02f));
        Destroy(bg.GetComponent<Collider>());
        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "Health Fill";
        fill.transform.SetParent(root.transform);
        fill.transform.localPosition = new Vector3(-0.425f, 0f, -0.015f);
        fill.transform.localScale = new Vector3(0.85f, 0.065f, 0.025f);
        healthFillRenderer = fill.GetComponent<Renderer>();
        healthFillRenderer.material = CreateMaterial(new Color(0.15f, 1f, 0.25f));
        Destroy(fill.GetComponent<Collider>());
        healthFill = fill.transform;
    }

    private void CreateFist(Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Right Boxing Glove";
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0.62f, 0.3f, 0.18f);
        go.transform.localScale = Vector3.one * 0.3f;
        go.GetComponent<Renderer>().material = CreateMaterial(Color.Lerp(color, Color.white, 0.35f));
        Destroy(go.GetComponent<Collider>());
        fist = go.transform;
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null) return;
        float ratio = Mathf.Clamp01(health / maxHealth);
        healthFill.localScale = new Vector3(0.85f * ratio, 0.065f, 0.025f);
        healthFill.localPosition = new Vector3(-0.425f + 0.425f * ratio, 0f, -0.015f);
        if (healthFillRenderer != null) healthFillRenderer.material.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    private void CreateHitEffect()
    {
        if (ArenaContent.Load() != null) return;
        GameObject e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        e.name = "Hit Flash";
        e.transform.position = transform.position + Vector3.up * 0.35f;
        e.transform.localScale = Vector3.one * 0.25f;
        e.GetComponent<Renderer>().material = CreateMaterial(new Color(1f, 0.8f, 0.05f));
        Destroy(e.GetComponent<Collider>());
        Destroy(e, 0.12f);
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        ownedMaterials.Add(mat);
        return mat;
    }
    private void OnDestroy()
    { foreach (var material in ownedMaterials) if (material != null) Destroy(material); }
}
