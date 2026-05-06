using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using ZFrame;
using ZLib;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using Utils;
using Game.Examples;

public partial class MainPanel
{
    private Button infantryFastBtn;
    private Button badgerTankFastBtn;
    private Button grizzlyTankFastBtn;
    private Button infantryReduceBtn;
    private Button badgerTankReduceBtn;
    private Button grizzlyTankReduceBtn;
    private Button unit1SellBtn;
    private Button unit2SellBtn;
    private Button unit3SellBtn;

    private int fastProductionRefreshTimerId = 0;
    private const float FAST_PRODUCTION_REFRESH_INTERVAL = 1f;

    private void BindFastProductionEvents()
    {
        if (infantryFastBtn != null) infantryFastBtn.onClick.AddListener(OnInfantryFastBtnClick);
        if (badgerTankFastBtn != null) badgerTankFastBtn.onClick.AddListener(OnBadgerTankFastBtnClick);
        if (grizzlyTankFastBtn != null) grizzlyTankFastBtn.onClick.AddListener(OnGrizzlyTankFastBtnClick);
        if (infantryReduceBtn != null) infantryReduceBtn.onClick.AddListener(OnInfantryReduceBtnClick);
        if (badgerTankReduceBtn != null) badgerTankReduceBtn.onClick.AddListener(OnBadgerTankReduceBtnClick);
        if (grizzlyTankReduceBtn != null) grizzlyTankReduceBtn.onClick.AddListener(OnGrizzlyTankReduceBtnClick);
        if (unit1SellBtn != null) unit1SellBtn.onClick.AddListener(OnUnit1SellBtnClick);
        if (unit2SellBtn != null) unit2SellBtn.onClick.AddListener(OnUnit2SellBtnClick);
        if (unit3SellBtn != null) unit3SellBtn.onClick.AddListener(OnUnit3SellBtnClick);
    }

    private void UnbindFastProductionEvents()
    {
        if (infantryFastBtn != null) infantryFastBtn.onClick.RemoveListener(OnInfantryFastBtnClick);
        if (badgerTankFastBtn != null) badgerTankFastBtn.onClick.RemoveListener(OnBadgerTankFastBtnClick);
        if (grizzlyTankFastBtn != null) grizzlyTankFastBtn.onClick.RemoveListener(OnGrizzlyTankFastBtnClick);
        if (infantryReduceBtn != null) infantryReduceBtn.onClick.RemoveListener(OnInfantryReduceBtnClick);
        if (badgerTankReduceBtn != null) badgerTankReduceBtn.onClick.RemoveListener(OnBadgerTankReduceBtnClick);
        if (grizzlyTankReduceBtn != null) grizzlyTankReduceBtn.onClick.RemoveListener(OnGrizzlyTankReduceBtnClick);
        if (unit1SellBtn != null) unit1SellBtn.onClick.RemoveListener(OnUnit1SellBtnClick);
        if (unit2SellBtn != null) unit2SellBtn.onClick.RemoveListener(OnUnit2SellBtnClick);
        if (unit3SellBtn != null) unit3SellBtn.onClick.RemoveListener(OnUnit3SellBtnClick);
    }

    private void CleanupFastProduction()
    {
        if (fastProductionRefreshTimerId != 0)
        {
            Tick.ClearTimeout(fastProductionRefreshTimerId);
            fastProductionRefreshTimerId = 0;
        }
    }

