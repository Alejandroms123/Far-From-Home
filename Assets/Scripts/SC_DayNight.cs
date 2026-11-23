using TMPro;
using UnityEngine;

public class SC_DayNight : MonoBehaviour
{
    [Header("Clock (hours, 0-24)")]
    [Range(0f, 24f)] public float TimeOfDay = 12f;

    [Header("Time Speed (in-game minutes per real second)")]
    [SerializeField] private float _timeSpeed = 12f;

    [Space(20)]
    [Header("UI")]
    [SerializeField] private TMP_Text _clockText;

    [Space(20)]
    [Header("Sun Hours")]
    [SerializeField, Range(0f, 24f)] private float _sunriseHour = 6f;
    [SerializeField, Range(0f, 24f)] private float _sunsetHour = 18f;

    [Space(20)]
    [Header("Skybox")]
    [SerializeField] private Material _skybox;

    [Space(10)]
    [SerializeField] private Color _skyboxSunriseColor1;
    [SerializeField] private Color _skyboxSunriseColor2;

    [Space(10)]
    [SerializeField] private Color _skyboxDayColor1;
    [SerializeField] private Color _skyboxDayColor2;

    [Space(10)]
    [SerializeField] private Color _skyboxSunsetColor1;
    [SerializeField] private Color _skyboxSunsetColor2;

    [Space(10)]
    [SerializeField] private Color _skyboxNightColor1;
    [SerializeField] private Color _skyboxNightColor2;

    [Space(20)]
    [Header("Water")]
    [SerializeField] private Material _waterMaterial;

    [Space(20)]
    [Header("Day/Night Change (hours)")]
    [SerializeField, Range(0f, 24f)] private float _lowerIntensityHour = 18f;
    [SerializeField, Range(0f, 24f)] private float _raiseIntensityHour = 6f;

    [Space(20)]
    [Header("Sunrise Colour Transition (hours)")]
    [SerializeField, Min(0f)] private float _nightToSunriseLength = 1f;
    [SerializeField, Min(0f)] private float _sunriseToDayLength = 1f;

    [Space(10)]
    [Header("Sunset Colour Transition (hours)")]
    [SerializeField, Min(0f)] private float _dayToSunsetLength = 1f;
    [SerializeField, Min(0f)] private float _sunsetToNightLength = 1f;

    public int Hours => TotalMinutes / 60;
    public int Minutes => TotalMinutes % 60;
    public string TimeText => $"{Hours:00}:{Minutes:00}";

    private const float HoursPerDay = 24f;

    private static readonly int Color1Id = Shader.PropertyToID("_Color1");
    private static readonly int Color2Id = Shader.PropertyToID("_Color2");
    private static readonly int SpumeColorId = Shader.PropertyToID("_SpumeColor");

    private Transform _sunTr;
    private Light _sunLight;
    private Vector3 _initialSunEuler;
    private Color _originalSpumeColor;

    private int _lastDisplayedMinute = -1;

    private int TotalMinutes => Mathf.FloorToInt(TimeOfDay * 60f) % 1440;

    private void Awake()
    {
        if (_waterMaterial != null)
            _originalSpumeColor = _waterMaterial.GetColor(SpumeColorId);
    }

    // The material is an asset, so its original colour is restored when the game stops.
    private void OnDestroy()
    {
        if (_skybox != null)
        {
            _skybox.SetColor(Color1Id, _skyboxDayColor1);
            _skybox.SetColor(Color2Id, _skyboxDayColor2);
        }

        if (_waterMaterial != null)
            _waterMaterial.SetColor(SpumeColorId, _originalSpumeColor);
    }

    private void Start()
    {
        _sunTr = transform;
        _sunLight = GetComponent<Light>();
        // Y and Z are kept from the rotation set in the Inspector; X is driven by TimeOfDay.
        _initialSunEuler = _sunTr.localEulerAngles;

        Apply();
    }

    private void Update()
    {
        // _timeSpeed is in minutes per second, so it is divided by 60 to get hours.
        TimeOfDay += Time.deltaTime * _timeSpeed / 60f;

        Apply();
        UpdateClockText();
    }

    private void Apply()
    {
        TimeOfDay = Mathf.Repeat(TimeOfDay, HoursPerDay);

        RotateSun();
        UpdateSunIntensity();
        UpdateSkyboxColors();
        UpdateWaterSpume();
    }

    // The text only changes when the in-game minute changes.
    private void UpdateClockText()
    {
        if (_clockText == null || TotalMinutes == _lastDisplayedMinute)
            return;

        _lastDisplayedMinute = TotalMinutes;
        _clockText.text = TimeText;
    }

