using UnityEngine;
using TMPro;

public class LocalPlayerLogin : MonoBehaviour
{
    public TMP_InputField id_info;
    public TMP_InputField pw_info;
    public GameObject ConfirmWindow;
    public GameObject LoginCanvas;
    public GameObject MenuCanvas;
    public GameObject NewCharacterButton;

    private void Start()
    {
        LoginOrCreatePlayer();
    }

    public void LoginOrCreatePlayer()
    {
        if (NewCharacterButton != null)
            NewCharacterButton.SetActive(false);

        PlayerManager.Instance.LoadLocalPlayerData(() =>
        {
            RefreshNewCharacterButton();
            LoginCanvas.SetActive(false);
            MenuCanvas.SetActive(true);
        }, error => ConfirmWindow.SetActive(true));
    }

    public void LoginOrCreatePlayer(string playerName, string password)
    {
        LoginOrCreatePlayer();
    }

    private void RefreshNewCharacterButton()
    {
        if (NewCharacterButton != null)
            NewCharacterButton.SetActive(!PlayerManager.Instance.HasLivingCharacter);
    }
}
