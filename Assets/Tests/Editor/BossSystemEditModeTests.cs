using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Boss 系統的快速 Edit Mode 單元測試。
/// 不等待 3 分鐘時間軸，也不依賴目前開啟的 Scene 內容。
/// </summary>
public sealed class BossSystemEditModeTests
{
    readonly List<GameObject> _createdObjects = new();

    [SetUp]
    public void SetUp()
    {
        ResetSingleton(typeof(BossArenaGrid), "Instance");
        ResetSingleton(typeof(BossMechanicController), "Instance");
        ResetSingleton(typeof(GameManager), "Instance");
        ArenaSetup.CellSize = 6f;
        ArenaSetup.GridCount = 4;
        Time.timeScale = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _createdObjects)
        {
            if (go != null)
                UnityEngine.Object.DestroyImmediate(go);
        }
        _createdObjects.Clear();

        ResetSingleton(typeof(BossArenaGrid), "Instance");
        ResetSingleton(typeof(BossMechanicController), "Instance");
        ResetSingleton(typeof(GameManager), "Instance");
        ArenaSetup.CellSize = 6f;
        ArenaSetup.GridCount = 4;
        Time.timeScale = 1f;
    }

    [Test]
    public void ArenaGrid_GetCellCenter_MapsNorthWestAndSouthEastCorners()
    {
        var grid = CreateComponent<BossArenaGrid>("Grid");

        AssertVector3(new Vector3(-9f, 0.05f, 9f), grid.GetCellCenter(0, 0));
        AssertVector3(new Vector3(9f, 0.05f, -9f), grid.GetCellCenter(3, 3));
    }

    [Test]
    public void ArenaGrid_WorldPositionRoundTrip_WorksForAllSixteenCells()
    {
        var grid = CreateComponent<BossArenaGrid>("Grid");

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                var result = grid.WorldPositionToCell(grid.GetCellCenter(row, col));
                Assert.That(result.row, Is.EqualTo(row), $"row mismatch at ({row},{col})");
                Assert.That(result.col, Is.EqualTo(col), $"col mismatch at ({row},{col})");
            }
        }
    }

    [Test]
    public void ArenaGrid_WorldPositionToCell_ClampsOutsidePositionsToArenaEdges()
    {
        var grid = CreateComponent<BossArenaGrid>("Grid");

        Assert.That(grid.WorldPositionToCell(new Vector3(-999f, 0f, 999f)), Is.EqualTo((0, 0)));
        Assert.That(grid.WorldPositionToCell(new Vector3(999f, 0f, -999f)), Is.EqualTo((3, 3)));
    }

    [Test]
    public void ArenaGrid_RowAndColumnBounds_CoverExactlyOneLane()
    {
        var grid = CreateComponent<BossArenaGrid>("Grid");
        var row = grid.GetRowBounds(2);
        var col = grid.GetColBounds(1);

        AssertVector3(new Vector3(24f, 0.01f, 6f), row.size);
        AssertVector3(new Vector3(6f, 0.01f, 24f), col.size);
        Assert.That(grid.WorldPositionToCell(row.center).row, Is.EqualTo(2));
        Assert.That(grid.WorldPositionToCell(col.center).col, Is.EqualTo(1));
    }

    [Test]
    public void PlayerStats_DamageHealAndReset_ClampToValidHpRange()
    {
        var stats = CreateComponent<PlayerStats>("Player");
        InvokePrivate(stats, "Awake");

        stats.TakeDamage(25);
        Assert.That(stats.CurrentHp, Is.EqualTo(75));

        stats.Heal(999);
        Assert.That(stats.CurrentHp, Is.EqualTo(100));

        stats.TakeDamage(150);
        Assert.That(stats.CurrentHp, Is.Zero);

        stats.FullReset();
        Assert.That(stats.CurrentHp, Is.EqualTo(stats.MaxHp));
    }

    [Test]
    public void PlayerStats_DeathEvent_FiresOnlyOnceAfterHpReachesZero()
    {
        var stats = CreateComponent<PlayerStats>("Player");
        InvokePrivate(stats, "Awake");
        int diedCount = 0;
        stats.OnDied += () => diedCount++;

        stats.TakeDamage(100);
        stats.TakeDamage(100);

        Assert.That(stats.CurrentHp, Is.Zero);
        Assert.That(diedCount, Is.EqualTo(1));
    }

    [Test]
    public void PlayerController_ZeroHp_ImmediatelyEntersDeadState()
    {
        var gameManager = CreateComponent<GameManager>("GameManager");
        InvokePrivate(gameManager, "Awake");
        gameManager.SetState(GameManager.GameState.Playing);

        var player = CreateGameObject("Player");
        var stats = player.AddComponent<PlayerStats>();
        var controller = player.AddComponent<PlayerController>();
        InvokePrivate(stats, "Awake");
        InvokePrivate(controller, "Awake");
        InvokePrivate(controller, "Start");

        stats.TakeDamage(100, "Test Lethal Hit", 10f);

        Assert.That(controller.IsDead, Is.True);
        Assert.That(gameManager.State, Is.EqualTo(GameManager.GameState.Dead));
    }

    [Test]
    public void PlayerController_OutsideArenaCheck_CatchesWallsAndFalling()
    {
        Assert.That(PlayerController.IsOutsideArena(new Vector3(12.01f, 1f, 0f), 12f), Is.True);
        Assert.That(PlayerController.IsOutsideArena(new Vector3(0f, 1f, -12.01f), 12f), Is.True);
        Assert.That(PlayerController.IsOutsideArena(new Vector3(0f, -1.01f, 0f), 12f), Is.True);
        Assert.That(PlayerController.IsOutsideArena(new Vector3(12f, 1f, -12f), 12f), Is.False);
    }

    [Test]
    public void PlayerStats_DamageRecord_CapturesSourceTimeAndEffectiveAmount()
    {
        var stats = CreateComponent<PlayerStats>("Player");
        InvokePrivate(stats, "Awake");
        DamageTakenRecord raisedRecord = null;
        stats.OnDamageTaken += record => raisedRecord = record;

        stats.TakeDamage(150, "Corner Blast", 125f);

        Assert.That(stats.DamageHistory, Has.Count.EqualTo(1));
        Assert.That(raisedRecord, Is.SameAs(stats.DamageHistory[0]));
        Assert.That(raisedRecord.Source, Is.EqualTo("Corner Blast"));
        Assert.That(raisedRecord.Amount, Is.EqualTo(100));
        Assert.That(raisedRecord.FormatTimestamp(), Is.EqualTo("02:05"));
    }

    [Test]
    public void PlayerStats_FullReset_ClearsDamageHistory()
    {
        var stats = CreateComponent<PlayerStats>("Player");
        InvokePrivate(stats, "Awake");
        stats.TakeDamage(20, "Earthquake Pulse", 8f);

        stats.FullReset();

        Assert.That(stats.CurrentHp, Is.EqualTo(stats.MaxHp));
        Assert.That(stats.DamageHistory, Is.Empty);
    }

    [Test]
    public void GameRecord_JsonRoundTrip_PreservesDamageTakenDetails()
    {
        var source = new GameRecord
        {
            BattleTime = 180f,
            DamageTaken = new List<DamageTakenRecord>
            {
                new DamageTakenRecord(8f, "Earthquake Pulse", 8),
                new DamageTakenRecord(125f, "Thunder Sweep", 25)
            }
        };

        var restored = JsonUtility.FromJson<GameRecord>(JsonUtility.ToJson(source));

        Assert.That(restored.DamageTaken, Has.Count.EqualTo(2));
        Assert.That(restored.DamageTaken[0].Source, Is.EqualTo("Earthquake Pulse"));
        Assert.That(restored.DamageTaken[1].Amount, Is.EqualTo(25));
        Assert.That(restored.DamageTaken[1].FormatTimestamp(), Is.EqualTo("02:05"));
    }

    [Test]
    public void GameRecordStore_DeleteFromDirectory_RemovesOnlySelectedJson()
    {
        string directory = Path.Combine(
            Application.temporaryCachePath,
            $"GameRecordDeleteTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            string selectedPath = Path.Combine(directory, "selected.json");
            string otherPath = Path.Combine(directory, "other.json");
            File.WriteAllText(selectedPath, "{}");
            File.WriteAllText(otherPath, "{}");
            var selected = new GameRecord { StorageFileName = "selected.json" };

            bool deleted = GameRecordStore.DeleteFromDirectory(selected, directory);

            Assert.That(deleted, Is.True);
            Assert.That(File.Exists(selectedPath), Is.False);
            Assert.That(File.Exists(otherPath), Is.True);
            Assert.That(selected.StorageFileName, Is.Null);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GameRecordStore_DeleteFromDirectory_RejectsPathTraversal()
    {
        string directory = Path.Combine(
            Application.temporaryCachePath,
            $"GameRecordTraversalTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var unsafeRecord = new GameRecord { StorageFileName = "../outside.json" };

            Assert.That(
                GameRecordStore.DeleteFromDirectory(unsafeRecord, directory),
                Is.False);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Mechanic_KnockbackDistance_IsReducedByEightyPercentWhenAnchored()
    {
        Assert.That(
            BossMechanicController.CalculateKnockbackDistance(18f, false),
            Is.EqualTo(18f).Within(0.0001f));
        Assert.That(
            BossMechanicController.CalculateKnockbackDistance(18f, true),
            Is.EqualTo(3.6f).Within(0.0001f));
    }

    [Test]
    public void Mechanic_KnockbackDirection_UsesBossPositionAsOrigin()
    {
        Vector3 direction = BossMechanicController.CalculateKnockbackDirection(
            new Vector3(0f, 1f, 2f),
            new Vector3(0f, 1.5f, 8f));

        AssertVector3(Vector3.back, direction);
    }

    [Test]
    public void Mechanic_SweepRadius_ReachesPastSquareArenaCorners()
    {
        float radius = BossMechanicController.CalculateArenaCoveringRadius(12f);

        Assert.That(radius, Is.GreaterThan(Mathf.Sqrt(12f * 12f + 12f * 12f)));
    }

    [Test]
    public void Mechanic_SectorHazard_IsContinuousFanMeshFromCenterToRadius()
    {
        var mechanic = CreateComponent<BossMechanicController>("Mechanic");
        var hazard = (GameObject)InvokePrivate(
            mechanic,
            "CreateSectorHazard",
            Vector3.up * 0.05f,
            18f,
            0f,
            90f);
        _createdObjects.Add(hazard);

        var mesh = hazard.GetComponent<MeshFilter>().sharedMesh;
        Assert.That(hazard.transform.childCount, Is.Zero);
        Assert.That(mesh, Is.Not.Null);
        AssertVector3(Vector3.zero, mesh.vertices[0]);
        Assert.That(mesh.vertices.Max(vertex => vertex.magnitude), Is.EqualTo(18f).Within(0.001f));
        Assert.That(mesh.triangles.Length, Is.EqualTo(54));
    }

    [Test]
    public void Mechanic_TetherFieldVisual_IsRaisedAndHasBeacon()
    {
        var mechanic = CreateComponent<BossMechanicController>("Mechanic");
        var field = (GameObject)InvokePrivate(
            mechanic,
            "CreateTetherFieldVisual",
            Vector3.zero,
            3f);
        _createdObjects.Add(field);

        var platform = field.transform.Find("TetherVisual");
        var beacon = field.transform.Find("TetherBeacon");
        Assert.That(platform, Is.Not.Null);
        Assert.That(platform.localScale.y, Is.GreaterThan(0.1f));
        Assert.That(platform.localPosition.y, Is.GreaterThan(0.1f));
        Assert.That(beacon, Is.Not.Null);
    }

    [Test]
    public void Mechanic_TetherField_ContainsOnlyPlayerInsideConfiguredRadius()
    {
        var mechanic = CreateComponent<BossMechanicController>("Mechanic");
        var player = CreateGameObject("Player");
        SetField(mechanic, "_playerTransform", player.transform);
        SetField(mechanic, "_tetherFieldActive", true);
        SetField(mechanic, "_tetherFieldCenter", new Vector3(3f, 0f, -3f));
        SetField(mechanic, "_tetherFieldRadius", 3f);

        player.transform.position = new Vector3(5f, 10f, -3f);
        Assert.That(mechanic.IsPlayerInTetherField(), Is.True);

        player.transform.position = new Vector3(7f, 0f, -3f);
        Assert.That(mechanic.IsPlayerInTetherField(), Is.False);
    }

    [Test]
    public void Mechanic_SectorCheck_IncludesBoundaryAndExcludesOutsideAngle()
    {
        var mechanic = CreateComponent<BossMechanicController>("Mechanic");
        var player = CreateGameObject("Player");
        SetField(mechanic, "_playerTransform", player.transform);

        player.transform.position = new Vector3(0f, 0f, 10f);
        Assert.That((bool)InvokePrivate(mechanic, "IsPlayerInSector", 0f, 90f), Is.True);

        player.transform.position = new Vector3(10f, 0f, 10f);
        Assert.That((bool)InvokePrivate(mechanic, "IsPlayerInSector", 0f, 90f), Is.True);

        player.transform.position = new Vector3(10f, 0f, 0f);
        Assert.That((bool)InvokePrivate(mechanic, "IsPlayerInSector", 0f, 90f), Is.False);
    }

    [Test]
    public void Timeline_BuildEvents_IsStrictlySortedAndContainsAllMilestones()
    {
        var timeline = CreateTimeline();
        InvokePrivate(timeline, "BuildEvents");
        var events = GetTimelineEvents(timeline);

        Assert.That(events.Count, Is.EqualTo(24));
        Assert.That(events.Select(e => e.time), Is.Ordered.Ascending);
        Assert.That(events.Select(e => e.time).Distinct().Count(), Is.EqualTo(events.Count));
        Assert.That(events, Does.Contain((8f, "震地脈衝")));
        Assert.That(events, Does.Contain((65f, "避雷錨點")));
        Assert.That(events, Does.Contain((125f, "雷環掃掠")));
        Assert.That(events, Does.Contain((170f, "天崩地裂讀條")));
        Assert.That(events, Does.Contain((178f, "最終波三四角")));
    }

    [Test]
    public void Timeline_StartAtDebugTime_SkipsPastEventsWithoutExecutingThem()
    {
        var timeline = CreateTimeline();
        timeline.debugStartTime = 120f;

        timeline.StartTimeline();

        Assert.That(GetField<float>(timeline, "_currentTime"), Is.EqualTo(120f));
        Assert.That(GetField<bool>(timeline, "_running"), Is.True);
        int nextIndex = GetField<int>(timeline, "_nextEventIndex");
        var events = GetTimelineEvents(timeline);
        Assert.That(nextIndex, Is.LessThan(events.Count));
        Assert.That(events[nextIndex].time, Is.GreaterThan(120f));
        Assert.That(events[nextIndex].name, Is.EqualTo("Phase3開始"));
    }

    [Test]
    public void Timeline_ResetTimeline_ClearsRuntimeStateAndSchedule()
    {
        var timeline = CreateTimeline();
        timeline.debugStartTime = 65f;
        timeline.StartTimeline();

        timeline.ResetTimeline();

        Assert.That(GetField<bool>(timeline, "_running"), Is.False);
        Assert.That(GetField<float>(timeline, "_currentTime"), Is.Zero);
        Assert.That(GetField<int>(timeline, "_nextEventIndex"), Is.Zero);
        Assert.That(GetField<string>(timeline, "_currentPhase"), Is.EqualTo("Idle"));
        Assert.That(GetField<string>(timeline, "_currentMechanic"), Is.EqualTo("None"));
        Assert.That(GetTimelineEvents(timeline), Is.Empty);
    }

    [Test]
    public void Timeline_Complete_TransitionsPlayingGameToVictory()
    {
        var gameManager = CreateComponent<GameManager>("GameManager");
        InvokePrivate(gameManager, "Awake");
        gameManager.SetState(GameManager.GameState.Playing);
        var timeline = CreateTimeline();

        InvokePrivate(timeline, "OnTimelineComplete");

        Assert.That(gameManager.State, Is.EqualTo(GameManager.GameState.Victory));
    }

    [Test]
    public void Timeline_ArenaCenterDestination_PreservesBossHeight()
    {
        AssertVector3(
            new Vector3(0f, 1.5f, 0f),
            BossTimelineController.GetArenaCenterAtCurrentHeight(new Vector3(4f, 1.5f, 8f)));
    }

    BossTimelineController CreateTimeline()
    {
        var timeline = CreateComponent<BossTimelineController>("BossTimeline");
        InvokePrivate(timeline, "Awake");
        timeline.autoStart = false;
        return timeline;
    }

    T CreateComponent<T>(string name) where T : Component
    {
        return CreateGameObject(name).AddComponent<T>();
    }

    GameObject CreateGameObject(string name)
    {
        var go = new GameObject(name);
        _createdObjects.Add(go);
        return go;
    }

    static List<(float time, string name)> GetTimelineEvents(BossTimelineController timeline)
    {
        var list = (IList)GetField<object>(timeline, "_events");
        var result = new List<(float time, string name)>();
        foreach (var item in list)
        {
            var type = item.GetType();
            float time = (float)type.GetField("time", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(item);
            string name = (string)type.GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(item);
            result.Add((time, name));
        }
        return result;
    }

    static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing private method {methodName}");
        return method.Invoke(target, args);
    }

    static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}");
        field.SetValue(target, value);
    }

    static T GetField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}");
        return (T)field.GetValue(target);
    }

    static void ResetSingleton(Type type, string propertyName)
    {
        var backingField = type.GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        backingField?.SetValue(null, null);
    }

    static void AssertVector3(Vector3 expected, Vector3 actual)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
    }
}
