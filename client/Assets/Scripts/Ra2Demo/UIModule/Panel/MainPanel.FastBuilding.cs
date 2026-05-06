using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZFrame;
using ZLib;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using Utils;
using Game.Examples;

/// <summary>
/// 主面板快捷建造分部，负责驱动 FastProduction/CommonBuild 的建筑切换、费用图标刷新和建造命令提交。
/// </summary>
public partial class MainPanel
{
    private GameObject commonBuildRoot;
    private Button commonBuildBtn;
    private Button commonBuildLeftBtn;
    private Button commonBuildRightBtn;
    private Image commonBuildImage;
    private TMP_Text commonBuildNameText;
    private TMP_Text commonBuildCostValueText;
    private readonly List<ConfBuildingPlace> fastBuildingAvailablePlaces = new();
    private int fastBuildingCurrentIndex;
    private int fastBuildingCurrentPlaceId;
    private int fastBuildingRefreshTimerId;
    private int fastBuildingSpriteRequestId;
    private const float FAST_BUILDING_REFRESH_INTERVAL = 1f;

    private void InitializeFastBuildingButtons()
    {
        Transform commonBuildTransform = PanelObject.transform.Find("FastProduction/CommonBuild");
        commonBuildRoot = commonBuildTransform?.gameObject;
        commonBuildBtn = commonBuildTransform?.Find("BuildBtn")?.GetComponent<Button>();
        commonBuildImage = commonBuildTransform?.Find("BuildBtn/BuildImg")?.GetComponent<Image>();
        commonBuildNameText = commonBuildTransform?.Find("BuildBtn/Text (TMP)")?.GetComponent<TMP_Text>();
        commonBuildCostValueText = commonBuildTransform?.Find("BuildBtn/CostValue")?.GetComponent<TMP_Text>();
        commonBuildLeftBtn = commonBuildTransform?.Find("LeftBtn")?.GetComponent<Button>();
        commonBuildRightBtn = commonBuildTransform?.Find("RightBtn")?.GetComponent<Button>();

        commonBuildRoot?.SetActive(false);
        fastBuildingCurrentIndex = 0;
        fastBuildingCurrentPlaceId = 0;
        fastBuildingAvailablePlaces.Clear();

        RefreshFastBuildingButtons();
    }

    private void BindFastBuildingEvents()
    {
        if (commonBuildBtn != null) commonBuildBtn.onClick.AddListener(OnCommonBuildBtnClick);
        if (commonBuildLeftBtn != null) commonBuildLeftBtn.onClick.AddListener(OnCommonBuildLeftBtnClick);
        if (commonBuildRightBtn != null) commonBuildRightBtn.onClick.AddListener(OnCommonBuildRightBtnClick);
    }

    private void UnbindFastBuildingEvents()
    {
        if (commonBuildBtn != null) commonBuildBtn.onClick.RemoveListener(OnCommonBuildBtnClick);
        if (commonBuildLeftBtn != null) commonBuildLeftBtn.onClick.RemoveListener(OnCommonBuildLeftBtnClick);
        if (commonBuildRightBtn != null) commonBuildRightBtn.onClick.RemoveListener(OnCommonBuildRightBtnClick);
    }

    private void CleanupFastBuilding()
    {
        if (fastBuildingRefreshTimerId != 0)
        {
            Tick.ClearTimeout(fastBuildingRefreshTimerId);
            fastBuildingRefreshTimerId = 0;
        }

        fastBuildingAvailablePlaces.Clear();
        fastBuildingCurrentIndex = 0;
        fastBuildingCurrentPlaceId = 0;
        fastBuildingSpriteRequestId++;
    }

    private void StartFastBuildingRefresh()
    {
        if (fastBuildingRefreshTimerId != 0)
        {
            Tick.ClearTimeout(fastBuildingRefreshTimerId);
        }

        fastBuildingRefreshTimerId = Tick.SetTimeout(RefreshFastBuildingButtons, FAST_BUILDING_REFRESH_INTERVAL);
    }

