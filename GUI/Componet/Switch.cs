namespace HaxxToyBox.GUI;

[RegisterInIl2Cpp]
public class Switch : MonoBehaviour
{
    public event Action<bool> OnChanged;

    private Button _button;
    private Animator _animator;

    private Image _bgEnabledImage;
    private Image _bgDisabledImage;

    private Image _handleEnabledImage;
    private Image _handleDisabledImage;

    private bool _switchEnabled;
    private bool _refreshVisuals;

    public Switch(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _animator = GetComponent<Animator>();

        _bgEnabledImage = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        _bgDisabledImage = transform.GetChild(0).GetChild(1).GetComponent<Image>();
        _handleEnabledImage = transform.GetChild(1).GetChild(0).GetComponent<Image>();
        _handleDisabledImage = transform.GetChild(1).GetChild(1).GetComponent<Image>();

        _switchEnabled = false;
        ApplyState(true);
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(Toggle);
        _refreshVisuals = true;
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(Toggle);
        _button.onClick.RemoveAllListeners();
    }

    private void LateUpdate()
    {
        if (!_refreshVisuals) return;

        _refreshVisuals = false;
        ApplyState(false);
    }

    public void Toggle()
    {
        SetToggled(!_switchEnabled);
    }

    public void SetToggled(bool value, bool notify = true)
    {
        if (_switchEnabled == value) return;

        _switchEnabled = value;
        ApplyState(true);
        _refreshVisuals = true;

        if (notify) {
            OnChanged?.Invoke(_switchEnabled);
        }
    }

    private void ApplyState(bool animate)
    {
        if (_switchEnabled) {
            _bgDisabledImage.gameObject.SetActive(false);
            _bgEnabledImage.gameObject.SetActive(true);
            _handleDisabledImage.gameObject.SetActive(false);
            _handleEnabledImage.gameObject.SetActive(true);
        }
        else {
            _bgEnabledImage.gameObject.SetActive(false);
            _bgDisabledImage.gameObject.SetActive(true);
            _handleEnabledImage.gameObject.SetActive(false);
            _handleDisabledImage.gameObject.SetActive(true);
        }

        if (!animate) return;

        _animator.ResetTrigger(_switchEnabled ? "Disable" : "Enable");
        _animator.SetTrigger(_switchEnabled ? "Enable" : "Disable");
    }

    public bool IsToggled()
    {
        return _switchEnabled;
    }

}
