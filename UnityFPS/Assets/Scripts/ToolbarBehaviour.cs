using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using System;

public class ToolbarBehaviour : MonoBehaviour
{
    public GameObject btnJoinRoom;
    public GameObject btnDisconnect;
    public GameObject txtStatus;
    public GameObject txtPing;
    public GameObject txtLogo;
    public GameObject txtLoading;
    public GameObject txtHelp;
    public GameObject ddRegion;
    public GameObject tbRoomId;
    public GameObject ddMode;
    public GameObject slFrequency;
    public GameObject txtFrequency;

    private bool initialized;
    private bool connectedPrevFrame;

    private void Awake()
    {
        txtHelp.SetActive(false);
        btnJoinRoom.SetActive(false);
        btnDisconnect.SetActive(false);
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(GetFPS), 1, 1);
    }

    private void GetFPS()
    {
        int fps = (int)(1f / Time.unscaledDeltaTime);
        var text = txtPing.GetComponent<Text>();
        text.color = Color.green;
        text.text = $"{fps} fps";
    }

    private void Update()
    {
        if (NetClient.Instance == null) return;

        bool connecting = NetClient.Instance.IsConnecting;
        bool connected = NetClient.Instance.IsConnected;

        if (!initialized && !connected)
        {
            ddRegion.SetDropDownValue(NetClient.Instance.Region);
            tbRoomId.GetComponent<InputField>().text = NetClient.Instance.RoomId;
            ddMode.SetDropDownValue(NetClient.Instance.InterpolationMode.ToString());
            slFrequency.GetComponent<Slider>().value = NetClient.Instance.frequency / 5;

            initialized = true;
        }

        if (!connectedPrevFrame && connected)   // just Connected
        {
            string region = ddRegion.GetComponent<Dropdown>().options
                .Where(o => NetClient.Instance.jumpEndpoint.AppName.Contains(o.text))
                .Select(o => o.text).FirstOrDefault();
            ddRegion.SetDropDownValue(region);
            tbRoomId.GetComponent<InputField>().text = NetClient.Instance.CurrentRoom.RoomId;

            txtHelp.SetActive(true);
        }
        
        if (connectedPrevFrame && !connected)   // just Disconnected
        {
            ddRegion.SetDropDownValue(NetClient.Instance.Region);
            tbRoomId.GetComponent<InputField>().text = NetClient.Instance.RoomId;

            txtHelp.SetActive(false);
        }

        btnJoinRoom.SetActive(!connecting && !connected);
        btnDisconnect.SetActive(connected);
        txtLogo.SetActive(!connecting && !connected);

        var textStatus = txtStatus.GetComponent<Text>();
        textStatus.color = NetClient.Instance.StatusColor;
        textStatus.text = NetClient.Instance.StatusText;

        var textProgress = txtLoading.GetComponent<Text>();
        textProgress.text = NetClient.Instance.ProgressText;

        txtFrequency.GetComponent<Text>().text = (slFrequency.GetComponent<Slider>().value * 5).ToString();

        if (connected)
        {
            NetClient.Instance.frequency = (int)slFrequency.GetComponent<Slider>().value * 5;
            NetClient.Instance.InterpolationMode = (InterpolationMode)Enum.Parse(typeof(InterpolationMode), ddMode.GetDropDownValue());
        }

        connectedPrevFrame = connected;
    }

    private void LateUpdate()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && btnJoinRoom.activeSelf)
            PerformConnection();

        if (Keyboard.current.escapeKey.wasPressedThisFrame && btnDisconnect.activeSelf)
            DisconnectServer();

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            bool active = !txtLoading.activeSelf;
            txtLoading.SetActive(active);
            ddRegion.SetActive(btnJoinRoom.activeSelf || active);
            tbRoomId.SetActive(btnJoinRoom.activeSelf || active);
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            bool active = !txtHelp.activeSelf;
            txtHelp.SetActive(active);
        }
    }

    public async void PerformConnection()
    {
        string region = ddRegion.GetDropDownValue(zero: "");
        NetClient.Instance.Region = region.Trim();

        string roomId = tbRoomId.GetComponent<InputField>().text;
        if (!string.IsNullOrWhiteSpace(roomId))
            NetClient.Instance.RoomId = roomId.Trim();

        await NetClient.Instance.PerformConnection();
    }

    public async void DisconnectServer()
    {
        await NetClient.Instance.DisconnectServer();
    }
}