    private void InitializeFastProductionButtons()
    {
        PanelObject.transform.Find("FastProduction")?.transform.gameObject.SetActive(true);
        infantryFastBtn = PanelObject.transform.Find("FastProduction/Unit1Btn")?.GetComponent<Button>();
        badgerTankFastBtn = PanelObject.transform.Find("FastProduction/Unit2Btn")?.GetComponent<Button>();
        grizzlyTankFastBtn = PanelObject.transform.Find("FastProduction/Unit3Btn")?.GetComponent<Button>();
        infantryReduceBtn = PanelObject.transform.Find("FastProduction/Unit1ReduceBtn")?.GetComponent<Button>();
        badgerTankReduceBtn = PanelObject.transform.Find("FastProduction/Unit2ReduceBtn")?.GetComponent<Button>();
        grizzlyTankReduceBtn = PanelObject.transform.Find("FastProduction/Unit3ReduceBtn")?.GetComponent<Button>();
        unit1SellBtn = PanelObject.transform.Find("FastProduction/Unit1SellBtn")?.GetComponent<Button>();
        unit2SellBtn = PanelObject.transform.Find("FastProduction/Unit2SellBtn")?.GetComponent<Button>();
        unit3SellBtn = PanelObject.transform.Find("FastProduction/Unit3SellBtn")?.GetComponent<Button>();
        InitializeFastProductionCostValues();

        if (infantryFastBtn != null) infantryFastBtn.gameObject.SetActive(true);
        if (badgerTankFastBtn != null) badgerTankFastBtn.gameObject.SetActive(true);
        if (grizzlyTankFastBtn != null) grizzlyTankFastBtn.gameObject.SetActive(true);
        if (infantryReduceBtn != null) infantryReduceBtn.gameObject.SetActive(false);
        if (badgerTankReduceBtn != null) badgerTankReduceBtn.gameObject.SetActive(false);
        if (grizzlyTankReduceBtn != null) grizzlyTankReduceBtn.gameObject.SetActive(false);
        if (unit1SellBtn != null) unit1SellBtn.gameObject.SetActive(false);
        if (unit2SellBtn != null) unit2SellBtn.gameObject.SetActive(false);
        if (unit3SellBtn != null) unit3SellBtn.gameObject.SetActive(false);

        UpdateBuildOrProduceBtnText(infantryFastBtn, 5, 0);
        UpdateBuildOrProduceBtnText(badgerTankFastBtn, 6, 0);
        UpdateBuildOrProduceBtnText(grizzlyTankFastBtn, 7, 0);

        StartFastProductionRefresh();
    }

    private void InitializeFastProductionCostValues()
    {
        InitializeFastProductionCostValue(infantryFastBtn, UnitType.Infantry);
        InitializeFastProductionCostValue(badgerTankFastBtn, UnitType.badgerTank);
        InitializeFastProductionCostValue(grizzlyTankFastBtn, UnitType.grizzlyTank);
    }

