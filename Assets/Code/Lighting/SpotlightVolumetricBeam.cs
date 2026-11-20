using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SpotlightVolumetricBeam : MonoBehaviour
{
    private static readonly int BeamColorPropertyId = Shader.PropertyToID("_BeamColor");
    private static readonly int IntensityPropertyId = Shader.PropertyToID("_Intensity");
    private static readonly int TransparencyPropertyId = Shader.PropertyToID("_Transparency");

    [Tooltip("Spot Light that drives the beam direction, length, cone angle, color, and intensity.")]
    [SerializeField] private Light spotLight;

    [Tooltip("Visual cone angle in degrees. The beam is clamped to the Spot Light's outer angle.")]
    [SerializeField, Range(1f, 179f)] private float beamAngle = 52f;

    [Tooltip("Additional warm tint applied to the Spot Light color.")]
    [SerializeField] private Color beamColor = new Color(1f, 0.76f, 0.46f, 1f);

    [Tooltip("Scales the Spot Light intensity for the visible beam.")]
    [SerializeField, Min(0f)] private float beamIntensity = 0.55f;

    [Tooltip("Controls the additive transparency of the beam volume.")]
    [SerializeField, Range(0f, 1f)] private float transparency = 0.1f;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private float _lastRange;
    private float _lastSpotAngle;
    private Color _lastColor;
    private float _lastIntensity;
    private float _lastTransparency;
    private bool _shapeInitialized;
    private bool _appearanceInitialized;

    private void OnEnable()
    {
        CacheComponents();
        _shapeInitialized = false;
        _appearanceInitialized = false;
        RefreshBeam();
    }

    private void CacheComponents()
    {
        TryGetComponent(out _meshFilter);
        TryGetComponent(out _meshRenderer);

        if (spotLight == null)
        {
            spotLight = GetComponentInParent<Light>();
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void RefreshBeam()
    {
        if (_meshRenderer == null)
        {
            return;
        }

        if (spotLight == null || _meshFilter == null)
        {
            _meshRenderer.enabled = false;
            return;
        }

        bool isVisible = spotLight.type == LightType.Spot
            && spotLight.enabled
            && spotLight.gameObject.activeInHierarchy
            && _meshFilter.sharedMesh != null
            && _meshRenderer.sharedMaterial != null;

        _meshRenderer.enabled = isVisible;
        if (!isVisible)
        {
            return;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        UpdateBeamShape();
        UpdateBeamAppearance();
    }

    private void UpdateBeamShape()
    {
        float range = Mathf.Max(0.01f, spotLight.range);
        float spotAngle = Mathf.Clamp(Mathf.Min(beamAngle, spotLight.spotAngle), 0.1f, 179f);
        if (_shapeInitialized && Mathf.Approximately(range, _lastRange) && Mathf.Approximately(spotAngle, _lastSpotAngle))
        {
            return;
        }

        float baseRadius = Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * range;
        transform.localScale = new Vector3(baseRadius, baseRadius, range);
        _lastRange = range;
        _lastSpotAngle = spotAngle;
        _shapeInitialized = true;
    }

    private void UpdateBeamAppearance()
    {
        Color spotColor = spotLight.color;
        Color effectiveColor = new Color(
            beamColor.r * spotColor.r,
            beamColor.g * spotColor.g,
            beamColor.b * spotColor.b,
            beamColor.a);
        float effectiveIntensity = beamIntensity * spotLight.intensity;

        if (_appearanceInitialized
            && effectiveColor == _lastColor
            && Mathf.Approximately(effectiveIntensity, _lastIntensity)
            && Mathf.Approximately(transparency, _lastTransparency))
        {
            return;
        }

        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BeamColorPropertyId, effectiveColor);
        _propertyBlock.SetFloat(IntensityPropertyId, effectiveIntensity);
        _propertyBlock.SetFloat(TransparencyPropertyId, transparency);
        _meshRenderer.SetPropertyBlock(_propertyBlock);

        _lastColor = effectiveColor;
        _lastIntensity = effectiveIntensity;
        _lastTransparency = transparency;
        _appearanceInitialized = true;
    }

    private void LateUpdate()
    {
        RefreshBeam();
    }

    private void OnDisable()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = false;
        }
    }
}
