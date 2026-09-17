using FishNet.Managing;
using FishNet.Transporting.Multipass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerRoomPanel : MonoBehaviour
{
    public bool IsReady => session != null && session.IsReady;
    public bool CanInvite => session != null && session.CanInvite;
    public NetworkManager networkManager;
    public Multipass transport;
    public GameObject panel;
    public TMP_InputField addressInput;
    public TMP_Text statusText;
    public TMP_Text membersText;
    public TMP_Text capacityText;
    public TMP_Text modeText;
    public TMP_Text readyText;
    public Button openButton;
    public Button hostButton;
    public Button joinButton;
    public Button capacityButton;
    public Button modeButton;
    public Button readyButton;
    public Button inviteButton;
    public Button copyButton;
    public Button closeButton;
    public Button leaveButton;
    public bool joinOnly;
    public Button resumeButton;
    public Button abandonSavedButton;

    private MultiplayerSession session;
    private bool friendlyOnly;

    private void Start()
    {
        session = MultiplayerSession.Instance;
        if (session == null)
        {
            Debug.LogError("MultiplayerSession is missing from MultiplayerNetworkRoot.");
            return;
        }
        if (openButton != null) openButton.onClick.AddListener(Open);
        hostButton.onClick.AddListener(HostRoom);
        joinButton.onClick.AddListener(JoinRoom);
        capacityButton.onClick.AddListener(CycleCapacity);
        modeButton.onClick.AddListener(ToggleMode);
        readyButton.onClick.AddListener(ToggleReady);
        inviteButton.onClick.AddListener(InviteFriend);
        copyButton.onClick.AddListener(CopyInviteCode);
        closeButton.onClick.AddListener(Close);
        if (leaveButton != null) leaveButton.onClick.AddListener(LeaveRoom);
        if (joinOnly) addressInput.onSubmit.AddListener(SubmitCode);
        session.OpenRequested += Open;
        if (joinOnly)
        {
            resumeButton.onClick.AddListener(session.ResumeSavedExpedition);
            abandonSavedButton.onClick.AddListener(session.AbandonSavedExpedition);
        }
        RefreshControls();
    }

    private void Update()
    {
        if (session != null)
        {
            if (joinOnly && session.Connected && session.Formation.quest != null)
            {
                if (friendlyOnly && !session.IsFriendlyRoom)
                {
                    session.LeaveRoom();
                    statusText.text = "친선전 방 코드가 아닙니다.";
                }
                else
                {
                    Close();
                }
            }
            RefreshControls();
        }
    }

    public void Open()
    {
        friendlyOnly = false;
        OpenPanel();
    }

    public void OpenFriendly()
    {
        friendlyOnly = true;
        OpenPanel();
    }

    private void OpenPanel()
    {
        panel.SetActive(true);
        if (session != null) addressInput.SetTextWithoutNotify(joinOnly ? "" : session.Address);
        if (joinOnly) addressInput.ActivateInputField();
        RefreshControls();
    }

    public void Close()
    {
        panel.SetActive(false);
        if (joinOnly && session != null && !session.Connected && !session.Editable)
            session.LeaveRoom();
    }

    public void HostRoom()
    {
        if (friendlyOnly)
            session.OpenFriendlyRoom();
        else
            session.HostRoom();
    }
    public void JoinRoom()
    {
        if (joinOnly)
        {
            session.JoinByCode(addressInput.text);
            return;
        }
        session.Address = addressInput.text;
        session.JoinRoom();
    }
    public void ToggleMode()
    {
        session.ToggleMode();
        addressInput.SetTextWithoutNotify(session.Address);
    }
    public void CycleCapacity() => session.CycleCapacity();
    public void ToggleReady() => session.ToggleReady();
    public void SetReady(bool value) => session.SetReady(value);
    public void InviteFriend() => session.InviteFriend();
    public void CopyInviteCode() => session.CopyInviteCode();
    public void LeaveRoom() => session.LeaveRoom();
    private void SubmitCode(string code) => JoinRoom();

    private void RefreshControls()
    {
        if (session == null) return;
        statusText.text = session.Status;
        membersText.text = session.MembersText;
        hostButton.interactable = joinButton.interactable = capacityButton.interactable = modeButton.interactable = session.Editable;
        addressInput.interactable = session.Editable;
        readyButton.interactable = session.Connected;
        if (leaveButton != null) leaveButton.interactable = !session.Editable;
        inviteButton.interactable = copyButton.interactable = session.CanInvite;
        modeText.text = session.UsesSteam ? "연결 방식: Steam" : "연결 방식: LAN/IP";
        capacityText.text = $"정원 {session.Capacity}명";
        readyText.text = session.IsReady ? "준비 취소" : "준비";
        if (friendlyOnly)
        {
            SetButtonText(hostButton, "방 만들기");
            SetButtonText(joinButton, "코드 참가");
        }
        else
        {
            SetButtonText(hostButton, "방 만들기");
            SetButtonText(joinButton, "참가");
        }
        if (joinOnly)
        {
            resumeButton.gameObject.SetActive(session.HasSavedExpedition);
            abandonSavedButton.gameObject.SetActive(session.HasSavedExpedition);
            resumeButton.interactable = session.Editable;
            abandonSavedButton.interactable = session.Editable && PlayerManager.Instance.SharedCheckpoint?.settlementPrepared != true;
        }
    }

    private static void SetButtonText(Button button, string value)
    {
        TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null)
            text.text = value;
    }

    private void OnDestroy()
    {
        if (session != null) session.OpenRequested -= Open;
        if (session != null && joinOnly)
        {
            resumeButton.onClick.RemoveListener(session.ResumeSavedExpedition);
            abandonSavedButton.onClick.RemoveListener(session.AbandonSavedExpedition);
        }
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        hostButton.onClick.RemoveListener(HostRoom);
        joinButton.onClick.RemoveListener(JoinRoom);
        capacityButton.onClick.RemoveListener(CycleCapacity);
        modeButton.onClick.RemoveListener(ToggleMode);
        readyButton.onClick.RemoveListener(ToggleReady);
        inviteButton.onClick.RemoveListener(InviteFriend);
        copyButton.onClick.RemoveListener(CopyInviteCode);
        closeButton.onClick.RemoveListener(Close);
        if (leaveButton != null) leaveButton.onClick.RemoveListener(LeaveRoom);
        if (joinOnly) addressInput.onSubmit.RemoveListener(SubmitCode);
    }
}
