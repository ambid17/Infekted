using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public Transform levelButtonContainer;
    public GameObject levelObjectPrefab;

    public GameObject levelsPanel;
    public PauseMenu pauseMenu;

    public string[] sceneNames;

    private void Start()
    {
        levelsPanel.SetActive(true);
        pauseMenu.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsMenu();
        }
    }

    public void ToggleSettingsMenu()
    {
        levelsPanel.SetActive(!levelsPanel.activeInHierarchy);
        pauseMenu.gameObject.SetActive(!pauseMenu.gameObject.activeInHierarchy);
    }

    public void Play()
    {
        SceneManager.LoadScene(1);
    }
}
