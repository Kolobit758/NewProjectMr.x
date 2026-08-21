using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterStats))]
public class KrakenBossAI3D : MonoBehaviour
{
    public enum BossState { Idle, NormalAttack, Burrowing, ReappearAttack, SkySlam, TentacleStorm, Recovery, Retreating, Dead }
    [Header("State Machine")]
    public BossState currentState = BossState.Idle;

    [Header("Prefabs & References")]
    public GameObject hitboxPreviewPrefab;
    public GameObject tentaclePrefab;
    public BossHealthBarUI bossUI;
    public Transform playerTransform;
    private CharacterStats bossStats;

    [Header("Boss Balancing")]
    public float actionCooldown = 3.5f;
    public float attackWarningTime = 1.5f;
    public int tentacleDamage = 35;
    public int bodySlamDamage = 60;

    [Range(1f, 10f)] public float normalAttackRadius = 2.5f;
    [Range(2f, 15f)] public float reappearAttackRadius = 4.5f;
    [Range(3f, 15f)] public float bodySlamRadius = 6.0f;

    [Header("Burrow & Jump Settings")]
    public float burrowDepth = -6f;
    public float transitionSpeed = 3f;
    public float jumpHeight = 25f;

    [Header("🛡️ Boss Recovery / Groggy Settings")]
    public float recoveryDuration = 5f;
    public int damageThresholdToStun = 300;
    private int accumulatedDamage = 0;
    private int lastCheckedHP;

    [Header("🛡️ ลิสต์ตรวจจับผู้เล่นและกองทัพทหาร")]
    public List<CharacterStats> activeTargetsInArena = new List<CharacterStats>();

    public float leashRange = 25f;
    public float returnHomeSpeed = 10f;
    private Vector3 homePosition;
    private float defaultY;
    private bool isExecutingAction = false;
    private bool isActivated = false;

    public GameObject RewardChest;

    // 🟥 ลบคำสั่งดักฟังใน Awake() ที่สายไฟหักออกไปเลยครับเพื่อน!

    void Start()
    {
        homePosition = transform.position;
        defaultY = transform.position.y;

        // 🟢 ดึง Component มาเก็บไว้ในตู้ตัวแปรให้เรียบร้อยก่อนใช้งานชัวร์ 100%
        bossStats = GetComponent<CharacterStats>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (bossStats != null)
        {
            bossStats.InitializeStats(1000, 1000);
            int[] stats = bossStats.GetPrivateData();
            lastCheckedHP = stats[1];

            // 🟢 [FIX SUCCESS]: แอดดักฟังอีเวนต์การเปลี่ยนเลือดตรงนี้อย่างปลอดภัย ไม่หลุดแน่นอน!
            bossStats.OnHPChanged += Die;
        }
    }

