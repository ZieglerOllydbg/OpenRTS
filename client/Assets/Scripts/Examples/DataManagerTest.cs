using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManagerTest : MonoBehaviour
{
    void Start()
    {
        TestConfNPC();
        TestConfItem();
        TestConfInitUnits();
        TestQueryFunctions(); // 测试新增的查询功能
        TestGetAllFunction(); // 测试新增的GetAll功能
    }

    void TestConfNPC()
    {
        Debug.Log("=== Testing ConfNPC ===");
        
        // 测试获取存在的NPC数据
        var npc1 = ConfigManager.Get<ConfNPC>("BS001");
        if (npc1 != null)
        {
            Debug.Log($"Found NPC: ID={npc1.ID}, Name={npc1.Name}, HP={npc1.HP}, Attack={npc1.Attack}, Defence={npc1.Defence}");
        }
        else
        {
            Debug.LogError("Failed to load NPC with ID BS001");
        }

        // 测试获取另一个存在的NPC数据
        var npc2 = ConfigManager.Get<ConfNPC>("BS002");
        if (npc2 != null)
        {
            Debug.Log($"Found NPC: ID={npc2.ID}, Name={npc2.Name}, HP={npc2.HP}, Attack={npc2.Attack}, Defence={npc2.Defence}");
        }
        else
        {
            Debug.LogError("Failed to load NPC with ID BS002");
        }

        // 测试获取不存在的NPC数据
        var npcNotFound = ConfigManager.Get<ConfNPC>("BS999");
        if (npcNotFound == null)
        {
            Debug.Log("Correctly returned null for non-existent NPC ID BS999");
        }
        else
        {
            Debug.LogError("Should have returned null for non-existent NPC ID BS999");
        }
    }

    void TestConfItem()
    {
        Debug.Log("\n=== Testing ConfItem ===");

        // 测试获取存在的Item数据
        var item1 = ConfigManager.Get<ConfItem>("WP001");
        if (item1 != null)
        {
            Debug.Log($"Found Item: ID={item1.ID}, Name={item1.Name}, AssetName={item1.AssetName}");
        }
        else
        {
            Debug.LogError("Failed to load Item with ID WP001");
        }

        // 测试获取另一个存在的Item数据
        var item2 = ConfigManager.Get<ConfItem>("WP002");
        if (item2 != null)
        {
            Debug.Log($"Found Item: ID={item2.ID}, Name={item2.Name}, AssetName={item2.AssetName}");
        }
        else
        {
            Debug.LogError("Failed to load Item with ID WP002");
        }
    }

    void TestConfInitUnits()
    {
        Debug.Log("\n=== Testing ConfInitUnits ===");

        // 测试获取存在的Unit数据
        var unit1 = ConfigManager.Get<ConfInitUnits>("1");
        if (unit1 != null)
        {
            Debug.Log($"Found Unit: ID={unit1.ID}, Camp={unit1.Camp}, UnitType={unit1.ConfID}, Position={unit1.Position}, Note={unit1.Note}");
        }
        else
        {
            Debug.LogError("Failed to load Unit with ID 1");
        }

        // 测试获取另一个存在的Unit数据
        var unit2 = ConfigManager.Get<ConfInitUnits>("2");
        if (unit2 != null)
        {
            Debug.Log($"Found Unit: ID={unit2.ID}, Camp={unit2.Camp}, UnitType={unit2.ConfID}, Position={unit2.Position}, Note={unit2.Note}");
        }
        else
        {
            Debug.LogError("Failed to load Unit with ID 2");
        }
    }
    
    void TestQueryFunctions()
    {
        Debug.Log("\n=== Testing Query Functions ===");
        
        // 测试单条件查询返回单个实例
        Debug.Log("--- Testing GetBy with single condition ---");
        var highHpNpc = ConfigManager.GetBy<ConfNPC>(npc => npc.HP > 100);
        if (highHpNpc != null)
        {
            Debug.Log($"Found high HP NPC: {highHpNpc.Name}, HP={highHpNpc.HP}");
        }
        else
        {
            Debug.Log("No NPC with HP > 100 found");
        }

        // 测试单条件查询返回实例列表
        Debug.Log("--- Testing GetListBy with single condition ---");
        var allNpcs = ConfigManager.GetListBy<ConfNPC>(npc => npc.Attack > 0);
        Debug.Log($"Found {allNpcs.Count} NPCs with Attack > 0");
        foreach (var npc in allNpcs)
        {
            Debug.Log($"  NPC: {npc.Name}, Attack={npc.Attack}");
        }

        // 测试多条件查询返回单个实例
        Debug.Log("--- Testing GetBy with multiple conditions ---");
        var specificNpc = ConfigManager.GetBy<ConfNPC>(
            npc => npc.Attack > 50,
            npc => npc.Defence > 20
        );
        if (specificNpc != null)
        {
            Debug.Log($"Found NPC with Attack > 50 and Defence > 20: {specificNpc.Name}, A={specificNpc.Attack}, D={specificNpc.Defence}");
        }
        else
        {
            Debug.Log("No NPC meets both Attack > 50 and Defence > 20 conditions");
        }

        // 测试多条件查询返回实例列表
        Debug.Log("--- Testing GetListBy with multiple conditions ---");
        var strongNpcs = ConfigManager.GetListBy<ConfNPC>(
            npc => npc.HP > 50,
            npc => npc.Attack > 30
        );
        Debug.Log($"Found {strongNpcs.Count} NPCs with HP > 50 and Attack > 30:");
        foreach (var npc in strongNpcs)
        {
            Debug.Log($"  NPC: {npc.Name}, HP={npc.HP}, Attack={npc.Attack}, Defence={npc.Defence}");
        }

        // 测试使用名称条件查询
        Debug.Log("--- Testing GetBy with name condition ---");
        var namedNpc = ConfigManager.GetBy<ConfNPC>(npc => npc.Name.Contains("Archer"));
        if (namedNpc != null)
        {
            Debug.Log($"Found NPC with 'Archer' in name: {namedNpc.Name}");
        }
        else
        {
            Debug.Log("No NPC with 'Archer' in name found");
        }

        // 测试查询返回空结果的情况
        Debug.Log("--- Testing queries with no results ---");
        var nonExistent = ConfigManager.GetBy<ConfNPC>(npc => npc.HP > 10000);
        if (nonExistent == null)
        {
            Debug.Log("Correctly returned null for condition with no matches (HP > 10000)");
        }
        else
        {
            Debug.LogError("Should have returned null for condition with no matches");
        }

        var emptyList = ConfigManager.GetListBy<ConfNPC>(npc => npc.Name == "NonExistent");
        if (emptyList.Count == 0)
        {
            Debug.Log("Correctly returned empty list for condition with no matches");
        }
        else
        {
            Debug.LogError("Should have returned empty list for condition with no matches");
        }
        
        Debug.Log("\n=== Query Functions Test Complete ===");
    }
    
    void TestGetAllFunction()
    {
        Debug.Log("\n=== Testing GetAll Function ===");
        
        // 测试获取所有NPC数据
        Debug.Log("--- Testing GetAll for NPCs ---");
        var allNpcs = ConfigManager.GetAll<ConfNPC>();
        Debug.Log($"Retrieved {allNpcs.Count} NPCs using GetAll:");
        foreach (var npc in allNpcs)
        {
            Debug.Log($"  NPC: ID={npc.ID}, Name={npc.Name}, HP={npc.HP}, Attack={npc.Attack}, Defence={npc.Defence}");
        }
        
        // 测试获取所有Item数据
        Debug.Log("--- Testing GetAll for Items ---");
        var allItems = ConfigManager.GetAll<ConfItem>();
        Debug.Log($"Retrieved {allItems.Count} Items using GetAll:");
        foreach (var item in allItems)
        {
            Debug.Log($"  Item: ID={item.ID}, Name={item.Name}, AssetName={item.AssetName}");
        }
        
        // 测试获取所有Unit数据
        Debug.Log("--- Testing GetAll for Units ---");
        var allUnits = ConfigManager.GetAll<ConfInitUnits>();
        Debug.Log($"Retrieved {allUnits.Count} Units using GetAll:");
        foreach (var unit in allUnits)
        {
            Debug.Log($"  Unit: ID={unit.ID}, Camp={unit.Camp}, UnitType={unit.ConfID}, Position={unit.Position}, Note={unit.Note}");
        }
        
        // 验证GetAll和GetListBy无条件查询结果的一致性
        Debug.Log("--- Verifying consistency between GetAll and GetListBy ---");
        var allNpcsFromGetAll = ConfigManager.GetAll<ConfNPC>();
        var allNpcsFromGetListBy = ConfigManager.GetListBy<ConfNPC>(_ => true); // 条件始终为真
        
        if (allNpcsFromGetAll.Count == allNpcsFromGetListBy.Count)
        {
            Debug.Log("✓ GetAll and GetListBy (with always-true condition) return same count");
        }
        else
        {
            Debug.LogError("✗ Count mismatch between GetAll and GetListBy");
        }
        
        Debug.Log("\n=== GetAll Function Test Complete ===");
    }
}