    private void RefreshFastBuildingButtons()
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null)
        {
            commonBuildRoot?.SetActive(false);
            StartFastBuildingRefresh();
            return;
        }

        int localPlayerCampId = EcsUtils.GetLocalPlayerCampId(game);
        int previousPlaceId = fastBuildingCurrentPlaceId;

        fastBuildingAvailablePlaces.Clear();
        fastBuildingAvailablePlaces.AddRange(GetAvailableFastBuildingPlaces(game, localPlayerCampId));

        if (fastBuildingAvailablePlaces.Count == 0)
        {
            fastBuildingCurrentIndex = 0;
            fastBuildingCurrentPlaceId = 0;
            commonBuildRoot?.SetActive(false);
            StartFastBuildingRefresh();
            return;
        }

        fastBuildingCurrentIndex = GetFastBuildingDisplayIndex(previousPlaceId);
        ConfBuildingPlace currentPlace = fastBuildingAvailablePlaces[fastBuildingCurrentIndex];
        fastBuildingCurrentPlaceId = currentPlace.ID;

        commonBuildRoot?.SetActive(true);
        UpdateFastBuildingViewAsync(currentPlace);
        StartFastBuildingRefresh();
    }

    private List<ConfBuildingPlace> GetAvailableFastBuildingPlaces(BattleGame game, int localPlayerCampId)
    {
        List<ConfBuildingPlace> result = new();
        List<ConfBuildingPlace> allBuildingPlaces = ConfigManager.GetAll<ConfBuildingPlace>();

        foreach (ConfBuildingPlace confBuildingPlace in allBuildingPlaces)
        {
            if (confBuildingPlace.Enabled == 0) continue;
            if (confBuildingPlace.CampID != localPlayerCampId) continue;
            if (IsFastProductionBuildingType(confBuildingPlace.Type)) continue;

            ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingPlace.Type);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[快捷建造] 未找到建筑配置, BuildingType={confBuildingPlace.Type}");
                continue;
            }

            int currentCount = GetFastBuildingCountByType(game, confBuildingPlace.Type, localPlayerCampId);
            if (currentCount >= confBuildingPlace.Count) continue;

            result.Add(confBuildingPlace);
        }

        return result;
    }

    private bool IsFastProductionBuildingType(int buildingType)
    {
        return buildingType == 5 || buildingType == 6 || buildingType == 7;
    }

    private int GetFastBuildingDisplayIndex(int previousPlaceId)
    {
        if (previousPlaceId != 0)
        {
            for (int i = 0; i < fastBuildingAvailablePlaces.Count; i++)
            {
                if (fastBuildingAvailablePlaces[i].ID == previousPlaceId)
                {
                    return i;
                }
            }
        }

        if (fastBuildingCurrentIndex < 0) return 0;
        if (fastBuildingCurrentIndex >= fastBuildingAvailablePlaces.Count) return fastBuildingAvailablePlaces.Count - 1;
        return fastBuildingCurrentIndex;
    }

    private async void UpdateFastBuildingViewAsync(ConfBuildingPlace confBuildingPlace)
    {
        ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingPlace.Type);
        if (confBuilding == null)
        {
            commonBuildRoot?.SetActive(false);
            return;
        }

        if (commonBuildCostValueText != null)
        {
            commonBuildCostValueText.text = $"${confBuilding.CostMoney}";
        }

        if (commonBuildNameText != null)
        {
            commonBuildNameText.text = confBuilding.Name;
        }

        int requestId = ++fastBuildingSpriteRequestId;
        Sprite sprite = await AssetManager.GetSpriteAsync(confBuildingPlace.Icon);
        if (requestId != fastBuildingSpriteRequestId) return;
        if (fastBuildingCurrentPlaceId != confBuildingPlace.ID) return;

        if (commonBuildImage != null && sprite != null)
        {
            commonBuildImage.sprite = sprite;
        }
    }

    private void OnCommonBuildBtnClick()
    {
        UISound.Instance.PlayClick();
        TryCreateCurrentFastBuilding();
    }

    private void OnCommonBuildLeftBtnClick()
    {
        UISound.Instance.PlayClick();
        SwitchFastBuilding(-1);
    }

    private void OnCommonBuildRightBtnClick()
    {
        UISound.Instance.PlayClick();
        SwitchFastBuilding(1);
    }

    private void SwitchFastBuilding(int offset)
    {
        if (fastBuildingAvailablePlaces.Count == 0) return;

        fastBuildingCurrentIndex += offset;
        if (fastBuildingCurrentIndex < 0)
        {
            fastBuildingCurrentIndex = fastBuildingAvailablePlaces.Count - 1;
        }
        else if (fastBuildingCurrentIndex >= fastBuildingAvailablePlaces.Count)
        {
            fastBuildingCurrentIndex = 0;
        }

        ConfBuildingPlace currentPlace = fastBuildingAvailablePlaces[fastBuildingCurrentIndex];
        fastBuildingCurrentPlaceId = currentPlace.ID;
        UpdateFastBuildingViewAsync(currentPlace);
    }

    private bool TryCreateCurrentFastBuilding()
    {
        var game = Ra2Demo?.GetBattleGame();
        if (game == null) return false;
        if (fastBuildingCurrentPlaceId == 0) return false;

        ConfBuildingPlace confBuildingPlace = ConfigManager.Get<ConfBuildingPlace>(fastBuildingCurrentPlaceId);
        if (confBuildingPlace == null)
        {
            zUDebug.LogError($"[快捷建造] 未找到建筑摆放配置, ConfBuildingPlaceID={fastBuildingCurrentPlaceId}");
            MessageUtils.ShowTips("未找到建筑配置");
            RefreshFastBuildingButtons();
            return false;
        }

        int localPlayerCampId = EcsUtils.GetLocalPlayerCampId(game);
        if (confBuildingPlace.Enabled == 0 || confBuildingPlace.CampID != localPlayerCampId)
        {
            RefreshFastBuildingButtons();
            return false;
        }

        ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingPlace.Type);
        if (confBuilding == null)
        {
            zUDebug.LogError($"[快捷建造] 未找到建筑配置, BuildingType={confBuildingPlace.Type}");
            MessageUtils.ShowTips("未找到建筑配置");
            RefreshFastBuildingButtons();
            return false;
        }

        int currentCount = GetFastBuildingCountByType(game, confBuildingPlace.Type, localPlayerCampId);
        if (currentCount >= confBuildingPlace.Count)
        {
            MessageUtils.ShowTips("建筑数量已达上限");
            RefreshFastBuildingButtons();
            return false;
        }

        Vector3[] positions = StringUtils.ParsePositions(confBuildingPlace.Position);
        if (positions == null || positions.Length == 0)
        {
            zUDebug.LogError($"[快捷建造] 建筑位置配置无效: {confBuildingPlace.Position}, BuildingType={confBuildingPlace.Type}");
            MessageUtils.ShowTips("建筑位置配置无效");
            RefreshFastBuildingButtons();
            return false;
        }

        Vector3 logicPosition = positions[currentCount % positions.Length];
        var createBuildingCommand = new CreateBuildingCommand(
            confID: confBuildingPlace.Type,
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
        zUDebug.Log($"[快捷建造] 发送建造命令, BuildingType={confBuildingPlace.Type}, ConfBuildingPlaceID={confBuildingPlace.ID}, Position={logicPosition}");
        RefreshFastBuildingButtons();
        return true;
    }

    private int GetFastBuildingCountByType(BattleGame game, int buildingType, int localPlayerCampId)
    {
        var componentManager = game.World.ComponentManager;
        int count = 0;

        var buildingEntities = componentManager.GetAllEntityIdsWith<BuildingComponent>();
        foreach (int entityId in buildingEntities)
        {
            var entity = new Entity(entityId);
            if (!componentManager.HasComponent<CampComponent>(entity)) continue;

            var campComponent = componentManager.GetComponent<CampComponent>(entity);
            if (campComponent.CampId != localPlayerCampId) continue;

            var buildingComponent = componentManager.GetComponent<BuildingComponent>(entity);
            if (buildingComponent.BuildingType == buildingType) count++;
        }

        return count;
    }
}