    // 🔴 ถอดสายไฟทิ้งเพื่อความปลอดภัยของแรมคอมพิวเตอร์ตอนที่ตัวบอสถูกทำลายออกจากแมพ
    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.OnHPChanged -= Die;
        }
    }

    void Update()
    {
        // ถ้าบอสหนี กลับบ้าน นอนสลบ หรือตายแล้ว ห้ามคำนวณดาเมจซ้อนเด็ดขาด UI จะได้ไม่เพี้ยน!
        if (!isActivated || currentState == BossState.Retreating || currentState == BossState.Recovery || currentState == BossState.Dead) return;

        CheckDamageForRecovery();
    }

    private void CheckDamageForRecovery()
    {
        if (bossStats == null) return;

        int[] stats = bossStats.GetPrivateData();
        int currentHP = stats[1];

        if (currentHP < lastCheckedHP)
        {
            int damageTaken = lastCheckedHP - currentHP;
            accumulatedDamage += damageTaken;
            lastCheckedHP = currentHP;

            if (accumulatedDamage >= damageThresholdToStun && !isExecutingAction)
            {
                accumulatedDamage = 0;
                StopAllCoroutines();
                isExecutingAction = false;
                StartCoroutine(BossRecoveryRoutine());
            }
        }
    }

    IEnumerator BossRecoveryRoutine()
    {
        currentState = BossState.Recovery;
        Debug.Log("🐙 บอสคราเคนหมดสภาพล้มนอนนิ่ง!! รีบสั่งทหารรุมยำด่วน!!");

        Vector3 knockDownPos = new Vector3(transform.position.x, defaultY - 1f, transform.position.z);
        transform.position = knockDownPos;
        SetBossColor(Color.gray);

        yield return new WaitForSeconds(recoveryDuration);

        // ดักทาง: ถ้าเกิดนอนสลบอยู่แล้วโดนทหารหวดจนตายกลางคัน ห้ามสั่งฟื้นตัวขึ้นมาตี!
        if (currentState == BossState.Dead) yield break;

        Debug.Log("🐙 บอสสะบัดหัวฟื้นตัวกลับมาแล้วคราบ!!");
        transform.position = new Vector3(transform.position.x, defaultY, transform.position.z);
        SetBossColor(Color.white);

        int[] stats = bossStats.GetPrivateData();
        lastCheckedHP = stats[1];

        currentState = BossState.Idle;
        StartCoroutine(BossBrainLoop());
    }

    public void ActivateBoss()
    {
        if (currentState == BossState.Retreating || currentState == BossState.Recovery || currentState == BossState.Dead) return;
        if (!isActivated)
        {
            isActivated = true;
            if (bossUI != null) bossUI.ShowBossUI("🐙 KRAKEN, THE OCEAN DOOMSDAY");
            StartCoroutine(BossBrainLoop());
        }
    }

    public void ForceBossRetreat()
    {
        if (!isActivated || currentState == BossState.Retreating || currentState == BossState.Dead) return;

        isActivated = false;
        isExecutingAction = false;
        accumulatedDamage = 0;
        activeTargetsInArena.Clear();

        if (bossUI != null) bossUI.HideBossUI();

        StopAllCoroutines();
        StartCoroutine(ReturnHomeAndResetRoutine());
    }

    IEnumerator BossBrainLoop()
    {
        yield return new WaitForSeconds(1.5f);

        while (currentState != BossState.Recovery && currentState != BossState.Dead)
        {
            if (playerTransform == null || currentState == BossState.Retreating) yield return null;

            if (!isExecutingAction)
            {
                int randomAttack = UnityEngine.Random.Range(1, 5);
                switch (randomAttack)
                {
                    case 1: StartCoroutine(TacticalTentacleSmashRoutine()); break;
                    case 2: StartCoroutine(SmoothBurrowTripleSmashRoutine()); break;
                    case 3: StartCoroutine(TentacleStormRoutine()); break;
                    case 4: StartCoroutine(MeteorBodySlamRoutine()); break;
                }
            }
            yield return new WaitForSeconds(actionCooldown);
        }
    }

    IEnumerator TentacleStormRoutine()
    {
        isExecutingAction = true;
        currentState = BossState.TentacleStorm;
        int burstCount = UnityEngine.Random.Range(6, 9);
        for (int i = 0; i < burstCount; i++)
        {
            if (currentState == BossState.Recovery || currentState == BossState.Dead) yield break;
            float angle = i * (360f / burstCount);
            float distance = UnityEngine.Random.Range(3f, 12f);
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * distance;
            Vector3 targetPos = transform.position + spawnOffset;

            SpawnIndicator3D(targetPos, normalAttackRadius, (pos) => ExecuteTentacleSmash(pos, normalAttackRadius));
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(attackWarningTime + 0.5f);
        isExecutingAction = false;
        currentState = BossState.Idle;
    }

    IEnumerator MeteorBodySlamRoutine()
    {
        isExecutingAction = true;
        currentState = BossState.SkySlam;
        Vector3 skyPos = new Vector3(transform.position.x, defaultY + jumpHeight, transform.position.z);
        while (Vector3.Distance(transform.position, skyPos) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, skyPos, (transitionSpeed * 4f) * Time.deltaTime);
            yield return null;
        }
        SetBossVisuals(false);
        yield return new WaitForSeconds(0.8f);

        Vector3 slamTargetPos = GetDensestTroopPosition();
        SpawnIndicator3D(slamTargetPos, bodySlamRadius, (pos) =>
        {
            if (currentState == BossState.Recovery || currentState == BossState.Dead) return;
            Collider[] hitColliders = Physics.OverlapSphere(pos, bodySlamRadius);
            foreach (var hit in hitColliders)
            {
                if (hit.TryGetComponent<CharacterStats>(out CharacterStats targetStats))
                {
                    targetStats.TakeDamage(bodySlamDamage, pos);
                }
            }
        });

        yield return new WaitForSeconds(attackWarningTime);
        transform.position = new Vector3(slamTargetPos.x, defaultY + jumpHeight, slamTargetPos.z);
        SetBossVisuals(true);

        Vector3 landPos = new Vector3(slamTargetPos.x, defaultY, slamTargetPos.z);
        while (Vector3.Distance(transform.position, landPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, landPos, (transitionSpeed * 6f) * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        isExecutingAction = false;
        currentState = BossState.Idle;
    }

    private Vector3 GetDensestTroopPosition()
    {
        if (activeTargetsInArena.Count == 0) return playerTransform.position;
        Vector3 bestTarget = playerTransform.position;
        int maxNearbyUnits = -1;
        foreach (var target in activeTargetsInArena)
        {
            if (target == null || !target.CompareTag("Unit")) continue;
            int nearbyCount = 0;
            foreach (var other in activeTargetsInArena)
            {
                if (other != null && other.CompareTag("Unit") && Vector3.Distance(target.transform.position, other.transform.position) < 5f)
                {
                    nearbyCount++;
                }
            }
            if (nearbyCount > maxNearbyUnits)
            {
                maxNearbyUnits = nearbyCount;
                bestTarget = target.transform.position;
            }
        }
        return bestTarget;
    }

    IEnumerator TacticalTentacleSmashRoutine()
    {
        isExecutingAction = true;
        currentState = BossState.NormalAttack;
        Vector3 targetPosition = playerTransform.position;
        if (UnityEngine.Random.value > 0.5f && activeTargetsInArena.Count > 0)
        {
            var randomTarget = activeTargetsInArena[UnityEngine.Random.Range(0, activeTargetsInArena.Count)];
            if (randomTarget != null) targetPosition = randomTarget.transform.position;
        }
        SpawnIndicator3D(targetPosition, normalAttackRadius, (pos) => ExecuteTentacleSmash(pos, normalAttackRadius));
        yield return new WaitForSeconds(attackWarningTime + 0.5f);
        isExecutingAction = false;
        currentState = BossState.Idle;
    }

    IEnumerator SmoothBurrowTripleSmashRoutine()
    {
        isExecutingAction = true;
        currentState = BossState.Burrowing;
        Vector3 buriedPos = new Vector3(transform.position.x, defaultY + burrowDepth, transform.position.z);
        while (Vector3.Distance(transform.position, buriedPos) > 0.1f && currentState == BossState.Burrowing)
        {
            transform.position = Vector3.MoveTowards(transform.position, buriedPos, transitionSpeed * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(1f);

        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0f, UnityEngine.Random.Range(-5f, 5f));
        Vector3 reappearFloorPos = playerTransform.position + randomOffset;
        transform.position = new Vector3(reappearFloorPos.x, defaultY + burrowDepth, reappearFloorPos.z);

        currentState = BossState.ReappearAttack;
        Vector3 finalReappearPos = new Vector3(transform.position.x, defaultY, transform.position.z);
        while (Vector3.Distance(transform.position, finalReappearPos) > 0.1f && currentState == BossState.ReappearAttack)
        {
            transform.position = Vector3.MoveTowards(transform.position, finalReappearPos, (transitionSpeed * 1.5f) * Time.deltaTime);
            yield return null;
        }

        Vector3 bossPos = transform.position;
        SpawnIndicator3D(bossPos, reappearAttackRadius, (pos) => ExecuteTentacleSmash(pos, reappearAttackRadius));
        Vector3 leftFlank = playerTransform.position + (Vector3.left * 3f);
        Vector3 rightFlank = playerTransform.position + (Vector3.right * 3f);
        SpawnIndicator3D(leftFlank, normalAttackRadius, (pos) => ExecuteTentacleSmash(pos, normalAttackRadius));
        SpawnIndicator3D(rightFlank, normalAttackRadius, (pos) => ExecuteTentacleSmash(pos, normalAttackRadius));

        yield return new WaitForSeconds(attackWarningTime + 0.5f);
        isExecutingAction = false;
        currentState = BossState.Idle;
    }

    IEnumerator ReturnHomeAndResetRoutine()
    {
        currentState = BossState.Retreating;
        SetBossVisuals(true);
        Vector3 targetHomePos = new Vector3(homePosition.x, defaultY, homePosition.z);
        while (Vector3.Distance(transform.position, targetHomePos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetHomePos, returnHomeSpeed * Time.deltaTime);
            yield return null;
        }
        if (bossStats != null)
        {
            int[] privateData = bossStats.GetPrivateData();
            int maxHP = privateData[0];
            bossStats.InitializeStats(maxHP, maxHP);
            lastCheckedHP = maxHP;
        }
        yield return new WaitForSeconds(1f);
        currentState = BossState.Idle;
        StartCoroutine(BossBrainLoop());
    }

    private void SpawnIndicator3D(Vector3 pos, float radius, Action<Vector3> timeUpCallback)
    {
        if (hitboxPreviewPrefab == null || currentState == BossState.Retreating || currentState == BossState.Recovery || currentState == BossState.Dead) return;
        Vector3 CentralizedPos = new Vector3(pos.x, playerTransform.position.y + 0.05f, pos.z);
        GameObject ind = Instantiate(hitboxPreviewPrefab, CentralizedPos, Quaternion.identity);
        if (ind.TryGetComponent<AttackIndicator3D>(out AttackIndicator3D indicator))
        {
            indicator.SetupIndicator3D(attackWarningTime, radius, timeUpCallback);
        }
    }

    private void ExecuteTentacleSmash(Vector3 smashPosition, float radius)
    {
        if (currentState == BossState.Retreating || currentState == BossState.Recovery || currentState == BossState.Dead) return;
        if (tentaclePrefab != null)
        {
            Vector3 spawnPos = smashPosition + (Vector3.down * 3f);
            GameObject tentacle = Instantiate(tentaclePrefab, spawnPos, Quaternion.identity);
            if (tentacle.TryGetComponent<TentacleVisual3D>(out TentacleVisual3D visual))
            {
                visual.SetupTentacleSize(radius);
            }
        }

        Collider[] hitColliders = Physics.OverlapSphere(smashPosition, radius);
        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<CharacterStats>(out CharacterStats targetStats))
            {
                targetStats.TakeDamage(tentacleDamage, smashPosition);
            }
        }
    }

    private void SetBossVisuals(bool visible)
    {
        if (TryGetComponent<MeshRenderer>(out MeshRenderer mr)) mr.enabled = visible;
        var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in skinnedRenderers) r.enabled = visible;
    }

    private void SetBossColor(Color targetColor)
    {
        if (TryGetComponent<Renderer>(out Renderer r)) r.material.color = targetColor;
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers) rend.material.color = targetColor;
    }

    public void Die()
    {
        if (bossStats != null && bossStats.currentHP <= 0 && currentState != BossState.Dead)
        {
            currentState = BossState.Dead;
            Debug.Log("🐙 คราเคนตายอย่างสมศักดิ์ศรี! เสกกล่องสมบัติรางวัล!");

            StopAllCoroutines();

            if (bossUI != null) bossUI.HideBossUI();

            if (RewardChest != null)
            {
                // 🟢 [FIX SUCCESS]: บังคับล็อกพิกัดเกิดของกล่องสมบัติ
                // โดยใช้พิกัด X, Z ปัจจุบันของบอส แต่ใช้ค่าความสูง (แกน Y) จากจุดเกิดปกติ (defaultY) 
                // เพื่อดักทางต่อให้บอสตายตอนมุดดิน กล่องก็จะเด้งขึ้นมาบนพื้นเป๊ะ ๆ ไม่จมดินแน่นอนครับ!
                Vector3 spawnPosition = new Vector3(transform.position.x, defaultY + 0.5f, transform.position.z);

                GameObject ChessBox = Instantiate(RewardChest, spawnPosition, Quaternion.identity);
                Debug.Log($"💎 เสกกล่องสมบัติที่พิกัดผิวโลกสำเร็จ: {spawnPosition}");
            }

            Destroy(gameObject);
        }
    }
}