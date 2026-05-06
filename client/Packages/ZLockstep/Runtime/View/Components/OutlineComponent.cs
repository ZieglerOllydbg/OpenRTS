using UnityEngine;

public class OutlineComponent : MonoBehaviour
{
    private const string RingPrefabPath = "Ring";

    [Header("Outline Settings")]
    [SerializeField] private GameObject ringInstance;
    [SerializeField] private Vector3 ringLocalPosition = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 ringLocalEulerAngles = Vector3.zero;

    private bool isRingVisible = false;
    private bool isMountingRing;
    private float ringSizeMultiplier = 1f;


    private void Awake()
    {
        EnsureRingMounted();
    }

    public void SetRingSizeMultiplier(float multiplier)
    {
        ringSizeMultiplier = multiplier;
        ApplyRingTransform();
    }

    public void ShowCircle()
    {
        isRingVisible = true;
        EnsureRingMounted();
        ApplyRingTransform();
        SetRingVisible(true);
    }

    public void SetRingLocalPosition(Vector3 localPosition)
    {
        ringLocalPosition = localPosition;
        ApplyRingTransform();
    }

    public void SetRingLocalEulerAngles(Vector3 localEulerAngles)
    {
        ringLocalEulerAngles = localEulerAngles;
        ApplyRingTransform();
    }

    public void HideCircle()
    {
        isRingVisible = false;
        EnsureRingMounted();
        SetRingVisible(false);
    }

    private async void EnsureRingMounted()
    {
        if (ringInstance != null)
            return;

        if (isMountingRing)
            return;

        isMountingRing = true;
        var prefab = await AssetManager.InstantiatePrefabAsync(RingPrefabPath, transform);
        isMountingRing = false;

        ringInstance = prefab;

        ApplyRingTransform();
        SetRingVisible(isRingVisible);
    }

    private void SetRingVisible(bool visible)
    {
        if (ringInstance != null)
        {
            ringInstance.SetActive(visible);
        }
    }

    private void ApplyRingTransform()
    {
        if (ringInstance == null)
            return;

        Transform ringTransform = ringInstance.transform;
        ringTransform.localPosition = ringLocalPosition;
        ringTransform.localRotation = Quaternion.Euler(ringLocalEulerAngles);
        ringTransform.localScale = Vector3.one * ringSizeMultiplier;
    }

}
