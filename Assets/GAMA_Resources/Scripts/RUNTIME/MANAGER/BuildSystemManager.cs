using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Quản lý hệ thống xây dựng dựa trên SurfaceConnector.
/// Flow: Nhấn Button (vd: Button_Pump) → icon "!" hiện trên ray tay phải
///       → Ray chạm SurfaceConnector → ghost hiện tại vị trí đó
///       → Nhấn cò (trigger) → xây dựng construction tại vị trí đã chọn.
/// </summary>
public class BuildSystemManager : MonoBehaviour
{
    [SerializeField] private List<ConstructionSO> constructions;
    [SerializeField] private BuildUI buildIU;

    [Header("Build Ray (XR Ray Interactor)")]
    [SerializeField] private XRRayInteractor buildRayInteractor;

    [Header("Build Mode Indicator")]
    [Tooltip("Icon '!' hiển thị trên ray tay phải khi đang ở chế độ xây dựng")]
    [SerializeField] private GameObject buildModeIndicator;

    [Header("Connector Detection")]
    [Tooltip("LayerMask cho SurfaceConnector (BuildConnector layer)")]
    [SerializeField] private LayerMask connectorLayerMask;
    [Tooltip("Bán kính tìm kiếm Connector xung quanh điểm raycast để dễ thao tác hơn")]
    [SerializeField] private float connectorDetectionRadius = 1.5f;

    [SerializeField] private SubsidenceManager subsidenceManager;

    private bool isBuilding = false;
    private int currentBuildingIndex = 0;
    private GameObject ghostConstruction;
    private Connector currentTargetConnector;

    // Getters
    public bool IsBuilding => isBuilding;
    public List<ConstructionSO> Constructions => constructions;

    public void Start()
    {
        foreach (ConstructionSO c in constructions)
        {
            c.Init();
        }
        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(false);
    }

    private void Update()
    {
        UpdateCooldowns(Time.deltaTime);
        if (isBuilding)
        {
            ProcessBuilding();
        }
    }

    /// <summary>
    /// Bắt đầu chế độ xây dựng cho construction tại index.
    /// Gọi từ BuildUI khi nhấn Button (vd: Button_Pump index=1).
    /// </summary>
    public void StartBuilding(int constructionIndex)
    {
        if (!IsBuildable(constructionIndex))
        {
            Debug.Log($"[BuildSystem] Cannot build index {constructionIndex}: cooldown or no quantity");
            return;
        }

        // Nếu đang build thứ khác, huỷ trước
        if (isBuilding)
            CancelBuilding();

        isBuilding = true;
        currentBuildingIndex = constructionIndex;
        currentTargetConnector = null;

        // Hiện icon "!" trên ray tay phải
        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(true);

        Debug.Log($"[BuildSystem] Build mode ON - construction: {constructions[constructionIndex].name}");
    }

    /// <summary>
    /// Huỷ chế độ xây dựng (nhấn nút cancel hoặc chọn construction khác).
    /// </summary>
    public void CancelBuilding()
    {
        DestroyGhost();
        isBuilding = false;
        currentTargetConnector = null;

        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(false);
    }

    public bool IsBuildable(int constructionIndex)
    {
        ConstructionSO construction = constructions[constructionIndex];
        return construction.CurrentTime <= 0 && construction.CurrentQuantity > 0;
    }

    /// <summary>
    /// Xác nhận xây dựng tại vị trí SurfaceConnector đang chọn.
    /// Gọi khi nhấn cò (trigger) từ InputManager.
    /// </summary>
    public void Build()
    {
        if (!isBuilding || ghostConstruction == null || currentTargetConnector == null) return;

        var ghost = ghostConstruction.GetComponent<GhostConstruction>();
        if (ghost == null || !ghost.IsBuildable) return;

        // Tạo construction thật tại vị trí ghost
        GameObject construction = Instantiate(
            constructions[currentBuildingIndex].finalPrefab,
            ghostConstruction.transform.position,
            ghost.UseIdentityRotation ? Quaternion.identity : ghostConstruction.transform.rotation
        );

        var remover = construction.GetComponent<ConstructionRemover>();
        if (remover != null)
            remover.buildSystemManager = this;

        // Cập nhật cooldown và số lượng
        constructions[currentBuildingIndex].ResetCooldown();
        constructions[currentBuildingIndex].DecreaseQuantity();
        buildIU.ImageCooldownList[currentBuildingIndex].fillAmount = 1;

        // Disable connectors đã sử dụng
        Connector[] connectors = ghostConstruction.GetComponentsInChildren<Connector>();
        foreach (Connector connector in connectors)
        {
            connector.UpdateConnector(false);
        }

        // Hiệu ứng game
        subsidenceManager.IncreaseSubsidenceLevel();
        subsidenceManager.DecreaseWaterLevel();

        // Thống kê
        UpdateStatistics(currentBuildingIndex);
        StatisticsManager.Instance.AddActionHistory(
            "Build",
            constructions[currentBuildingIndex].name,
            ghostConstruction.transform.position
        );

        FinishBuilding();
    }

