using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class DeviceHandler : MonoBehaviour
{
    public GameObject LeftHand;
    public GameObject RightHand;
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
            LeftHand.SetActive(false);
            RightHand.SetActive(false);
        }
    }
    private void DeviceConnected(InputDevice device)
    {
        Debug.Log($"Device connected: {device.name}");
        bool mxInkConnected = device.name.ToLower().Contains("logitech");
        if (mxInkConnected)
        {
            bool isOnRightHand = (device.characteristics & InputDeviceCharacteristics.Right) != 0;
            LeftHand.SetActive(!isOnRightHand);
            RightHand.SetActive(isOnRightHand);

            MxInkHandler MxInkStylus = FindFirstObjectByType<MxInkHandler>();
            if (MxInkStylus)
            {
                MxInkStylus.SetHandedness(isOnRightHand);
                LineDrawing lineDrawing = FindFirstObjectByType<LineDrawing>();
                if (lineDrawing)
                {
                    lineDrawing.Stylus = MxInkStylus;
                }
            }
        }
    }
}