    private void InitializeFastProductionCostValue(Button btn, UnitType unitType)
    {
        if (btn == null) return;

        ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)unitType);
        if (confUnit == null) return;

        TMP_Text costValueText = btn.transform.Find("CostValue")?.GetComponent<TMP_Text>();
        if (costValueText == null) return;

        costValueText.text = $"${confUnit.CostMoney}";
    }

    private void UpdateFastBtnCountText(Button btn, int displayCount)
    {
        TMP_Text numValueText = btn.transform.Find("Num/Value")?.GetComponent<TMP_Text>();
        if (numValueText != null)
        {
            numValueText.text = $"+{displayCount}";
        }
    }

    private void StartFastProductionRefresh()
    {
        if (fastProductionRefreshTimerId != 0)
        {
            Tick.ClearTimeout(fastProductionRefreshTimerId);
        }
        fastProductionRefreshTimerId = Tick.SetTimeout(RefreshFastProductionButtons, FAST_PRODUCTION_REFRESH_INTERVAL);
    }

    private void RefreshFastProductionButtons()
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null)
        {
            StartFastProductionRefresh();
            return;
        }

        var components = game.World.ComponentManager
            .GetComponentsWithCondition<ProduceComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        var supportedTypes = new HashSet<UnitType>();
        var produceCounts = new Dictionary<UnitType, int>();
        var buildingTypeCounts = new Dictionary<int, int>();
        foreach (var (produceComponent, entity) in components)
        {
            foreach (var unitType in produceComponent.SupportedUnitTypes)
            {
                supportedTypes.Add(unitType);
                int count = 0;
                if (produceComponent.ProduceNumbers.ContainsKey(unitType))
                {
                    count = produceComponent.ProduceNumbers[unitType];
                }
                if (!produceCounts.ContainsKey(unitType)) produceCounts[unitType] = 0;
                produceCounts[unitType] += count;
            }
        }

        var buildingComponents = game.World.ComponentManager
            .GetComponentsWithCondition<BuildingComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        foreach (var (buildingComponent, entity) in buildingComponents)
        {
            if (!buildingTypeCounts.ContainsKey(buildingComponent.BuildingType))
            {
                buildingTypeCounts[buildingComponent.BuildingType] = 0;
            }
            buildingTypeCounts[buildingComponent.BuildingType]++;
        }

        UpdateFastBtnCountText(infantryFastBtn, produceCounts.ContainsKey(UnitType.Infantry) ? produceCounts[UnitType.Infantry] : 0);
        UpdateFastBtnCountText(badgerTankFastBtn, produceCounts.ContainsKey(UnitType.badgerTank) ? produceCounts[UnitType.badgerTank] : 0);
        UpdateFastBtnCountText(grizzlyTankFastBtn, produceCounts.ContainsKey(UnitType.grizzlyTank) ? produceCounts[UnitType.grizzlyTank] : 0);
        UpdateReduceBtn(infantryReduceBtn, UnitType.Infantry, supportedTypes, produceCounts);
        UpdateReduceBtn(badgerTankReduceBtn, UnitType.badgerTank, supportedTypes, produceCounts);
        UpdateReduceBtn(grizzlyTankReduceBtn, UnitType.grizzlyTank, supportedTypes, produceCounts);

        int unit1Count = buildingTypeCounts.TryGetValue(5, out int unit1BuiltCount) ? unit1BuiltCount : 0;
        int unit2Count = buildingTypeCounts.TryGetValue(6, out int unit2BuiltCount) ? unit2BuiltCount : 0;
        int unit3Count = buildingTypeCounts.TryGetValue(7, out int unit3BuiltCount) ? unit3BuiltCount : 0;

        UpdateBuildOrProduceBtnText(infantryFastBtn, 5, unit1Count);
        UpdateBuildOrProduceBtnText(badgerTankFastBtn, 6, unit2Count);
        UpdateBuildOrProduceBtnText(grizzlyTankFastBtn, 7, unit3Count);
        UpdateSellBtn(unit1SellBtn, unit1Count);
        UpdateSellBtn(unit2SellBtn, unit2Count);
        UpdateSellBtn(unit3SellBtn, unit3Count);

        StartFastProductionRefresh();
    }

    private void UpdateReduceBtn(Button btn, UnitType unitType, HashSet<UnitType> supportedTypes, Dictionary<UnitType, int> produceCounts)
    {
        if (btn == null) return;

        bool canReduce = supportedTypes.Contains(unitType) &&
            produceCounts.TryGetValue(unitType, out int count) &&
            count > 0;

        btn.gameObject.SetActive(canReduce);
    }

    private void UpdateBuildOrProduceBtnText(Button btn, int buildingType, int builtCount)
    {
        if (btn == null) return;

        btn.gameObject.SetActive(true);

        TMP_Text label = btn.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (label == null)
        {
            zUDebug.LogWarning($"[快捷建筑] 未找到按钮文本节点 Text (TMP), BuildingType={buildingType}");
            return;
        }

        label.text = builtCount > 0 ? "生产" : "建造";
    }

    private void UpdateSellBtn(Button btn, int builtCount)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(builtCount > 0);
    }

    private void OnInfantryFastBtnClick()
    {
        UISound.Instance.PlayClick();
        if (TryHandleBuildButtonClick(infantryFastBtn, 5)) return;
        FastProduceUnit(UnitType.Infantry);
    }

    private void OnBadgerTankFastBtnClick()
    {
        UISound.Instance.PlayClick();
        if (TryHandleBuildButtonClick(badgerTankFastBtn, 6)) return;
        FastProduceUnit(UnitType.badgerTank);
    }

    private void OnGrizzlyTankFastBtnClick()
    {
        UISound.Instance.PlayClick();
        if (TryHandleBuildButtonClick(grizzlyTankFastBtn, 7)) return;
        FastProduceUnit(UnitType.grizzlyTank);
    }

    private void OnInfantryReduceBtnClick()
    {
        UISound.Instance.PlayClick();
        FastReduceUnit(UnitType.Infantry);
    }

    private void OnBadgerTankReduceBtnClick()
    {
        UISound.Instance.PlayClick();
        FastReduceUnit(UnitType.badgerTank);
    }

    private void OnGrizzlyTankReduceBtnClick()
    {
        UISound.Instance.PlayClick();
        FastReduceUnit(UnitType.grizzlyTank);
    }

    private void OnUnit1SellBtnClick()
    {
        UISound.Instance.PlayClick();
        ConfirmFastSellBuildingByType(5);
    }

    private void OnUnit2SellBtnClick()
    {
        UISound.Instance.PlayClick();
        ConfirmFastSellBuildingByType(6);
    }

    private void OnUnit3SellBtnClick()
    {
        UISound.Instance.PlayClick();
        ConfirmFastSellBuildingByType(7);
    }

    private bool TryHandleBuildButtonClick(Button btn, int buildingType)
    {
        if (btn == null) return false;

        var game = Ra2Demo?.GetBattleGame();
        if (game == null) return false;

        int localPlayerCampId = EcsUtils.GetLocalPlayerCampId(game);
        int builtCount = GetCompletedBuildingCountByType(game, buildingType, localPlayerCampId);
        if (builtCount > 0) return false;

        return TryCreateBuildingByType(buildingType);
    }

    private bool TryCreateBuildingByType(int buildingType)
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null) return false;

        int localPlayerCampId = EcsUtils.GetLocalPlayerCampId(game);
        List<ConfBuildingPlace> allBuildingPlaces = ConfigManager.GetAll<ConfBuildingPlace>();

        ConfBuildingPlace targetBuildingPlace = null;
        foreach (var confBuildingPlace in allBuildingPlaces)
        {
            if (confBuildingPlace.Enabled == 0) continue;
            if (confBuildingPlace.CampID != localPlayerCampId) continue;
            if (confBuildingPlace.Type != buildingType) continue;
            targetBuildingPlace = confBuildingPlace;
            break;
        }

        if (targetBuildingPlace == null)
        {
            zUDebug.LogError($"[快捷建筑] 未找到建筑摆放配置, BuildingType={buildingType}, CampId={localPlayerCampId}");
            MessageUtils.ShowTips("未找到建筑配置");
            return false;
        }

        int currentCount = GetCompletedBuildingCountByType(game, buildingType, localPlayerCampId);
        if (currentCount >= targetBuildingPlace.Count)
        {
            zUDebug.LogWarning($"[快捷建筑] 建筑数量达到上限: {currentCount}/{targetBuildingPlace.Count}, BuildingType={buildingType}");
            MessageUtils.ShowTips("建筑数量已达上限");
            return false;
        }

        UnityEngine.Vector3[] positions = StringUtils.ParsePositions(targetBuildingPlace.Position);
        if (positions == null || positions.Length == 0)
        {
            zUDebug.LogError($"[快捷建筑] 建筑位置配置无效: {targetBuildingPlace.Position}, BuildingType={buildingType}");
            MessageUtils.ShowTips("建筑位置配置无效");
            return false;
        }

        int nextPositionIndex = currentCount % positions.Length;
        UnityEngine.Vector3 logicPosition = positions[nextPositionIndex];

        var createBuildingCommand = new CreateBuildingCommand(
            confID: targetBuildingPlace.Type,
            campId: 0,
            position: new zUnity.zVector3(
                zfloat.FromRaw((long)(logicPosition.x * zfloat.SCALE_10000)),
                zfloat.FromRaw((long)(logicPosition.y * zfloat.SCALE_10000)),
                zfloat.FromRaw((long)(logicPosition.z * zfloat.SCALE_10000))
            )
        )
        {
            Source = CommandSource.Local
        };

        game.SubmitCommand(createBuildingCommand);
        zUDebug.Log($"[快捷建筑] 发送建造命令, BuildingType={buildingType}, ConfID={targetBuildingPlace.Type}, Position={logicPosition}");
        RefreshFastProductionButtons();
        return true;
    }

    private int GetCompletedBuildingCountByType(BattleGame game, int buildingType, int localPlayerCampId)
    {
        var componentManager = game.World.ComponentManager;
        int count = 0;

        var buildingEntities = componentManager.GetAllEntityIdsWith<BuildingComponent>();
        foreach (var entityId in buildingEntities)
        {
            var entity = new Entity(entityId);
            if (!componentManager.HasComponent<CampComponent>(entity)) continue;
            if (componentManager.HasComponent<BuildingConstructionComponent>(entity)) continue;

            var campComponent = componentManager.GetComponent<CampComponent>(entity);
            if (campComponent.CampId != localPlayerCampId) continue;

            var buildingComponent = componentManager.GetComponent<BuildingComponent>(entity);
            if (buildingComponent.BuildingType == buildingType) count++;
        }

        return count;
    }

    private void ConfirmFastSellBuildingByType(int buildingType)
    {
        if (confirmDialog == null)
        {
            FastSellBuildingByType(buildingType);
            return;
        }

        string buildingName = $"建筑类型{buildingType}";
        ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(buildingType);
        if (confBuilding != null && !string.IsNullOrEmpty(confBuilding.Name))
        {
            buildingName = confBuilding.Name;
        }

        confirmDialog.Show(
            onConfirm: () =>
            {
                FastSellBuildingByType(buildingType);
            },
            message: $"是否要出售\"{buildingName}\"建筑？"
        );
    }

    private void FastProduceUnit(UnitType unitType)
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null) return;

        var components = game.World.ComponentManager
            .GetComponentsWithCondition<ProduceComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        foreach (var (produceComponent, entity) in components)
        {
            if (produceComponent.SupportedUnitTypes.Contains(unitType))
            {
                var produceCommand = new ProduceCommand(
                    campId: 0,
                    entityId: entity.Id,
                    unitType: unitType,
                    changeValue: 1)
                {
                    Source = CommandSource.Local
                };
                game.SubmitCommand(produceCommand);
                RefreshFastProductionButtons();
                zUDebug.Log($"[快捷生产] 发送生产命令: 单位{unitType}, 工厂{entity.Id}");
                return;
            }
        }
    }

    private void FastReduceUnit(UnitType unitType)
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null) return;

        var components = game.World.ComponentManager
            .GetComponentsWithCondition<ProduceComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        foreach (var (produceComponent, entity) in components)
        {
            if (!produceComponent.SupportedUnitTypes.Contains(unitType))
            {
                continue;
            }

            if (!produceComponent.ProduceNumbers.TryGetValue(unitType, out int count) || count <= 0)
            {
                continue;
            }

            var produceCommand = new ProduceCommand(
                campId: 0,
                entityId: entity.Id,
                unitType: unitType,
                changeValue: -1)
            {
                Source = CommandSource.Local
            };
            game.SubmitCommand(produceCommand);
            RefreshFastProductionButtons();

            zUDebug.Log($"[快捷生产] 发送减少生产命令: 单位{unitType}, 工厂{entity.Id}");
            return;
        }
    }

    private void FastSellBuildingByType(int buildingType)
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null) return;

        var buildingComponents = game.World.ComponentManager
            .GetComponentsWithCondition<BuildingComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        int selectedEntityId = -1;
        foreach (var (buildingComponent, entity) in buildingComponents)
        {
            if (buildingComponent.BuildingType != buildingType)
            {
                continue;
            }

            if (selectedEntityId == -1 || entity.Id < selectedEntityId)
            {
                selectedEntityId = entity.Id;
            }
        }

        if (selectedEntityId == -1)
        {
            ShowMessage($"未找到可出售的建筑(类型{buildingType})");
            RefreshFastProductionButtons();
            return;
        }

        var sellCommand = new SellStructureCommand(campId: 0, entityId: selectedEntityId)
        {
            Source = CommandSource.Local
        };
        game.SubmitCommand(sellCommand);
        RefreshFastProductionButtons();
    }
}