    public string GetConstructionInfo(int constructionIndex)
    {
        return constructions[constructionIndex].description;
    }

    // ========== PRIVATE METHODS ==========

    /// <summary>
    /// Mỗi frame khi đang ở chế độ build:
    /// 1. Raycast từ tay phải
    /// 2. Nếu chạm SurfaceConnector (trực tiếp hoặc gần) → hiện ghost tại đó
    /// 3. Nếu không chạm → ẩn ghost
    /// </summary>
    private void ProcessBuilding()
    {
        if (buildRayInteractor == null) return;

        if (buildRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // 1. Kiểm tra hit trực tiếp lên Connector
            Connector hitConnector = hit.collider.GetComponent<Connector>();
            if (hitConnector != null && hitConnector.canConnectTo)
            {
                SetTargetConnector(hitConnector);
                return;
            }

            // 2. Tìm Connector gần điểm raycast (OverlapSphere) → dễ thao tác hơn
            Collider[] nearby = Physics.OverlapSphere(hit.point, connectorDetectionRadius, connectorLayerMask);
            Connector bestConnector = FindBestConnector(nearby);
            if (bestConnector != null)
            {
                SetTargetConnector(bestConnector);
                return;
            }
        }

        // Không tìm thấy Connector hợp lệ → ẩn ghost
        ClearTargetConnector();
    }

    private Connector FindBestConnector(Collider[] colliders)
    {
        foreach (Collider col in colliders)
        {
            Connector connector = col.GetComponent<Connector>();
            if (connector != null && connector.canConnectTo)
                return connector;
        }
        return null;
    }

    /// <summary>
    /// Khi tìm thấy Connector hợp lệ → tạo/hiện ghost tại vị trí Connector.
    /// Chỉ reposition ghost khi target Connector thay đổi (tránh xung đột với GhostConstruction snap).
    /// </summary>
    private void SetTargetConnector(Connector connector)
    {
        bool connectorChanged = (connector != currentTargetConnector);
        currentTargetConnector = connector;

        if (ghostConstruction == null)
        {
            ghostConstruction = Instantiate(constructions[currentBuildingIndex].modelBuildPrefab);
            ghostConstruction.transform.position = connector.transform.position;
            ghostConstruction.transform.rotation = connector.transform.rotation;
        }
        else if (connectorChanged)
        {
            // Di chuyển ghost đến connector mới, GhostConstruction sẽ snap chính xác
            ghostConstruction.transform.position = connector.transform.position;
            ghostConstruction.transform.rotation = connector.transform.rotation;
        }

        if (!ghostConstruction.activeSelf)
            ghostConstruction.SetActive(true);
    }

    private void ClearTargetConnector()
    {
        currentTargetConnector = null;
        if (ghostConstruction != null && ghostConstruction.activeSelf)
            ghostConstruction.SetActive(false);
    }

    private void FinishBuilding()
    {
        DestroyGhost();
        isBuilding = false;
        currentTargetConnector = null;

        if (buildModeIndicator != null)
            buildModeIndicator.SetActive(false);
    }

    private void DestroyGhost()
    {
        if (ghostConstruction != null)
        {
            Destroy(ghostConstruction);
            ghostConstruction = null;
        }
    }

    private void UpdateStatistics(int index)
    {
        switch (index)
        {
            case 0: StatisticsManager.Instance.IncreateSluiceGateCount(); break;
            case 1: StatisticsManager.Instance.IncreateWaterPumpCount(); break;
            case 2: StatisticsManager.Instance.IncreateLakeCount(); break;
        }
    }

    private void UpdateCooldowns(float deltaTime)
    {
        foreach (var construction in constructions)
        {
            construction.DecreaseCooldown(deltaTime);
        }
    }
}