    private void RotateSun()
    {
        // The sun is at 0 degrees at sunrise and at 180 degrees at sunset.
        // Always between 0.01 and 23.99 hours to avoid dividing by zero.
        float dayHours = Mathf.Clamp(Mathf.Repeat(_sunsetHour - _sunriseHour, HoursPerDay), 0.01f, HoursPerDay - 0.01f);
        float nightHours = HoursPerDay - dayHours;
        float hoursSinceSunrise = Mathf.Repeat(TimeOfDay - _sunriseHour, HoursPerDay);

        float angle;

        if (hoursSinceSunrise < dayHours)
            angle = hoursSinceSunrise / dayHours * 180f;
        else
            angle = 180f + (hoursSinceSunrise - dayHours) / nightHours * 180f;

        _sunTr.localRotation = Quaternion.Euler(angle, _initialSunEuler.y, _initialSunEuler.z);
    }

    // It is "day" from _raiseIntensityHour until _lowerIntensityHour.
    private bool IsDay()
    {
        if (_raiseIntensityHour <= _lowerIntensityHour)
            return TimeOfDay >= _raiseIntensityHour && TimeOfDay < _lowerIntensityHour;

        return TimeOfDay >= _raiseIntensityHour || TimeOfDay < _lowerIntensityHour;
    }

    private void UpdateSunIntensity()
    {
        if (_sunLight == null)
            return;

        _sunLight.intensity = IsDay() ? 1f : 0f;
    }

    // Hours between TimeOfDay and a moment of the day (negative = before it, positive = after it).
    private float GetHourOffset(float hour)
    {
        float half = HoursPerDay * 0.5f;

        return Mathf.Repeat(TimeOfDay - hour + half, HoursPerDay) - half;
    }

    // offset < 0: blends from -> peak during the "before" hours.
    // offset >= 0: blends peak -> to during the "after" hours.
    private static Color Blend(Color from, Color peak, Color to, float offset, float before, float after)
    {
        if (offset < 0f)
            return Color.Lerp(from, peak, Mathf.InverseLerp(-before, 0f, offset));

        return Color.Lerp(peak, to, Mathf.InverseLerp(0f, after, offset));
    }

    // 1 = day, 0 = night. It only changes during the night side of the transitions:
    // fades out after _lowerIntensityHour (sunset -> night) and fades in before _raiseIntensityHour (night -> sunrise).
    private float GetWaterFactor()
    {
        float sunriseOffset = GetHourOffset(_raiseIntensityHour);
        float sunsetOffset = GetHourOffset(_lowerIntensityHour);

        if (sunriseOffset > -_nightToSunriseLength && sunriseOffset < 0f)
            return Mathf.InverseLerp(-_nightToSunriseLength, 0f, sunriseOffset);

        if (sunsetOffset >= 0f && sunsetOffset < _sunsetToNightLength)
            return Mathf.InverseLerp(_sunsetToNightLength, 0f, sunsetOffset);

        return IsDay() ? 1f : 0f;
    }

    private void UpdateWaterSpume()
    {
        if (_waterMaterial == null)
            return;

        Color color = _originalSpumeColor * GetWaterFactor();
        color.a = _originalSpumeColor.a;

        _waterMaterial.SetColor(SpumeColorId, color);
    }

    private void UpdateSkyboxColors()
    {
        if (_skybox == null)
            return;

        // The sunrise/sunset colour is at its peak exactly when the sun intensity changes.
        float sunriseOffset = GetHourOffset(_raiseIntensityHour);
        float sunsetOffset = GetHourOffset(_lowerIntensityHour);

        Color color1;
        Color color2;

        if (sunriseOffset > -_nightToSunriseLength && sunriseOffset < _sunriseToDayLength)
        {
            // Night -> Sunrise -> Day
            color1 = Blend(_skyboxNightColor1, _skyboxSunriseColor1, _skyboxDayColor1, sunriseOffset, _nightToSunriseLength, _sunriseToDayLength);
            color2 = Blend(_skyboxNightColor2, _skyboxSunriseColor2, _skyboxDayColor2, sunriseOffset, _nightToSunriseLength, _sunriseToDayLength);
        }
        else if (sunsetOffset > -_dayToSunsetLength && sunsetOffset < _sunsetToNightLength)
        {
            // Day -> Sunset -> Night
            color1 = Blend(_skyboxDayColor1, _skyboxSunsetColor1, _skyboxNightColor1, sunsetOffset, _dayToSunsetLength, _sunsetToNightLength);
            color2 = Blend(_skyboxDayColor2, _skyboxSunsetColor2, _skyboxNightColor2, sunsetOffset, _dayToSunsetLength, _sunsetToNightLength);
        }
        else if (IsDay())
        {
            color1 = _skyboxDayColor1;
            color2 = _skyboxDayColor2;
        }
        else
        {
            color1 = _skyboxNightColor1;
            color2 = _skyboxNightColor2;
        }

        _skybox.SetColor(Color1Id, color1);
        _skybox.SetColor(Color2Id, color2);
    }
}
