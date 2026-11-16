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
    private float health;
    private float nextTargetSearch;
    private float nextAttack;
    private bool isPunching;
    private bool isEliminated;
    private float shieldHealth;
    private bool visualMoving;
    private float attackProgress;
    private int attackCycle;

    public bool IsAlive => !isEliminated && health > 0f;
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

    public void AttachExternalVisual(UnityEngine.Object visualPrefab, Vector3 localPosition, Vector3 localScale, RuntimeAnimatorController animatorController = null)
    {
        if (visualPrefab == null)
        {
            return;
        }

        HideBuiltInVisuals();
        visualRig?.SetVisible(false);

        int childCountBeforeInstantiate = transform.childCount;
        GameObject visualInstance;
#if UNITY_EDITOR
        // Unity 6 stores this imported package asset as a Prefab object. The
        // editor API resolves its root GameObject correctly during Play Mode.
        visualInstance = PrefabUtility.InstantiatePrefab(visualPrefab, transform) as GameObject;
#else
        visualInstance = Instantiate(visualPrefab, transform) as GameObject;
#endif

        // Some Unity 6 imported Prefab assets return a non-GameObject wrapper
        // even though the GameObject instance was created successfully.
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
            Debug.LogError("ArenaFighter: Harici karakter modeli oluşturulamadı.");
            return;
        }
        visualInstance.name = "Male Athlate Visual (Test Only)";
        visualInstance.transform.localPosition = localPosition;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = localScale;

        Animator animator = visualInstance.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogWarning("ArenaFighter: Male Athlate prefabında Animator bulunamadı; model Idle animasyonu oynatamayacak.");
            return;
        }

        // The imported package prefab points to Male Young Guy.controller.
        // Override it only on this test visual so the package asset remains untouched.
        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        animator.applyRootMotion = false;
        // No compatible Walk clip exists for this Generic model. Keep the
        // Animator component and controller reference intact for later work,
        // but disable animation on this PLAYER-only test visual so an imported
        // root/armature position curve cannot move the whole model.
        animator.enabled = false;
        visualMotion = visualInstance.GetComponent<VisualMotionDriver>() ?? visualInstance.AddComponent<VisualMotionDriver>();
        // Enable procedural locomotion only for the PLAYER test visual.
        // It never moves this fighter root or changes its physics components.
        visualMotion.enabled = true;
        // Male Athlate is intentionally imported as Generic for this test.
        // An empty Avatar is therefore expected and is not a runtime error.
    }

    private void HideBuiltInVisuals()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = false;
        }

        string[] visualPartNames = { "Head", "Arm", "Leg", "Eye", "Belt", "Boot", "Glove" };
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == bodyRenderer || renderer.transform == transform)
            {
                continue;
            }

            foreach (string partName in visualPartNames)
            {
                if (renderer.gameObject.name.Contains(partName))
                {
                    renderer.enabled = false;
                    break;
                }
            }
        }
    }

    public void Initialize(BattleArenaManager manager, string displayName, Color color)
    {
        arena = manager;
        DisplayName = displayName;
        health = maxHealth;

        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        if (body == null)
        {
            Debug.LogError("ArenaFighter: " + gameObject.name + " nesnesine Rigidbody eklenemedi.");
            enabled = false;
            return;
        }

        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.mass = 1f;
        body.linearDamping = 5f;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.material = CreateMaterial(color);
        }

        CreateCharacterVisuals(color);
        HideBuiltInVisuals();
        visualRig = gameObject.AddComponent<FighterVisualRig>();
        int visualStyle = nextVisualStyle++ % 4;
        visualRig.Build(color, visualStyle);
        CreateNameLabel();
        CreateHealthBar();
        CreateFist(color);
        UpdateHealthBar();
    }

    private void Update()
    {
        if (!IsAlive || arena == null || arena.IsFinished)
        {
            return;
        }

        if (Time.time >= nextTargetSearch || target == null || !target.IsAlive)
        {
            target = arena.FindNearestOpponent(this);
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

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        bool moving = distance > attackRange;
        visualMoving = moving;
        visualMotion?.SetMoving(moving);

        if (moving)
        {
            Vector3 nextPosition = transform.position + transform.forward * moveSpeed * Time.deltaTime;
            nextPosition.x = Mathf.Clamp(nextPosition.x, -arena.ArenaRadius + 0.8f, arena.ArenaRadius - 0.8f);
            nextPosition.z = Mathf.Clamp(nextPosition.z, -arena.ArenaRadius + 0.8f, arena.ArenaRadius - 0.8f);
            body.MovePosition(nextPosition);
        }
        else if (Time.time >= nextAttack && !isPunching)
        {
            nextAttack = Time.time + attackCooldown;
            StartCoroutine(PunchRoutine());
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
        Collider[] hits = Physics.OverlapSphere(fist.position, 1.05f);
        foreach (Collider hit in hits)
        {
            ArenaFighter victim = hit.GetComponentInParent<ArenaFighter>();
            if (victim != null && victim != this && victim.IsAlive)
            {
                victim.TakeDamage(attackDamage, this);
            }
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

    public void TakeDamage(float damage, ArenaFighter attacker)
    {
        if (!IsAlive)
        {
            return;
        }

        float absorbedDamage = Mathf.Min(shieldHealth, damage);
        shieldHealth -= absorbedDamage;
        health = Mathf.Max(0f, health - (damage - absorbedDamage));
        UpdateHealthBar();
        CreateHitEffect();

        if (health <= 0f)
        {
            isEliminated = true;
            arena.NotifyElimination(this, attacker);
            gameObject.SetActive(false);
        }
    }

    private void CreateNameLabel()
    {
        GameObject labelObject = new GameObject("Username");
        labelObject.transform.SetParent(transform);
        labelObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        nameLabel = labelObject.AddComponent<TextMesh>();
        nameLabel.text = DisplayName;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.characterSize = 0.22f;
        nameLabel.fontSize = 48;
        nameLabel.color = Color.white;
        labelObject.AddComponent<BillboardLabel>();
    }

    private void CreateCharacterVisuals(Color color)
    {
        CreateVisualPart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.15f, 0f), new Vector3(0.72f, 0.72f, 0.72f), color);
        CreateVisualPart(PrimitiveType.Capsule, "Left Arm", new Vector3(-0.58f, 0.28f, 0f), new Vector3(0.18f, 0.45f, 0.18f), color, new Vector3(0f, 0f, -18f));
        CreateVisualPart(PrimitiveType.Capsule, "Right Arm", new Vector3(0.58f, 0.28f, 0f), new Vector3(0.18f, 0.45f, 0.18f), color, new Vector3(0f, 0f, 18f));
        CreateVisualPart(PrimitiveType.Capsule, "Left Leg", new Vector3(-0.25f, -0.85f, 0f), new Vector3(0.25f, 0.55f, 0.25f), Color.Lerp(color, Color.black, 0.25f));
        CreateVisualPart(PrimitiveType.Capsule, "Right Leg", new Vector3(0.25f, -0.85f, 0f), new Vector3(0.25f, 0.55f, 0.25f), Color.Lerp(color, Color.black, 0.25f));

        // Simple face: two dark eyes facing the fighter's local forward direction.
        CreateVisualPart(PrimitiveType.Sphere, "Left Eye", new Vector3(-0.14f, 1.22f, 0.31f), new Vector3(0.1f, 0.13f, 0.06f), Color.black);
        CreateVisualPart(PrimitiveType.Sphere, "Right Eye", new Vector3(0.14f, 1.22f, 0.31f), new Vector3(0.1f, 0.13f, 0.06f), Color.black);

        // Belt and boots make the silhouette read as a fighter from the camera.
        CreateVisualPart(PrimitiveType.Cube, "Belt", new Vector3(0f, -0.25f, 0f), new Vector3(0.82f, 0.14f, 0.52f), Color.Lerp(color, Color.black, 0.45f));
        CreateVisualPart(PrimitiveType.Sphere, "Left Boot", new Vector3(-0.25f, -1.3f, 0.12f), new Vector3(0.32f, 0.2f, 0.45f), Color.black);
        CreateVisualPart(PrimitiveType.Sphere, "Right Boot", new Vector3(0.25f, -1.3f, 0.12f), new Vector3(0.32f, 0.2f, 0.45f), Color.black);

        // The left glove stays visible while the right glove is animated during attacks.
        CreateVisualPart(PrimitiveType.Sphere, "Left Glove", new Vector3(-0.67f, 0.2f, 0.12f), new Vector3(0.3f, 0.3f, 0.3f), Color.Lerp(color, Color.white, 0.35f));
    }

    private void CreateVisualPart(PrimitiveType primitiveType, string partName, Vector3 localPosition, Vector3 localScale, Color color, Vector3 localEulerAngles = default)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(transform);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles);
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = CreateMaterial(color);
        Destroy(part.GetComponent<Collider>());
    }

    private void CreateHealthBar()
    {
        GameObject barRoot = new GameObject("Health Bar");
        barRoot.transform.SetParent(transform);
        barRoot.transform.localPosition = new Vector3(0f, 1.35f, 0f);

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
        background.name = "Health Background";
        background.transform.SetParent(barRoot.transform);
        background.transform.localScale = new Vector3(1.2f, 0.12f, 0.06f);
        background.GetComponent<Renderer>().material = CreateMaterial(new Color(0.08f, 0.02f, 0.02f));
        Destroy(background.GetComponent<Collider>());

        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "Health Fill";
        fill.transform.SetParent(barRoot.transform);
        fill.transform.localPosition = new Vector3(-0.6f, 0f, -0.04f);
        fill.transform.localScale = new Vector3(1.2f, 0.09f, 0.07f);
        healthFillRenderer = fill.GetComponent<Renderer>();
        healthFillRenderer.material = CreateMaterial(new Color(0.15f, 1f, 0.25f));
        Destroy(fill.GetComponent<Collider>());
        healthFill = fill.transform;
    }

    private void CreateFist(Color color)
    {
        GameObject fistObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fistObject.name = "Right Boxing Glove";
        fistObject.transform.SetParent(transform);
        fistObject.transform.localPosition = new Vector3(0.62f, 0.3f, 0.18f);
        fistObject.transform.localScale = Vector3.one * 0.3f;
        fistObject.GetComponent<Renderer>().material = CreateMaterial(Color.Lerp(color, Color.white, 0.35f));
        Destroy(fistObject.GetComponent<Collider>());
        fist = fistObject.transform;
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null)
        {
            return;
        }

        float ratio = Mathf.Clamp01(health / maxHealth);
        healthFill.localScale = new Vector3(1.2f * ratio, 0.09f, 0.07f);
        healthFill.localPosition = new Vector3(-0.6f + 0.6f * ratio, 0f, -0.04f);
        if (healthFillRenderer != null)
        {
            healthFillRenderer.material.color = Color.Lerp(Color.red, Color.green, ratio);
        }
    }

    private void CreateHitEffect()
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "Hit Flash";
        effect.transform.position = transform.position + Vector3.up * 0.35f;
        effect.transform.localScale = Vector3.one * 0.25f;
        effect.GetComponent<Renderer>().material = CreateMaterial(new Color(1f, 0.8f, 0.05f));
        Destroy(effect.GetComponent<Collider>());
        Destroy(effect, 0.12f);
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader) { color = color };
    }
}
