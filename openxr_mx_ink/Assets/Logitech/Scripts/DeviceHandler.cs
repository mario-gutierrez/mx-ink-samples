using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class DeviceHandler : MonoBehaviour
{
    public MxInkHandler MxInkStylus;
    [SerializeField] private GameObject _leftHand;
    [SerializeField] private GameObject _rightHand;
    private void Awake()
    {
        InputDevices.deviceConnected += DeviceConnected;
        InputDevices.deviceDisconnected += DeviceDisconnected;
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        foreach (InputDevice device in devices)
        {
            DeviceConnected(device);
        }
    }
    private void OnDestroy()
    {
        InputDevices.deviceConnected -= DeviceConnected;
    }

    private void DeviceDisconnected(InputDevice device)
    {
        Debug.Log($"Device disconnected: {device.name}");
        bool mxInkDisconnected = device.name.ToLower().Contains("logitech");
        if (mxInkDisconnected)
        {
            _leftHand.SetActive(false);
            _rightHand.SetActive(false);
        }
    }
    private void DeviceConnected(InputDevice device)
    {
        Debug.Log($"Device connected: {device.name}");
        bool mxInkConnected = device.name.ToLower().Contains("logitech");
        if (mxInkConnected)
        {
            bool isOnRightHand = (device.characteristics & InputDeviceCharacteristics.Right) != 0;
            _leftHand.SetActive(!isOnRightHand);
            _rightHand.SetActive(isOnRightHand);

            MxInkStylus = FindFirstObjectByType<MxInkHandler>();
            MxInkStylus.SetHandedness(isOnRightHand);
        }
    }
}
