using UnityEngine;
using UnityEngine.InputSystem;

public class VrStylusHandler : StylusHandler
{
    [SerializeField] private GameObject _mxInk_model;
    [SerializeField] private GameObject _tip;
    [SerializeField] private GameObject _cluster_front;
    [SerializeField] private GameObject _cluster_middle;
    [SerializeField] private GameObject _cluster_back;

    [SerializeField] private GameObject _left_touch_controller;
    [SerializeField] private GameObject _right_touch_controller;

    public Color active_color = Color.green;
    public Color double_tap_active_color = Color.cyan;
    public Color default_color = Color.white;

    private float _hapticClickDuration = 0.011f;
    private float _hapticClickAmplitude = 1.0f;

    [SerializeField]
    private InputActionReference _middleActionRef;
    [SerializeField]
    private InputActionReference _tipActionRef;
    [SerializeField]
    private InputActionReference _grabActionRef;
    [SerializeField]
    private InputActionReference _optionActionRef;

    private void Awake()
    {
        _tipActionRef.action.Enable();
        _grabActionRef.action.Enable();
        _optionActionRef.action.Enable();
        _middleActionRef.action.Enable();

        _stylus.isActive = false;
        InputSystem.onDeviceChange += OnDeviceChange;
        UnityEngine.XR.InputDevices.deviceConnected += DeviceConnected;
    }

    private void DeviceConnected(UnityEngine.XR.InputDevice device)
    {
        Debug.Log($"Device connected: {device.name}");
        bool mxInkConnected = device.name.ToLower().Contains("logitech");
        if (mxInkConnected)
        {
            _stylus.isOnRightHand = (device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Right) != 0;
            _stylus.isActive = true;
        }
    }
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device.name.ToLower().Contains("logitech"))
        {
            switch (change)
            {
                case InputDeviceChange.Disconnected:
                    _tipActionRef.action.Disable();
                    _grabActionRef.action.Disable();
                    _optionActionRef.action.Disable();
                    _middleActionRef.action.Disable();
                    _stylus.isActive = false;
                    break;
                case InputDeviceChange.Reconnected:
                    _tipActionRef.action.Enable();
                    _grabActionRef.action.Enable();
                    _optionActionRef.action.Enable();
                    _middleActionRef.action.Enable();
                    _stylus.isActive = true;
                    break;
            }
        }
        _mxInk_model.SetActive(_stylus.isActive);
        _left_touch_controller.SetActive(!_stylus.isActive || _stylus.isOnRightHand);
        _right_touch_controller.SetActive(!_stylus.isActive || !_stylus.isOnRightHand);
    }

    void Update()
    {
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(_stylus.isOnRightHand ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand);
        GetControllerTransform(device);
        _stylus.inkingPose.position = transform.position;
        _stylus.inkingPose.rotation = transform.rotation;
        _stylus.tip_value = _tipActionRef.action.ReadValue<float>();
        _stylus.cluster_middle_value = _middleActionRef.action.ReadValue<float>();
        _stylus.cluster_front_value = _grabActionRef.action.IsPressed();
        _stylus.cluster_back_value = _optionActionRef.action.IsPressed();

        _stylus.any = _stylus.tip_value > 0 || _stylus.cluster_front_value ||
                        _stylus.cluster_middle_value > 0 || _stylus.cluster_back_value ||
                        _stylus.cluster_back_double_tap_value;

        _tip.GetComponent<MeshRenderer>().material.color = _stylus.tip_value > 0 ? active_color : default_color;
        _cluster_front.GetComponent<MeshRenderer>().material.color = _stylus.cluster_front_value ? active_color : default_color;
        _cluster_middle.GetComponent<MeshRenderer>().material.color = _stylus.cluster_middle_value > 0 ? active_color : default_color;
        _cluster_back.GetComponent<MeshRenderer>().material.color = _stylus.cluster_back_value ? active_color : default_color;

    }

    void GetControllerTransform(UnityEngine.XR.InputDevice device)
    {
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 position))
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rotation))
            {
                // Apply transform to a GameObject if needed
                transform.position = position;
                transform.rotation = rotation;
            }
        }
    }

    public void TriggerHapticPulse(float amplitude, float duration)
    {
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(_stylus.isOnRightHand ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand);
        device.SendHapticImpulse(0, amplitude, duration);
    }

    public void TriggerHapticClick()
    {
        TriggerHapticPulse(_hapticClickAmplitude, _hapticClickDuration);
    }
}
