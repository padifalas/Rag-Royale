using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SceneScript : MonoBehaviour
{
    [Header("Canvas's")]
    [SerializeField] private GameObject mainCanvasGO;
    [SerializeField] private GameObject cutSceneCanvasGO;
    [SerializeField] private GameObject optionsCanvasGO;
    [SerializeField] private GameObject howToPlayCanvasGO;
    [SerializeField] private GameObject controllerCanvasGO;
    [SerializeField] private GameObject keyboardCanvasGO;
    [SerializeField] private GameObject creditsCanvasGO;

    [Header("Cut Scene Panels")]
    [SerializeField] private GameObject[] scenePanels;

    [Header("Cutscene Buttons")]
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject startGameButton;

    private int currentScenePanel = 0;

    [Header("Controller First Selected Menu Options")]
    [SerializeField] private GameObject playFirst;
    [SerializeField] private GameObject optionsFirst;
    [SerializeField] private GameObject howToPlayFirst;
    [SerializeField] private GameObject controllerFirst;
    [SerializeField] private GameObject keyboardFirst;
    [SerializeField] private GameObject creditsFirst;

    [Header("First Selected Scene Panels")]
    [SerializeField] private GameObject[] scenePanelFirstButtons;


    void Start()
    {
        mainCanvasGO.SetActive(true);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);


        EventSystem.current.SetSelectedGameObject(playFirst);

    }

    #region Scene Switcher
    public void SceneSwitch(string sceneName)
    {
        LevelManager.Instance.LoadScene(sceneName);
        Debug.Log($"Loading scene: {sceneName}");

    }
    #endregion

    #region Canvas & Panels Activations/Deactivations

    private void OpenMainMenu()
    {
        mainCanvasGO.SetActive(true);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(playFirst);

    }

    private void OpenCutScene()
    {
        mainCanvasGO.SetActive(false);
        cutSceneCanvasGO.SetActive(true);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);

        // nextButton.SetActive(true);

        currentScenePanel = 0;

        ShowScenePanel(currentScenePanel);

        //EventSystem.current.SetSelectedGameObject(scenePanelFirstButtons);

    }

    public void NextCutScene()
    {
        currentScenePanel++;

        if (currentScenePanel >= scenePanels.Length)
        {
            currentScenePanel = scenePanels.Length - 1;
        }

        ShowScenePanel(currentScenePanel);
    }

    private void ShowScenePanel(int panelIndex)
    {
        for (int i = 0; i < scenePanels.Length; i++)
        {
            scenePanels[i].SetActive(false);
        }

        scenePanels[panelIndex].SetActive(true);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(scenePanelFirstButtons[panelIndex]);
        // nextButton.SetActive(true);


        // Last panel logic
        if (panelIndex == scenePanels.Length - 1)
        {
            nextButton.SetActive(false);
            startGameButton.SetActive(true);
        }
        else
        {
            nextButton.SetActive(true);
            startGameButton.SetActive(false);
        }
    }

    private void OpenOptions()
    {
        mainCanvasGO.SetActive(false);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(true);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);


        EventSystem.current.SetSelectedGameObject(optionsFirst);


    }

    private void OpenHowToPlay()
    {
        mainCanvasGO.SetActive(false);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(true);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);


        EventSystem.current.SetSelectedGameObject(howToPlayFirst);
    }

    private void OpenController()
    {
        mainCanvasGO.SetActive(false);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(true);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(controllerFirst);


    }

    private void OpenKeyboard()
    {
        mainCanvasGO.SetActive(false);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(true);
        creditsCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(keyboardFirst);


    }

    private void OpenCredits()
    {
        mainCanvasGO.SetActive(false);
        cutSceneCanvasGO.SetActive(false);
        optionsCanvasGO.SetActive(false);
        howToPlayCanvasGO.SetActive(false);
        controllerCanvasGO.SetActive(false);
        keyboardCanvasGO.SetActive(false);
        creditsCanvasGO.SetActive(true);

        EventSystem.current.SetSelectedGameObject(creditsFirst);
    }

    #endregion

    #region Main Menu Actions

    public void OnPlayPress()
    {
        OpenCutScene();
    }

    public void OnOptionsPress()
    {
        OpenOptions();
    }

    public void BackToMain()
    {
        OpenMainMenu();
    }

    public void OnCreditsPress()
    {
        OpenCredits();
    }

    #endregion

    #region Options Menu Actions

    public void OnHowToPlayPress()
    {
        OpenHowToPlay();
    }
    public void OnControllerPress()
    {
        OpenController();
    }

    public void OnKeyboardPress()
    {
        OpenKeyboard();
    }

    #endregion
    #region Close Application
    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion

}
