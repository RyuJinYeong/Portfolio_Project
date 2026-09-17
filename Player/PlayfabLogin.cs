// Import all the necessary namespaces
using UnityEngine;
using TMPro;
using PlayFab.ClientModels;
using PlayFab;

public class PlayfabLogin : MonoBehaviour
{
    public TMP_InputField id_info;
    public TMP_InputField pw_info;
    public GameObject ConfirmWindow;
    public GameObject LoginCanvas;
    public GameObject MenuCanvas;
    public GameObject NewCharacterButton;

    // PlayFab 로그인을 시도하는 메서드
    public void LoginOrCreatePlayer()
    {
        string playerName = id_info.text;
        string password = pw_info.text;

        var request = new LoginWithPlayFabRequest
        {
            Username = playerName, // 아이디
            Password = password // 비밀번호
        };

        // PlayFab API를 사용하여 로그인을 시도하고 응답을 처리하는 콜백 메서드를 지정합니다.
        PlayFabClientAPI.LoginWithPlayFab(request,
            result => OnLoginSuccess(result, playerName),
            error => OnLoginFailure(error, playerName,password));
    }

    public void LoginOrCreatePlayer(string playerName, string password) // 메소드 오버로딩으로 아이디와 패스워드값 전달
    {
        var request = new LoginWithPlayFabRequest
        {
            Username = playerName, // 아이디
            Password = password // 비밀번호
        };

        PlayFabClientAPI.LoginWithPlayFab(request,
            result => OnLoginSuccess(result, playerName),
            error => OnLoginFailure(error, playerName, password));
    }

    // 로그인이 성공했을 때 호출되는 콜백 메서드
    private void OnLoginSuccess(LoginResult result, string playerName)
    {
        Debug.Log("Login successful!");

        if (NewCharacterButton != null)
            NewCharacterButton.SetActive(false);

        PlayerManager.Instance.LoadPlayerDataFromPlayFab(RefreshNewCharacterButton);
        LoginCanvas.SetActive(false);
        MenuCanvas.SetActive(true);
    }

    private void RefreshNewCharacterButton()
    {
        if (NewCharacterButton != null)
            NewCharacterButton.SetActive(!PlayerManager.Instance.HasLivingCharacter);
    }

    // 로그인이 실패했을 때 호출되는 콜백 메서드
    private void OnLoginFailure(PlayFabError error, string playerName,string password)
    {
        // PlayFab은 없는 아이디와 잘못된 비밀번호를 같은 오류로 반환할 수 있습니다.
        // 가입을 시도한 뒤 기존 아이디라면 UsernameNotAvailable 오류로 처리합니다.
        if (error.Error == PlayFabErrorCode.AccountNotFound ||
            error.Error == PlayFabErrorCode.InvalidUsernameOrPassword)
        {
            RegisterNewPlayer(playerName,password);
        }
        else
        {
            Debug.LogError("Login failed: " + error.GenerateErrorReport());
            ConfirmWindow.SetActive(true);
        }
    }

    // 새로운 플레이어를 생성하는 메서드
    private void RegisterNewPlayer(string playerName, string password)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Username = playerName, // 아이디
            Password = password, // 비밀번호
            RequireBothUsernameAndEmail = false // 이메일 필드가 비어있어도 플레이어 생성 허용
        };

        // PlayFab API를 사용하여 새로운 플레이어를 생성하고 응답을 처리하는 콜백 메서드를 지정합니다.
        PlayFabClientAPI.RegisterPlayFabUser(request,
            result => OnRegisterSuccess(playerName,password),
            error =>
            {
                Debug.LogError("Failed to register new player: " + error.GenerateErrorReport());
                ConfirmWindow.SetActive(true);
            });
    }

    // 새로운 플레이어 생성이 성공했을 때 호출되는 콜백 메서드
    private void OnRegisterSuccess(string playerName, string password)
    {
        Debug.Log("New player registered: " + playerName);
        LoginOrCreatePlayer(playerName, password);
    }
}
